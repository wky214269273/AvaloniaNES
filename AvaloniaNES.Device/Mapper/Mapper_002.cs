using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

public class Mapper_002 : IMapperService
{
    private byte _prgBank;
    private byte _chrBank;
    private byte _prgBankSelect;
    private byte _prgBankFix;

    public void MapperInit(byte prgBanks, byte chrBanks)
    {
        _prgBank = prgBanks;
        _chrBank = chrBanks;
        Reset();
    }

    public void Reset()
    {
        _prgBankSelect = 0;
        // 确保_prgBank至少为1，避免出现负数
        _prgBankFix = (byte)((_prgBank > 0) ? (_prgBank - 1) : 0);
    }

    public MirroringType GetMirrorType()
    {
        return MirroringType.Hardware;
    }

    public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
    {
        if (address >= 0x8000)
        {
            if (address < 0xC000)
            {
                // 低16KB：可切换的Bank
                // 添加边界检查，确保Bank索引不会超出范围
                byte effectiveBank = (byte)(_prgBankSelect % _prgBank);
                mapAddress = (uint)(effectiveBank * 0x4000 + (address & 0x3FFF));
            }
            else
            {
                // 高16KB：固定为最后一个Bank
                mapAddress = (uint)(_prgBankFix * 0x4000 + (address & 0x3FFF));
            }
            return true;
        }
        return false;
    }

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
    {
        if (address >= 0x8000)
        {
            // Mapper 002只修改Bank选择寄存器，不进行实际写入
            _prgBankSelect = (byte)(data & 0x0F);
            // 提前进行边界检查
            _prgBankSelect %= _prgBank;
        }
        return false; // 不处理写入
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