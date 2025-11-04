namespace AvaloniaNES.Device.CPU;

public partial class Olc6502
{
    public Dictionary<ushort, string> disassemble(ushort start, ushort stop)
    {
        uint addr = start;
        byte value, lo, hi;
        Dictionary<ushort, string> mapLines = [];
        ushort line_addr = 0;
        while (addr <= (uint)stop)
        {
            line_addr = (ushort)addr;
            var sInst = $"${Hex(addr, 4)}: ";
            var opcode = _bus.CPURead((ushort)addr);
            addr++;

            sInst += $"{instructions[opcode].Name} ";

            if (instructions[opcode].AddrMode == IMP)
            {
                sInst += " {IMP}";
            }
            else if (instructions[opcode].AddrMode == IMM)
            {
                value = _bus.CPURead((ushort)addr);
                addr++;
                sInst += $"#${Hex(value, 2)}" + " {IMM}";
            }
            else if (instructions[opcode].AddrMode == ZP0)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = 0x00;
                sInst += $"${Hex(lo, 2)}" + " {ZP0}";
            }
            else if (instructions[opcode].AddrMode == ZPX)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = 0x00;
                sInst += $"${Hex(lo, 2)}" + ", X {ZPX}";
            }
            else if (instructions[opcode].AddrMode == ZPY)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = 0x00;
                sInst += $"${Hex(lo, 2)}" + ", Y {ZPY}";
            }
            else if (instructions[opcode].AddrMode == IZX)
            { 
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = 0x00;
                sInst += $"(${Hex(lo, 2)})" + ", X) {IZX}";
            }
            else if (instructions[opcode].AddrMode == IZY)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = 0x00;
                sInst += $"(${Hex(lo, 2)}" + "), Y {IZY}";
            }
            else if (instructions[opcode].AddrMode == ABS)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = _bus.CPURead((ushort)addr);
                addr++;
                sInst += $"${Hex((ushort)(hi << 8 | lo), 4)}" + " {ABS}";
            }
            else if (instructions[opcode].AddrMode == ABX)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = _bus.CPURead((ushort)addr);
                addr++;
                sInst += $"${Hex((ushort)(hi << 8 | lo), 4)}" + ", X {ABX}";
            }
            else if (instructions[opcode].AddrMode == ABY)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = _bus.CPURead((ushort)addr);
                addr++;
                sInst += $"${Hex((ushort)(hi << 8 | lo), 4)}" + ", Y {ABY}";
            }
            else if (instructions[opcode].AddrMode == IND)
            {
                lo = _bus.CPURead((ushort)addr);
                addr++;
                hi = _bus.CPURead((ushort)addr);
                addr++;
                sInst += $"(${Hex((ushort)(hi << 8 | lo), 4)})" + " {IND}";
            }
            else if (instructions[opcode].AddrMode == REL)
            {
                value = _bus.CPURead((ushort)addr);
                addr++;
                sInst += $"${Hex(value, 2)} [${Hex((uint)(addr + (sbyte)value),4)}]" + " {REL}";
            }

            mapLines[line_addr] = sInst;
        }

        return mapLines;
    }
    
    private string Hex(uint n, byte d)
    {
        return n.ToString("X" + d).PadLeft(d, '0');
    }
}