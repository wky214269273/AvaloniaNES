using AvaloniaNES.Device.BUS;
using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.PPU;

public partial class Olc2C02
{
    private readonly Bus _bus;

    public Olc2C02(Bus bus)
    {
        _bus = bus;
        _bus.ConnectPPU(this);
    }

    // Memory
    // 0x0000 - 0x0FFF : Pattern Table 0
    // 0x1000 - 0x1FFF : Pattern Table 1
    // 0x2000 - 0x23FF : Nametable 0
    // 0x2400 - 0x27FF : Nametable 1
    // 0x2800 - 0x2BFF : Nametable 2
    // 0x2C00 - 0x2FFF : Nametable 3

    // 0x3F00 - 0x3F1F : PPU Palette, 3F01 - 3F0D: Background, 3F11 - 3F1D: Sprite
    // how to find color?
    // in platette, refenceid: 0 - 7, map 3F01 3F05 3F09 ... 3F1D
    // if id is 1,and pixel location is 3 ,address is 3F00 + 1*4 + 3 = 3F07

    // every nametable is 32x32,but use to show the screen is only 32x30(256x240)
    private byte[][] _tblName =
    [
        new byte[1024],
        new byte[1024]
    ];  //what looks like

    private byte[] _tblPalette = new byte[32];  //how to draw

    private byte[][] _tblPattern =
    {
        new byte[4096],
        new byte[4096]
    };  //where to look

    // Cartridge
    private Cartridge? _cart;

    public void ConnectCartridge(Cartridge cart)
    {
        _cart = cart;
    }

    // Flag
    public bool Nmi = false;

    // Control Function
    public void Reset()
    {
        fine_x = 0x00;
        address_latch = 0x00;
        ppu_data_buffer = 0x00;
        // 修正：预渲染行应为 -1，确保预渲染周期中的寄存器清理与状态置位能被执行
        _scanLine = -1;
        _cycle = 0;
        bg_next_tile_id = 0x00;
        bg_next_tile_attr = 0x00;
        bg_next_tile_lsb = 0x00;
        bg_next_tile_msb = 0x00;
        bg_shifter_attrib_lo = 0x0000;
        bg_shifter_attrib_hi = 0x0000;
        bg_shifter_pattern_lo = 0x0000;
        bg_shifter_pattern_hi = 0x0000;
        _status.reg = 0x00;
        _mask.reg = 0x00;
        _control.reg = 0x00;
        vram_addr.reg = 0x0000;
        tram_addr.reg = 0x0000;

        Screen.Reset();
    }

