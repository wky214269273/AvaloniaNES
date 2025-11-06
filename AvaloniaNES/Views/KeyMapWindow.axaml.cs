using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaNES.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaNES.Views;

public partial class KeyMapWindow : Window
{
    private readonly NESStatus _status = App.Services.GetRequiredService<NESStatus>();
    public KeyMapWindow()
    {
        InitializeComponent();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _status.HasShowKeyMapper = false;
    }
}