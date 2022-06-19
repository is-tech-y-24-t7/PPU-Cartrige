using System;

namespace GraphicProcessingUnit
{
    public class PPU
    {
        readonly PpuMemory _memory;
        readonly Console _console;
        
        public Registers registers;

        private byte[] _oam;
        private ushort _oamAddr;
        private byte[] _sprites;
        private int[] _spriteIndicies;
        private int _numSprites;

        ushort _baseNametableAddress;
        int _vRamIncrement;
        ushort _bgPatternTableAddress;
        ushort _spritePatternTableAddress;

        byte _lastRegisterWrite;
        byte _flagBaseNametableAddr;
        byte _flagVRamIncrement;
        byte _flagSpritePatternTableAddr;
        byte _flagBgPatternTableAddr;
        byte _flagSpriteSize;
        byte _flagMasterSlaveSelect;

        byte _nmiOccurred;
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


        public PPU(Console console)
        {
            _memory = console.PpuMemory;
            _console = console;

            BitmapData = new byte[256 * 240];

            _oam = new byte[256];
            _sprites = new byte[32];
            _spriteIndicies = new int[8];
        }

        public void Reset()
        {
            Array.Clear(BitmapData, 0, BitmapData.Length);

            Scanline = 240;
            Cycle = 340;

            w = 0;
            f = 0;

            Array.Clear(_oam, 0, _oam.Length);
            Array.Clear(_sprites, 0, _sprites.Length);
        }

        void CopyHorizontalData()
        {
            v = (ushort)((v & 0x7BE0) | (t & 0x041F));
        }

        void CopyVerticalData()
        {
            v = (ushort)((v & 0x041F) | (t & 0x7BE0));
        }

        int CoarseX()
        {
            return v & 0x001F;
        }

        int CoarseY()
        {
            return (v & 0x03E0) >> 5;
        }

        void FetchTileByte()
        {
            ushort address = (ushort)(0x2000 | (v & 0x0FFF));
            _nameTableByte = _memory.Read(address);
        }

        void FetchAttributeByte()
        {
            ushort address = (ushort)(0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07));
            _attributeTableByte = _memory.Read(address);
        }
    }
}
