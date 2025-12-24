using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FoldR.Core;
using FoldR.Helpers;

namespace FoldR.Controls
{
    /// <summary>
    /// FolderWidget - Drag-Drop operations (file drop, item reordering, desktop restore)
    /// </summary>
    public partial class FolderWidget
    {
        // Stored drag items for multi-selection support
        private System.Collections.Generic.List<DisplayItem> _currentDragItems;
        
        #region External File Drop
        
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                AnimationHelper.DropHighlight(GlowEffect, IconScale);
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            AnimationHelper.DropReset(GlowEffect, IconScale);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            AnimationHelper.DropReset(GlowEffect, IconScale);
            
            // Prevent drops if widget is locked
            if (_data.IsLocked)
            {
                e.Handled = true;
                return;
            }
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var sourcePath in paths)
                {
                    try
                    {
                        // Skip if already in storage or already exists in widget
                        string storagePath = Utils.GetStoragePath();
                        if (sourcePath.StartsWith(storagePath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Already in storage - just add reference if not exists
                            if (!_data.Items.Any(i => i.Path.Equals(sourcePath, StringComparison.OrdinalIgnoreCase)))
                            {
                                _data.Items.Add(new WidgetItem(sourcePath));
                            }
                            continue;
                        }
                        
                        // Get unique destination path
                        string destPath = Utils.GetUniqueStoragePath(sourcePath);
                        
                        // Move file or folder (using copy+delete for cross-drive support)
                        if (System.IO.Directory.Exists(sourcePath))
                        {
                            // It's a folder - copy then delete
                            CopyDirectory(sourcePath, destPath);
                            System.IO.Directory.Delete(sourcePath, true);
                        }
                        else if (System.IO.File.Exists(sourcePath))
                        {
                            // It's a file - copy then delete (works across drives)
                            System.IO.File.Copy(sourcePath, destPath, false);
                            System.IO.File.Delete(sourcePath);
                        }
                        else
                        {
                            continue; // Skip if doesn't exist
                        }
                        
                        // Add with new path
                        if (!_data.Items.Any(i => i.Path.Equals(destPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            _data.Items.Add(new WidgetItem(destPath));
                        }
                    }
                    catch (Exception ex)
                    {
                        // If move fails, just add as reference (old behavior)
                        System.Diagnostics.Debug.WriteLine($"Move failed: {ex.Message}");
                        if (!_data.Items.Any(i => i.Path.Equals(sourcePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            _data.Items.Add(new WidgetItem(sourcePath));
                        }
                    }
                }
                UpdateUI();
                OnDataChanged?.Invoke();
                
                if (!_isExpanded)
                {
                    TogglePanel();
                }
            }
        }

        #endregion

        #region Item Drag (Reordering) & Selection

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && sender is Border b && b.DataContext is DisplayItem item)
            {
                _itemDragStartPos = e.GetPosition(this);
                _draggedItem = item;
                
                // Handle selection
                bool ctrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                
                if (ctrlPressed)
                {
                    // Ctrl+Click: Toggle selection
                    item.IsSelected = !item.IsSelected;
                }
                else
                {
                    // Normal click: If clicking unselected item, clear others and select this one
                    // If clicking selected item, keep selection (for multi-drag)
                    if (!item.IsSelected)
                    {
                        ClearAllSelections();
                        item.IsSelected = true;
                    }
                }
                
                Mouse.Capture(b);
            }
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null)
                return;
            
            var currentPos = e.GetPosition(this);
            var diff = currentPos - _itemDragStartPos;
            
            // Check if drag threshold exceeded
            if (!_isDraggingItem && (Math.Abs(diff.X) > DRAG_THRESHOLD || Math.Abs(diff.Y) > DRAG_THRESHOLD))
            {
                _isDraggingItem = true;
                
                // Get all selected items (or just the dragged one if none selected)
                var selectedItems = GetSelectedItems();
                if (selectedItems.Count == 0)
                {
                    selectedItems = new System.Collections.Generic.List<DisplayItem> { _draggedItem };
                }
                
                // Store for use in Panel_QueryContinueDrag
                _currentDragItems = selectedItems;
                
                // Create drag visual
                var dragWindow = CreateDragVisual(selectedItems);
                
                // Start drag-drop operation with all selected items
                var dataObject = new DataObject();
                
                // For internal use
                dataObject.SetData("FoldRItems", selectedItems);
                dataObject.SetData("FoldRItem", _draggedItem); // Backward compatibility
                
                // For external drop (file list)
                var fileList = new System.Collections.Specialized.StringCollection();
                foreach (var sel in selectedItems)
                {
                    fileList.Add(sel.Path);
                }
                dataObject.SetFileDropList(fileList);
                
                Mouse.Capture(null);
                
                // Use GiveFeedback to update drag visual position
                GiveFeedbackEventHandler feedbackHandler = (s, args) =>
                {
                    if (dragWindow != null && dragWindow.IsVisible)
                    {
                        var screenPos = System.Windows.Forms.Cursor.Position;
                        dragWindow.Left = screenPos.X + 10;
                        dragWindow.Top = screenPos.Y + 10;
                    }
                    args.UseDefaultCursors = true;
                    args.Handled = true;
                };
                
                ((Border)sender).GiveFeedback += feedbackHandler;
                
                try
                {
                    DragDrop.DoDragDrop((DependencyObject)sender, dataObject, DragDropEffects.Move);
                }
                finally
                {
                    ((Border)sender).GiveFeedback -= feedbackHandler;
                    dragWindow?.Close();
                }
                
                _isDraggingItem = false;
                _draggedItem = null;
            }
        }

