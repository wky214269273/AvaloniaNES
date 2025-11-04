using AvaloniaNES.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNES.Models;

public partial class AssemblyItem  : ViewModelBase
{
    [ObservableProperty]private bool _isPointHere;
    public bool IsBreakpoint { get; set; } = false;
    public ushort Address { get; set; }
    public string Instruction { get; set; }
}