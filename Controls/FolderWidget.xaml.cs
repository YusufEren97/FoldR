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
    /// Display item for the ItemsControl
    /// </summary>
    /// <summary>
    /// Display item for the ItemsControl
    /// </summary>
    public class DisplayItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Path { get; set; }
        
        private ImageSource _icon;
        public ImageSource Icon 
        { 
            get => _icon; 
            set 
            { 
                _icon = value; 
                OnPropertyChanged("Icon"); 
            } 
        }
        
        public int Index { get; set; }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

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
            
            Loaded += (s, e) =>
            {
                // Apply theme colors (includes DrawFolderIcon, UpdatePanelColor, UpdateUI)
                RefreshTheme();
                UpdatePinButtonVisual();
                
                // If panel was pinned, auto-open it after startup
                if (_data.IsPanelPinned)
                {
                    Dispatcher.BeginInvoke(new Action(() => TogglePanel()), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
                
                this.Opacity = 0;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                this.BeginAnimation(OpacityProperty, fadeIn);
            };
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
            var items = _data.Items.Select((item, index) => new DisplayItem
            {
                Name = string.IsNullOrEmpty(item.Name) ? System.IO.Path.GetFileName(item.Path) : item.Name,
                Path = item.Path,
                Icon = null, // Load asynchronously to prevent freeze
                Index = index
            }).ToList();
            
            ItemsContainer.ItemsSource = items;
            
            // Load icons in background
            System.Threading.Tasks.Task.Run(() =>
            {
                foreach (var item in items)
                {
                    try
                    {
                        var icon = GetFileIcon(item.Path);
                        if (icon != null)
                        {
                            // Update on UI thread not strictly necessary since DisplayItem notifies, 
                            // but safer for ImageSource updates
                            icon.Freeze();
                            item.Icon = icon;
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

        #endregion
    }
}