        private void Item_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            
            if (!_isDraggingItem && _draggedItem != null && e.ChangedButton == MouseButton.Left)
            {
                // It was a click, not a drag - open the file
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _draggedItem.Path, UseShellExecute = true }); } 
                catch (Exception ex) { MessageBox.Show($"Cannot open: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
            
            _isDraggingItem = false;
            _draggedItem = null;
        }
        
        /// <summary>
        /// Creates a transparent popup showing dragged item icons
        /// </summary>
        private Window CreateDragVisual(System.Collections.Generic.List<DisplayItem> items)
        {
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                IsHitTestVisible = false,
                SizeToContent = SizeToContent.WidthAndHeight
            };
            
            // Create visual container
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Opacity = 0.7
            };
            
            // Show up to 4 icons
            int maxIcons = Math.Min(items.Count, 4);
            for (int i = 0; i < maxIcons; i++)
            {
                var item = items[i];
                var border = new Border
                {
                    Width = 48,
                    Height = 48,
                    Margin = new Thickness(i == 0 ? 0 : -20, 0, 0, 0), // Stack overlapping
                    Background = new SolidColorBrush(Color.FromArgb(100, 60, 60, 60)),
                    CornerRadius = new CornerRadius(8)
                };
                
                var img = new Image
                {
                    Source = item.Icon,
                    Width = 32,
                    Height = 32,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                border.Child = img;
                panel.Children.Add(border);
            }
            
            // Show count if more than 4 items
            if (items.Count > 4)
            {
                var countBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                countBadge.Child = new TextBlock
                {
                    Text = $"+{items.Count - 4}",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };
                panel.Children.Add(countBadge);
            }
            
            window.Content = panel;
            
            // Position at cursor
            var cursorPos = System.Windows.Forms.Cursor.Position;
            window.Left = cursorPos.X + 10;
            window.Top = cursorPos.Y + 10;
            
            window.Show();
            return window;
        }
        
        private System.Collections.Generic.List<DisplayItem> GetSelectedItems()
        {
            var items = ItemsContainer.ItemsSource as System.Collections.Generic.List<DisplayItem>;
            if (items == null) return new System.Collections.Generic.List<DisplayItem>();
            return items.Where(i => i.IsSelected).ToList();
        }
        
        private void ClearAllSelections()
        {
            var items = ItemsContainer.ItemsSource as System.Collections.Generic.List<DisplayItem>;
            if (items == null) return;
            
            foreach (var item in items)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }
            
            // Also hide selection rectangle if visible
            if (SelectionRect.Visibility == Visibility.Visible)
            {
                SelectionRect.Visibility = Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// Clears selection when clicking on panel background
        /// </summary>
        private void ExpandedPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only clear if clicking directly on panel background, not on items
            var hit = e.OriginalSource as DependencyObject;
            
            // Check if we clicked on an item
            while (hit != null && hit != ExpandedPanel)
            {
                if (hit is Border b && b.DataContext is DisplayItem)
                {
                    // Clicked on an item - don't clear
                    return;
                }
                hit = VisualTreeHelper.GetParent(hit);
            }
            
            // Clear selection if not clicking on an item
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ClearAllSelections();
            }
        }
        
        #endregion
        
        #region Lasso Selection
        
        private bool _isLassoSelecting = false;
        private Point _lassoStartPoint;
        
        private void ItemsContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only start lasso if clicking on empty area (not on an item)
            var hitResult = VisualTreeHelper.HitTest(ItemsContainer, e.GetPosition(ItemsContainer));
            if (hitResult != null)
            {
                // Check if we hit an item border
                var element = hitResult.VisualHit as DependencyObject;
                while (element != null && element != ItemsContainer)
                {
                    if (element is Border b && b.DataContext is DisplayItem)
                    {
                        // Clicked on an item - don't start lasso
                        return;
                    }
                    element = VisualTreeHelper.GetParent(element);
                }
            }
            
            // Start lasso selection
            _isLassoSelecting = true;
            _lassoStartPoint = e.GetPosition(ItemsContainer);
            
            // Clear existing selection unless Ctrl is held
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ClearAllSelections();
            }
            
            // Initialize selection rectangle
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Margin = new Thickness(_lassoStartPoint.X, _lassoStartPoint.Y, 0, 0);
            SelectionRect.Visibility = Visibility.Visible;
            
            Mouse.Capture(ItemsContainer);
            e.Handled = true;
        }
        
        private void ItemsContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isLassoSelecting) return;
            
            var currentPoint = e.GetPosition(ItemsContainer);
            
            // Calculate rectangle bounds
            double x = Math.Min(_lassoStartPoint.X, currentPoint.X);
            double y = Math.Min(_lassoStartPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _lassoStartPoint.X);
            double height = Math.Abs(currentPoint.Y - _lassoStartPoint.Y);
            
            // Update selection rectangle
            SelectionRect.Margin = new Thickness(x, y, 0, 0);
            SelectionRect.Width = width;
            SelectionRect.Height = height;
            
            // Select items within rectangle
            var selectionBounds = new Rect(x, y, width, height);
            SelectItemsInRect(selectionBounds);
        }
        
        private void ItemsContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isLassoSelecting) return;
            
            _isLassoSelecting = false;
            SelectionRect.Visibility = Visibility.Collapsed;
            Mouse.Capture(null);
        }
        
        private void SelectItemsInRect(Rect selectionBounds)
        {
            // Minimum size to prevent selection on simple click (must drag at least 5 pixels)
            if (selectionBounds.Width < 5 && selectionBounds.Height < 5)
                return;
            
            var items = ItemsContainer.ItemsSource as System.Collections.Generic.List<DisplayItem>;
            if (items == null) return;
            
            // Get the items panel (UniformGrid)
            var itemsPanel = GetItemsPanel(ItemsContainer);
            if (itemsPanel == null) return;
            
            for (int i = 0; i < items.Count; i++)
            {
                var container = ItemsContainer.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;
                
                // Get item bounds relative to ItemsContainer
                var itemBounds = container.TransformToAncestor(ItemsContainer)
                    .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                
                // Check if item intersects with selection rectangle
                bool intersects = selectionBounds.IntersectsWith(itemBounds);
                
                // If Ctrl is held, only add to selection, don't deselect
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (intersects)
                        items[i].IsSelected = true;
                }
                else
                {
                    items[i].IsSelected = intersects;
                }
            }
        }
        
        private Panel GetItemsPanel(ItemsControl itemsControl)
        {
            var itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
            if (itemsPresenter == null) return null;
            return VisualTreeHelper.GetChild(itemsPresenter, 0) as Panel;
        }
        
        private T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var descendant = GetVisualChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }
        
        #endregion

        #region Item Reorder Drop
        
        private void ItemsContainer_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FoldRItem"))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                
                // Show drop indicator at target position
                var pos = e.GetPosition(ItemsContainer);
                var (targetItem, insertAfter, targetBorder) = GetItemAndInsertPositionWithBorder(pos);
                
                if (targetBorder != null)
                {
                    // Get position relative to ItemsContainer (which is in Row 1)
                    // DropIndicator is also in Row 1, so Y positions match directly
                    var borderPos = targetBorder.TranslatePoint(new Point(0, 0), ItemsContainer);
                    
                    // X position: left or right side of item + ItemsContainer margin (8px)
                    double indicatorX = insertAfter 
                        ? borderPos.X + targetBorder.ActualWidth + 8 + 2  // Right of item + margin + gap
                        : borderPos.X + 8 - 2;  // Left of item + margin - gap
                    
                    // Y position: same as item top + small offset
                    double indicatorY = borderPos.Y + 4 + 8;  // ItemsContainer top margin (4px) + offset
                    
                    // Clamp positions to valid range (prevent going above panel)
                    indicatorX = Math.Max(0, indicatorX);
                    indicatorY = Math.Max(4, indicatorY);  // Minimum 4px from top
                    
                    DropIndicator.Margin = new Thickness(indicatorX, indicatorY, 0, 0);
                    DropIndicator.Visibility = Visibility.Visible;
                }
                else
                {
                    DropIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }
        
        private void ItemsContainer_DragLeave(object sender, DragEventArgs e)
        {
            DropIndicator.Visibility = Visibility.Collapsed;
        }
        
        private void ItemsContainer_Drop(object sender, DragEventArgs e)
        {
            // Hide indicator
            DropIndicator.Visibility = Visibility.Collapsed;
            
            // Handle internal item reordering
            if (e.Data.GetDataPresent("FoldRItem"))
            {
                var draggedDisplayItem = e.Data.GetData("FoldRItem") as DisplayItem;
                if (draggedDisplayItem == null) return;
                
                var pos = e.GetPosition(ItemsContainer);
                var (targetDisplayItem, insertAfter, _) = GetItemAndInsertPositionWithBorder(pos);
                
                var sourceItem = _data.Items.FirstOrDefault(i => i.Path == draggedDisplayItem.Path);
                if (sourceItem == null) return;
                
                int sourceIndex = _data.Items.IndexOf(sourceItem);
                
                if (targetDisplayItem != null && targetDisplayItem.Path != draggedDisplayItem.Path)
                {
                    var targetItem = _data.Items.FirstOrDefault(i => i.Path == targetDisplayItem.Path);
                    if (targetItem != null)
                    {
                        int targetIndex = _data.Items.IndexOf(targetItem);
                        
                        // Remove from old position
                        _data.Items.RemoveAt(sourceIndex);
                        
                        // Calculate new index (adjust if source was before target)
                        int newIndex = targetIndex;
                        if (sourceIndex < targetIndex)
                            newIndex--; // Adjust because we removed an item before target
                        
                        // Insert after if dropped on right half of target
                        if (insertAfter)
                            newIndex++;
                        
                        // Clamp to valid range
                        newIndex = Math.Max(0, Math.Min(newIndex, _data.Items.Count));
                        
                        _data.Items.Insert(newIndex, sourceItem);
                    }
                }
                else if (targetDisplayItem == null)
                {
                    // Dropped on empty area - move to end
                    _data.Items.RemoveAt(sourceIndex);
                    _data.Items.Add(sourceItem);
                }
                
                ForceRefreshUI();
                e.Handled = true;
                return;
            }
            
            // Handle external file drop (from desktop/explorer)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Prevent drops if widget is locked
                if (_data.IsLocked)
                {
                    e.Handled = true;
                    return;
                }
                
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var sourcePath in paths)
                {
                    try
                    {
                        // Skip if already in storage or already exists in widget
                        string storagePath = Utils.GetStoragePath();
                        if (sourcePath.StartsWith(storagePath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Already in storage - just add reference if not exists
                            if (!_data.Items.Any(i => i.Path.Equals(sourcePath, StringComparison.OrdinalIgnoreCase)))
                            {
                                _data.Items.Add(new WidgetItem(sourcePath));
                            }
                            continue;
                        }
                        
                        // Get unique destination path
                        string destPath = Utils.GetUniqueStoragePath(sourcePath);
                        
                        // Move file or folder
                        if (System.IO.Directory.Exists(sourcePath))
                        {
                            CopyDirectory(sourcePath, destPath);
                            System.IO.Directory.Delete(sourcePath, true);
                        }
                        else if (System.IO.File.Exists(sourcePath))
                        {
                            System.IO.File.Copy(sourcePath, destPath, false);
                            System.IO.File.Delete(sourcePath);
                        }
                        else
                        {
                            continue;
                        }
                        
                        // Add with new path
                        if (!_data.Items.Any(i => i.Path.Equals(destPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            _data.Items.Add(new WidgetItem(destPath));
                        }
                    }
                    catch (Exception)
                    {
                        // If move fails, just add as reference
                        if (!_data.Items.Any(i => i.Path.Equals(sourcePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            _data.Items.Add(new WidgetItem(sourcePath));
                        }
                    }
                }
                
                ForceRefreshUI();
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// Force complete UI refresh - fixes visual glitches on pinned panels
        /// </summary>
        private void ForceRefreshUI()
        {
            Dispatcher.Invoke(() =>
            {
                // Clear and rebuild ItemsSource for clean refresh
                ItemsContainer.ItemsSource = null;
                UpdateUI();
                OnDataChanged?.Invoke();
            }, System.Windows.Threading.DispatcherPriority.Send);
        }
        
        private (DisplayItem item, bool insertAfter, Border targetBorder) GetItemAndInsertPositionWithBorder(Point pos)
        {
            // Find which item is at the given position and whether to insert before or after
            var hitResult = VisualTreeHelper.HitTest(ItemsContainer, pos);
            if (hitResult != null)
            {
                var element = hitResult.VisualHit as DependencyObject;
                Border targetBorder = null;
                
                while (element != null)
                {
                    if (element is Border border && border.DataContext is DisplayItem)
                    {
                        targetBorder = border;
                        break;
                    }
                    element = VisualTreeHelper.GetParent(element);
                }
                
                if (targetBorder != null && targetBorder.DataContext is DisplayItem item)
                {
                    // Get position relative to the target item
                    var itemPos = pos - targetBorder.TranslatePoint(new Point(0, 0), ItemsContainer);
                    // If dropped on right half, insert after
                    bool insertAfter = itemPos.X > targetBorder.ActualWidth / 2;
                    return (item, insertAfter, targetBorder);
                }
            }
            return (null, false, null);
        }
        
        private void Panel_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Check if mouse is outside the window (dropped on desktop)
            if (e.KeyStates == DragDropKeyStates.None)
            {
                // Drag ended - check if outside window
                var screenPos = System.Windows.Forms.Cursor.Position;
                var windowRect = new System.Drawing.Rectangle(
                    (int)Left, (int)Top, (int)Width, (int)Height);
                
                if (!windowRect.Contains(screenPos.X, screenPos.Y))
                {
                    // Use stored drag items (set at drag start)
                    var selectedItems = _currentDragItems ?? new System.Collections.Generic.List<DisplayItem>();
                    
                    // If no stored selection, use the single dragged item as fallback
                    if (selectedItems.Count == 0 && _draggedItem != null)
                    {
                        var itemToRemove = _data.Items.FirstOrDefault(i => i.Path == _draggedItem.Path);
                        if (itemToRemove != null)
                        {
                            selectedItems = new System.Collections.Generic.List<DisplayItem> { _draggedItem };
                        }
                    }
                    
                    if (selectedItems.Count > 0)
                    {
                        // Restore all selected items to desktop
                        foreach (var selItem in selectedItems)
                        {
                            var itemToRemove = _data.Items.FirstOrDefault(i => i.Path == selItem.Path);
                            if (itemToRemove != null)
                            {
                                RestoreSingleItemToDesktop(itemToRemove.Path);
                                _data.Items.Remove(itemToRemove);
                            }
                        }
                        
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            UpdateUI();
                            OnDataChanged?.Invoke();
                        }));
                    }
                }
            }
        }

        #endregion
    }
}
