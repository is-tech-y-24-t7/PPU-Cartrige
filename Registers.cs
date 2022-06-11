namespace GraphicProcessingUnit
{
    public class Registers : IRegisters
    {
        readonly PpuMemory _memory;
        readonly Console _console;
        byte[] _oam;
        ushort _oamAddr;
        int _vRamIncrement;
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
            // допилить
        }

        // $2001
        public void WritePpuMask(byte data)
        {
            // допилить
        }

        // $2002
        public byte ReadPpuStatus()
        {
            // допилить
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
            // допилить
        }

        // $2006
        public void WritePpuAddr(byte data)
        {
            // допилить
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