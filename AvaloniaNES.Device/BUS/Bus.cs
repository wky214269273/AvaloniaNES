using AvaloniaNES.Device.Cart;
using AvaloniaNES.Device.CPU;
using AvaloniaNES.Device.PPU;

namespace AvaloniaNES.Device.BUS;

public class Bus
{
    //CPU RAM Range
    //0x0000 - 0x07FF : 2kb Ram, 0000-00FF: Zero Page, 0100-01FF: Stack, 0200-07FF: ram
    //0x0800 - 0x1FFF : 2kb Mirror Ram x 3, total 8kb CPU ram

    //Register Map RAM Range
    //0x2000 - 0x2007 : PPU IO Registers
    //0x2008 - 0x4000 : Mirror, total 8kb PPU ram
    //0x4000 - 0x401F : Other Registers

    //Rom Range
    //0x4020 - 0x5FFF : Expansion ROM
    //0x6000 - 0x7FFF : WRAM
    //0x8000 - 0xFFFF : Cartridge ROM
    private byte[] cpuRam = new byte[2 * 1024];  //2kb ram

    private uint nSystemClockCounter = 0;

    //Info Update Delegate
    public Action? UpdateInfo;

    //Device List
    public Olc6502? CPU;

    public Olc2C02? PPU;
    public Cartridge? CART;

    //Controller
    public byte[] controller = new byte[2];

    private byte[] controller_state = new byte[2];

    //DMA
    private byte dma_addr = 0x00;

    private byte dma_page = 0x00;
    private byte dma_data = 0x00;
    private bool dma_istransfer = false;
    private bool dma_isdummy = true;

    public void ConnectCPU(Olc6502 cpu)
    {
        CPU = cpu;
    }

    public void ConnectPPU(Olc2C02 ppu)
    {
        PPU = ppu;
    }

    public void InitDevice()
    {
        CPU = new Olc6502(this);
        PPU = new Olc2C02(this);
    }

    public void InsertCartridge(Cartridge catridge)
    {
        CART = catridge;
        PPU?.ConnectCartridge(CART);
    }

    public void RemoveCartridge()
    {
        CART = null;
        Reset();
    }

    /* Read & Write Memory */

    public byte CPURead(ushort address, bool bReadOnly = false)
    {
        byte result = 0x00;
        if (CART != null && CART.CPURead(address, ref result))
        {
        }
        else if (address <= 0x1FFF) //8kb total cpu ram
        {
            // cpu ram
            result = cpuRam[address & 0x07FF]; //map to first 2kb ram
        }
        else if (address <= 0x3FFF)
        {
            result = PPU!.CPURead((ushort)(address & 0x0007), bReadOnly);
        }
        else if (address >= 0x4016 && address <= 0x4017)
        {
            // always loop
            result = (controller_state[address & 0x0001] & 0x80) > 0 ? (byte)1 : (byte)0;
            controller_state[address & 0x0001] <<= 1;
        }

        return result;
    }

    public void CPUWrite(ushort address, byte value)
    {
        if (CART != null && CART.CPUWrite(address, value))
        {
        }
        else if (address <= 0x1FFF) //8kb total cpu ram
        {
            // cpu ram
            cpuRam[address & 0x07FF] = value;  //map to first 2kb ram
        }
        else if (address <= 0x3FFF)
        {
            // ppu ram
            PPU!.CPUWrite((ushort)(address & 0x0007), value);
        }
        else if (address == 0x4014)
        {
            // dma
            dma_page = value;
            dma_addr = 0x00;
            dma_istransfer = true;
        }
        else if (address >= 0x4016 && address <= 0x4017)
        {
            // controller
            controller_state[address & 0x0001] = controller[address & 0x0001];
        }
    }

    public void Reset()
    {
        //Device Reset
        CART?.Reset();
        CPU?.Reset();
        PPU?.Reset();

        //Clear clocks
        nSystemClockCounter = 0;

        //Flash DataView
        UpdateInfo?.Invoke();
    }

    public void Clock()
    {
        if (CART == null) return;  //No cartridge

        //PPU - 始终运行一个时钟周期
        PPU!.Clock();

        //CPU - 每3个PPU时钟运行一个CPU时钟周期（NES的3:1比例）
        if (nSystemClockCounter % 3 == 0) 
        {
            if (dma_istransfer)
            {
                // DMA传输时CPU被锁定
                // 修正：DMA传输的时序处理，确保正确的读写周期
                if (dma_isdummy)
                {
                    // 修正：dummy周期在奇数系统时钟时结束
                    if (nSystemClockCounter % 2 == 1)
                    {
                        dma_isdummy = false;
                    }
                }
                else
                {
                    if (nSystemClockCounter % 2 == 0)
                    {
                        // 偶数时钟读取数据
                        dma_data = CPURead((ushort)((dma_page << 8) | dma_addr));
                    }
                    else
                    {
                        // 奇数时钟写入OAM
                        PPU!.WriteOAMByte(dma_addr, dma_data);
                        dma_addr++;

                        if (dma_addr == 0x00)
                        {
                            // 传输完成，重置标志
                            dma_istransfer = false;
                            dma_isdummy = true;
                        }
                    }
                }
            }
            else
            {
                CPU!.Clock();
            }
        }

        //Check NMI Flag
        if (PPU!.Nmi)
        {
            PPU!.Nmi = false;
            CPU!.Nmi();
        }

        if (CART!.GetMapper().irqState())
        {
            CART!.GetMapper().irqClear();
            CPU!.Irq();
        }

        //Flash DataView
        if (CPU != null && CPU.Complete())
        {
            UpdateInfo?.Invoke();
        }

        nSystemClockCounter++;
    }
}