namespace AvaloniaNES.Device.Cart;

public partial class Cartridge
{
    //Cartridge Connect CPUBus And internal Bus
    public bool CPURead(ushort address, ref byte value)
    {
        uint mapAddress = 0;
        if (_mapper.CPUMapRead(address, ref mapAddress))
        {
            value = _prgRam[mapAddress];
            return true;
        }
        return false;
    }
    public bool CPUWrite(ushort address, byte value)
    {
        uint mapAddress = 0;
        if (_mapper.CPUMapWrite(address, ref mapAddress))
        {
            _prgRam[mapAddress] = value;
            return true;
        }
        return false;
    }
    public bool PPURead(ushort address, ref byte value)
    {
        uint mapAddress = 0;
        if (_mapper.PPUMapRead(address, ref mapAddress))
        {
            value = _chrRam[mapAddress];
            return true;
        }
        return false;
    }
    public bool PPUWrite(ushort address, byte value)
    {
        uint mapAddress = 0;
        if (_mapper.PPUMapRead(address, ref mapAddress))
        {
            _chrRam[mapAddress] = value;
            return true;
        }
        return false;
    }
}