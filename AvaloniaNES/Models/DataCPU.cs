using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaNES.Models;

public partial class DataCPU : ViewModelBase
{
    //Status
    public BUS_STATE BusState { get; set; }
    
    //Register
    [ObservableProperty]private byte _registerA;
    [ObservableProperty]private byte _registerX;
    [ObservableProperty]private byte _registerY;
    [ObservableProperty]private byte _stackPointer;
    [ObservableProperty]private ushort _programCounter;
    
    //Status
    [ObservableProperty]private byte _carryFlag;
    [ObservableProperty]private byte _zeroFlag;
    [ObservableProperty]private byte _interruptDisableFlag;
    [ObservableProperty]private byte _decimalModeFlag;
    [ObservableProperty]private byte _breakCommandFlag;
    [ObservableProperty]private byte _unusedFlag;
    [ObservableProperty]private byte _overflowFlag;
    [ObservableProperty]private byte _negativeFlag;
    
    //disassembly
    [ObservableProperty]private ObservableCollection<AssemblyItem> _mapAssembly = [];
    [ObservableProperty]private AssemblyItem? _selectedAssembly;

    public void UpdateSelectItem()
    {
        if (SelectedAssembly != null) SelectedAssembly.IsPointHere = false;
        SelectedAssembly = null;
        var item = MapAssembly.FirstOrDefault(p => p.Address == ProgramCounter);
        if (item != null)
        {
            item.IsPointHere = true;
            SelectedAssembly = item;
        }
    }
}