using System;
using System.IO;

namespace GraphicProcessingUnit
{
    public class Cartridge
    {
        private const int Signature = 0x1A53454E;

        private byte[] _prgRom;
        private byte[] _chr;
        private byte[] _prgRam;
        public Console Console { get; set; }
        public int PrgRomBanks { get; private set; }
        public int ChrBanks { get; private set; }
        public bool VerticalVramMirroring { get; private set; }
        public bool BatteryBackedMemory { get; private set; }
        public bool ContainsTrainer { get; private set; }
        public bool UsesChrRam { get; private set; }
        public int MapperNumber { get; private set; }
        public bool Invalid { get; private set; }

        private int _flags6;
        
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
            // 0-3 сигнатура файла
            // 4 размер PRG ROM
            // 5 Размер CHR ROM (или 0 если CHR RAM)
            // 6 - флаг на маппер и вертикальный/горизонатальный мирроринг
            // 7 и более - не используем (считывать NES2 не будем)

            uint Num = reader.ReadUInt32();
            if (Num != HeaderMagic)
            {
                System.Console.WriteLine("Значение заголовка (" + Num.ToString("X4") + ") неверно");
                Invalid = true;
                return;
            }

            PrgRomBanks = reader.ReadByte();
            System.Console.WriteLine("PRG ROM = " + (16 * PrgRomBanks).ToString() + " Kb");

            ChrBanks = reader.ReadByte();
            if (ChrBanks == 0)
            {
                System.Console.WriteLine("Использование CHR RAM");
                ChrBanks = 2;
                UsesChrRam = true;
            }
            else
            {
                System.Console.WriteLine((8 * ChrBanks).ToString() + "Kb of CHR ROM");
                UsesChrRam = false;
            }

            // 0 бит тип mirroring
            // 1 бит содержит ли battery-backed PRG RAM
            // 2 бит есть ли трейнер
            // 3 бит пока-что не используем (вертикально-горизонтальный mirroring)
            _flags6 = reader.ReadByte();
            VerticalVramMirroring = ((_flags6 & 0b00000001) != 0);
            System.Console.WriteLine("VRAM mirroring type: " + (VerticalVramMirroring ? "vertical" : "horizontal"));
            if (VerticalVramMirroring)
                System.Console.WriteLine("Вертикальное отражение");
            else
                System.Console.WriteLine("Горизонтальное отражение")

            BatteryBackedMemory = (_flags6 & 0x02) != 0;
            ContainsTrainer = (_flags6 & 0x04) != 0;

            //TODO: в будущем реализовать считывание 4-х экранного отражения
            //TODO: реализовать считывание редко-используемых полей (байты 7-10)
        }
    }   
}