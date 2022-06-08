using System;
using System.IO;

namespace GraphicProcessingUnit
{
    public class Cartridge
    {
        const int HeaderMagic = 0x1A53454E;

        byte[] _prgRom;
        byte[] _chr;
        byte[] _prgRam;
        public Console Console { get; set; }
        public int PrgRomBanks { get; private set; }
        public int ChrBanks { get; private set; }
        public bool VerticalVramMirroring { get; private set; }
        public bool BatteryBackedMemory { get; private set; }
        public bool ContainsTrainer { get; private set; }
        public bool UsesChrRam { get; private set; }
        public int MapperNumber { get; private set; }
        public bool Invalid { get; private set; }

        int _flags6;
        int _flags7;
        
        public Cartridge(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            Invalid = false;
            ParseHeader(reader);
            LoadPrgRom(reader);
            LoadChr(reader);

            _prgRam = new byte[8192];
        }
        
        public byte ReadPrgRom(int index)
        {
            return _prgRom[index];
        }
        
        public byte ReadPrgRam(int index)
        {
            return _prgRam[index];
        }
        
        public void WritePrgRam(int index, byte data)
        {
            _prgRam[index] = data;
        }
        
        public byte ReadChr(int index)
        {
            return _chr[index];
        }
        
        public void WriteChr(int index, byte data)
        {
            if (!UsesChrRam) throw new Exception("Попытка записи в CHR ROM по индексу " + index.ToString("X4"));
            _chr[index] = data;
        }

        void LoadPrgRom(BinaryReader reader)
        {
            int _prgRomOffset = ContainsTrainer ? 16 + 512 : 16;

            reader.BaseStream.Seek(_prgRomOffset, SeekOrigin.Begin);

            _prgRom = new byte[PrgRomBanks * 16384];
            reader.Read(_prgRom, 0, PrgRomBanks * 16384);
        }

        void LoadChr(BinaryReader reader)
        {
            if (UsesChrRam)
            {
                _chr = new byte[8192];
            }
            else
            {
                _chr = new byte[ChrBanks * 8192];
                reader.Read(_chr, 0, ChrBanks * 8192);
            }
        }

        void ParseHeader(BinaryReader reader)
        {
            uint Num = reader.ReadUInt32();
            if (Num != HeaderMagic)
            {
                System.Console.WriteLine("Значение заголовка (" + Num.ToString("X4") + ") неверно");
                Invalid = true;
                return;
            }
            // допилить
        }
    }   
}