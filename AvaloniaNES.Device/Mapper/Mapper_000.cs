using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

// this code is written by deepseek
public class Mapper_000 : IMapperService
{
    private byte _prgBank;
    private byte _chrBank;

    public void MapperInit(byte prgBanks, byte chrBanks)
    {
        _prgBank = prgBanks;
        _chrBank = chrBanks;
        Reset();
    }

    public void Reset()
    {
    }

    public MirroringType GetMirrorType()  // in 000,this is invalid function
    {
        return MirroringType.Hardware;
    }

    public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
    {
        if (address >= 0x8000)
        {
            // Mapper 0: 16KB ROM重复映射到32KB空间，32KB ROM直接映射
            mapAddress = (uint)(address & (_prgBank > 1 ? 0x7FFF : 0x3FFF));
            return true;
        }
        return false;
    }

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
    {
        if (address >= 0x8000)
        {
            // Mapper 0通常不支持PRG RAM写入，但保留地址映射逻辑
            // 严格来说，Mapper 0应该返回false表示不处理写入
            // mapAddress = (uint)(address & (_prgBank > 1 ? 0x7FFF : 0x3FFF));
            return false;
        }
        return false;
    }

    public bool PPUMapRead(ushort address, ref uint mapAddress)
    {
        if (address <= 0x1FFF)
        {
            mapAddress = address;
            return true;
        }
        return false;
    }

    public bool PPUMapWrite(ushort address, ref uint mapAddress)
    {
        if (address <= 0x1FFF)
        {
            if (_chrBank == 0)
            {
                // Treat as RAM
                mapAddress = address;
                return true;
            }
        }
        return false;
    }

    public bool irqState()
    {
        return false;
    }

    public void irqClear()
    {
        return;
    }

    public void scanline()
    {
        return;
    }
}