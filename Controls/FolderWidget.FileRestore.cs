using System;
using FoldR.Core;

namespace FoldR.Controls
{
    /// <summary>
    /// FolderWidget - File restore operations (restore to desktop, copy directory)
    /// </summary>
    public partial class FolderWidget
    {
        #region File Restore
        
        /// <summary>
        /// Restores all items from storage back to desktop when widget is deleted
        /// </summary>
        private void RestoreItemsToDesktop()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string storagePath = Utils.GetStoragePath();
            
            foreach (var item in _data.Items)
            {
                try
                {
                    // Only restore files that are in our storage folder
                    if (!item.Path.StartsWith(storagePath, StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    string fileName = System.IO.Path.GetFileName(item.Path);
                    string destPath = System.IO.Path.Combine(desktopPath, fileName);
                    
                    // Handle duplicate names on desktop
                    int counter = 1;
                    string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string ext = System.IO.Path.GetExtension(fileName);
                    
                    while (System.IO.File.Exists(destPath) || System.IO.Directory.Exists(destPath))
                    {
                        destPath = System.IO.Path.Combine(desktopPath, $"{nameWithoutExt} ({counter}){ext}");
                        counter++;
                    }
                    
                    // Move back to desktop
                    if (System.IO.Directory.Exists(item.Path))
                    {
                        System.IO.Directory.Move(item.Path, destPath);
                    }
                    else if (System.IO.File.Exists(item.Path))
                    {
                        System.IO.File.Move(item.Path, destPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Restore failed: {ex.Message}");
                    // File stays in storage if restore fails
                }
            }
        }
        
        /// <summary>
        /// Restores a single item from storage back to desktop
        /// </summary>
        private void RestoreSingleItemToDesktop(string filePath)
        {
            try
            {
                string storagePath = Utils.GetStoragePath();
                
                // Only restore files that are in our storage folder
                if (!filePath.StartsWith(storagePath, StringComparison.OrdinalIgnoreCase))
                    return;
                
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = System.IO.Path.GetFileName(filePath);
                string destPath = System.IO.Path.Combine(desktopPath, fileName);
                
                // Handle duplicate names on desktop
                int counter = 1;
                string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string ext = System.IO.Path.GetExtension(fileName);
                
                while (System.IO.File.Exists(destPath) || System.IO.Directory.Exists(destPath))
                {
                    destPath = System.IO.Path.Combine(desktopPath, $"{nameWithoutExt} ({counter}){ext}");
                    counter++;
                }
                
                // Move back to desktop
                if (System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.Move(filePath, destPath);
                }
                else if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Move(filePath, destPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Restore single item failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Recursively copies a directory (for cross-drive moves)
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            var dir = new System.IO.DirectoryInfo(sourceDir);
            System.IO.Directory.CreateDirectory(destDir);
            
            // Copy files
            foreach (var file in dir.GetFiles())
            {
                file.CopyTo(System.IO.Path.Combine(destDir, file.Name), false);
            }
            
            // Copy subdirectories
            foreach (var subDir in dir.GetDirectories())
            {
                CopyDirectory(subDir.FullName, System.IO.Path.Combine(destDir, subDir.Name));
            }
        }
        
        #endregion
    }
}
