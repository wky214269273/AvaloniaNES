using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.PPU;

public partial class Olc2C02
{
    //PPU Connect CPUBus And internal Bus
    public byte CPURead(ushort address, bool rdonly)
    {
        byte result = 0x00;
        if (rdonly)
        {
            switch (address)
            {
                case 0x0000:  //control
                    result = _control.reg;
                    break;

                case 0x0001:  //mask
                    result = _mask.reg;
                    break;

                case 0x0002:  //status
                    result = _status.reg;
                    break;

                case 0x0003:  //oam_addr
                    break;

                case 0x0004:  //oam_data
                    break;

                case 0x0005:  // scroll
                    break;

                case 0x0006:  //ppu address
                    break;

                case 0x0007:  //ppu data
                    break;
            }
        }
        else
        {
            switch (address)
            {
                case 0x0000:  //control
                    break;

                case 0x0001:  //mask
                    break;

                case 0x0002:  //status
                    result = (byte)((_status.reg & 0xE0) | (ppu_data_buffer & 0x1F));
                    _status.vertical_blank = 0;
                    address_latch = 0;
                    break;

                case 0x0003:  //oam_addr
                    break;

                case 0x0004:  //oam_data
                    result = oam_memory[oam_addr];
                    break;

                case 0x0005:  // scroll
                    break;

                case 0x0006:  //ppu address
                    break;

                case 0x0007:  //ppu data
                    // 修正：符合硬件的读缓冲行为
                    // 对非 palette 区域：返回 buffered value，然后把 buffer 更新为当前地址读取值
                    // 对 palette 区域：返回直接读取的 palette 值，同时 buffer 应被更新为对应的 nametable（非-palette）读取值
                    if (vram_addr.reg >= 0x3F00)
                    {
                        // read palette directly
                        result = PPURead(vram_addr.reg);
                        // update buffer with mirrored nametable read (address & 0x3EFF)
                        ppu_data_buffer = PPURead((ushort)(vram_addr.reg & 0x3EFF));
                    }
                    else
                    {
                        // normal buffered read
                        result = ppu_data_buffer;
                        ppu_data_buffer = PPURead(vram_addr.reg);
                    }

                    vram_addr.reg = (ushort)(vram_addr.reg + (_control.increment_mode > 0 ? 32 : 1));
                    break;
            }
        }
        return result;
    }

    public void CPUWrite(ushort address, byte value)
    {
        switch (address)
        {
            case 0x0000:  //control
                _control.reg = value;
                tram_addr.nametable_x = _control.nametable_x;
                tram_addr.nametable_y = _control.nametable_y;
                break;

            case 0x0001:  //mask
                _mask.reg = value;
                break;

            case 0x0002:  //status
                break;

            case 0x0003:  //oam_addr
                oam_addr = value;
                break;

            case 0x0004:  //oam_data
                oam_memory[oam_addr] = value;
                break;

            case 0x0005:  // scroll
                if (address_latch == 0)
                {
                    fine_x = (byte)(value & 0x07);
                    tram_addr.coarse_x = (byte)(value >> 3);
                    address_latch = 1;
                }
                else
                {
                    tram_addr.fine_y = (byte)(value & 0x07);
                    tram_addr.coarse_y = (byte)(value >> 3);
                    address_latch = 0;
                }
                break;

            case 0x0006:  //ppu address
                if (address_latch == 0)
                {
                    tram_addr.reg = (ushort)((tram_addr.reg & 0x00FF) | ((value & 0x3F) << 8));
                    address_latch = 1;
                }
                else
                {
                    tram_addr.reg = (ushort)((tram_addr.reg & 0xFF00) | value);
                    vram_addr = new loopy_register
                    {
                        reg = tram_addr.reg
                    };
                    address_latch = 0;
                }
                break;

            case 0x0007:  //ppu data
                PPUWrite(vram_addr.reg, value);
                vram_addr.reg = (ushort)(vram_addr.reg + (_control.increment_mode > 0 ? 32 : 1));  //PPU will auto increment after write
                //vram_addr.reg++;
                break;
        }
    }

