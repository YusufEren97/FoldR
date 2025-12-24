using System.Windows.Media;
using FoldR.Controls;

namespace FoldR.Core
{
    /// <summary>
    /// Centralized theme management for the application
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Returns true if current theme is dark
        /// </summary>
        public static bool IsDarkTheme
        {
            get
            {
                string theme = WidgetManager.Instance?.Config?.Theme ?? "dark";
                return theme == "dark";
            }
        }
        
        // Panel colors
        public static Color PanelBackground => IsDarkTheme 
            ? Color.FromRgb(40, 40, 40) 
            : Color.FromRgb(250, 250, 250);
            
        public static Color PanelHeaderBackground => IsDarkTheme 
            ? Color.FromArgb(50, 255, 255, 255) 
            : Color.FromArgb(40, 0, 0, 0);
            
        public static Color TextColor => IsDarkTheme 
            ? Colors.White 
            : Color.FromRgb(40, 40, 40);
            
        public static Color SecondaryTextColor => IsDarkTheme 
            ? Color.FromRgb(180, 180, 180) 
            : Color.FromRgb(100, 100, 100);
        
        // Item colors
        public static Color ItemBackground => IsDarkTheme 
            ? Color.FromArgb(60, 255, 255, 255) 
            : Color.FromArgb(80, 0, 0, 0);
            
        public static Color ItemHoverBackground => IsDarkTheme 
            ? Color.FromArgb(100, 255, 255, 255) 
            : Color.FromArgb(40, 0, 0, 0);
        
        // Menu colors (always dark)
        public static readonly Color MenuBackground = Color.FromRgb(40, 40, 40);
        public static readonly Color MenuBorder = Color.FromRgb(60, 60, 60);
        public static readonly Color MenuHover = Color.FromRgb(60, 60, 60);
        
        // Dialog colors (always dark)
        public static readonly Color DialogBackground = Color.FromRgb(30, 30, 30);
        public static readonly Color DialogBorder = Color.FromRgb(60, 60, 60);
        public static readonly Color InputBackground = Color.FromRgb(45, 45, 45);
        public static readonly Color InputBorder = Color.FromRgb(70, 70, 70);
        
        // Accent colors
        public static readonly Color AccentBlue = Color.FromRgb(59, 130, 246);
        
        /// <summary>
        /// Applies theme to all widgets
        /// </summary>
        public static void ApplyTheme()
        {
            if (WidgetManager.Instance == null) return;
            
            foreach (var widget in WidgetManager.Instance.Widgets)
            {
                widget.RefreshTheme();
            }
        }
    }
}
