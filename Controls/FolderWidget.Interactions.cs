using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FoldR.Core;
using FoldR.Helpers;
using Localization = FoldR.Core.Localization;

namespace FoldR.Controls
{
    /// <summary>
    /// FolderWidget - Panel toggle, keyboard, folder icon mouse events, item hover/click, context menu
    /// </summary>
    public partial class FolderWidget
    {
        #region Static Brushes & Colors (Performance)
        
        private static readonly Brush HoverBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
        private static readonly Color PinGoldColor = Color.FromRgb(0xFF, 0xD7, 0x00);
        private static readonly Color PinDarkGoldColor = Color.FromRgb(0xB8, 0x86, 0x0B);
        private static readonly Color PinUnpinnedFill = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);
        private static readonly Color PinUnpinnedStroke = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF);
        
        static FolderWidget()
        {
            // Freeze brushes for thread safety and performance
            ((Freezable)HoverBrush).Freeze();
        }
        
        #endregion
        
        #region Expand/Collapse
        
        private double _originalLeft; // Store original window position when expanding
        private double _originalTop;  // Store original Y position when expanding
        private bool _openedToLeft;   // Track which direction panel opened
        
        private void TogglePanel()
        {
            _isExpanded = !_isExpanded;
            
            if (_isExpanded)
            {
                var (panelWidth, panelHeight) = CalculatePanelSize();
                ExpandedPanel.Width = panelWidth;
                ExpandedPanel.Height = panelHeight;
                ExpandedPanel.Visibility = Visibility.Visible;
                
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                double totalWidth = WIDGET_WIDTH + ICON_SPACING + panelWidth;
                
                // Store original window position
                _originalLeft = Left;
                _originalTop = Top;
                
                // Check if panel would go off right edge
                _openedToLeft = (Left + totalWidth) > screenWidth;
                
                Width = totalWidth;
                Height = Math.Max(110, panelHeight);
                
                // Check if panel would go off bottom edge and adjust
                double newTop = Top;
                if (Top + Height > screenHeight)
                {
                    newTop = screenHeight - Height;
                    if (newTop < 0) newTop = 0; // Don't go above screen
                    Top = newTop;
                }
                
                if (_openedToLeft)
                {
                    // Open to LEFT: Panel on left, folder icon on right
                    // Move window left so folder icon stays in same screen position
                    Left = _originalLeft - panelWidth - ICON_SPACING;
                    
                    // Position elements within Canvas
                    System.Windows.Controls.Canvas.SetLeft(ExpandedPanel, 0);  // Panel at left
                    System.Windows.Controls.Canvas.SetLeft(FolderIconGrid, panelWidth + ICON_SPACING); // Icon at right
                }
                else
                {
                    // Open to RIGHT: Folder icon on left, panel on right (default)
                    System.Windows.Controls.Canvas.SetLeft(FolderIconGrid, 0);  // Icon at left
                    System.Windows.Controls.Canvas.SetLeft(ExpandedPanel, DEFAULT_PANEL_LEFT); // Panel at right
                }
                
                // Animate in with slide + fade effect
                AnimationHelper.PanelOpen(ExpandedPanel, _openedToLeft);
            }
            else
            {
                double restoreLeft = _originalLeft;
                double restoreTop = _originalTop;
                
                AnimationHelper.PanelClose(ExpandedPanel, _openedToLeft, () =>
                {
                    ExpandedPanel.Visibility = Visibility.Collapsed;
                    ExpandedPanel.RenderTransform = null; // Reset transform
                    
                    // Reset Canvas positions to default
                    System.Windows.Controls.Canvas.SetLeft(FolderIconGrid, 0);
                    System.Windows.Controls.Canvas.SetLeft(ExpandedPanel, DEFAULT_PANEL_LEFT);
                    
                    Width = WIDGET_WIDTH;
                    Height = WIDGET_HEIGHT;
                    
                    // Restore original window position
                    Left = restoreLeft;
                    Top = restoreTop;
                });
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Don't close panel if pinned
            if (_isExpanded && !_data.IsPanelPinned)
            {
                TogglePanel();
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isExpanded)
            {
                TogglePanel();
                e.Handled = true;
            }
        }
        
        private void PinButton_Click(object sender, MouseButtonEventArgs e)
        {
            _data.IsPanelPinned = !_data.IsPanelPinned;
            UpdatePinButtonVisual();
            OnDataChanged?.Invoke();
            e.Handled = true;
        }
        
        private void UpdatePinButtonVisual()
        {
            if (_data.IsPanelPinned)
            {
                // Pinned state - rotate to 0 (vertical) and highlight
                PinIconRotation.Angle = 0;
                PinIcon.Fill = new SolidColorBrush(PinGoldColor);
                PinIcon.Stroke = new SolidColorBrush(PinDarkGoldColor);
                PinButton.ToolTip = "Unpin panel (close on focus loss)";
            }
            else
            {
                // Unpinned state - rotate 45 degrees and dim
                PinIconRotation.Angle = 45;
                PinIcon.Fill = new SolidColorBrush(PinUnpinnedFill);
                PinIcon.Stroke = new SolidColorBrush(PinUnpinnedStroke);
                PinButton.ToolTip = "Pin panel (keep open after restart)";
            }
        }
        
        #endregion

        #region Folder Icon Mouse Events

        private void FolderIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _mouseDownPos = e.GetPosition(this);
                _mouseDownScreenPos = PointToScreen(_mouseDownPos);
                Mouse.Capture(FolderIconGrid);
            }
        }

        private void FolderIcon_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            
            var currentPos = PointToScreen(e.GetPosition(this));
            var diff = currentPos - _mouseDownScreenPos;
            
            // Check if this is a drag operation (moved more than 5 pixels)
            if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
            {
                // Prevent dragging if widget is locked
                if (_data.IsLocked)
                {
                    Mouse.Capture(null);
                    _isDraggingWindow = false;
                    return;
                }
                
                if (!_isDraggingWindow)
                {
                    _isDraggingWindow = true;
                    AnimationHelper.StartDrag(this);
                }
                
                // Calculate new position
                double newLeft = currentPos.X - _mouseDownPos.X;
                double newTop = currentPos.Y - _mouseDownPos.Y;
                
                // Clamp to screen bounds (keep at least half of widget visible - 48px)
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                double minVisible = 48; // Half of 96px widget width
                
                newLeft = Math.Max(-minVisible, Math.Min(newLeft, screenWidth - minVisible));
                newTop = Math.Max(0, Math.Min(newTop, screenHeight - minVisible));
                
                Left = newLeft;
                Top = newTop;
            }
        }

        private void FolderIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            
            if (_isDraggingWindow)
            {
                _isDraggingWindow = false;
                AnimationHelper.EndDrag(this);
                
                // If panel is open, update the original position so it doesn't jump back on close
                if (_isExpanded)
                {
                    // Calculate the folder icon's actual position based on open direction
                    if (_openedToLeft)
                    {
                        // Folder icon is on right side, so its screen position = current Left + panel width + spacing
                        var (panelWidth, _) = CalculatePanelSize();
                        _originalLeft = Left + panelWidth + ICON_SPACING;
                    }
                    else
                    {
                        // Folder icon is on left side, so its screen position = current Left
                        _originalLeft = Left;
                    }
                    _originalTop = Top;
                }
                
                // Save new position
                _data.PosX = (int)(_isExpanded ? _originalLeft : Left);
                _data.PosY = (int)Top;
                OnDataChanged?.Invoke();
            }
            else
            {
                // It's a click, toggle panel
                TogglePanel();
            }
        }

        private void FolderIcon_RightClick(object sender, MouseButtonEventArgs e)
        {
            ShowFolderContextMenu();
            e.Handled = true;
        }

        private void FolderIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isDraggingWindow)
            {
                AnimationHelper.HoverEnter(IconScale);
            }
        }

        private void FolderIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingWindow)
            {
                AnimationHelper.HoverLeave(IconScale);
            }
        }

        #endregion

        #region Item Hover Events

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border b && !_isDraggingItem)
                b.Background = HoverBrush;
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border b && !_isDraggingItem)
                b.Background = Brushes.Transparent;
        }

        private void Item_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.DataContext is DisplayItem item)
            {
                var menu = new ContextMenu();
                
                var openItem = new MenuItem { Header = Localization.Get("Menu_Open") };
                openItem.Click += (s, a) => { try { Process.Start(new ProcessStartInfo { FileName = item.Path, UseShellExecute = true }); } catch { } };
                
                var locItem = new MenuItem { Header = Localization.Get("Menu_OpenLocation") };
                locItem.Click += (s, a) => { try { Process.Start("explorer.exe", $"/select,\"{item.Path}\""); } catch { } };
                
                // Remove from widget (restore to desktop if in storage)
                var remItem = new MenuItem { Header = Localization.Get("Menu_RemoveItem") };
                remItem.Click += (s, a) => 
                { 
                    var itemToRemove = _data.Items.FirstOrDefault(i => i.Path == item.Path);
                    if (itemToRemove != null)
                    {
                        // Restore to desktop if file is in FoldR storage
                        RestoreSingleItemToDesktop(itemToRemove.Path);
                        
                        _data.Items.Remove(itemToRemove);
                        UpdateUI();
                        OnDataChanged?.Invoke();
                    }
                };
                
                // Copy Path to clipboard
                var copyPathItem = new MenuItem { Header = Localization.Get("Menu_CopyPath") };
                copyPathItem.Click += (s, a) => 
                { 
                    try { System.Windows.Clipboard.SetText(item.Path); } catch { }
                };
                
                menu.Items.Add(openItem);
                menu.Items.Add(locItem);
                menu.Items.Add(copyPathItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(remItem);
                menu.IsOpen = true;
                e.Handled = true;
            }
        }

        #endregion

        #region Context Menu

        private void ShowFolderContextMenu()
        {
            var menu = new ContextMenu();
            
            var renameItem = new MenuItem { Header = Localization.Get("Menu_Rename") };
            renameItem.Click += (s, a) => ShowRenameDialog();
            
            var colorItem = new MenuItem { Header = Localization.Get("Menu_ChangeColor") };
            colorItem.Click += (s, a) => ShowColorPicker();
            
            // Lock/Unlock widget
            var lockItem = new MenuItem 
            { 
                Header = _data.IsLocked ? Localization.Get("Menu_UnlockWidget") : Localization.Get("Menu_LockWidget")
            };
            lockItem.Click += (s, a) =>
            {
                _data.IsLocked = !_data.IsLocked;
                UpdateUI(); // Refresh lock badge
                OnDataChanged?.Invoke();
            };
            
            // Grid columns submenu
            var gridItem = new MenuItem { Header = Localization.Get("Menu_GridSize") };
            for (int cols = 2; cols <= 6; cols++)
            {
                int c = cols;
                var colItem = new MenuItem { Header = $"{cols} " + Localization.Get("Menu_Columns"), IsChecked = _data.GridColumns == cols };
                colItem.Click += (s, a) => 
                { 
                    _data.GridColumns = c; 
                    UpdateUI();
                    OnDataChanged?.Invoke(); 
                    
                    // Resize panel if open
                    if (_isExpanded) 
                    { 
                        var (panelWidth, panelHeight) = CalculatePanelSize();
                        double oldWidth = Width;
                        double newWidth = WIDGET_WIDTH + ICON_SPACING + panelWidth;
                        
                        ExpandedPanel.Width = panelWidth;
                        ExpandedPanel.Height = panelHeight;
                        Width = newWidth;
                        Height = Math.Max(110, panelHeight);
                        
                        // Update Canvas positions and window Left based on direction
                        if (_openedToLeft)
                        {
                            Left = Left - (newWidth - oldWidth);
                            System.Windows.Controls.Canvas.SetLeft(ExpandedPanel, 0);
                            System.Windows.Controls.Canvas.SetLeft(FolderIconGrid, panelWidth + 8);
                        }
                        else
                        {
                            System.Windows.Controls.Canvas.SetLeft(FolderIconGrid, 0);
                            System.Windows.Controls.Canvas.SetLeft(ExpandedPanel, 104);
                        }
                    } 
                };
                gridItem.Items.Add(colItem);
            }
            
            // Item Size submenu - affects all widgets
            var sizeItem = new MenuItem { Header = Localization.Get("Menu_ItemSize") };
            var sizes = new[] { 
                ("Small", 0.9), 
                ("Medium", 1.1), 
                ("Default", 1.2),
                ("Large", 1.3), 
                ("Extra Large", 1.5) 
            };
            double currentScale = WidgetManager.Instance.Config.ItemScale;
            foreach (var (name, scale) in sizes)
            {
                double s = scale;
                var scaleItem = new MenuItem 
                { 
                    Header = name, 
                    IsChecked = Math.Abs(currentScale - scale) < 0.05 
                };
                scaleItem.Click += (ss, aa) =>
                {
                    WidgetManager.Instance.Config.ItemScale = s;
                    WidgetManager.Instance.SaveConfig();
                    
                    // Refresh all widgets with new size
                    foreach (var widget in WidgetManager.Instance.Widgets)
                    {
                        widget.UpdateUI();
                        // Resize panel if open
                        if (widget._isExpanded)
                        {
                            widget.TogglePanel(); // Close
                            widget.TogglePanel(); // Reopen with new size
                        }
                    }
                };
                sizeItem.Items.Add(scaleItem);
            }
            
            var deleteItem = new MenuItem { Header = Localization.Get("Menu_Delete") };
            deleteItem.Click += (s, a) =>
            {
                if (MessageBox.Show(Localization.Format("Dialog_DeleteWidget", _data.Name), 
                    Localization.Get("Dialog_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    // Restore files to desktop before deleting widget
                    RestoreItemsToDesktop();
                    
                    OnDeleted?.Invoke(this);
                    Close();
                }
            };
            
            menu.Items.Add(renameItem);
            menu.Items.Add(colorItem);
            menu.Items.Add(lockItem);
            menu.Items.Add(gridItem);
            menu.Items.Add(sizeItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
        }

        #endregion
    }
}
