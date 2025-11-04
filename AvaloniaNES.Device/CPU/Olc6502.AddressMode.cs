namespace AvaloniaNES.Device.CPU;

public partial class Olc6502
{
    private delegate byte AddressingMode();

    // all functions return addtional cycle count
    private byte IMP()
    {
        fetched = A;
        return 0;
    }

    private byte IMM()
    {
        addr_abs = PC++;
        return 0;
    }
    
    private byte ZP0()
    {
        addr_abs = Read(PC);
        PC++;
        addr_abs &= 0x00FF;
        return 0;
    }

    private byte ZPX()
    {
        addr_abs = (ushort)(Read(PC) + X);
        PC++;
        addr_abs &= 0x00FF;
        return 0;
    }
    
    private byte ZPY()
    {
        addr_abs = (ushort)(Read(PC) + Y);
        PC++;
        addr_abs &= 0x00FF;
        return 0;
    }

    private byte REL()
    {
        addr_rel = Read(PC);
        PC++;
        if ((addr_rel & 0x80) > 0)
        {
            addr_rel |= 0xFF00;
        }
        return 0;
    }

    private byte ABS()
    {
        var lo = Read(PC);
        PC++;
        var hi = Read(PC);
        PC++;
        addr_abs = (ushort)((hi << 8) | lo);
        return 0;
    }

    private byte ABX()
    {
        var lo = Read(PC);
        PC++;
        var hi = Read(PC);
        PC++;
        addr_abs = (ushort)((hi << 8) | lo);
        addr_abs += X;
        if ((addr_abs & 0xFF00) != (hi << 8))
        {
            return 1;
        }
        return 0;
    }

    private byte ABY()
    {
        var lo = Read(PC);
        PC++;
        var hi = Read(PC);
        PC++;
        addr_abs = (ushort)((hi << 8) | lo);
        addr_abs += Y;
        if ((addr_abs & 0xFF00) != (hi << 8))
        {
            return 1;
        }
        return 0;
    }

    private byte IND()
    {
        var ptr_lo = Read(PC);
        PC++;
        var ptr_hi = Read(PC);
        PC++;
        var ptr = (ushort)((ptr_hi << 8) | ptr_lo);
        if (ptr_lo == 0x00FF)  // Simulate Page Bug
        {
            addr_abs = (ushort)((Read((ushort)(ptr & 0xFF00)) << 8) | Read(ptr));
        }
        else
        {
            addr_abs = (ushort)((Read((ushort)(ptr + 1)) << 8) | Read(ptr));
        }

        return 0;
    }

    private byte IZX()
    {
        var t = Read(PC);
        PC++;
        var lo = Read((ushort)((t + X) & 0x00FF));
        var hi = Read((ushort)((t + X + 1) & 0x00FF));
        addr_abs = (ushort)((hi << 8) | lo);
        return 0;
    }
    
    private byte IZY()
    { 
        var t = Read(PC);
        PC++;
        var lo = Read((ushort)(t & 0x00FF));
        var hi = Read((ushort)((t + 1) & 0x00FF));
        addr_abs = (ushort)((hi << 8) | lo);
        addr_abs += Y;
        if ((addr_abs & 0xFF00) != (hi << 8))
        {
            return 1;
        }
        return 0;
    }
}