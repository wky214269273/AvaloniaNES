using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper
{
    public class Mapper_004 : IMapperService
    {
        private byte _prgBank;
        private byte _chrBank;

        private byte targetRegister = 0x00;
        private bool isPrgBankMode = false;
        private bool isChrInversion = false;
        private MirroringType mirroringType = MirroringType.Horizontal;

        private byte[] pRegister = new byte[8];
        private uint[] pChrBank = new uint[8];
        private uint[] pPrgBank = new uint[4];

        private bool bIRQActive = false;
        private bool bIRQEnable = false;
        private bool bIRQUpdate = false;
        private ushort nIRQCounter = 0;
        private ushort nIRQReload = 0;
        private byte[] _ram = new byte[32 * 1024];

        public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)
            {
                mapAddress = 0xFFFFFFFF;
                data = _ram[address & 0x1FFF];
                return true;
            }
            if (address >= 0x8000 && address <= 0x9FFF)
            {
                mapAddress = pPrgBank[0] + (uint)(address & 0x1FFF);
                return true;
            }
            if (address >= 0xA000 && address <= 0xBFFF)
            {
                mapAddress = pPrgBank[1] + (uint)(address & 0x1FFF);
                return true;
            }
            if (address >= 0xC000 && address <= 0xDFFF)
            {
                mapAddress = pPrgBank[2] + (uint)(address & 0x1FFF);
                return true;
            }
            if (address >= 0xE000 && address <= 0xFFFF)
            {
                mapAddress = pPrgBank[3] + (uint)(address & 0x1FFF);
                return true;
            }
            return false;
        }

        public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
        {
            if (address >= 0x6000 && address <= 0x7FFF)
            {
                mapAddress = 0xFFFFFFFF;
                _ram[address & 0x1FFF] = data;
                return true;
            }
            if (address >= 0x8000 && address <= 0x9FFF)
            {
                if ((address & 0x0001) == 0)
                {
                    targetRegister = (byte)(data & 0x07);
                    isPrgBankMode = (data & 0x40) != 0;
                    isChrInversion = (data & 0x80) != 0;
                }
                else
                {
                    pRegister[targetRegister] = data;
                    UpdateBanks();
                }
                return false;
            }

            if (address >= 0xA000 && address <= 0xBFFF)
            {
                if ((address & 0x0001) == 0)
                {
                    mirroringType = (data & 0x01) > 0 ? MirroringType.Horizontal : MirroringType.Vertical;
                }
                return false;
            }

            if (address >= 0xC000 && address <= 0xDFFF)
            {
                if ((address & 0x0001) == 0)
                {
                    nIRQReload = data;
                }
                else
                {
                    nIRQCounter = 0;
                }
                return false;
            }

            if (address >= 0xE000 && address <= 0xEFFF)
            {
                if ((address & 0x0001) == 0)
                {
                    bIRQEnable = false;
                    bIRQActive = false;
                }
                else
                {
                    bIRQEnable = true;
                }
                return false;
            }

            return false;
        }

        public MirroringType GetMirrorType()
        {
            return mirroringType;
        }

        public void MapperInit(byte prgBanks, byte chrBanks)
        {
            _prgBank = prgBanks;
            _chrBank = chrBanks;
            Reset();
        }

        public bool PPUMapRead(ushort address, ref uint mapAddress)
        {
            // 统一用bank映射，无论是CHR ROM还是CHR RAM
            if (address <= 0x03FF)
                mapAddress = pChrBank[0] + (uint)(address & 0x03FF);
            else if (address <= 0x07FF)
                mapAddress = pChrBank[1] + (uint)(address & 0x03FF);
            else if (address <= 0x0BFF)
                mapAddress = pChrBank[2] + (uint)(address & 0x03FF);
            else if (address <= 0x0FFF)
                mapAddress = pChrBank[3] + (uint)(address & 0x03FF);
            else if (address <= 0x13FF)
                mapAddress = pChrBank[4] + (uint)(address & 0x03FF);
            else if (address <= 0x17FF)
                mapAddress = pChrBank[5] + (uint)(address & 0x03FF);
            else if (address <= 0x1BFF)
                mapAddress = pChrBank[6] + (uint)(address & 0x03FF);
            else if (address <= 0x1FFF)
                mapAddress = pChrBank[7] + (uint)(address & 0x03FF);
            else
                return false;
            return true;
        }

        public bool PPUMapWrite(ushort address, ref uint mapAddress)
        {
            // 只要是CHR RAM模式，允许写入
            if (_chrBank == 0)
            {
                if (address <= 0x03FF)
                    mapAddress = pChrBank[0] + (uint)(address & 0x03FF);
                else if (address <= 0x07FF)
                    mapAddress = pChrBank[1] + (uint)(address & 0x03FF);
                else if (address <= 0x0BFF)
                    mapAddress = pChrBank[2] + (uint)(address & 0x03FF);
                else if (address <= 0x0FFF)
                    mapAddress = pChrBank[3] + (uint)(address & 0x03FF);
                else if (address <= 0x13FF)
                    mapAddress = pChrBank[4] + (uint)(address & 0x03FF);
                else if (address <= 0x17FF)
                    mapAddress = pChrBank[5] + (uint)(address & 0x03FF);
                else if (address <= 0x1BFF)
                    mapAddress = pChrBank[6] + (uint)(address & 0x03FF);
                else if (address <= 0x1FFF)
                    mapAddress = pChrBank[7] + (uint)(address & 0x03FF);
                else
                    return false;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            targetRegister = 0x00;
            isPrgBankMode = false;
            isChrInversion = false;
            mirroringType = MirroringType.Horizontal;

            bIRQActive = false;
            bIRQEnable = false;
            bIRQUpdate = false;
            nIRQCounter = 0x0000;
            nIRQReload = 0x0000;

            for (int i = 0; i < 4; i++) pPrgBank[i] = 0;
            for (int i = 0; i < 8; i++) { pRegister[i] = (byte)i; }
            UpdateBanks();
        }

        public bool irqState()
        {
            return bIRQActive;
        }

        public void irqClear()
        {
            bIRQActive = false;
        }

        public void scanline()
        {
            if (nIRQCounter == 0)
            {
                nIRQCounter = nIRQReload;
            }
            else
            {
                nIRQCounter--;
            }

            if (nIRQCounter == 0 && bIRQEnable)
            {
                bIRQActive = true;
            }
        }

        // 关键修正：MMC3 Bank 切换逻辑
        private void UpdateBanks()
        {
            // PRG ROM 总bank数
            int prgBankCount = _prgBank * 2;
            int chrBankCount = (_chrBank == 0) ? 8 : _chrBank * 8;

            // 边界保护
            byte prg6 = (byte)(pRegister[6] % prgBankCount);
            byte prg7 = (byte)(pRegister[7] % prgBankCount);

            // PRG Bank 映射
            if (isPrgBankMode)
            {
                // 0x8000-0x9FFF: 固定倒数第二bank
                pPrgBank[0] = (uint)((prgBankCount - 2) * 0x2000);
                // 0xA000-0xBFFF: 可切换bank 7
                pPrgBank[1] = (uint)(prg7 * 0x2000);
                // 0xC000-0xDFFF: 可切换bank 6
                pPrgBank[2] = (uint)(prg6 * 0x2000);
                // 0xE000-0xFFFF: 固定最后一个bank
                pPrgBank[3] = (uint)((prgBankCount - 1) * 0x2000);
            }
            else
            {
                // 0x8000-0x9FFF: 可切换bank 6
                pPrgBank[0] = (uint)(prg6 * 0x2000);
                // 0xA000-0xBFFF: 可切换bank 7
                pPrgBank[1] = (uint)(prg7 * 0x2000);
                // 0xC000-0xDFFF: 固定倒数第二bank
                pPrgBank[2] = (uint)((prgBankCount - 2) * 0x2000);
                // 0xE000-0xFFFF: 固定最后一个bank
                pPrgBank[3] = (uint)((prgBankCount - 1) * 0x2000);
            }

            // CHR Bank 映射
            // 2K/1K 切换，倒置模式
            if (_chrBank > 0)
            {
                if (isChrInversion)
                {
                    // 2K: 0,1 -> pRegister[2], pRegister[3]
                    pChrBank[0] = (uint)(((pRegister[2] & 0xFE) % chrBankCount) * 0x400);
                    pChrBank[1] = (uint)(((pRegister[2] | 0x01) % chrBankCount) * 0x400);
                    pChrBank[2] = (uint)(((pRegister[3] & 0xFE) % chrBankCount) * 0x400);
                    pChrBank[3] = (uint)(((pRegister[3] | 0x01) % chrBankCount) * 0x400);
                    // 1K: 4,5,6,7
                    pChrBank[4] = (uint)((pRegister[0] % chrBankCount) * 0x400);
                    pChrBank[5] = (uint)((pRegister[1] % chrBankCount) * 0x400);
                    pChrBank[6] = (uint)((pRegister[4] % chrBankCount) * 0x400);
                    pChrBank[7] = (uint)((pRegister[5] % chrBankCount) * 0x400);
                }
                else
                {
                    // 2K: 0,1 -> pRegister[0], pRegister[1]
                    pChrBank[0] = (uint)(((pRegister[0] & 0xFE) % chrBankCount) * 0x400);
                    pChrBank[1] = (uint)(((pRegister[0] | 0x01) % chrBankCount) * 0x400);
                    pChrBank[2] = (uint)(((pRegister[1] & 0xFE) % chrBankCount) * 0x400);
                    pChrBank[3] = (uint)(((pRegister[1] | 0x01) % chrBankCount) * 0x400);
                    // 1K: 2,3,4,5
                    pChrBank[4] = (uint)((pRegister[2] % chrBankCount) * 0x400);
                    pChrBank[5] = (uint)((pRegister[3] % chrBankCount) * 0x400);
                    pChrBank[6] = (uint)((pRegister[4] % chrBankCount) * 0x400);
                    pChrBank[7] = (uint)((pRegister[5] % chrBankCount) * 0x400);
                }
            }
        }
    }
}