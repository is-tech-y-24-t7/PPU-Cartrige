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
            
        }
        
    }   
}