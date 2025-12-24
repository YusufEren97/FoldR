using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FoldR.Core;
using Path = System.Windows.Shapes.Path;

namespace FoldR.Controls
{
    /// <summary>
    /// FolderWidget - Theme related methods
    /// </summary>
    public partial class FolderWidget
    {
        /// <summary>
        /// Refreshes entire widget theme - called when theme changes or on startup
        /// </summary>
        public void RefreshTheme()
        {
            // Apply all theme colors synchronously
            ApplyThemeColors();
            
            // Refresh folder icon with new colors
            DrawFolderIcon();
            
            // Refresh items display (includes text color binding)
            UpdateUI();
        }
        
        /// <summary>
        /// Central method to apply all theme colors
        /// </summary>
        private void ApplyThemeColors()
        {
            var textBrush = ThemeManager.TextBrush;
            
            // Folder name (widget label) - always white for visibility on any wallpaper
            FolderNameText.Foreground = new SolidColorBrush(Colors.White);
            FolderNameText.Effect = null; // No effect for crisp text
            
            // Panel header text
            PanelHeaderText.Foreground = textBrush;
            
            // Empty state text
            EmptyText.Foreground = textBrush;
            
            // Update panel background colors
            UpdatePanelColor();
        }
        
        /// <summary>
        /// Apply text colors to all items in the panel (for theme change updates)
        /// </summary>
        private void ApplyItemTextColors()
        {
            var textBrush = ThemeManager.TextBrush;
            
            // Update DisplayItem TextColor property - binding will handle the rest
            var items = ItemsContainer.ItemsSource as System.Collections.Generic.List<DisplayItem>;
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.TextColor = textBrush;
                }
            }
        }
        
        private void UpdatePanelColor()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            Color darkColor = Utils.DarkenColor(baseColor, 0.3);
            
            // Use ThemeManager for theme state
            bool isDark = ThemeManager.IsDarkTheme;
            
            // Panel background
            var panelBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            
            if (isDark)
            {
                // Dark theme
                panelBrush.GradientStops.Add(new GradientStop(Color.FromArgb(210, darkColor.R, darkColor.G, darkColor.B), 0));
                panelBrush.GradientStops.Add(new GradientStop(Color.FromArgb(230, (byte)(darkColor.R * 0.6), (byte)(darkColor.G * 0.6), (byte)(darkColor.B * 0.6)), 1));
            }
            else
            {
                // Light theme - brighter, pastel-like colors
                Color lightColor = Utils.LightenColor(baseColor, 0.6);
                panelBrush.GradientStops.Add(new GradientStop(Color.FromArgb(240, 250, 250, 250), 0));
                panelBrush.GradientStops.Add(new GradientStop(Color.FromArgb(250, lightColor.R, lightColor.G, lightColor.B), 1));
            }
            
            ExpandedPanel.Background = panelBrush;
            
            // Panel header
            var headerBrush = new SolidColorBrush(isDark 
                ? Color.FromArgb(50, 255, 255, 255) 
                : Color.FromArgb(40, 0, 0, 0));
            PanelHeader.Background = headerBrush;
            
            // Header text color
            PanelHeaderText.Foreground = new SolidColorBrush(isDark ? Colors.White : Color.FromRgb(40, 40, 40));
            
            // Folder name text below icon - always white (already set in ApplyThemeColors)
            // No shadow effect for crisp, clean text
            
            var glowBrush = GlowEffect.Fill as RadialGradientBrush;
            if (glowBrush != null && glowBrush.GradientStops.Count > 0)
            {
                glowBrush.GradientStops[0].Color = Color.FromArgb(128, baseColor.R, baseColor.G, baseColor.B);
            }
        }

        private void DrawFolderIcon()
        {
            FolderIconCanvas.Children.Clear();
            
            string iconStyle = WidgetManager.Instance.Config.IconStyle ?? "classic";
            
            switch (iconStyle)
            {
                case "modern": DrawModernIcon(); break;
                case "minimal": DrawMinimalIcon(); break;
                case "rounded": DrawRoundedIcon(); break;
                case "flat": DrawFlatIcon(); break;
                case "gradient": DrawGradientIcon(); break;
                default: DrawClassicIcon(); break;
            }
        }
        
        private void DrawClassicIcon()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            Color darkColor = Utils.DarkenColor(baseColor, 0.55);
            Color darkerColor = Utils.DarkenColor(baseColor, 0.35);
            Color lightColor = Utils.LightenColor(baseColor, 1.3);
            Color highlightColor = Utils.LightenColor(baseColor, 1.6);
            
            // Main folder body with rounded corners
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(8, 4), IsClosed = true };
            // Tab top
            figure.Segments.Add(new LineSegment(new Point(22, 4), true));
            figure.Segments.Add(new LineSegment(new Point(28, 12), true));
            // Top right with corner
            figure.Segments.Add(new LineSegment(new Point(54, 12), true));
            figure.Segments.Add(new ArcSegment(new Point(60, 18), new Size(6, 6), 0, false, SweepDirection.Clockwise, true));
            // Right side
            figure.Segments.Add(new LineSegment(new Point(60, 52), true));
            figure.Segments.Add(new ArcSegment(new Point(54, 58), new Size(6, 6), 0, false, SweepDirection.Clockwise, true));
            // Bottom
            figure.Segments.Add(new LineSegment(new Point(10, 58), true));
            figure.Segments.Add(new ArcSegment(new Point(4, 52), new Size(6, 6), 0, false, SweepDirection.Clockwise, true));
            // Left side back to top
            figure.Segments.Add(new LineSegment(new Point(4, 10), true));
            figure.Segments.Add(new ArcSegment(new Point(8, 4), new Size(6, 6), 0, false, SweepDirection.Clockwise, true));
            geometry.Figures.Add(figure);
            
            // Rich gradient for 3D effect
            var gradientBrush = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            gradientBrush.GradientStops.Add(new GradientStop(highlightColor, 0));
            gradientBrush.GradientStops.Add(new GradientStop(lightColor, 0.15));
            gradientBrush.GradientStops.Add(new GradientStop(baseColor, 0.4));
            gradientBrush.GradientStops.Add(new GradientStop(darkColor, 0.85));
            gradientBrush.GradientStops.Add(new GradientStop(darkerColor, 1));
            
            var folderPath = new Path
            {
                Data = geometry,
                Fill = gradientBrush,
                Effect = new System.Windows.Media.Effects.DropShadowEffect 
                { 
                    BlurRadius = 15, 
                    ShadowDepth = 5, 
                    Opacity = 0.45, 
                    Direction = 270,
                    Color = Color.FromRgb(0, 0, 0)
                }
            };
            FolderIconCanvas.Children.Add(folderPath);
            
            // Top highlight line (gives shine effect)
            FolderIconCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = 10, Y1 = 15, X2 = 54, Y2 = 15,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 
                StrokeThickness = 1.5,
                StrokeStartLineCap = PenLineCap.Round, 
                StrokeEndLineCap = PenLineCap.Round
            });
            
            // Inner content lines (document representation)
            var lineBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            double[] lineWidths = { 36, 28, 20 };
            for (int i = 0; i < 3; i++)
            {
                FolderIconCanvas.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = 14, Y1 = 26 + i * 9, X2 = 14 + lineWidths[i], Y2 = 26 + i * 9,
                    Stroke = lineBrush, StrokeThickness = 2.5,
                    StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
                });
            }
            
            // Subtle edge highlight on left
            FolderIconCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = 5, Y1 = 14, X2 = 5, Y2 = 50,
                Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 
                StrokeThickness = 1
            });
        }
        
        private void DrawModernIcon()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            
            // Modern flat folder - no gradients, clean design
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(4, 8), IsClosed = true };
            figure.Segments.Add(new LineSegment(new Point(24, 8), true));
            figure.Segments.Add(new LineSegment(new Point(28, 14), true));
            figure.Segments.Add(new LineSegment(new Point(60, 14), true));
            figure.Segments.Add(new LineSegment(new Point(60, 56), true));
            figure.Segments.Add(new LineSegment(new Point(4, 56), true));
            figure.Segments.Add(new LineSegment(new Point(4, 8), true));
            geometry.Figures.Add(figure);
            
            var folderPath = new Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(baseColor),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.4, Direction = 270 }
            };
            FolderIconCanvas.Children.Add(folderPath);
            
            // Tab notch
            FolderIconCanvas.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Width = 20, Height = 4, Fill = new SolidColorBrush(Utils.DarkenColor(baseColor, 0.8)),
                RadiusX = 2, RadiusY = 2
            });
            System.Windows.Controls.Canvas.SetLeft(FolderIconCanvas.Children[1], 6);
            System.Windows.Controls.Canvas.SetTop(FolderIconCanvas.Children[1], 10);
        }
        
        private void DrawMinimalIcon()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            bool isDark = ThemeManager.IsDarkTheme;
            
            // Minimal outline folder with rounded corners
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(8, 12), IsClosed = true };
            
            // Tab part
            figure.Segments.Add(new LineSegment(new Point(22, 12), true));
            figure.Segments.Add(new LineSegment(new Point(28, 18), true));
            
            // Top right corner
            figure.Segments.Add(new LineSegment(new Point(54, 18), true));
            figure.Segments.Add(new ArcSegment(new Point(58, 22), new Size(4, 4), 0, false, SweepDirection.Clockwise, true));
            
            // Right side and bottom right
            figure.Segments.Add(new LineSegment(new Point(58, 50), true));
            figure.Segments.Add(new ArcSegment(new Point(54, 54), new Size(4, 4), 0, false, SweepDirection.Clockwise, true));
            
            // Bottom and bottom left
            figure.Segments.Add(new LineSegment(new Point(10, 54), true));
            figure.Segments.Add(new ArcSegment(new Point(6, 50), new Size(4, 4), 0, false, SweepDirection.Clockwise, true));
            
            // Left side and top left
            figure.Segments.Add(new LineSegment(new Point(6, 16), true));
            figure.Segments.Add(new ArcSegment(new Point(8, 12), new Size(4, 4), 0, false, SweepDirection.Clockwise, true));
            
            geometry.Figures.Add(figure);
            
            var folderPath = new Path
            {
                Data = geometry,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(baseColor),
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = isDark ? 0.4 : 0.2, Direction = 270 }
            };
            FolderIconCanvas.Children.Add(folderPath);
            
            // Inner document lines
            var lineBrush = new SolidColorBrush(Color.FromArgb((byte)(isDark ? 150 : 100), baseColor.R, baseColor.G, baseColor.B));
            for (int i = 0; i < 2; i++)
            {
                FolderIconCanvas.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = 14, Y1 = 32 + i * 10, X2 = 50, Y2 = 32 + i * 10,
                    Stroke = lineBrush, StrokeThickness = 2,
                    StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
                });
            }
        }
        
        private void DrawRoundedIcon()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            Color darkColor = Utils.DarkenColor(baseColor, 0.6);
            Color lightColor = Utils.LightenColor(baseColor, 0.4);
            bool isDark = ThemeManager.IsDarkTheme;
            
            // iOS-style folder - main body with large radius
            var mainBody = new System.Windows.Shapes.Rectangle
            {
                Width = 54,
                Height = 40,
                RadiusX = 10,
                RadiusY = 10,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 4, Opacity = 0.5, Direction = 270 }
            };
            
            var gradientBrush = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            gradientBrush.GradientStops.Add(new GradientStop(lightColor, 0));
            gradientBrush.GradientStops.Add(new GradientStop(baseColor, 0.4));
            gradientBrush.GradientStops.Add(new GradientStop(darkColor, 1));
            mainBody.Fill = gradientBrush;
            
            System.Windows.Controls.Canvas.SetLeft(mainBody, 5);
            System.Windows.Controls.Canvas.SetTop(mainBody, 16);
            FolderIconCanvas.Children.Add(mainBody);
            
            // Tab on top - iOS style rounded
            var tab = new System.Windows.Shapes.Rectangle
            {
                Width = 24,
                Height = 12,
                RadiusX = 5,
                RadiusY = 5
            };
            
            var tabGradient = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            tabGradient.GradientStops.Add(new GradientStop(lightColor, 0));
            tabGradient.GradientStops.Add(new GradientStop(baseColor, 1));
            tab.Fill = tabGradient;
            
            System.Windows.Controls.Canvas.SetLeft(tab, 5);
            System.Windows.Controls.Canvas.SetTop(tab, 6);
            FolderIconCanvas.Children.Add(tab);
            
            // Highlight line at top of body
            FolderIconCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = 12, Y1 = 20, X2 = 52, Y2 = 20,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 
                StrokeThickness = 1.5,
                StrokeStartLineCap = PenLineCap.Round, 
                StrokeEndLineCap = PenLineCap.Round
            });
            
            // Inner folder icon (small document)
            var innerDoc = new System.Windows.Shapes.Rectangle
            {
                Width = 18,
                Height = 14,
                RadiusX = 3,
                RadiusY = 3,
                Fill = new SolidColorBrush(Color.FromArgb((byte)(isDark ? 60 : 40), 255, 255, 255)),
                Stroke = new SolidColorBrush(Color.FromArgb((byte)(isDark ? 100 : 60), 255, 255, 255)),
                StrokeThickness = 1
            };
            System.Windows.Controls.Canvas.SetLeft(innerDoc, 23);
            System.Windows.Controls.Canvas.SetTop(innerDoc, 30);
            FolderIconCanvas.Children.Add(innerDoc);
        }
        
        private void DrawFlatIcon()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            Color darkColor = Utils.DarkenColor(baseColor, 0.85);
            
            // Simple flat folder back
            var back = new System.Windows.Shapes.Rectangle
            {
                Width = 52,
                Height = 38,
                RadiusX = 4,
                RadiusY = 4,
                Fill = new SolidColorBrush(darkColor)
            };
            Canvas.SetLeft(back, 6);
            Canvas.SetTop(back, 18);
            FolderIconCanvas.Children.Add(back);
            
            // Flat folder front
            var front = new System.Windows.Shapes.Rectangle
            {
                Width = 52,
                Height = 34,
                RadiusX = 4,
                RadiusY = 4,
                Fill = new SolidColorBrush(baseColor)
            };
            Canvas.SetLeft(front, 6);
            Canvas.SetTop(front, 22);
            FolderIconCanvas.Children.Add(front);
            
            // Tab
            var tab = new System.Windows.Shapes.Rectangle
            {
                Width = 20,
                Height = 8,
                RadiusX = 3,
                RadiusY = 3,
                Fill = new SolidColorBrush(baseColor)
            };
            Canvas.SetLeft(tab, 6);
            Canvas.SetTop(tab, 14);
            FolderIconCanvas.Children.Add(tab);
        }
        
        private void DrawGradientIcon()
        {
            Color baseColor = Utils.HexToColor(_data.Color);
            Color darkColor = Utils.DarkenColor(baseColor, 0.6);
            Color lightColor = Utils.LightenColor(baseColor, 1.4);
            Color accentColor = Utils.LightenColor(baseColor, 1.8);
            
            // Back shadow layer
            var shadow = new System.Windows.Shapes.Rectangle
            {
                Width = 52,
                Height = 40,
                RadiusX = 6,
                RadiusY = 6,
                Fill = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))
            };
            Canvas.SetLeft(shadow, 8);
            Canvas.SetTop(shadow, 20);
            FolderIconCanvas.Children.Add(shadow);
            
            // Main body with rich gradient
            var body = new System.Windows.Shapes.Rectangle
            {
                Width = 52,
                Height = 40,
                RadiusX = 6,
                RadiusY = 6
            };
            
            var bodyGradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            bodyGradient.GradientStops.Add(new GradientStop(accentColor, 0));
            bodyGradient.GradientStops.Add(new GradientStop(lightColor, 0.3));
            bodyGradient.GradientStops.Add(new GradientStop(baseColor, 0.7));
            bodyGradient.GradientStops.Add(new GradientStop(darkColor, 1));
            body.Fill = bodyGradient;
            
            Canvas.SetLeft(body, 6);
            Canvas.SetTop(body, 16);
            FolderIconCanvas.Children.Add(body);
            
            // Gradient tab
            var tab = new System.Windows.Shapes.Rectangle
            {
                Width = 22,
                Height = 10,
                RadiusX = 4,
                RadiusY = 4
            };
            
            var tabGradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            tabGradient.GradientStops.Add(new GradientStop(accentColor, 0));
            tabGradient.GradientStops.Add(new GradientStop(lightColor, 1));
            tab.Fill = tabGradient;
            
            Canvas.SetLeft(tab, 6);
            Canvas.SetTop(tab, 8);
            FolderIconCanvas.Children.Add(tab);
            
            // Shine highlight
            var shine = new System.Windows.Shapes.Rectangle
            {
                Width = 44,
                Height = 8,
                RadiusX = 4,
                RadiusY = 4,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
            };
            Canvas.SetLeft(shine, 10);
            Canvas.SetTop(shine, 18);
            FolderIconCanvas.Children.Add(shine);
        }
        
        #region Helper Methods

        private UniformGrid FindUniformGrid(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is UniformGrid ug) return ug;
                var result = FindUniformGrid(child);
                if (result != null) return result;
            }
            return null;
        }

        private (int width, int height) CalculatePanelSize()
        {
            int cols = Math.Max(1, _data.GridColumns);
            int itemCount = Math.Max(1, _data.Items.Count); // At least 1 for empty state
            int rows = (int)Math.Ceiling((double)itemCount / cols);
            
            int width = cols * GetScaledItemWidth() + PADDING;
            int height = HEADER_HEIGHT + rows * GetScaledItemHeight() + PADDING;
            
            // Minimum dimensions
            width = Math.Max(width, 180);
            height = Math.Max(height, 120);
            
            return (width, height);
        }
        
        #endregion
    }
}
