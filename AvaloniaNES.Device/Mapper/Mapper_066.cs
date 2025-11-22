using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

public class Mapper_066 : IMapperService
{
    private byte _prgBank;
    private byte _chrBank;
    private byte _prgBankSelect;
    private byte _chrBankSelect;

    public void MapperInit(byte prgBanks, byte chrBanks)
    {
        _prgBank = prgBanks;
        _chrBank = chrBanks;
        Reset();
    }

    public void Reset()
    {
        _prgBankSelect = 0;
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
            // Mapper 066: 32KB PRG Bank切换
            byte effectiveBank = (byte)(_prgBankSelect % _prgBank);
            mapAddress = (uint)(effectiveBank * 0x8000 + (address & 0x7FFF));
            return true;
        }
        return false;
    }

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
    {
        if (address >= 0x8000)
        {
            // Mapper 066寄存器格式：
            // D7 D6 D5 D4 D3 D2 D1 D0
            // x  x  x  x  x  x | |
            //                  | +-- PRG Bank Select (2 bits)
            //                  +----
            //            +--+
            //            |  |
            // D7 D6 D5 D4 D3 D2 D1 D0
            // x  x  |  |  x  x  x  x
            //       +--+
            //       CHR Bank Select (2 bits)
            
            // 设置PRG Bank选择 (低2位)
            _prgBankSelect = (byte)(data & 0x03);
            // 添加边界检查
            _prgBankSelect %= _prgBank;
            
            // 设置CHR Bank选择 (位4-5)
            _chrBankSelect = (byte)((data & 0x30) >> 4);
            // 添加边界检查
            if (_chrBank > 0)
            {
                _chrBankSelect %= _chrBank;
            }
        }
        return false;
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
        // 只有CHR-RAM模式下才允许写入
        if (address <= 0x1FFF && _chrBank == 0)
        {
            mapAddress = address;
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