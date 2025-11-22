using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

/// <summary>
/// Mapper023 - CNROM变体，用于支持魂斗罗1等游戏
/// 特点：
/// - 支持更大的PRG ROM容量
/// - CHR Bank选择通过0x8000-0xFFFF地址写入
/// - PRG ROM固定映射，不进行Bank切换
/// - CHR ROM支持Bank切换
/// </summary>
public class Mapper_023 : IMapperService
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
        // Mapper023使用硬件镜像类型
        return MirroringType.Hardware;
    }

    public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
    {
        if (address >= 0x8000)
        {
            // Mapper023: 根据PRG Bank数量映射地址空间
            // 16KB模式：0x8000-0xBFFF映射第一个16KB，0xC000-0xFFFF映射最后一个16KB
            // 32KB模式：直接映射整个32KB空间
            if (_prgBank > 1)
            {
                // 超过1个Bank的情况（16KB模式）
                if (address < 0xC000)
                {
                    mapAddress = (uint)(address & 0x3FFF); // 映射到第一个16KB Bank
                }
                else
                {
                    mapAddress = (uint)(((_prgBank - 1) * 0x4000) + (address & 0x3FFF)); // 映射到最后一个16KB Bank
                }
            }
            else
            {
                mapAddress = (uint)(address & 0x7FFF); // 单个Bank的情况（32KB模式）
            }
            return true;
        }
        return false;
    }

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
    {
        if (address >= 0x8000)
        {
            // 修复：对于魂斗罗1，正确的Bank选择逻辑
            // 通常CNROM使用低2位选择Bank，这能更好地匹配大多数游戏ROM实现
            _chrBankSelect = (byte)(data & 0x03);
            
            // 严格的边界检查，确保Bank索引不会超出范围
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
            // 修复：优化CHR Bank映射逻辑
            // 对于CHR-ROM，使用选择的Bank
            // 对于CHR-RAM，忽略Bank选择
            if (_chrBank > 0)
            {
                // 使用位运算确保正确映射，防止地址计算错误
                mapAddress = (uint)((_chrBankSelect * 0x2000) | (address & 0x1FFF));
                
                // 确保映射地址不超出实际ROM大小
                uint maxAddress = (uint)(_chrBank * 0x2000);
                if (mapAddress >= maxAddress)
                {
                    mapAddress %= maxAddress;
                }
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
        // Mapper023不支持IRQ中断
        return false;
    }

    public void irqClear()
    {
        // Mapper023不支持IRQ中断
        return;
    }

    public void scanline()
    {
        // Mapper023不支持扫描线计数
        return;
    }
}