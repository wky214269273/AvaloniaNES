using System;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaNES.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _renderTimer;
    private readonly NESStatus _status = App.Services.GetRequiredService<NESStatus>();
    private readonly Bus _bus = App.Services.GetRequiredService<Bus>();
    public MainWindow()
    {
        InitializeComponent();
        m_Video.Source = _bus.PPU!.GetScreen();
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // 60FPS
        };
        _renderTimer.Tick += OnRenderFrame;
        _renderTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Environment.Exit(0);
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        if (_status.HasLoadRom)
        {
            // Run Clock
            if (_status.BusState == BUS_STATE.RUN)
            {
                do
                {
                    _bus.Clock();
                } while (!_bus.PPU!.FrameCompleted);
                _bus.PPU!.FrameCompleted = false;
            }
            
            // update image
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // method 1
                //var temp = m_Video.Source;
                // m_Video.Source = null;
                // m_Video.Source = _bus.PPU!.GetScreen();
            
                // method 2
                m_Video.InvalidateMeasure();
                m_Video.InvalidateArrange();
                m_Video.InvalidateVisual();
            }, DispatcherPriority.Render);
        }
    }
}