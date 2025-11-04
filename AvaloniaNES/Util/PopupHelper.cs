using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;

namespace AvaloniaNES.Util;

public class PopupHelper
{
    public Window? _mainWnd { get; set; }
    public WindowNotificationManager? Manager { get; set; }

    public void ShowNotification(string title, string message, NotificationType type)
    {
        Manager?.Show(new Notification(title,message), type);
    }

    public async Task<string> ShowFileChooseDialog()
    {
        try
        {
            var result = string.Empty;
            var sp = GetStorageProvider();
            if (sp == null) throw new Exception();
            
            var pkFiles = await sp.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "select rom file",
                AllowMultiple = false,
            });
            if (pkFiles.Count > 0)
            {
                result = pkFiles[0].TryGetLocalPath() ?? string.Empty;
            }
            return result;
        }
        catch
        {
            return string.Empty;
        }
        
    }
    
    private IStorageProvider? GetStorageProvider()
    {
        return _mainWnd?.StorageProvider;
    }
}