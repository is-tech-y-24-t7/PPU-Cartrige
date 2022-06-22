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

        readonly int Scanline;
        readonly int Cycle;

        ulong _tileShiftReg;
        byte _nameTableByte;
        byte _attributeTableByte;
        byte _tileBitfieldLo;
        byte _tileBitfieldHi;

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

        public void Step()
        {
            UpdateState();

            bool renderingEnabled = (_flagShowBackground != 0) || (_flagShowSprites != 0);
            bool renderCycle = Cycle > 0 && Cycle <= 256;
            bool preFetchCycle = Cycle >= 321 && Cycle <= 336;
            bool fetchCycle = renderCycle || preFetchCycle;
            
            if (Scanline == 261 && Cycle == 1)
            {
                _nmiOccurred = 0;
                // noindroid TODO: overflow = 0
                // noindroid TODO: zeroHit = 0
            }

            if (renderingEnabled)
            {
                if (Cycle == 257)
                {
                    if (0 <= Scanline && Scanline <= 239) 
                        //noindroid TODO: EvalSprites();
                    else 
                        _numSprites = 0;
                }

                if (renderCycle && (0 <= Scanline && Scanline <= 239))
                    RenderPixel();

                if (fetchCycle && ((0 <= Scanline && Scanline <= 239) || Scanline == 261))
                {
                    _tileShiftReg >>= 4;
                    switch (Cycle % 8)
                    {
                        case 1:
                            FetchTileByte();
                            break;
                        case 3:
                            FetchAttributeByte();
                            break;
                        case 5:
                            FetchBitfieldLow();
                            break;
                        case 7:
                            FetchBitfieldHigh();
                            break;
                        case 0:
                            // noindroid TODO: save tile data
                            IncrementX();
                            if (Cycle == 256) 
                                IncrementY();
                            break;
                    }

                }
                
                if (Cycle > 257 && Cycle <= 320 && (Scanline == 261 || (0 <= Scanline && Scanline <= 239))) 
                    _oamAddr = 0;
                
                if (Cycle == 257 && ((0 <= Scanline && Scanline <= 239) || Scanline == 261)) 
                    CopyHorizPositionData();

                if (Cycle >= 280 && Cycle <= 304 && Scanline == 261) 
                    CopyVertPositionData();
            }
        }

        void UpdateState()
        {
            if (Scanline == 241 && Cycle == 1)
            {
                _nmiOccurred = 1;
                if (_nmiOutput != 0) _console.Cpu.TriggerNmi();
            }

            bool renderingEnabled = (_flagShowBackground != 0) || (_flagShowSprites != 0);
            
            if (renderingEnabled)
            {
                if (Scanline == 261 && f == 1 && Cycle == 339)
                {
                    f = 0;
                    Scanline = 0;
                    Cycle = -1;
                    _console.DrawFrame();
                    return;
                }
            }
            Cycle++;
            
            if (Cycle > 340)
            {
                if (Scanline == 261)
                {
                    if (f == 0)
                        f = 1;
                    else
                        f = 0;
                    Scanline = 0;
                    Cycle = -1;
                    _console.Draw(); // TODO: check if Console method named like this
                }
                else
                {
                    Cycle = -1;
                    Scanline++;
                }
            }
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

        void IncrementX()
        {
            if ((v & 0x001F) == 31)
            {
                v = v & ~0x001F;
                v = v ^ 0x0400;
            }
            else
            {
                v += 1;
            }
        }

        int CoarseY()
        {
            return (v & 0x03E0) >> 5;
        }

        void IncrementY()
        {
            if ((v & 0x7000) != 0x7000)
            {
                v += 0x1000;
            }
            else
            {
                v = v & ~0x7000;
                int y = (v & 0x03E0) >> 5;
                if (y == 29)
                {
                    y = 0;
                    v = v ^ 0x0800;
                }
                else if (y == 31)
                {
                    y = 0;
                }
                else
                {
                    y += 1;
                }
            }

            v = (v & ~0x03E0) | (y << 5);
        }

        int FineY()
        {
            return (v >> 12) & 0x7;
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

        void FetchBitfieldLow()
        {
            ushort address = (ushort)(_bgPatternTableAddress + (_nameTableByte * 16) + FineY());
            _tileBitfieldLo = _memory.Read(address);
        }

        void FetchBitfieldHigh()
        {
            ushort address = (ushort)(_bgPatternTableAddress + (_nameTableByte * 16) + FineY() + 8);
            _tileBitfieldHi = _memory.Read(address);
        }
    }
}
