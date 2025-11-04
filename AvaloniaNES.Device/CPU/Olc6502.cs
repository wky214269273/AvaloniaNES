using AvaloniaNES.Device.BUS;

namespace AvaloniaNES.Device.CPU;

public partial class Olc6502
{
    private readonly Bus _bus;
    public Olc6502(Bus bus)
    { 
        _bus = bus;
        _bus.ConnectCPU(this);
        InitializeInstructions();
    }
    
    // Register
    public byte A { get; set; }      
    public byte X { get; set; }      
    public byte Y { get; set; }     
    public byte SP { get; set; }     
    public ushort PC { get; set; }   
    public byte Status { get; set; } 
    
    // Status Flag
    public const byte CARRY_FLAG = 0x01;
    public const byte ZERO_FLAG = 0x02;
    public const byte INTERRUPT_DISABLE_FLAG = 0x04;
    public const byte DECIMAL_MODE_FLAG = 0x08;
    public const byte BREAK_COMMAND_FLAG = 0x10;
    public const byte UNUSED_FLAG = 0x20;
    public const byte OVERFLOW_FLAG = 0x40;
    public const byte NEGATIVE_FLAG = 0x80;

    // Assisstive variables to facilitate emulation
    private byte fetched = 0x00; // Represents the working input value to the ALU
    private ushort temp = 0x0000; // A convenience variable used everywhere
    private ushort addr_abs = 0x0000; // All used memory addresses end up in here
    private ushort addr_rel = 0x00; // Represents absolute address following a branch
    private byte opcode = 0x00; // Is the instruction byte
    private byte cycles = 0; // Counts how many cycles the instruction has remaining
    private uint clock_count = 0; // A global accumulation of the number of clocks

    // Flag Control Function
    private void SetFlag(byte flag, bool condition)
    {
        if (condition)
            Status |= flag;
        else
            Status &= (byte)~flag;
    }
    private bool GetFlag(byte flag)
    {
        return (Status & flag) != 0;
    }
    
    // Control Function
    public void Reset()
    {
        // reset pc
        addr_abs = 0xFFFC;
        var lo = Read(addr_abs);
        var hi = Read((ushort)(addr_abs + 1));
        PC = (ushort)((hi << 8) | lo);
        
        // reset register
        A = 0x00;
        X = 0x00;
        Y = 0x00;
        SP = 0xFD;
        Status = 0x00 | UNUSED_FLAG;
        
        // clear internal variables
        addr_rel = 0x0000;
        addr_abs = 0x0000;
        fetched = 0x00;
        
        // reset takes time
        cycles = 8;
    }

    public void Irq()
    {
        if (!GetFlag(INTERRUPT_DISABLE_FLAG))
        {
            // push program counter to stack
            Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0xFF));
            SP--;
            Write((ushort)(0x0100 + SP), (byte)(PC & 0xFF));
            SP--;
            
            // push status register to stack
            SetFlag(BREAK_COMMAND_FLAG, false);
            SetFlag(UNUSED_FLAG, true);
            SetFlag(INTERRUPT_DISABLE_FLAG, true);
            Write((ushort)(0x0100 + SP), Status);
            SP--;
            
            // read new program counter
            addr_abs = 0xFFFE;
            var lo = Read(addr_abs);
            var hi = Read((ushort)(addr_abs + 1));
            PC = (ushort)((hi << 8) | lo);
            
            // irq takes time
            cycles = 7;
        }
    }

    public void Nmi()
    {
        // push program counter to stack
        Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0xFF));
        SP--;
        Write((ushort)(0x0100 + SP), (byte)(PC & 0xFF));
        SP--;
            
        // push status register to stack
        SetFlag(BREAK_COMMAND_FLAG, false);
        SetFlag(UNUSED_FLAG, true);
        SetFlag(INTERRUPT_DISABLE_FLAG, true);
        Write((ushort)(0x0100 + SP), Status);
        SP--;
            
        // read new program counter
        addr_abs = 0xFFFA;
        var lo = Read(addr_abs);
        var hi = Read((ushort)(addr_abs + 1));
        PC = (ushort)((hi << 8) | lo);
            
        // irq takes time
        cycles = 8;
    }

    public void Clock()
    {
        if (cycles == 0)
        {
            opcode = Read(PC);
            SetFlag(UNUSED_FLAG,true);
            PC++;
            
            cycles = instructions[opcode].Cycles;
            var additional_cycle1 = instructions[opcode].AddrMode();
            var additional_cycle2 = instructions[opcode].Operation();

            cycles += (byte)(additional_cycle1 & additional_cycle2);
            
            SetFlag(UNUSED_FLAG,true);
        }

        clock_count++;  //all clocks
        cycles--;  // this function cost 1 clock
    }

    public bool Complete()
    {
        return cycles == 0;
    }
}