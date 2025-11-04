using AvaloniaNES.Device.Mapper;
using Microsoft.Win32.SafeHandles;

namespace AvaloniaNES.Device.Cart;

    
public enum MirroringType
{
    Horizontal,
    Vertical,
    OneScreen_Lo,
    OneScreen_Hi
}
    
public enum TvSystem
{
    NTSC,
    PAL
}

public partial class Cartridge
{
    public Cartridge(string filePath)
    {
        var rom = NesRomReader.ReadRom(filePath);
        _prgRam = rom.PrgRom;
        _chrRam = rom.ChrRom;
        _prgBanks = rom.Header.PrgRomBanks;
        _chrBanks = rom.Header.ChrRomBanks;
        Mirror = rom.MirrorType;

        switch (rom.MapperId)
        {
            case 0x000:
                _mapper = new Mapper_000();
                _mapper.MapperInit(_prgBanks,_chrBanks);
                break;
            default:
                throw new Exception($"Not supported mapper: {rom.MapperId:X3} now!");
        }
    }

    public void Reset()
    {
        _mapper.Reset();
    }
    
    public MirroringType Mirror { get; private set; }
    
    //Parameter
    private byte _prgBanks = 0;
    private byte _chrBanks = 0;
    
    //Memory
    private byte[] _prgRam;
    private byte[] _chrRam;
    
    //Mapper
    private readonly IMapperService _mapper;
}

public class NesRomReader
{
    private const int HEADER_SIZE = 16;
    private const string NES_SIGNATURE = "NES\x1A";
    
    public class NesRom
    {
        public NesHeader Header { get; set; }
        public byte MapperId { get; set; }
        public MirroringType MirrorType { get; set; }
        public byte[] Trainer { get; set; }
        public byte[] PrgRom { get; set; }
        public byte[] ChrRom { get; set; }
    }
    public class NesHeader
    {
        public byte[] Signature { get; set; } = new byte[4];
        public byte PrgRomBanks { get; set; }
        public byte ChrRomBanks { get; set; }
        public byte Mapper1 { get; set; }
        public byte Mapper2 { get; set; }
        public byte PrgRamSize { get; set; }
        public byte TvSystem1 { get; set; }
        public byte TvSystem2 { get; set; }
        public byte[] Padding { get; set; } = new byte[5];
    }

    
    public static NesRom ReadRom(string filePath)
    {
        var rom = new NesRom();

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);
        // read header
        rom.Header = ReadHeader(reader);
            
        // validation
        if (!IsValidNesFile(rom.Header.Signature))
        {
            throw new InvalidDataException("Invalid nes rom file");
        }
            
        // read trainer (if exist)
        if ((rom.Header.Mapper1 & 0x04) > 0)
        {
            rom.Trainer = reader.ReadBytes(512);
        }
            
        // read Mapper
        rom.MapperId = (byte)(((rom.Header.Mapper2 >> 4) << 4) | (rom.Header.Mapper1 >> 4));
        rom.MirrorType = (rom.Header.Mapper1 & 0x01) > 0 ? MirroringType.Vertical : MirroringType.Horizontal;
            
        // "Discover" File Format
        byte nFileType = 1;

        if (nFileType == 0)
        {
            //Reserved
        }
        else if (nFileType == 1)
        {
            var prgBanks = rom.Header.PrgRomBanks;
            rom.PrgRom = reader.ReadBytes(prgBanks * 16384);
                
            var chrBanks = rom.Header.ChrRomBanks;
            if (chrBanks == 0)
            {
                // Create CHR RAM
                rom.ChrRom = reader.ReadBytes(8192);
            }
            else
            {
                // Allocate for ROM
                rom.ChrRom = reader.ReadBytes(chrBanks * 8192);
            }
        }

        else if (nFileType == 2)
        {
            //Reserved
        }

        return rom;
    }
    
    private static NesHeader ReadHeader(BinaryReader reader)
    {
        var header = new NesHeader();
        
        header.Signature = reader.ReadBytes(4);
        header.PrgRomBanks = reader.ReadByte();
        header.ChrRomBanks = reader.ReadByte();
        header.Mapper1 = reader.ReadByte();
        header.Mapper2 = reader.ReadByte();
        header.PrgRamSize = reader.ReadByte();
        header.TvSystem1 = reader.ReadByte();
        header.TvSystem2 = reader.ReadByte();
        header.Padding = reader.ReadBytes(5);
        
        return header;
    }
    
    private static bool IsValidNesFile(byte[] signature)
    {
        if (signature.Length != 4) return false;
        
        string sigString = "";
        for (int i = 0; i < 4; i++)
        {
            sigString += (char)signature[i];
        }
        
        return sigString == NES_SIGNATURE;
    }
}