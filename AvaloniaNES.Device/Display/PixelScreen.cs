using System.Drawing;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaNES.Device.Display;

public class PixelScreen
{
    private readonly WriteableBitmap _screenBuffer;
    private readonly int _width;
    private readonly int _height;
    private readonly byte[] _renderBuffer; // final render buffer

    public PixelScreen(int width, int height)
    {
        _height = height;
        _width = width;
        _screenBuffer = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888);
        _renderBuffer = new byte[width * height * 4];
    }

    public void SetPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;
    
        // BGRA
        var index = (y * _width + x) * 4;
        _renderBuffer[index] = color.B;
        _renderBuffer[index + 1] = color.G;
        _renderBuffer[index + 2] = color.R;
        _renderBuffer[index + 3] = 0xFF;
    }
    
    public WriteableBitmap GetScreen()
    {
        return _screenBuffer;
    }
    
    public void UpdateScreenBuffer()
    {
        using (var locked = _screenBuffer.Lock())
        {
            System.Runtime.InteropServices.Marshal.Copy(
                _renderBuffer, 0, locked.Address, _renderBuffer.Length);
        }
    }

    public void Reset()
    {
        for (var i = 0; i < _renderBuffer.Length; i++)
        {
            _renderBuffer[i] = 0x00;
        }
        UpdateScreenBuffer();
    }
}