namespace GraphicProcessingUnit
{
    public class Registers : IRegisters
    {
        readonly PpuMemory _memory;
        readonly Console _console;
        byte[] _oam;
        ushort _oamAddr;
		ushort _baseNametableAddress;
        int _vRamIncrement;
        ushort _bgPatternTableAddress;
        ushort _spritePatternTableAddress;
        int _vRamIncrement;
        byte _lastRegisterWrite;
        byte _flagBaseNametableAddr;
        byte _flagVRamIncrement;
        byte _flagSpritePatternTableAddr;
        byte _flagBgPatternTableAddr;
        byte _flagSpriteSize;
        byte _flagMasterSlaveSelect;
        byte _nmiOutput;
		byte _flagGreyscale;
        byte _flagShowBackgroundLeft;
        byte _flagShowSpritesLeft;
        byte _flagShowBackground;
        byte _flagShowSprites;
        byte _flagEmphasizeRed;
        byte _flagEmphasizeGreen;
        byte _flagEmphasizeBlue;
        ushort v;
        ushort t;
        byte x;
        byte w;
        byte f;

        public byte ReadFromRegister(ushort address)
        {
            byte data;
            switch (address)
            {
                case 0x2002:
                    data = ReadPpuStatus();
                    break;
                case 0x2004:
                    data = ReadOamData();
                    break;
                case 0x2007:
                    data = ReadPpuData();
                    break;
                default:
                    throw new Exception("Неверный регистр PPU был считан из регистра: " + address.ToString("X4"));
            }

            return data;
        }

        public void WriteToRegister(ushort address, byte data)
        {
            _lastRegisterWrite = data;
            switch (address)
            {
                case 0x2000:
                    WritePpuCtrl(data);
                    break;
                case 0x2001:
                    WritePpuMask(data);
                    break;
                case 0x2003:
                    WriteOamAddr(data);
                    break;
                case 0x2004:
                    WriteOamData(data);
                    break;
                case 0x2005:
                    WritePpuScroll(data);
                    break;
                case 0x2006:
                    WritePpuAddr(data);
                    break;
                case 0x2007:
                    WritePpuData(data);
                    break;
                case 0x4014:
                    WriteOamDma(data);
                    break;
                default:
                    throw new Exception("Неверный регистр PPU был записан в регистр: " + address.ToString("X4"));
            }
        }

        // $2000
        public void WritePpuCtrl(byte data)
        {
            _flagBaseNametableAddr = (byte)(data & 0x3);
            _flagVRamIncrement = (byte)((data >> 2) & 1);
            _flagSpritePatternTableAddr = (byte)((data >> 3) & 1);
            _flagBgPatternTableAddr = (byte)((data >> 4) & 1);
            _flagSpriteSize = (byte)((data >> 5) & 1);
            _flagMasterSlaveSelect = (byte)((data >> 6) & 1);
            _nmiOutput = (byte)((data >> 7) & 1);

            // Установка значений на основе флагов
            _baseNametableAddress = (ushort)(0x2000 + 0x400 * _flagBaseNametableAddr);
            _vRamIncrement = (_flagVRamIncrement == 0) ? 1 : 32;
            _bgPatternTableAddress = (ushort)(_flagBgPatternTableAddr == 0 ? 0x0000 : 0x1000);
            _spritePatternTableAddress = (ushort)(0x1000 * _flagSpritePatternTableAddr);

            t = (ushort)((t & 0xF3FF) | ((data & 0x03) << 10));
        }

        // $2001
        public void WritePpuMask(byte data)
        {
            _flagGreyscale = (byte)(data & 1);
            _flagShowBackgroundLeft = (byte)((data >> 1) & 1);
            _flagShowSpritesLeft = (byte)((data >> 2) & 1);
            _flagShowBackground = (byte)((data >> 3) & 1);
            _flagShowSprites = (byte)((data >> 4) & 1);
            _flagEmphasizeRed = (byte)((data >> 5) & 1);
            _flagEmphasizeGreen = (byte)((data >> 6) & 1);
            _flagEmphasizeBlue = (byte)((data >> 7) & 1);
        }

        // $2002
        public byte ReadPpuStatus()
        {
            byte retVal = 0;
            retVal |= (byte)(_lastRegisterWrite & 0x1F); // Наименее значащие 5 бит записи последнего регистра
            retVal |= (byte)(_flagSpriteOverflow << 5);
            retVal |= (byte)(_flagSpriteZeroHit << 6);
            retVal |= (byte)(_nmiOccurred << 7);
            _nmiOccurred = 0;
            w = 0;

            return retVal;
        }

        // $2004
        public byte ReadOamData()
        {
            return _oam[_oamAddr];
        }
        
        // $2004
        public void WriteOamData(byte data)
        {
            _oam[_oamAddr] = data;
            _oamAddr++;
        }

        // $2005
        public void WritePpuScroll(byte data)
        {
            if (w == 0) // Если это первая запись
            {
                t = (ushort)((t & 0xFFE0) | (data >> 3));
                x = (byte)(data & 0x07);
                w = 1;
            }
            else
            {
                t = (ushort)(t & 0xC1F);
                t |= (ushort)((data & 0x07) << 12);
                t |= (ushort)((data & 0xF8) << 2);
                w = 0;
            }
        }

        // $2006
        public void WritePpuAddr(byte data)
        {
            if (w == 0) // Если это первая запись
            {
                t = (ushort)((t & 0x00FF) | (data << 8));
                w = 1;
            }
            else
            {
                t = (ushort)((t & 0xFF00) | data);
                v = t;
                w = 0;
            }
        }

        // $2007
        public byte ReadPpuData()
        {
            // допилить
        }
        
        // $2007
        public void WritePpuData(byte data)
        {
            _memory.Write(v, data);
            v += (ushort)(_vRamIncrement);
        }

        // $4014
        public void WriteOamDma(byte data)
        {
            // допилить
        }
        
        // $4014
        public void WriteOamAddr(byte data)
        {
            _oamAddr = data;
        }
    }
}