using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
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
    private readonly HashSet<Key> _pressedKeys = new();
    private readonly Dictionary<string, Key> _keyMap1 = new()
    {
        { "A", Key.J },
        { "B", Key.K },
        { "Up", Key.W },
        { "Down", Key.S },
        { "Left", Key.A },
        { "Right", Key.D },
        { "Select", Key.RightCtrl },
        { "Start", Key.Enter },
    };
    private readonly Dictionary<string, Key> _keyMap2 = new()
    {
        { "A", Key.NumPad1 },
        { "B", Key.NumPad2 },
        { "Up", Key.Up },
        { "Down", Key.Down },
        { "Left", Key.Left },
        { "Right", Key.Right },
        { "Select", Key.Add },
        { "Start", Key.Subtract },
    };

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
    
    /* Key */
    public void HandleKeyDown(Key key)
    {
        _pressedKeys.Add(key);
        ProcessControllerInput();
    }
    
    public void HandleKeyUp(Key key)
    {
        _pressedKeys.Remove(key);
        ProcessControllerInput();
    }
    
    private bool IsKeyPressed(Key key)
    {
        return _pressedKeys.Contains(key);
    }
    
    private void ProcessControllerInput()
    {
        // player 1
        var isAPressed = IsKeyPressed(_keyMap1["A"]) ? (byte)0x80 : (byte)0x00;
        var isBPressed = IsKeyPressed(_keyMap1["B"]) ? (byte)0x40 : (byte)0x00;
        var isSelectPressed = IsKeyPressed(_keyMap1["Select"]) ? (byte)0x20 : (byte)0x00;
        var isStartPressed = IsKeyPressed(_keyMap1["Start"]) ? (byte)0x10 : (byte)0x00;
        var isUpPressed = IsKeyPressed(_keyMap1["Up"]) ? (byte)0x08 : (byte)0x00;
        var isDownPressed = IsKeyPressed(_keyMap1["Down"]) ? (byte)0x04 : (byte)0x00;
        var isLeftPressed = IsKeyPressed(_keyMap1["Left"]) ? (byte)0x02 : (byte)0x00;
        var isRightPressed = IsKeyPressed(_keyMap1["Right"]) ? (byte)0x01 : (byte)0x00;
        
        // player 2
        var isAPressed2 = IsKeyPressed(_keyMap2["A"]) ? (byte)0x80 : (byte)0x00;
        var isBPressed2 = IsKeyPressed(_keyMap2["B"]) ? (byte)0x40 : (byte)0x00;
        var isSelectPressed2 = IsKeyPressed(_keyMap2["Select"]) ? (byte)0x20 : (byte)0x00;
        var isStartPressed2 = IsKeyPressed(_keyMap2["Start"]) ? (byte)0x10 : (byte)0x00;
        var isUpPressed2 = IsKeyPressed(_keyMap2["Up"]) ? (byte)0x08 : (byte)0x00;
        var isDownPressed2 = IsKeyPressed(_keyMap2["Down"]) ? (byte)0x04 : (byte)0x00;
        var isLeftPressed2 = IsKeyPressed(_keyMap2["Left"]) ? (byte)0x02 : (byte)0x00;
        var isRightPressed2 = IsKeyPressed(_keyMap2["Right"]) ? (byte)0x01 : (byte)0x00;
    
        // now, we just use one controller
        _nes.controller[0] = (byte)(isAPressed | isBPressed | isSelectPressed | isStartPressed | isUpPressed | isDownPressed |
                             isLeftPressed | isRightPressed);
        _nes.controller[1] = (byte)(isAPressed2 | isBPressed2 | isSelectPressed2 | isStartPressed2 | isUpPressed2 | isDownPressed2 |
                             isLeftPressed2 | isRightPressed2);
    }
    
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
            /* choose file */
            var LoadPath = await _popupHelper.ShowFileChooseDialog();
            if (string.IsNullOrWhiteSpace(LoadPath)) return;

            /* parse */
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
            
            /* update debugger window */
            Data.UpdateSelectItem();
            Status.HasLoadRom = true;
            Status.RomName = Path.GetFileName(LoadPath);
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

    [RelayCommand]
    private void Reset()
    {
        _nes.Reset();
    }

    [RelayCommand]
    private void ShowKeyMap()
    {
        if (!Status.HasShowKeyMapper)
        {
            var buffer = new KeyMapWindow();
            buffer.Show();
            Status.HasShowKeyMapper = true;
        }
    }
}