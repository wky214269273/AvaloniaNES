using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Models;
using AvaloniaNES.Util;
using AvaloniaNES.ViewModels;
using AvaloniaNES.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaNES;

public partial class App : Application
{
    public static ServiceProvider Services { get; set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var collection = new ServiceCollection();
            collection.AddCommonServices();

            // BuildServiceProvider
            Services = collection.BuildServiceProvider();
            
            // Init Bus
            var _nes = Services.GetRequiredService<Bus>();
            _nes.InitDevice();
            _nes.Reset();
            
            // Init UI
            var mainWindow = new MainWindow()
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
            Services.GetRequiredService<PopupHelper>()._mainWnd = mainWindow;
            Services.GetRequiredService<PopupHelper>().Manager = new WindowNotificationManager(mainWindow)
            {
                Position = NotificationPosition.BottomRight
            };
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<Bus>();
        
        collection.AddSingleton<MainWindowViewModel>();
        collection.AddSingleton<DebuggerViewModel>();
        collection.AddSingleton<PPUDebuggerViewModel>();

        collection.AddSingleton<NESStatus>();
        collection.AddSingleton<PopupHelper>();
        collection.AddSingleton<DataCPU>();
    }
}