using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Device.Cart;
using AvaloniaNES.Device.CPU;
using AvaloniaNES.Models;
using AvaloniaNES.Util;
using AvaloniaNES.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaNES.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private Bus _nes;
    private PopupHelper _popupHelper;

    public MainWindowViewModel(NESStatus status,
        PopupHelper popupHelper, DataCPU data, Bus nes)
    {
        Status = status;
        _popupHelper = popupHelper;
        Data = data;
        _nes = nes;

        _nes.UpdateInfo = UpdateInfo;
    }
    
    /* Info Update */
    private void UpdateInfo()
    {
        if (_nes.CPU != null)
        {
            Data.BusState = Status.BusState;
            Data.RegisterA = _nes.CPU.A;
            Data.RegisterX = _nes.CPU.X;
            Data.RegisterY = _nes.CPU.Y;
            Data.StackPointer = _nes.CPU.SP;
            Data.ProgramCounter = _nes.CPU.PC;
            Data.CarryFlag = (_nes.CPU.Status & Olc6502.CARRY_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.ZeroFlag = (_nes.CPU.Status & Olc6502.ZERO_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.InterruptDisableFlag = (_nes.CPU.Status & Olc6502.INTERRUPT_DISABLE_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.DecimalModeFlag = (_nes.CPU.Status & Olc6502.DECIMAL_MODE_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.BreakCommandFlag = (_nes.CPU.Status & Olc6502.BREAK_COMMAND_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.UnusedFlag = (_nes.CPU.Status & Olc6502.UNUSED_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.OverflowFlag = (_nes.CPU.Status & Olc6502.OVERFLOW_FLAG) > 0 ? (byte)1 : (byte)0;
            Data.NegativeFlag = (_nes.CPU.Status & Olc6502.NEGATIVE_FLAG) > 0 ? (byte)1 : (byte)0;
        }
    }
    
    /* mvvm */
    [ObservableProperty]private NESStatus _status;
    [ObservableProperty]private DataCPU _data;
    
    /* Command */
    [RelayCommand]
    private void ShowDebugger()
    {
        if (!Status.HasShowDebugger)
        {
            var buffer = new DebuggerWindow()
            {
                DataContext = App.Services.GetRequiredService<DebuggerViewModel>()
            };
            Status.BusState = BUS_STATE.DEBUG;
            Data.UpdateSelectItem();
            buffer.Show();
            Status.HasShowDebugger = true;
        }
    }
    
    [RelayCommand]
    private void Exit()
    {
        Environment.Exit(0);
    }
    
    [RelayCommand]
    private async Task LoadRom()
    {
        try
        {
            /* 选择文件 */
            var LoadPath = await _popupHelper.ShowFileChooseDialog();
            if (string.IsNullOrWhiteSpace(LoadPath)) return;

            /* 解析 */
            Status.HasTask = true;
            Status.HasLoadRom = false;
            var disAssembly = new Dictionary<ushort, string>();
            await Task.Run(() =>
            {
                Thread.Sleep(1000);
                _nes.InsertCartridge(new Cartridge(LoadPath));
                _nes.Reset();
                Data.MapAssembly.Clear();
                disAssembly = _nes.CPU!.disassemble(0x0000, 0xFFFF);
            });
            foreach (var item in disAssembly)
            {
                Data.MapAssembly.Add(new AssemblyItem()
                {
                    IsBreakpoint = false,
                    Address = item.Key,
                    Instruction = item.Value
                });
            }
            
            /* 主动更新一次CPU状态 */
            Data.UpdateSelectItem();
            Status.HasLoadRom = true;
            Status.RomName = Path.GetFileName(LoadPath);
            _popupHelper.ShowNotification("Success","Load Rom Success", NotificationType.Success);
        }
        catch (Exception ex)
        {
            _popupHelper.ShowNotification("Error", ex.Message, NotificationType.Error);
        }
        finally
        {
            Status.HasTask = false;
        }
    }
    
    [RelayCommand]
    private void RemoveRom()
    {
        _nes.RemoveCartridge();
        Status.RomName = string.Empty;
        Status.HasLoadRom = false;
        Data.MapAssembly.Clear();
    }
}