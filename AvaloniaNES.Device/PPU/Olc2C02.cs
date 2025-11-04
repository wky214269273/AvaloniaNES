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
        address_latch = 0x00;
        ppu_data_buffer = 0x00;
        _scanLine = 0;
        _cycle = 0;
        _status.reg = 0x00;
        _mask.reg = 0x00;
        _control.reg = 0x00;

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
                    vram_addr.nametable_x = (byte)~vram_addr.nametable_x;
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
                        vram_addr.nametable_y = (byte)~vram_addr.nametable_y;
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
                vram_addr.coarse_x    = tram_addr.coarse_x;
            }
        }
        void TransferAddressY()
        {
            if (_mask.render_background > 0 || _mask.render_sprites > 0)
            {
                vram_addr.fine_y      = tram_addr.fine_y;
                vram_addr.nametable_y = tram_addr.nametable_y;
                vram_addr.coarse_y    = tram_addr.coarse_y;
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
        Screen.SetPixel(_cycle - 1, _scanLine, GetColorFromPaletteRam(bg_palette, bg_pixel));
        
        _cycle++;
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