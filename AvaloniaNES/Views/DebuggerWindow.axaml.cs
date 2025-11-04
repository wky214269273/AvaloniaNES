using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Models;
using AvaloniaNES.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaNES.Views;

public partial class DebuggerWindow : Window
{
    private readonly NESStatus _status = App.Services.GetRequiredService<NESStatus>();
    private readonly Bus _bus = App.Services.GetRequiredService<Bus>();
    public DebuggerWindow()
    {
        InitializeComponent();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _status.HasShowDebugger = false;
        _status.BusState = BUS_STATE.RUN;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_status.HasLoadRom) return;
        byte _select_palette_index = 0;
        if (m_Radio_0.IsChecked == true) _select_palette_index = 0x00;
        if (m_Radio_1.IsChecked == true) _select_palette_index = 0x01;
        if (m_Radio_2.IsChecked == true) _select_palette_index = 0x02;
        if (m_Radio_3.IsChecked == true) _select_palette_index = 0x03;
        if (m_Radio_4.IsChecked == true) _select_palette_index = 0x04;
        if (m_Radio_5.IsChecked == true) _select_palette_index = 0x05;
        if (m_Radio_6.IsChecked == true) _select_palette_index = 0x06;
        if (m_Radio_7.IsChecked == true) _select_palette_index = 0x07;
        
        m_Video_1.Source = null;
        m_Video_1.Source = _bus.PPU!.GetPatternTable(0, _select_palette_index);
        m_Video_2.Source = null;
        m_Video_2.Source = _bus.PPU!.GetPatternTable(1, _select_palette_index);
    }
}