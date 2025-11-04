namespace AvaloniaNES.Device.Mapper;

public interface IMapperService
{
    public void MapperInit(byte prgBanks, byte chrBanks);
    public void Reset();

    public bool CPUMapRead(ushort address, ref uint mapAddress);
    public bool CPUMapWrite(ushort address, ref uint mapAddress);
    public bool PPUMapRead(ushort address, ref uint mapAddress);
    public bool PPUMapWrite(ushort address, ref uint mapAddress);
}