using System.Threading.Tasks;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Models;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaNES.ViewModels;

public partial class PPUDebuggerViewModel : ViewModelBase
{
    private readonly DataCPU _data;
    private readonly Bus _bus;
    public PPUDebuggerViewModel(DataCPU data, Bus bus)
    {
        _data = data;
        _bus = bus;
    }
    
    [RelayCommand]
    private async Task DrawSingleFrame()
    {
        _data.UpdateSelectItem();
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
    }
}