namespace AvaloniaNES.Device.CPU;

public partial class Olc6502
{
    private byte Read(ushort address)
    {
        return _bus.CPURead(address, false);
    }
    private void Write(ushort address, byte value)
    {
        _bus.CPUWrite(address, value);
    }
}