using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper
{
    public class Mapper_001 : IMapperService
    {
        private byte _prgBank;
        private byte _chrBank;

        private byte _chrBankSelect4Lo;
        private byte _chrBankSelect4Hi;
        private byte _chrBankSelect8;

        private byte _prgBankSelect16Lo;
        private byte _prgBankSelect16Hi;
        private byte _prgBankSelect32;

        private byte nLoadRegister;
        private byte nLoadCount;
        private byte nControlRegister;

        private MirroringType _mirrorType = MirroringType.Horizontal;
        private byte[] _ram = new byte[32 * 1024];

        public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)  // Cartridge RAM
            {
                mapAddress = 0xFFFFFFFF; // Indicate to use returned data
                data = _ram[address & 0x1FFF];
                return true;
            }

            if (address >= 0x8000)
            {
                //16K Mode
                if ((nControlRegister & 0x08) > 0)
                {
                    if (address <= 0xBFFF)
                    {
                        mapAddress = (uint)(_prgBankSelect16Lo * 0x4000 + (address & 0x3FFF));
                        return true;
                    }
                    else
                    {
                        mapAddress = (uint)(_prgBankSelect16Hi * 0x4000 + (address & 0x3FFF));
                        return true;
                    }
                }
                else
                {
                    //32k mode
                    mapAddress = (uint)(_prgBankSelect32 * 0x8000 + (address & 0x7FFF));
                    return true;
                }
            }

            return false;
        }

        public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)  // Cartridge RAM
            {
                mapAddress = 0xFFFFFFFF; // Indicate to use returned data
                _ram[address & 0x1FFF] = data;
                return true;
            }

            if (address >= 0x8000)
            {
                //write to register
                if ((data & 0x80) > 0)
                {
                    //reset
                    nLoadCount = 0;
                    nLoadRegister = 0x00;
                    nControlRegister |= 0x0C; // set to 16k mode, switch bank at $C000
                }
                else
                {
                    //write 5 bits to load register
                    nLoadRegister >>= 1;
                    nLoadRegister |= (byte)((data & 0x01) << 4);
                    nLoadCount++;

                    if (nLoadCount == 5)
                    {
                        byte targetRegister = (byte)((address >> 13) & 0x03); // $8000-$9FFF:0, $A000-$BFFF:1, $C000-$DFFF:2, $E000-$FFFF:3
                        if (targetRegister == 0)
                        {
                            // set mirror
                            nControlRegister = (byte)(nLoadRegister & 0x1F);
                            switch (nControlRegister & 0x03)
                            {
                                case 0:
                                    _mirrorType = MirroringType.OneScreen_Lo;
                                    break;

                                case 1:
                                    _mirrorType = MirroringType.OneScreen_Hi;
                                    break;

                                case 2:
                                    _mirrorType = MirroringType.Vertical;
                                    break;

                                case 3:
                                    _mirrorType = MirroringType.Horizontal;
                                    break;
                            }
                        }
                        else if (targetRegister == 1)
                        {
                            // set chr bank lo
                            if ((nControlRegister & 0x10) > 0)
                            {
                                // 4k mode
                                _chrBankSelect4Lo = (byte)(nLoadRegister & 0x1F);
                                // 添加边界检查
                                _chrBankSelect4Lo %= (byte)(_chrBank * 2);
                            }
                            else
                            {
                                // 8k mode
                                _chrBankSelect8 = (byte)(nLoadRegister & 0x1E);
                                // 添加边界检查
                                _chrBankSelect8 %= _chrBank;
                            }
                        }
                        else if (targetRegister == 2)
                        {
                            // set chr bank hi
                            if ((nControlRegister & 0x10) > 0)
                            {
                                // 4k mode
                                _chrBankSelect4Hi = (byte)(nLoadRegister & 0x1F);
                                // 添加边界检查
                                _chrBankSelect4Hi %= (byte)(_chrBank * 2);
                            }
                        }
                        else
                        {
                            byte prgMode = (byte)((nControlRegister >> 2) & 0x03);
                            if (prgMode == 0 || prgMode == 1)
                            {
                                // 32k mode
                                _prgBankSelect32 = (byte)((nLoadRegister & 0x0E) >> 1);
                                // 添加边界检查，确保Bank索引不会超出范围
                                _prgBankSelect32 %= (byte)(_prgBank / 2);
                            }
                            else if (prgMode == 2)
                            {
                                // fix first bank at $8000, switch 16k bank at $C000
                                _prgBankSelect16Hi = (byte)(nLoadRegister & 0x0F);
                                // 添加边界检查
                                _prgBankSelect16Hi %= _prgBank;
                                _prgBankSelect16Lo = 0;
                            }
                            else if (prgMode == 3)
                            {
                                // fix last bank at $C000, switch 16k bank at $8000
                                _prgBankSelect16Lo = (byte)(nLoadRegister & 0x0F);
                                // 添加边界检查
                                _prgBankSelect16Lo %= _prgBank;
                                _prgBankSelect16Hi = (byte)(_prgBank - 1);
                            }
                        }

                        //reset load register
                        nLoadCount = 0;
                        nLoadRegister = 0x00;
                    }
                }
            }

            return false;
        }

        public MirroringType GetMirrorType()
        {
            return _mirrorType;
        }

        public void MapperInit(byte prgBanks, byte chrBanks)
        {
            _prgBank = prgBanks;
            _chrBank = chrBanks;
            Reset();
        }

        public bool PPUMapRead(ushort address, ref uint mapAddress)
        {
            if (address < 0x2000)
            {
                if (_chrBank == 0)
                {
                    mapAddress = address;
                    return true;
                }
                else
                {
                    //4k mode
                    if ((nControlRegister & 0x10) > 0)
                    {
                        if (address < 0x1000)
                        {
                            mapAddress = (uint)(_chrBankSelect4Lo * 0x1000 + (address & 0x0FFF));
                            return true;
                        }
                        else
                        {
                            mapAddress = (uint)(_chrBankSelect4Hi * 0x1000 + (address & 0x0FFF));
                            return true;
                        }
                    }
                    else
                    {
                        //8k mode
                        mapAddress = (uint)(_chrBankSelect8 * 0x2000 + (address & 0x1FFF));
                        return true;
                    }
                }
            }

            return false;
        }

        public bool PPUMapWrite(ushort address, ref uint mapAddress)
        {
            if (address < 0x2000)
            {
                // 只有CHR-RAM模式下才允许写入
                if (_chrBank == 0)
                {
                    mapAddress = address;
                    return true;
                }
                // CHR-ROM模式不允许写入
                return false;
            }

            return false;
        }

        public void Reset()
        {
            nControlRegister = 0x1C;
            nLoadRegister = 0x00;
            nLoadCount = 0;

            _chrBankSelect4Lo = 0;
            _chrBankSelect4Hi = 0;
            _chrBankSelect8 = 0;

            _prgBankSelect16Lo = 0;
            _prgBankSelect16Hi = (byte)(_prgBank - 1);
            _prgBankSelect32 = 0;

            _mirrorType = MirroringType.Horizontal;
        }

        public bool irqState()
        {
            return false;
        }

        public void irqClear()
        {
            return;
        }

        public void scanline()
        {
            return;
        }
    }
}