using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaNES.ViewModels;

public partial class DebuggerViewModel : ViewModelBase
{
    private readonly Bus _bus;
    private readonly NESStatus _status;
    [ObservableProperty] private DataCPU _data = null!;

    public DebuggerViewModel(DataCPU data, Bus bus,NESStatus status)
    {
        Data = data;
        _bus = bus;
        _status = status;
    }

    /* Command */
    [RelayCommand]
    private void ReLocate()
    {
        Data.SelectedAssembly = Data.MapAssembly.FirstOrDefault();
        Data.UpdateSelectItem();
    }

    [RelayCommand]
    private async Task Step()
    {
        if (!_status.HasLoadRom) return;
        Data.UpdateSelectItem();
        await Task.Run(() =>
        {
            do
            {
                _bus.Clock();
            } while (!_bus.CPU!.Complete());

            do
            {
                _bus.Clock();
            } while (_bus.CPU!.Complete());
        });
        Data.UpdateSelectItem();
    }
    
    [RelayCommand]
    private async Task DrawSingleFrame()
    {
        if (!_status.HasLoadRom) return;
        Data.UpdateSelectItem();
        await Task.Run(() =>
        {
            do
            {
                _bus.Clock();
            } while (!_bus.PPU!.FrameCompleted);

            do
            {
                _bus.Clock();
            } while (!_bus.CPU!.Complete());
        });
        _bus.PPU!.FrameCompleted = false;
        Data.UpdateSelectItem();
    }

    [RelayCommand]
    private void Reset()
    {
        _bus.Reset();
        Data.UpdateSelectItem();
    }
}