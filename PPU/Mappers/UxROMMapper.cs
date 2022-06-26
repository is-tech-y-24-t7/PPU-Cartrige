using System;

namespace GraphicProcessingUnit.Mappers
{
    public class UxRomMapper : Mapper
    {
        int _bank0Offset;
        int _bank1Offset;
        
        public UxRomMapper(Console console)
        {
            _console = console;
            _bank0Offset = 0;
            _bank1Offset = (_console.Cartridge.PrgRomBanks - 1) * 0x4000;

            _vramMirroringType = _console.Cartridge.VerticalVramMirroring ? VramMirroring.Vertical : VramMirroring.Horizontal;
        }
        public override byte Read(ushort address)
        {
            byte data;
            if (address < 0x2000) 
            {
                data = _console.Cartridge.ReadChr(address);
            }
            else if (address >= 0x6000 && address < 0x8000)
            {
                data = 0x00;
            }
            else if (address <= 0xC000) 
            {
                data = _console.Cartridge.ReadPrgRom(_bank0Offset + (address - 0x8000));
            }
            else if (address <= 0xFFFF) 
            {
                data = _console.Cartridge.ReadPrgRom(_bank1Offset + (address - 0xC000));
            }
            else
            {
                throw new Exception("Invalid mapper read at address: " + address.ToString("X4"));
            }
            return data;
        }
        
        public override void Write(ushort address, byte data)
        {
            if (address < 0x2000) 
            {
                _console.Cartridge.WriteChr(address, data);
            }
            else if (address >= 0x6000 && address < 0x8000)
            {
                
            }
            else if (address >= 0x8000)
            {
                WriteBankSelect(data);
            }
            else
            {
                throw new Exception("Invalid mapper write at address: " + address.ToString("X4"));
            }
        }

        void WriteBankSelect(byte data)
        {
            _bank0Offset = (data & 0x0F) * 0x4000;
        }
    }
}