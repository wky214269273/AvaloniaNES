using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

public class Mapper_003 : IMapperService
{
    private byte _prgBank;
    private byte _chrBank;
    private byte _chrBankSelect;

    public void MapperInit(byte prgBanks, byte chrBanks)
    {
        _prgBank = prgBanks;
        _chrBank = chrBanks;
        Reset();
    }

    public void Reset()
    {
        _chrBankSelect = 0;
    }

    public MirroringType GetMirrorType()
    {
        return MirroringType.Hardware;
    }

    public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
    {
        if (address >= 0x8000)
        {
            // Mapper 003: 16KB ROM重复映射到32KB空间，32KB ROM直接映射
            mapAddress = (uint)(address & (_prgBank > 1 ? 0x7FFF : 0x3FFF));
            return true;
        }
        return false;
    }

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
    {
        if (address >= 0x8000)
        {
            // 设置CHR Bank选择
            _chrBankSelect = (byte)(data & 0x03);
            // 添加边界检查，确保Bank索引不会超出范围
            if (_chrBank > 0)
            {
                _chrBankSelect %= _chrBank;
            }
        }
        return false; // 不处理写入到PRG ROM
    }

    public bool PPUMapRead(ushort address, ref uint mapAddress)
    {
        if (address <= 0x1FFF)
        {
            // 对于CHR-ROM，使用选择的Bank
            // 对于CHR-RAM，忽略Bank选择
            if (_chrBank > 0)
            {
                mapAddress = (uint)(_chrBankSelect * 0x2000 + (address & 0x1FFF));
            }
            else
            {
                mapAddress = address;
            }
            return true;
        }
        return false;
    }

    public bool PPUMapWrite(ushort address, ref uint mapAddress)
    {
        // 如果存在CHR-RAM，则允许写入
        if (address <= 0x1FFF && _chrBank == 0)
        {
            mapAddress = (uint)(_chrBankSelect * 0x2000 + (address & 0x1FFF));
            return true;
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