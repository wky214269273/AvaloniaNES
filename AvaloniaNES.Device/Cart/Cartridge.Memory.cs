using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AvaloniaNES.Device.Cart;

public partial class Cartridge
{
    //Cartridge Connect CPUBus And internal Bus
    public bool CPURead(ushort address, ref byte value)
    {
        uint mapAddress = 0;
        if (_mapper.CPUMapRead(address, ref mapAddress, ref value))
        {
            if (mapAddress != 0xFFFFFFFF) // this is a flag indicating that if the value use returned from the mapper
            {
                value = _prgRam[mapAddress];
                return true;
            }
            return true;
        }
        return false;
    }

    public bool CPUWrite(ushort address, byte value)
    {
        uint mapAddress = 0;
        if (_mapper.CPUMapWrite(address, ref mapAddress, value))
        {
            if (mapAddress != 0xFFFFFFFF)
            {
                _prgRam[mapAddress] = value;
                return true;
            }
            return true;
        }
        return false;
    }

    public bool PPURead(ushort address, ref byte value)
    {
        uint mapAddress = 0;
        if (_mapper.PPUMapRead(address, ref mapAddress))
        {
            if (_chrBanks == 0)
                value = _chrRam[mapAddress % 0x2000];
            else
                value = _chrRom[mapAddress];
            return true;
        }
        return false;
    }

    public bool PPUWrite(ushort address, byte value)
    {
        uint mapAddress = 0;
        if (_mapper.PPUMapWrite(address, ref mapAddress))
        {
            if (_chrBanks == 0)
                _chrRam[mapAddress % 0x2000] = value;
            else
                _chrRom[mapAddress] = value;
            return true;
        }
        return false;
    }
}