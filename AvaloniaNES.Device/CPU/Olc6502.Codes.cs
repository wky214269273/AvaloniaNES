namespace AvaloniaNES.Device.CPU;

public partial class Olc6502
{
    private byte ADC()
    {
        fetch();
        temp =(ushort)(A + fetched + (GetFlag(CARRY_FLAG) ? 1 : 0));
        SetFlag(CARRY_FLAG, temp > 0xFF);
        SetFlag(ZERO_FLAG, (byte)(temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x80) > 0);
        SetFlag(OVERFLOW_FLAG, (~(A ^ fetched) & (A ^ temp) & 0x0080) > 0);
        A = (byte)(temp & 0x00FF);
        return 1;
    }

    private byte SBC()
    {
        fetch();
        var value = (ushort)(fetched ^ 0x00FF);
        temp = (ushort)(A + value + (GetFlag(CARRY_FLAG) ? 1 : 0));
        SetFlag(CARRY_FLAG, (temp & 0xFF00) > 0);
        SetFlag(ZERO_FLAG, (byte)(temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x80) > 0);
        SetFlag(OVERFLOW_FLAG, ((temp ^ A) & (temp ^ value) & 0x0080) > 0);
        A = (byte)(temp & 0x00FF);
        return 1;
    }

    private byte AND()
    {
        fetch();
        A &= fetched;
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x80) > 0);
        return 1;
    }

    private byte ASL()
    {
        fetch();
        temp = (ushort)(fetched << 1);
        SetFlag(CARRY_FLAG, (temp & 0xFF00) > 0);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x80) > 0);
        if (instructions[opcode].AddrMode == IMP)
        {
            A = (byte)(temp & 0x00FF);
        }
        else
        {
            Write(addr_abs, (byte)(temp & 0x00FF));
        }
        return 0;
    }

    private byte BCC()
    {
        if (!GetFlag(CARRY_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }
        return 0;
    }

    private byte BCS()
    {
        if (GetFlag(CARRY_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }
        return 0;
    }

    private byte BEQ()
    {
        if (GetFlag(ZERO_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }
        return 0;
    }

    private byte BIT()
    {
        fetch();
        temp = (ushort)(A & fetched);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (fetched & (1 << 7)) > 0);
        SetFlag(OVERFLOW_FLAG, (fetched & (1 << 6)) > 0);
        return 0;
    }

    private byte BMI()
    {
        if (GetFlag(NEGATIVE_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }
        return 0;
    }

    private byte BNE()
    {
        if (!GetFlag(ZERO_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }
        return 0;
    }

    private byte BPL()
    {
        if (!GetFlag(NEGATIVE_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }
        return 0;
    }

    private byte BRK()
    {
        PC++;
        
        SetFlag(INTERRUPT_DISABLE_FLAG, true);
        Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0x00FF));
        SP--;
        Write((ushort)(0x0100 + SP), (byte)(PC & 0x00FF));
        SP--;
        
        SetFlag(BREAK_COMMAND_FLAG, true);
        Write((ushort)(0x0100 + SP), Status);
        SP--;
        SetFlag(BREAK_COMMAND_FLAG, false);
        
        PC = (ushort)(Read(0xFFFE) | (Read(0xFFFF) << 8));
        return 0;
    }

    private byte BVC()
    {
        if (!GetFlag(OVERFLOW_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }

        return 0;
    }

    private byte BVS()
    {
        if (GetFlag(OVERFLOW_FLAG))
        {
            cycles++;
            addr_abs = (ushort)(PC + addr_rel);
            if ((addr_abs & 0xFF00) != (PC & 0xFF00))
            {
                cycles++;
            }
            PC = addr_abs;
        }

        return 0;
    }

    private byte CLC()
    {
        SetFlag(CARRY_FLAG, false);
        return 0;
    }
    
    private byte CLD()
    {
        SetFlag(DECIMAL_MODE_FLAG, false);
        return 0;
    }
    
    private byte CLI()
    {
        SetFlag(INTERRUPT_DISABLE_FLAG, false);
        return 0;
    }
    
    private byte CLV()
    {
        SetFlag(OVERFLOW_FLAG, false);
        return 0;
    }

    private byte CMP()
    {
        fetch();
        temp = (ushort)(A - fetched);
        SetFlag(CARRY_FLAG, A >= fetched);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        return 1;
    }
    
    private byte CPX()
    {
        fetch();
        temp = (ushort)(X - fetched);
        SetFlag(CARRY_FLAG, X >= fetched);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        return 0;
    }
    
    private byte CPY()
    {
        fetch();
        temp = (ushort)(Y - fetched);
        SetFlag(CARRY_FLAG, Y >= fetched);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        return 0;
    }

    private byte DEC()
    {
        fetch();
        temp = (ushort)(fetched - 1);
        Write(addr_abs, (byte)(temp & 0x00FF));
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        return 0;
    }
    
    private byte DEX()
    {
        X--;
        SetFlag(ZERO_FLAG, X == 0x00);
        SetFlag(NEGATIVE_FLAG, (X & 0x0080) > 0);
        return 0;
    }
    
    private byte DEY()
    {
        Y--;
        SetFlag(ZERO_FLAG, Y == 0x00);
        SetFlag(NEGATIVE_FLAG, (Y & 0x0080) > 0);
        return 0;
    }
    
    private byte EOR()
    {
        fetch();
        A = (byte)(A ^ fetched);
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x0080) > 0);
        return 1;
    }

    private byte INC()
    {
        fetch();
        temp = (ushort)(fetched + 1);
        Write(addr_abs, (byte)(temp & 0x00FF));
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x00);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        return 0;
    }
    
    private byte INX()
    {
        X++;
        SetFlag(ZERO_FLAG, X == 0x00);
        SetFlag(NEGATIVE_FLAG, (X & 0x0080) > 0);
        return 0;
    }
    
    private byte INY()
    {
        Y++;
        SetFlag(ZERO_FLAG, Y == 0x00);
        SetFlag(NEGATIVE_FLAG, (Y & 0x0080) > 0);
        return 0;
    }

    private byte JMP()
    {
        PC = addr_abs;
        return 0;
    }

    private byte JSR()
    {
        PC--;
        
        Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0x00FF));
        SP--;
        Write((ushort)(0x0100 + SP), (byte)(PC & 0x00FF));
        SP--;
        
        PC = addr_abs;
        return 0;
    }

    private byte LDA()
    {
        fetch();
        A = fetched;
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x80) > 0);
        return 1;
    }

    private byte LDX()
    {
        fetch();
        X = fetched;
        SetFlag(ZERO_FLAG, X == 0x00);
        SetFlag(NEGATIVE_FLAG, (X & 0x0080) > 0);
        return 1;
    }

    private byte LDY()
    {
        fetch();
        Y = fetched;
        SetFlag(ZERO_FLAG, Y == 0x00);
        SetFlag(NEGATIVE_FLAG, (Y & 0x0080) > 0);
        return 1;
    }

    private byte LSR()
    {
        fetch();
        SetFlag(CARRY_FLAG, (fetched & 0x0001) == 1);
        temp = (ushort)(fetched >> 1);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x0000);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        if (instructions[opcode].AddrMode == IMP)
        {
            A = (byte)(temp & 0x00FF);
        }
        else
        {
            Write(addr_abs, (byte)(temp & 0x00FF));
        }

        return 0;
    }

    private byte NOP()
    {
        switch (opcode)
        {
            case 0x1C:
            case 0x3C:
            case 0x5C:
            case 0x7C:
            case 0xDC:
            case 0xFC:
                return 1;
            default:
                break;
        }
        return 0;
    }

    private byte ORA()
    {
        fetch();
        A = (byte)(A | fetched);
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x0080) > 0);
        return 1;
    }

    private byte PHA()
    {
        Write((ushort)(0x0100 + SP), A);
        SP--;
        return 0;
    }

    private byte PHP()
    {
        Write((ushort)(0x0100 + SP), (byte)(Status | BREAK_COMMAND_FLAG | UNUSED_FLAG));
        SetFlag(BREAK_COMMAND_FLAG, false);
        SetFlag(UNUSED_FLAG, false);
        SP--;
        return 0;
    }

    private byte PLA()
    {
        SP++;
        A = Read((ushort)(0x0100 + SP));
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x0080) > 0);
        return 0;
    }
    
    private byte PLP()
    {
        SP++;
        Status = Read((ushort)(0x0100 + SP));
        SetFlag(UNUSED_FLAG, true);
        return 0;
    }

    private byte ROL()
    {
        fetch();
        temp = (ushort)((fetched << 1) | (GetFlag(CARRY_FLAG) ? 1 : 0));
        SetFlag(CARRY_FLAG, (temp & 0xFF00) > 0);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x0000);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        if (instructions[opcode].AddrMode == IMP)
        {
            A = (byte)(temp & 0x00FF);
        }
        else
        {
            Write(addr_abs, (byte)(temp & 0x00FF));
        }
        return 0;
    }
    
    private byte ROR()
    {
        fetch();
        temp = (ushort)((GetFlag(CARRY_FLAG) ? 1 : 0) << 7 | (fetched >> 1));
        SetFlag(CARRY_FLAG, (fetched & 0x01) > 0);
        SetFlag(ZERO_FLAG, (temp & 0x00FF) == 0x0000);
        SetFlag(NEGATIVE_FLAG, (temp & 0x0080) > 0);
        if (instructions[opcode].AddrMode == IMP)
        {
            A = (byte)(temp & 0x00FF);
        }
        else
        {
            Write(addr_abs, (byte)(temp & 0x00FF));
        }
        return 0;
    }

    private byte RTI()
    {
        SP++;
        Status = Read((ushort)(0x0100 + SP));
        Status &= (~BREAK_COMMAND_FLAG) & 0xFF;
        Status &= (~UNUSED_FLAG) & 0xFF;
        
        SP++;
        PC = Read((ushort)(0x0100 + SP));
        SP++;
        PC |= (ushort)(Read((ushort)(0x0100 + SP)) << 8);
        return 0;
    }

    private byte RTS()
    {
        SP++;
        PC = Read((ushort)(0x0100 + SP));
        SP++;
        PC |= (ushort)(Read((ushort)(0x0100 + SP)) << 8);
        PC++;
        return 0;
    }

    private byte SEC()
    {
        SetFlag(CARRY_FLAG, true);
        return 0;
    }
    
    private byte SED()
    {
        SetFlag(DECIMAL_MODE_FLAG, true);
        return 0;
    }
    
    private byte SEI()
    {
        SetFlag(INTERRUPT_DISABLE_FLAG, true);
        return 0;
    }

    private byte STA()
    {
        Write(addr_abs, A);
        return 0;
    }
    
    private byte STX()
    {
        Write(addr_abs, X);
        return 0;
    }
    
    private byte STY()
    {
        Write(addr_abs, Y);
        return 0;
    }
    
    private byte TAX()
    {
        X = A;
        SetFlag(ZERO_FLAG, X == 0x00);
        SetFlag(NEGATIVE_FLAG, (X & 0x0080) > 0);
        return 0;
    }
    
    private byte TAY()
    {
        Y = A;
        SetFlag(ZERO_FLAG, Y == 0x00);
        SetFlag(NEGATIVE_FLAG, (Y & 0x0080) > 0);
        return 0;
    }
    
    private byte TSX()
    {
        X = SP;
        SetFlag(ZERO_FLAG, X == 0x00);
        SetFlag(NEGATIVE_FLAG, (X & 0x0080) > 0);
        return 0;
    }
    
    private byte TXA()
    {
        A = X;
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x0080) > 0);
        return 0;
    }
    
    private byte TXS()
    {
        SP = X;
        return 0;
    }
    
    private byte TYA()
    {
        A = Y;
        SetFlag(ZERO_FLAG, A == 0x00);
        SetFlag(NEGATIVE_FLAG, (A & 0x0080) > 0);
        return 0;
    }

    private byte XXX()
    {
        return 0;
    }
}