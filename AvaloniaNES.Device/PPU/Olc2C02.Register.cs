using System.Runtime.InteropServices;

namespace AvaloniaNES.Device.PPU;

public partial class Olc2C02
{
    public class PPU_Status
    {
        public byte sprite_overflow { get; set; } = 0x00;
        public byte sprite_zero_hit { get; set; } = 0x00;
        public byte vertical_blank { get; set; } = 0x00;

        public byte reg
        {
            get => (byte)(((sprite_overflow & 0x01) << 5) | ((sprite_zero_hit & 0x01) << 6) | ((vertical_blank & 0x01) << 7));
            set
            {
                sprite_overflow = (byte)(value >> 5 & 0x01);
                sprite_zero_hit = (byte)(value >> 6 & 0x01);
                vertical_blank = (byte)(value >> 7 & 0x01);
            }
        }
    }

    public class PPU_Mask
    {
        public byte grayscale { get; set; } = 0x00;
        public byte render_background_left { get; set; } = 0x00;
        public byte render_sprites_left { get; set; } = 0x00;
        public byte render_background { get; set; } = 0x00;
        public byte render_sprites { get; set; } = 0x00;
        public byte enhance_red { get; set; } = 0x00;
        public byte enhance_green { get; set; } = 0x00;
        public byte enhance_blue { get; set; } = 0x00;

        public byte reg
        {
            get => (byte)(((grayscale & 0x01) << 0) | ((render_background_left & 0x01) << 1) |
                          ((render_sprites_left & 0x01) << 2) | ((render_background & 0x01) << 3) |
                          ((render_sprites & 0x01) << 4) | ((enhance_red & 0x01) << 5) | ((enhance_green & 0x01) << 6) |
                          ((enhance_blue & 0x01) << 7));
            set
            {
                grayscale = (byte)(value & 0x01);
                render_background_left = (byte)(value >> 1 & 0x01);
                render_sprites_left = (byte)(value >> 2 & 0x01);
                render_background = (byte)(value >> 3 & 0x01);
                render_sprites = (byte)(value >> 4 & 0x01);
                enhance_red = (byte)(value >> 5 & 0x01);
                enhance_green = (byte)(value >> 6 & 0x01);
                enhance_blue = (byte)(value >> 7 & 0x01);
            }
        }
    }

    public class PPU_Control
    {
        public byte nametable_x { get; set; } = 0x00;
        public byte nametable_y { get; set; } = 0x00;
        public byte increment_mode { get; set; } = 0x00;
        public byte pattern_sprite { get; set; } = 0x00;
        public byte pattern_background { get; set; } = 0x00;
        public byte sprite_size { get; set; } = 0x00;
        public byte slave_mode { get; set; } = 0x00;
        public byte enable_nmi { get; set; } = 0x00;

        public byte reg
        {
            get => (byte)(((nametable_x & 0x01) << 0) | ((nametable_y & 0x01) << 1) | ((increment_mode & 0x01) << 2) |
                          ((pattern_sprite & 0x01) << 3) | ((pattern_background & 0x01) << 4) | ((sprite_size & 0x01) << 5) |
                          ((slave_mode & 0x01) << 6) | ((enable_nmi & 0x01) << 7));
            set
            {
                nametable_x = (byte)(value & 0x01);
                nametable_y = (byte)(value >> 1 & 0x01);
                increment_mode = (byte)(value >> 2 & 0x01);
                pattern_sprite = (byte)(value >> 3 & 0x01);
                pattern_background = (byte)(value >> 4 & 0x01);
                sprite_size = (byte)(value >> 5 & 0x01);
                slave_mode = (byte)(value >> 6 & 0x01);
                enable_nmi = (byte)(value >> 7 & 0x01);
            }
        }
    }
    
    //define
    private PPU_Mask _mask = new PPU_Mask();
    private PPU_Control _control = new PPU_Control();
    private PPU_Status _status = new PPU_Status();

    private byte address_latch = 0x00;
    private byte ppu_data_buffer = 0x00;
    //private ushort ppu_address = 0x0000;

    public class loopy_register
    {
        public byte coarse_x { get; set; } = 0x00;
        public byte coarse_y { get; set; } = 0x00;
        public byte nametable_x { get; set; } = 0x00;
        public byte nametable_y { get; set; } = 0x00;
        public byte fine_y { get; set; } = 0x00;
        public byte temp { get; set; } = 0x00;
        
        public ushort reg
        {
            get => (ushort)(((coarse_x & 0x1F) << 0) | ((coarse_y & 0x1F) << 5) | ((nametable_x & 0x01) << 10) |
                            ((nametable_y & 0x01) << 11) | ((fine_y & 0x07) << 12) | ((temp & 0x01) << 15));
            set
            {
                coarse_x = (byte)(value & 0x1F);
                coarse_y = (byte)((value >> 5) & 0x1F);
                nametable_x = (byte)((value >> 10) & 0x01);
                nametable_y = (byte)((value >> 11) & 0x01);
                fine_y = (byte)((value >> 12) & 0x07);
                temp = (byte)((value >> 15) & 0x01);
            }
        }
    }

    private loopy_register vram_addr = new();
    private loopy_register tram_addr = new();
    private byte fine_x = 0x00;

    private byte bg_next_tile_id = 0x00;
    private byte bg_next_tile_attr = 0x00;
    private byte bg_next_tile_lsb = 0x00;
    private byte bg_next_tile_msb = 0x00;
    
    private ushort bg_shifter_pattern_lo = 0x0000;
    private ushort bg_shifter_pattern_hi = 0x0000;
    private ushort bg_shifter_attrib_lo = 0x0000;
    private ushort bg_shifter_attrib_hi = 0x0000;
}