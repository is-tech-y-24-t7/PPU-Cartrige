namespace GraphicProcessingUnit
{
    public class Registers : IRegisters
    {
        private PPU _ppu;

        public Registers(PPU ppu)
        {
            _ppu = ppu;
        }

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
            _ppu._flagBaseNametableAddr = (byte)(data & 0x3);
            _ppu._flagVRamIncrement = (byte)((data >> 2) & 1);
            _ppu._flagSpritePatternTableAddr = (byte)((data >> 3) & 1);
            _ppu._flagBgPatternTableAddr = (byte)((data >> 4) & 1);
            _ppu._flagSpriteSize = (byte)((data >> 5) & 1);
            _ppu._flagMasterSlaveSelect = (byte)((data >> 6) & 1);
            _ppu._nmiOutput = (byte)((data >> 7) & 1);

            // Установка значений на основе флагов
            _ppu._baseNametableAddress = (ushort)(0x2000 + 0x400 * _ppu._flagBaseNametableAddr);
            _ppu._vRamIncrement = (_ppu._flagVRamIncrement == 0) ? 1 : 32;
            _ppu._bgPatternTableAddress = (ushort)(_ppu._flagBgPatternTableAddr == 0 ? 0x0000 : 0x1000);
            _ppu._spritePatternTableAddress = (ushort)(0x1000 * _ppu._flagSpritePatternTableAddr);

            t = (ushort)((t & 0xF3FF) | ((data & 0x03) << 10));
        }

        // $2001
        public void WritePpuMask(byte data)
        {
            _ppu._flagGreyscale = (byte)(data & 1);
            _ppu._flagShowBackgroundLeft = (byte)((data >> 1) & 1);
            _ppu._flagShowSpritesLeft = (byte)((data >> 2) & 1);
            _ppu._flagShowBackground = (byte)((data >> 3) & 1);
            _ppu._flagShowSprites = (byte)((data >> 4) & 1);
            _ppu._flagEmphasizeRed = (byte)((data >> 5) & 1);
            _ppu._flagEmphasizeGreen = (byte)((data >> 6) & 1);
            _ppu._flagEmphasizeBlue = (byte)((data >> 7) & 1);
        }

        // $2002
        public byte ReadPpuStatus()
        {
            byte retVal = 0;
            retVal |= (byte)(_ppu._lastRegisterWrite & 0x1F); // Наименее значащие 5 бит записи последнего регистра
            retVal |= (byte)(_ppu._flagSpriteOverflow << 5);
            retVal |= (byte)(_ppu._flagSpriteZeroHit << 6);
            retVal |= (byte)(_ppu._nmiOccurred << 7);
            _ppu._nmiOccurred = 0;
            w = 0;

            return retVal;
        }

        // $2004
        public byte ReadOamData()
        {
            return _ppu._oam[_oamAddr];
        }
        
        // $2004
        public void WriteOamData(byte data)
        {
            _ppu._oam[_oamAddr] = data;
            _ppu._oamAddr++;
        }

        // $2005
        public void WritePpuScroll(byte data)
        {
            if (_ppu.w == 0) // Если это первая запись
            {
                _ppu.t = (ushort)((_ppu.t & 0xFFE0) | (data >> 3));
                _ppu.x = (byte)(data & 0x07);
                _ppu.w = 1;
            }
            else
            {
                _ppu.t = (ushort)(_ppu.t & 0xC1F);
                _ppu.t |= (ushort)((data & 0x07) << 12);
                _ppu.t |= (ushort)((data & 0xF8) << 2);
                _ppu.w = 0;
            }
        }

        // $2006
        public void WritePpuAddr(byte data)
        {
            if (_ppu.w == 0) // Если это первая запись
            {
                _ppu.t = (ushort)((_ppu.t & 0x00FF) | (data << 8));
                _ppu.w = 1;
            }
            else
            {
                _ppu.t = (ushort)((_ppu.t & 0xFF00) | data);
                _ppu.v = _ppu.t;
                _ppu.w = 0;
            }
        }

        // $2007
        public byte ReadPpuData()
        {
            byte data = _memory.Read(_ppu.v);

            if (_ppu.v < 0x3F00)
            {
                byte bufferedData = _ppu._ppuDataBuffer;
                _ppu._ppuDataBuffer = data;
                data = bufferedData;
            }
            else
            {
                _ppu._ppuDataBuffer = _memory.Read((ushort) (_ppu.v - 0x1000));
            }

            _ppu.v += (ushort)(_ppu._vRamIncrement);
            return data;
        }
        
        // $2007
        public void WritePpuData(byte data)
        {
            _memory.Write(_ppu.v, data);
            _ppu.v += (ushort)(_ppu._vRamIncrement);
        }

        // $4014
        public void WriteOamDma(byte data)
        {
            ushort startAddr = (ushort)(data << 8);
            _ppu._console.CpuMemory.ReadBufWrapping(_ppu._oam, _ppu._oamAddr, startAddr, 256);

            // OAM DMA всегда занимает не менее 513 циклов CPU
            _ppu._console.Cpu.AddIdleCycles(513);

            // OAM DMA занимает дополнительный цикл ЦП, если выполняется в нечетный цикл CPU
            if (_ppu._console.Cpu.Cycles % 2 == 1) _ppu._console.Cpu.AddIdleCycles(1);
        }
        }
        
        // $4014
        public void WriteOamAddr(byte data)
        {
            _ppu._oamAddr = data;
        }
    }
}