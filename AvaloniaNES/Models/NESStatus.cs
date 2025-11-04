using AvaloniaNES.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNES.Models;

public enum BUS_STATE
{
    RUN,
    DEBUG
}

public partial class NESStatus : ViewModelBase
{
    [ObservableProperty]private bool hasLoadRom = false;
    [ObservableProperty]private string _romName = string.Empty;
    [ObservableProperty]private bool _hasTask = false;
    public bool HasShowDebugger { get; set; } = false;
    public BUS_STATE BusState{ get; set; } = BUS_STATE.RUN;
}