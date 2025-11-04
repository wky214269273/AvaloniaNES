using System.Drawing;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaNES.Device.Display;

namespace AvaloniaNES.Device.PPU;

public partial class Olc2C02
{
    // private
    private static readonly Color[] palScreen = 
    [
        Color.FromArgb(84, 84, 84),
        Color.FromArgb(0, 30, 116),
        Color.FromArgb(8, 16, 144),
        Color.FromArgb(48, 0, 136),
        Color.FromArgb(68, 0, 100),
        Color.FromArgb(92, 0, 48),
        Color.FromArgb(84, 4, 0),
        Color.FromArgb(60, 24, 0),
        Color.FromArgb(32, 42, 0),
        Color.FromArgb(8, 58, 0),
        Color.FromArgb(0, 64, 0),
        Color.FromArgb(0, 60, 0),
        Color.FromArgb(0, 50, 60),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        
        Color.FromArgb(152, 150, 152),
        Color.FromArgb(8, 76, 196),
        Color.FromArgb(48, 50, 236),
        Color.FromArgb(92, 30, 228),
        Color.FromArgb(136, 20, 176),
        Color.FromArgb(160, 20, 100),
        Color.FromArgb(152, 34, 32),
        Color.FromArgb(120, 60, 0),
        Color.FromArgb(84, 90, 32),
        Color.FromArgb(40, 114, 56),
        Color.FromArgb(8, 124, 76),
        Color.FromArgb(0, 118, 92),
        Color.FromArgb(0, 102, 120),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        
        Color.FromArgb(236, 238, 236),
        Color.FromArgb(76, 154, 236),
        Color.FromArgb(120, 124, 236),
        Color.FromArgb(176, 98, 236),
        Color.FromArgb(228, 84, 236),
        Color.FromArgb(236, 88, 180),
        Color.FromArgb(236, 106, 100),
        Color.FromArgb(212, 136, 32),
        Color.FromArgb(160, 170, 0),
        Color.FromArgb(116, 196, 0),
        Color.FromArgb(76, 208, 32),
        Color.FromArgb(56, 204, 108),
        Color.FromArgb(56, 180, 204),
        Color.FromArgb(60, 60, 60),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        
        Color.FromArgb(236, 238, 236),
        Color.FromArgb(168, 204, 236),
        Color.FromArgb(188, 188, 236),
        Color.FromArgb(212, 178, 236),
        Color.FromArgb(236, 174, 236),
        Color.FromArgb(236, 174, 212),
        Color.FromArgb(236, 180, 176),
        Color.FromArgb(228, 196, 144),
        Color.FromArgb(204, 210, 120),
        Color.FromArgb(180, 222, 120),
        Color.FromArgb(168, 226, 144),
        Color.FromArgb(152, 226, 180),
        Color.FromArgb(160, 214, 228),
        Color.FromArgb(160, 162, 160),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
    ];

    private short _scanLine;  //row
    private short _cycle;  //col
    
    // Display
    public WriteableBitmap GetScreen()
    {
        return Screen.GetScreen();
    }
    public WriteableBitmap GetPatternTable(byte index, byte palette)
    {
        // 16 x 16 = 256 tiles
        for (var nTileY = 0; nTileY < 16; nTileY++)
        {
            for (var nTileX = 0; nTileX < 16; nTileX++)
            {
                var offset = nTileY * 256 + nTileX * 16;
                
                // 8 x 8 = 64 pixels
                // pixel 0 2 3 ... 7
                //       1 ... ... 7
                //       2 ... ... 7
                //       . ... ... 7
                //       . ... ... 7
                //       7 ... ... 7
                for (var row = 0; row < 8; row++)
                {
                    
                    // 2-Bit Pixels       LSB Bit Plane     MSB Bit Plane
                    // 0 0 0 0 0 0 0 0	  0 0 0 0 0 0 0 0   0 0 0 0 0 0 0 0
                    // 0 1 1 0 0 1 1 0	  0 1 1 0 0 1 1 0   0 0 0 0 0 0 0 0
                    // 0 1 2 0 0 2 1 0	  0 1 1 0 0 1 1 0   0 0 1 0 0 1 0 0
                    // 0 0 0 0 0 0 0 0 =  0 0 0 0 0 0 0 0 + 0 0 0 0 0 0 0 0
                    // 0 1 1 0 0 1 1 0	  0 1 1 0 0 1 1 0   0 0 0 0 0 0 0 0
                    // 0 0 1 1 1 1 0 0	  0 0 1 1 1 1 0 0   0 0 0 0 0 0 0 0
                    // 0 0 0 2 2 0 0 0	  0 0 0 1 1 0 0 0   0 0 0 1 1 0 0 0
                    // 0 0 0 0 0 0 0 0	  0 0 0 0 0 0 0 0   0 0 0 0 0 0 0 0
                    var tile_lsb = PPURead((ushort)(index * 0x1000 + offset + row));
                    var tile_msb = PPURead((ushort)(index * 0x1000 + offset + row + 8));
                    for (var col = 0; col < 8; col++)
                    {
                        // every pixel cost 2 bits, 
                        var pixel = (byte)((tile_lsb & 0x01) + (tile_msb & 0x01));
                        // because byte resolve start with last bit,so pixel is from right to left
                        ScreenPatternTable[index].SetPixel(
                            nTileX * 8 + (7 - col), 
                            nTileY * 8 + row, 
                            GetColorFromPaletteRam(palette, pixel)
                            );
                        //resolve next bit
                        tile_lsb >>= 1;
                        tile_msb >>= 1;
                    }
                }
            }
        }
        ScreenPatternTable[index].UpdateScreenBuffer();
        return ScreenPatternTable[index].GetScreen();
    }
    
    public WriteableBitmap GetNameTable(int index)
    {
        return ScreenNameTable[index].GetScreen();
    }
    public bool FrameCompleted { get; set; } = false;
    
    // private pixel buffer
    // 1 Pattern Table is 4kb, 256 tiles, so every tile is 16 byte, 1 tile is 8x8 pixel, so 1 pixel cost 2b
    private PixelScreen Screen = new PixelScreen(256,240);
    private PixelScreen[] ScreenNameTable =
    [
        new PixelScreen(256,240),
        new PixelScreen(256,240)
    ];
    private PixelScreen[] ScreenPatternTable =
    [
        new PixelScreen(128,128),
        new PixelScreen(128,128)
    ];
    
    // private
    private void RenderFrame()
    {
        Screen.UpdateScreenBuffer();
    }

    private Color GetColorFromPaletteRam(byte palette, byte pixel)
    {
        var refId = PPURead((ushort)(0x3F00 + (palette << 2) + pixel));
        return palScreen[refId & 0x3F];
    }
}