    public byte PPURead(ushort address)
    {
        byte result = 0x00;
        address &= 0x3FFF;
        if (_cart!.PPURead(address, ref result))
        {
        }
        else if (address <= 0x1FFF)
        {
            // pattern memory
            result = _tblPattern[(address & 0x1000) >> 12][address & 0xFFF];
        }
        else if (address <= 0x3EFF)
        {
            // nametable memory
            address &= 0x0FFF;
            if (_cart.GetMirror() == MirroringType.Vertical)
            {
                // mirror is left/right
                if (address <= 0x03FF)
                {
                    result = _tblName[0][address & 0x03FF];
                }
                else if (address <= 0x07FF)
                {
                    result = _tblName[1][address & 0x03FF];
                }
                else if (address <= 0x0BFF)
                {
                    result = _tblName[0][address & 0x03FF];
                }
                else
                {
                    result = _tblName[1][address & 0x03FF];
                }
            }
            else if (_cart.GetMirror() == MirroringType.Horizontal)
            {
                // mirror is top/bottom
                if (address <= 0x03FF)
                {
                    result = _tblName[0][address & 0x03FF];
                }
                else if (address <= 0x07FF)
                {
                    result = _tblName[0][address & 0x03FF];
                }
                else if (address <= 0x0BFF)
                {
                    result = _tblName[1][address & 0x03FF];
                }
                else
                {
                    result = _tblName[1][address & 0x03FF];
                }
            }
        }
        else if (address <= 0x3FFF)
        {
            //palette memory
            address &= 0x001F;
            //map first byte
            if (address == 0x0010) address = 0x0000;
            if (address == 0x0014) address = 0x0004;
            if (address == 0x0018) address = 0x0008;
            if (address == 0x001C) address = 0x000C;
            result = (byte)(_tblPalette[address] & (_mask.grayscale > 0 ? 0x30 : 0x3F));
        }

        return result;
    }

    public void PPUWrite(ushort address, byte value)
    {
        address &= 0x3FFF;

        if (_cart!.PPUWrite(address, value))
        {
        }
        else if (address <= 0x1FFF)
        {
            // pattern memory
            _tblPattern[(address & 0x1000) >> 12][address & 0xFFF] = value;
        }
        else if (address <= 0x3EFF)
        {
            // nametable memory
            address &= 0x0FFF;
            if (_cart.GetMirror() == MirroringType.Vertical)
            {
                // mirror is left/right
                if (address <= 0x03FF)
                {
                    _tblName[0][address & 0x03FF] = value;
                }
                else if (address <= 0x07FF)
                {
                    _tblName[1][address & 0x03FF] = value;
                }
                else if (address <= 0x0BFF)
                {
                    _tblName[0][address & 0x03FF] = value;
                }
                else
                {
                    _tblName[1][address & 0x03FF] = value;
                }
            }
            else if (_cart.GetMirror() == MirroringType.Horizontal)
            {
                // mirror is top/bottom
                if (address <= 0x03FF)
                {
                    _tblName[0][address & 0x03FF] = value;
                }
                else if (address <= 0x07FF)
                {
                    _tblName[0][address & 0x03FF] = value;
                }
                else if (address <= 0x0BFF)
                {
                    _tblName[1][address & 0x03FF] = value;
                }
                else
                {
                    _tblName[1][address & 0x03FF] = value;
                }
            }
        }
        else if (address <= 0x3FFF)
        {
            //palette memory
            address &= 0x001F;
            //map
            if (address == 0x0010) address = 0x0000;
            if (address == 0x0014) address = 0x0004;
            if (address == 0x0018) address = 0x0008;
            if (address == 0x001C) address = 0x000C;
            _tblPalette[address] = value;
        }
    }
}