    public void Clock()
    {
        void IncrementScollX()
        {
            if (_mask.render_background > 0 || _mask.render_sprites > 0)
            {
                if (vram_addr.coarse_x == 31)
                {
                    vram_addr.coarse_x = 0;
                    vram_addr.nametable_x = (byte)(vram_addr.nametable_x ^ 0x01);
                }
                else
                {
                    vram_addr.coarse_x++;
                }
            }
        }
        void IncrementScollY()
        {
            if (_mask.render_background > 0 || _mask.render_sprites > 0)
            {
                if (vram_addr.fine_y < 7)
                {
                    vram_addr.fine_y++;
                }
                else
                {
                    vram_addr.fine_y = 0;
                    if (vram_addr.coarse_y == 29)
                    {
                        vram_addr.coarse_y = 0;
                        // 修复：使用 XOR 切换 nametable_y（之前错误使用 ~ 会产生 0xFF）
                        vram_addr.nametable_y = (byte)(vram_addr.nametable_y ^ 0x01);
                    }
                    else if (vram_addr.coarse_y == 31)
                    {
                        vram_addr.coarse_y = 0;
                    }
                    else
                    {
                        vram_addr.coarse_y++;
                    }
                }
            }
        }
        void TransferAddressX()
        {
            if (_mask.render_background > 0 || _mask.render_sprites > 0)
            {
                vram_addr.nametable_x = tram_addr.nametable_x;
                vram_addr.coarse_x = tram_addr.coarse_x;
            }
        }
        void TransferAddressY()
        {
            if (_mask.render_background > 0 || _mask.render_sprites > 0)
            {
                vram_addr.fine_y = tram_addr.fine_y;
                vram_addr.nametable_y = tram_addr.nametable_y;
                vram_addr.coarse_y = tram_addr.coarse_y;
            }
        }
        void LoadBackgroundShifter()
        {
            bg_shifter_pattern_lo = (ushort)((bg_shifter_pattern_lo & 0xFF00) | bg_next_tile_lsb);
            bg_shifter_pattern_hi = (ushort)((bg_shifter_pattern_hi & 0xFF00) | bg_next_tile_msb);
            bg_shifter_attrib_lo =
                (ushort)((bg_shifter_attrib_lo & 0xFF00) | ((bg_next_tile_attr & 0x01) > 0 ? 0xFF : 0x00));
            bg_shifter_attrib_hi =
                (ushort)((bg_shifter_attrib_hi & 0xFF00) | ((bg_next_tile_attr & 0x02) > 0 ? 0xFF : 0x00));
        }
        void UpdateShifters()
        {
            if (_mask.render_background > 0)
            {
                // Shifting background tile pattern row
                bg_shifter_pattern_lo <<= 1;
                bg_shifter_pattern_hi <<= 1;

                // Shifting palette attributes by 1
                bg_shifter_attrib_lo <<= 1;
                bg_shifter_attrib_hi <<= 1;
            }

            if (_mask.render_sprites > 0 && _cycle >= 1 && _cycle < 258)
            {
                for (var i = 0; i < sprite_count; i++)
                {
                    if (spriteScanLine[i].x > 0)
                    {
                        spriteScanLine[i].x--;
                    }
                    else
                    {
                        sprite_shifter_pattern_lo[i] <<= 1;
                        sprite_shifter_pattern_hi[i] <<= 1;
                    }
                }
            }
        }

        byte flipbyte(byte input)
        {
            var result = input;
            result = (byte)((result & 0xF0) >> 4 | (result & 0x0F) << 4);
            result = (byte)((result & 0xCC) >> 2 | (result & 0x33) << 2);
            result = (byte)((result & 0xAA) >> 1 | (result & 0x55) << 1);
            return result;
        }

        //https://www.nesdev.org/wiki/File:Ppu.svg
        if (_scanLine >= -1 && _scanLine < 240)
        {
            if (_scanLine == 0 && _cycle == 0)
            {
                // "Odd Frame" cycle skip
                _cycle = 1;
            }

            //Leave Vertical Blank
            if (_scanLine == -1 && _cycle == 1)
            {
                _status.vertical_blank = 0;
                _status.sprite_zero_hit = 0;
                _status.sprite_overflow = 0;
                for (var i = 0; i < 8; i++)
                {
                    sprite_shifter_pattern_lo[i] = 0;
                    sprite_shifter_pattern_hi[i] = 0;
                }
            }

            if ((_cycle >= 2 && _cycle < 258) || (_cycle >= 321 && _cycle < 338))
            {
                UpdateShifters();
                switch ((_cycle - 1) % 8)
                {
                    case 0:
                        LoadBackgroundShifter();
                        bg_next_tile_id = PPURead((ushort)(0x2000 | (vram_addr.reg & 0x0FFF)));
                        break;

                    case 2:
                        bg_next_tile_attr = PPURead((ushort)(0x23C0 | (vram_addr.nametable_y << 11)
                                                                    | (vram_addr.nametable_x << 10)
                                                                    | ((vram_addr.coarse_y >> 2) << 3)
                                                                    | (vram_addr.coarse_x >> 2)));
                        if ((vram_addr.coarse_y & 0x02) > 0) bg_next_tile_attr >>= 4;
                        if ((vram_addr.coarse_x & 0x02) > 0) bg_next_tile_attr >>= 2;
                        bg_next_tile_attr &= 0x03;
                        break;

                    case 4:
                        bg_next_tile_lsb = PPURead((ushort)((_control.pattern_background << 12) + (bg_next_tile_id <<
                                                            4) + vram_addr.fine_y + 0));
                        break;

                    case 6:
                        bg_next_tile_msb = PPURead((ushort)((_control.pattern_background << 12) + (bg_next_tile_id <<
                                                            4) + vram_addr.fine_y + 8));
                        break;

                    case 7:
                        IncrementScollX();
                        break;
                }
            }

            if (_cycle == 256)
            {
                IncrementScollY();
            }

            if (_cycle == 257)
            {
                LoadBackgroundShifter();
                TransferAddressX();
            }

            if (_cycle == 338 || _cycle == 340)
            {
                bg_next_tile_id = PPURead((ushort)(0x2000 | (vram_addr.reg & 0x0FFF)));
            }

            if (_scanLine == -1 && _cycle >= 280 && _cycle < 305)
            {
                TransferAddressY();
            }

            //in one scanline,when pos is out of visible range,clear spriteline
            if (_cycle == 257 && _scanLine >= 0)
            {
                // reset spriteScanLine
                foreach (var item in spriteScanLine)
                {
                    item.attributes = 0xFF;
                    item.id = 0xFF;
                    item.x = 0xFF;
                    item.y = 0xFF;
                }
                sprite_count = 0;
                isSpriteZeroHitPossible = false;
                byte nOAMEntry = 0;

                while (nOAMEntry < 64 && sprite_count < 9)  //need check overflow,so here is 9
                {
                    //find first 8 sprites in all 64 oam
                    var diff = _scanLine - oam[nOAMEntry].y;
                    if (diff >= 0 && diff < (_control.sprite_size > 0 ? 16 : 8))  //sprite size can control sprite height
                    {
                        //visible
                        if (sprite_count < 8)
                        {
                            if (nOAMEntry == 0)
                            {
                                isSpriteZeroHitPossible = true;
                            }

                            spriteScanLine[sprite_count].y = oam[nOAMEntry].y;
                            spriteScanLine[sprite_count].id = oam[nOAMEntry].id;
                            spriteScanLine[sprite_count].attributes = oam[nOAMEntry].attributes;
                            spriteScanLine[sprite_count].x = oam[nOAMEntry].x;
                            sprite_count++;
                        }
                    }

                    nOAMEntry++;
                }
                _status.sprite_overflow = sprite_count > 8 ? (byte)1 : (byte)0;
            }

            if (_cycle == 340)
            {
                // render all visible sprite
                for (var i = 0; i < sprite_count; i++)
                {
                    byte pattern_bits_lo = 0x00;
                    byte pattern_bits_hi = 0x00;
                    ushort pattern_addr_lo = 0x0000;
                    ushort pattern_addr_hi = 0x0000;
                    if (_control.sprite_size == 0)
                    {
                        // 8x8
                        if ((spriteScanLine[i].attributes & 0x80) == 0)
                        {
                            pattern_addr_lo = (ushort)((_control.pattern_sprite << 12)
                                              | (spriteScanLine[i].id << 4)
                                              | (_scanLine - spriteScanLine[i].y));
                        }
                        else
                        {
                            // vertical reverse
                            pattern_addr_lo = (ushort)((_control.pattern_sprite << 12)
                                                       | (spriteScanLine[i].id << 4)
                                                       | (7 - (_scanLine - spriteScanLine[i].y)));
                        }
                    }
                    else
                    {
                        // 8x16
                        if ((spriteScanLine[i].attributes & 0x80) == 0)
                        {
                            if (_scanLine - spriteScanLine[i].y < 8)
                            {
                                pattern_addr_lo = (ushort)(((spriteScanLine[i].id & 0x01) << 12)
                                                  | ((spriteScanLine[i].id & 0xFE) << 4)
                                                  | ((_scanLine - spriteScanLine[i].y) & 0x07));
                            }
                            else
                            {
                                pattern_addr_lo = (ushort)(((spriteScanLine[i].id & 0x01) << 12)
                                                           | (((spriteScanLine[i].id & 0xFE) + 1) << 4)
                                                           | ((_scanLine - spriteScanLine[i].y) & 0x07));
                            }
                        }
                        else
                        {
                            // vertical reverse
                            if (_scanLine - spriteScanLine[i].y < 8)
                            {
                                pattern_addr_lo = (ushort)(((spriteScanLine[i].id & 0x01) << 12)
                                                           | (((spriteScanLine[i].id & 0xFE) + 1) << 4)
                                                           | ((7 - (_scanLine - spriteScanLine[i].y)) & 0x07));
                            }
                            else
                            {
                                pattern_addr_lo = (ushort)(((spriteScanLine[i].id & 0x01) << 12)
                                                           | ((spriteScanLine[i].id & 0xFE) << 4)
                                                           | ((7 - (_scanLine - spriteScanLine[i].y)) & 0x07));
                            }
                        }
                    }

                    pattern_addr_hi = (ushort)(pattern_addr_lo + 8);
                    pattern_bits_lo = PPURead(pattern_addr_lo);
                    pattern_bits_hi = PPURead(pattern_addr_hi);

                    if ((spriteScanLine[i].attributes & 0x40) > 0)  // horizontal flip
                    {
                        pattern_bits_lo = flipbyte(pattern_bits_lo);
                        pattern_bits_hi = flipbyte(pattern_bits_hi);
                    }

                    sprite_shifter_pattern_lo[i] = pattern_bits_lo;
                    sprite_shifter_pattern_hi[i] = pattern_bits_hi;
                }
            }
        }

        if (_scanLine == 240)
        {
        }

        //Enter Vertical Blank
        if (_scanLine >= 241 && _scanLine < 261)
        {
            if (_scanLine == 241 && _cycle == 1)
            {
                _status.vertical_blank = 1;
                if (_control.enable_nmi > 0)
                {
                    Nmi = true;
                }
            }
        }

        //test
        // var random = new Random();
        // var colorIndex = (byte)(random.Next() % 2 == 0? 0x3F:0x30);
        // Screen.SetPixel(_cycle - 1, _scanLine, palScreen[colorIndex]);

        byte bg_pixel = 0x00;
        byte bg_palette = 0x00;
        if (_mask.render_background > 0)
        {
            var bit_mux = (ushort)(0x8000 >> fine_x);

            var p0_pixel = (byte)((bg_shifter_pattern_lo & bit_mux) > 0 ? 1 : 0);
            var p1_pixel = (byte)((bg_shifter_pattern_hi & bit_mux) > 0 ? 1 : 0);
            bg_pixel = (byte)((p1_pixel << 1) | p0_pixel);

            var bg_pal0 = (byte)((bg_shifter_attrib_lo & bit_mux) > 0 ? 1 : 0);
            var bg_pal1 = (byte)((bg_shifter_attrib_hi & bit_mux) > 0 ? 1 : 0);
            bg_palette = (byte)((bg_pal1 << 1) | bg_pal0);
        }

        byte fg_pixel = 0x00;
        byte fg_palette = 0x00;
        byte fg_priority = 0x00;
        if (_mask.render_sprites > 0)
        {
            isSpriteZeroBeingRendered = false;

            for (var i = 0; i < 8; i++)
            {
                if (spriteScanLine[i].x == 0)
                {
                    var fg_pixel_hi = (byte)((sprite_shifter_pattern_hi[i] & 0x80) > 0 ? 1 : 0);
                    var fg_pixel_lo = (byte)((sprite_shifter_pattern_lo[i] & 0x80) > 0 ? 1 : 0);
                    fg_pixel = (byte)((fg_pixel_hi << 1) | fg_pixel_lo);
                    fg_palette = (byte)((spriteScanLine[i].attributes & 0x03) + 0x04);
                    fg_priority = (spriteScanLine[i].attributes & 0x20) == 0 ? (byte)0x01 : (byte)0x00;
                    if (fg_pixel > 0)
                    {
                        if (i == 0)
                        {
                            isSpriteZeroBeingRendered = true;
                        }
                        break;
                    }
                }
            }
        }

        //final
        byte pixel = 0x00;
        byte palette = 0x00;
        if (bg_pixel == 0 && fg_pixel == 0)
        {
            pixel = 0x00;
            palette = 0x00;
        }
        else if (bg_pixel == 0 && fg_pixel > 0)
        {
            pixel = fg_pixel;
            palette = fg_palette;
        }
        else if (bg_pixel > 0 && fg_pixel == 0)
        {
            pixel = bg_pixel;
            palette = bg_palette;
        }
        else
        {
            if (fg_priority > 0)
            {
                pixel = fg_pixel;
                palette = fg_palette;
            }
            else
            {
                pixel = bg_pixel;
                palette = bg_palette;
            }

            if (isSpriteZeroBeingRendered && isSpriteZeroHitPossible)
            {
                if ((_mask.render_background & _mask.render_sprites) > 0)
                {
                    if (_mask.render_sprites_left == 0 || _mask.render_background_left == 0)
                    {
                        if (_cycle >= 9 && _cycle < 258)
                        {
                            _status.sprite_zero_hit = 1;
                        }
                    }
                    else
                    {
                        if (_cycle >= 1 && _cycle < 258)
                        {
                            _status.sprite_zero_hit = 1;
                        }
                    }
                }
            }
        }

        Screen.SetPixel(_cycle - 1, _scanLine, GetColorFromPaletteRam(palette, pixel));

        _cycle++;
        if (_mask.render_background > 0 || _mask.render_sprites > 0)
        {
            if (_cycle == 260 && _scanLine < 240)
            {
                _cart!.GetMapper().scanline();
            }
        }

        if (_cycle >= 341)
        {
            _cycle = 0;
            _scanLine++;
            if (_scanLine >= 261)
            {
                _scanLine = -1;
                RenderFrame();
                FrameCompleted = true;
            }
        }
    }
}