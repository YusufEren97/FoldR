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

        #region Item Drag (Reordering)

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && sender is Border b && b.DataContext is DisplayItem item)
            {
                _itemDragStartPos = e.GetPosition(this);
                _draggedItem = item;
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
                
                // Start drag-drop operation
                var dataObject = new DataObject();
                dataObject.SetData("FoldRItem", _draggedItem);
                dataObject.SetFileDropList(new System.Collections.Specialized.StringCollection { _draggedItem.Path });
                
                Mouse.Capture(null);
                DragDrop.DoDragDrop((DependencyObject)sender, dataObject, DragDropEffects.Move);
                
                // Check if dropped outside (on desktop) - item will be removed by drop handler
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
                
                // Force immediate UI update
                Dispatcher.Invoke(() =>
                {
                    UpdateUI();
                    OnDataChanged?.Invoke();
                }, System.Windows.Threading.DispatcherPriority.Send);
                
                e.Handled = true;
            }
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
                
                if (!windowRect.Contains(screenPos.X, screenPos.Y) && _draggedItem != null)
                {
                    // Dropped outside - restore to desktop
                    var itemToRemove = _data.Items.FirstOrDefault(i => i.Path == _draggedItem.Path);
                    if (itemToRemove != null)
                    {
                        RestoreSingleItemToDesktop(itemToRemove.Path);
                        _data.Items.Remove(itemToRemove);
                        
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
