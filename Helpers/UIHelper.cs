using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FoldR.Helpers
{
    /// <summary>
    /// UI Helper methods for creating modern dialog elements
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Creates a modern styled TextBox
        /// </summary>
        public static TextBox CreateTextBox(string text = "", string placeholder = "")
        {
            var tb = new TextBox
            {
                Text = text,
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI"),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                BorderThickness = new Thickness(1),
                CaretBrush = Brushes.White
            };
            
            // Add rounded corners via template would be ideal, but for simplicity:
            tb.Resources.Add(SystemColors.WindowBrushKey, new SolidColorBrush(Color.FromRgb(45, 45, 45)));
            
            return tb;
        }

        /// <summary>
        /// Creates a modern styled Button
        /// </summary>
        public static Button CreateButton(string content, bool isPrimary = false)
        {
            var btn = new Button
            {
                Content = content,
                Padding = new Thickness(20, 8, 20, 8),
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            if (isPrimary)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue
                btn.Foreground = Brushes.White;
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                btn.Foreground = Brushes.White;
            }

            return btn;
        }

        /// <summary>
        /// Creates a modern styled Label
        /// </summary>
        public static TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 6)
            };
        }

        /// <summary>
        /// Creates a section header
        /// </summary>
        public static TextBlock CreateHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 12)
            };
        }

        /// <summary>
        /// Creates a modern styled CheckBox
        /// </summary>
        public static CheckBox CreateCheckBox(string content, bool isChecked = false)
        {
            return new CheckBox
            {
                Content = content,
                IsChecked = isChecked,
                Foreground = Brushes.White,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        /// <summary>
        /// Creates a modern styled ComboBox
        /// </summary>
        public static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                MinWidth = 120
            };
        }

        /// <summary>
        /// Creates a horizontal separator
        /// </summary>
        public static Border CreateSeparator()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Margin = new Thickness(0, 16, 0, 16)
            };
        }

        /// <summary>
        /// Applies dark window style
        /// </summary>
        public static void ApplyDarkStyle(Window window)
        {
            window.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }
    }
}
