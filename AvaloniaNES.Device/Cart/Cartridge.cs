using AvaloniaNES.Device.Mapper;

namespace AvaloniaNES.Device.Cart;

public enum MirroringType
{
    Hardware,
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
        _chrRom = rom.ChrRom;
        _prgBanks = rom.PrgBanks;
        _chrBanks = rom.ChrBanks;
        Mirror = rom.MirrorType;
        _mapperId = rom.MapperId;

        _mapper = rom.MapperId switch
        {
            000 => new Mapper_000(),
            001 => new Mapper_001(),
            002 => new Mapper_002(),
            003 => new Mapper_003(),
            004 => new Mapper_004(),
            066 => new Mapper_066(),
            _ => throw new Exception($"Not supported mapper: {rom.MapperId:D3} now!")
        };

        _mapper.MapperInit(_prgBanks, _chrBanks);
    }

    public void Reset()
    {
        _mapper.Reset();
    }

    public MirroringType GetMirror()
    {
        var m = _mapper.GetMirrorType();
        return m == MirroringType.Hardware ? Mirror : m;
    }

    public byte GetMapperId()
    {
        return _mapperId;
    }

    public IMapperService GetMapper()
    {
        return _mapper;
    }

    //Parameter
    private MirroringType Mirror;

    private byte _prgBanks = 0;
    private byte _chrBanks = 0;
    private byte _mapperId = 0;

    //Memory
    private byte[] _prgRam;

    private byte[] _chrRom;

    private byte[] _chrRam = new byte[0x2000];

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
        public byte PrgBanks { get; set; }
        public byte ChrBanks { get; set; }
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
        byte nFileType = 1;  // INES1.0 or 2.0
        if ((rom.Header.Mapper2 & 0x0C) == 0x08) nFileType = 2;
        if (nFileType == 1)
        {
            rom.PrgBanks = rom.Header.PrgRomBanks;
            rom.PrgRom = reader.ReadBytes(rom.PrgBanks * 16384);

            rom.ChrBanks = rom.Header.ChrRomBanks;
            if (rom.ChrBanks == 0)
            {
                // Create CHR RAM
                rom.ChrRom = new byte[8192];
            }
            else
            {
                // Allocate for ROM
                rom.ChrRom = reader.ReadBytes(rom.ChrBanks * 8192);
            }
        }
        else if (nFileType == 2)
        {
            rom.PrgBanks = (byte)(((rom.Header.PrgRamSize & 0x07) << 8) | rom.Header.PrgRomBanks);
            rom.PrgRom = reader.ReadBytes(rom.PrgBanks * 16384);
            rom.ChrBanks = (byte)(((rom.Header.PrgRamSize & 0x38) << 8) | rom.Header.ChrRomBanks);
            rom.ChrRom = reader.ReadBytes(rom.ChrBanks * 8192);
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