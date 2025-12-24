using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FoldR.Core;
using Localization = FoldR.Core.Localization;

namespace FoldR.Controls
{
    /// <summary>
    /// FolderWidget - Main class with core functionality
    /// Partial classes: Theme.cs, Dialogs.cs, Interactions.cs
    /// </summary>
    public partial class FolderWidget : Window
    {
        #region Fields
        
        private FolderData _data;
        private bool _isExpanded = false;
        private Point _mouseDownPos;
        private Point _mouseDownScreenPos;
        private bool _isDraggingWindow = false;
        
        // Item drag-drop fields
        private bool _isDraggingItem = false;
        private Point _itemDragStartPos;
        private DisplayItem _draggedItem = null;
        private const double DRAG_THRESHOLD = 10;
        
        // Widget size constants
        private const int WIDGET_WIDTH = 96;
        private const int WIDGET_HEIGHT = 110;
        private const int ICON_SPACING = 8;
        private const int DEFAULT_PANEL_LEFT = 104; // WIDGET_WIDTH + ICON_SPACING
        
        // Base item sizes (scale 1.0)
        private const int BASE_ITEM_WIDTH = 72;
        private const int BASE_ITEM_HEIGHT = 85;
        private const int ITEM_MARGIN = 4;
        private const int HEADER_HEIGHT = 40;
        private const int PADDING = 16;
        
        // Get scaled item dimensions
        private int GetScaledItemWidth() => (int)(BASE_ITEM_WIDTH * GetItemScale()) + ITEM_MARGIN;
        private int GetScaledItemHeight() => (int)(BASE_ITEM_HEIGHT * GetItemScale()) + ITEM_MARGIN;
        
        private double GetItemScale()
        {
            try { return WidgetManager.Instance.Config.ItemScale; }
            catch { return 1.3; } // Default if manager not ready
        }
        
        #endregion

        #region Properties and Events

        public event Action<FolderWidget> OnDeleted;
        public event Action OnDataChanged;
        public FolderData Data => _data;
        public string FolderId => _data.Id;
        
        #endregion

        #region Constructor

        public FolderWidget(FolderData data)
        {
            InitializeComponent();
            _data = data;
            Left = data.PosX;
            Top = data.PosY;
            
            // Subscribe to theme changes for automatic updates
            ThemeManager.ThemeChanged += OnThemeChanged;
            
            Loaded += (s, e) =>
            {
                // Apply theme colors (includes DrawFolderIcon, UpdatePanelColor, UpdateUI)
                RefreshTheme();
                UpdatePinButtonVisual();
                
                // If panel was pinned, auto-open it after layout is ready
                if (_data.IsPanelPinned)
                {
                    // Wait for layout to complete before opening panel
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        UpdateLayout(); // Force layout update
                        TogglePanel();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
                
                this.Opacity = 0;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                this.BeginAnimation(OpacityProperty, fadeIn);
            };
            
            Closing += (s, e) =>
            {
                // Unsubscribe from theme changes
                ThemeManager.ThemeChanged -= OnThemeChanged;
            };
        }
        
        private void OnThemeChanged()
        {
            Dispatcher.Invoke(() => RefreshTheme());
        }
        
        #endregion

        #region UI Updates

        public void UpdateUI()
        {
            FolderNameText.Text = _data.Name;
            PanelHeaderText.Text = _data.Name;
            
            // Badge
            if (_data.Items.Count > 0)
            {
                BadgeBorder.Visibility = Visibility.Visible;
                BadgeBorder.Background = new SolidColorBrush(Utils.HexToColor(_data.Color));
                BadgeText.Text = _data.Items.Count > 99 ? "99+" : _data.Items.Count.ToString();
            }
            else
            {
                BadgeBorder.Visibility = Visibility.Collapsed;
            }
            
            // Lock indicator
            LockBadge.Visibility = _data.IsLocked ? Visibility.Visible : Visibility.Collapsed;
            
            // Apply item scale transform (reuse existing transform)
            double scale = GetItemScale();
            if (ItemsContainer.LayoutTransform is ScaleTransform existingScale)
            {
                existingScale.ScaleX = scale;
                existingScale.ScaleY = scale;
            }
            else
            {
                ItemsContainer.LayoutTransform = new ScaleTransform(scale, scale);
            }
            
            // Items - include index for drag-drop reordering
            // Get current theme text color from ThemeManager
            var textBrush = ThemeManager.TextBrush;
            
            var items = _data.Items.Select((item, index) => new DisplayItem
            {
                Name = GetDisplayName(string.IsNullOrEmpty(item.Name) ? item.Path : item.Name),
                Path = item.Path,
                Icon = null, // Load asynchronously to prevent freeze
                Index = index,
                TextColor = textBrush
            }).ToList();
            
            ItemsContainer.ItemsSource = items;
            
            // Load icons progressively - each icon appears immediately when loaded
            System.Threading.Tasks.Task.Run(() =>
            {
                foreach (var item in items)
                {
                    try
                    {
                        var icon = GetFileIcon(item.Path);
                        if (icon != null)
                        {
                            icon.Freeze();
                            // Update immediately on UI thread
                            Dispatcher.BeginInvoke(new Action(() => item.Icon = icon), 
                                System.Windows.Threading.DispatcherPriority.Normal);
                        }
                    }
                    catch { /* Ignore icon load errors */ }
                }
            });
            
            // Update grid columns
            var uniformGrid = FindUniformGrid(ItemsContainer);
            if (uniformGrid != null)
            {
                uniformGrid.Columns = Math.Max(1, _data.GridColumns);
            }
            
            // Empty state
            EmptyState.Visibility = _data.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyText.Text = Localization.Get("UI_DropHere");
            
            // Update panel colors (for theme changes)
            UpdatePanelColor();
            
            // Apply item text colors after items are rendered
            Dispatcher.BeginInvoke(new Action(() => ApplyItemTextColors()), 
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        /// <summary>
        /// Gets display name for a file - removes .lnk extension from shortcuts
        /// </summary>
        private string GetDisplayName(string path)
        {
            string fileName = System.IO.Path.GetFileName(path);
            
            // Remove .lnk extension from shortcuts for cleaner display
            if (fileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - 4);
            }
            
            return fileName;
        }

        #endregion
    }
}
