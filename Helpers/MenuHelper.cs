using System;
using System.Windows.Controls;
using FoldR.Core;

namespace FoldR.Helpers
{
    /// <summary>
    /// Helper class for creating styled context menu items
    /// </summary>
    public static class MenuHelper
    {
        /// <summary>
        /// Creates a localized menu item with click handler
        /// </summary>
        public static MenuItem Create(string localizationKey, Action onClick)
        {
            var item = new MenuItem { Header = Localization.Get(localizationKey) };
            item.Click += (s, e) => onClick();
            return item;
        }
        
        /// <summary>
        /// Creates a menu item with custom header and click handler
        /// </summary>
        public static MenuItem Create(string header, Action onClick, bool isCustomHeader)
        {
            var item = new MenuItem { Header = header };
            item.Click += (s, e) => onClick();
            return item;
        }
        
        /// <summary>
        /// Creates a separator for context menus
        /// </summary>
        public static Separator CreateSeparator() => new Separator();
    }
}
