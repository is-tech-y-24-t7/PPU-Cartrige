namespace GraphicProcessingUnit.Mappers
{
    class NROMMapper : Mapper
    {
        public NROMMapper(Console console)
        {
            _console = console;
            _vramMirroringType = _console.Cartridge.VerticalVramMirroring ? VramMirroring.Vertical : VramMirroring.Horizontal;
        }

        int AddressToPrgRomIndex(ushort address)
        {
            ushort mappedAddress = (ushort)(address - 0x8000); 
            return _console.Cartridge.PrgRomBanks == 1 ? (ushort)(mappedAddress % 16384) : mappedAddress; 
        }
        
        public override byte Read(ushort address)
        {
            byte data;
            if (address < 0x2000) // $0000 to $1FFF
            {
                data = _console.Cartridge.ReadChr(address);
            }
            else if (address >= 0x8000) // $8000 and above
            {
                data = _console.Cartridge.ReadPrgRom(AddressToPrgRomIndex(address));
            }
            else
            {
                // Open bus behaviour
                data = 0;
            }
            return data;
        }
        public override void Write(ushort address, byte data)
        {
            if (address < 0x2000) // CHR RAM
            {
                _console.Cartridge.WriteChr(address, data);
            }
        }
    }
}