using System;
using MemoryService;
using Console = MemoryService.Console;

namespace GraphicProcessingUnit
{
    public class PPU
    {
        readonly MemoryService.PpuMemory _memory;
        readonly MemoryService.Console _console;
        
        public Registers registers;

        private byte[] _oam;
        private ushort _oamAddr;
        private byte[] _sprites;
        private int[] _spriteIndicies;
        private int _numSprites;

        public int Scanline{ get; private set; } 
        public int Cycle{ get; private set; }

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
        
        byte _flagSpriteOverflow;
        byte _flagSpriteZeroHit;

        ushort v;
        ushort t;
        byte x;
        byte w;
        byte f;
        
        public bool RenderingEnabled
        {
            get { return _flagShowSprites != 0 || _flagShowBackground != 0; }
        }
        
        public byte[] BitmapData { get; }


        public PPU(Console console)
        {
            _memory = console.PpuMemory;
            _console = console;

            registers = new Registers();

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

            _nmiOccurred = 0;
            _nmiOutput = 0;

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
                _flagSpriteOverflow = 0;
                _flagSpriteZeroHit = 0;
            }

            if (renderingEnabled)
            {
                if (Cycle == 257)
                {
                    if (0 <= Scanline && Scanline <= 239)
                        EvalSprites();
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
                            SaveTileData();
                            IncrementX();
                            if (Cycle == 256) 
                                IncrementY();
                            break;
                    }

                }
                
                if (Cycle > 257 && Cycle <= 320 && (Scanline == 261 || (0 <= Scanline && Scanline <= 239))) 
                    _oamAddr = 0;
                
                if (Cycle == 257 && ((0 <= Scanline && Scanline <= 239) || Scanline == 261)) 
                    CopyHorizontalData();

                if (Cycle >= 280 && Cycle <= 304 && Scanline == 261) 
                    CopyVerticalData();
            }
        }

        void UpdateState()
        {
            if (Scanline == 241 && Cycle == 1)
            {
                _nmiOccurred = 1;
                if (_nmiOutput != 0) _console.Cpu.TriggerNMI();
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
                    _console.DrawFrame();
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
                v = (ushort) (v & ~0x001F);
                v = (ushort) (v ^ 0x0400);
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
                v = (ushort) (v & ~0x7000);
                int y = (v & 0x03E0) >> 5;
                switch (y)
                {
                    case 29:
                        y = 0;
                        v = (ushort) (v ^ 0x0800);
                        break;
                    case 31:
                        y = 0;
                        break;
                    default:
                        y += 1;
                        break;
                }
                v = (ushort) ((v & ~0x03E0) | (y << 5));
            }
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
        
        void EvalSprites()
        {
            Array.Clear(_sprites, 0, _sprites.Length);
            Array.Clear(_spriteIndicies, 0, _spriteIndicies.Length);

            // 8x8 или 8x16 
            int h = _flagSpriteSize == 0 ? 7 : 15;

            _numSprites = 0;
            int y = Scanline;
            
            for (int i = _oamAddr; i < 256; i += 4)
            {
                byte spriteYTop = _oam[i];
                int offset = y - spriteYTop;
                if (offset <= h && offset >= 0)
                {
                    if (_numSprites == 8)
                    {
                        _flagSpriteOverflow = 1;
                        break;
                    } 
                    else
                    {
                        Array.Copy(_oam, i, _sprites, _numSprites * 4, 4);
                        _spriteIndicies[_numSprites] = (i - _oamAddr) / 4;
                        _numSprites++;
                    }
                }
            }
        }
        
        void SaveTileData()
        {
            byte _palette = (byte)((_attributeTableByte >> ((CoarseX() & 0x2) | ((CoarseY() & 0x2) << 1))) & 0x3);
            
            ulong data = 0;
            for (int i = 0; i < 8; i++)
            {
                byte loColorBit = (byte)((_tileBitfieldLo >> (7 - i)) & 1);
                byte hiColorBit = (byte)((_tileBitfieldHi >> (7 - i)) & 1);
                byte colorNum = (byte)((hiColorBit << 1) | (loColorBit) & 0x03);

                byte fullPixelData = (byte)(((_palette << 2) | colorNum) & 0xF);

                data |= (uint)(fullPixelData << (4 * i));
            }

            _tileShiftReg &= 0xFFFFFFFF;
            _tileShiftReg |= (data << 32);
        }

        byte LookupBackgroundColor(byte data)
        {
            int colorNum = data & 0x3;
            int paletteNum = (data >> 2) & 0x3;
            
            if (colorNum == 0) return _memory.Read(0x3F00);

            ushort paletteAddress;
            switch (paletteNum)
            {
                case 0:
                    paletteAddress = (ushort)0x3F01;
                    break;
                case 1:
                    paletteAddress = (ushort)0x3F05;
                    break;
                case 2:
                    paletteAddress = (ushort)0x3F09;
                    break;
                case 3:
                    paletteAddress = (ushort)0x3F0D;
                    break;
                default:
                    throw new Exception("Invalid background palette Number: " + paletteNum.ToString());
            }

            paletteAddress += (ushort)(colorNum - 1);
            return _memory.Read(paletteAddress);
        }

        byte LookupSpriteColor(byte data)
        {
            int colorNum = data & 0x3;
            int paletteNum = (data >> 2) & 0x3;

            if (colorNum == 0) return _memory.Read(0x3F00);

            ushort paletteAddress;
            switch (paletteNum)
            {
                case 0:
                    paletteAddress = (ushort) 0x3F11;
                    break;
                case 1:
                    paletteAddress = (ushort) 0x3F15;
                    break;
                case 2:
                    paletteAddress = (ushort) 0x3F19;
                    break;
                case 3:
                    paletteAddress = (ushort) 0x3F1D;
                    break;
                default:
                    throw new Exception("Invalid background palette Number: " + paletteNum.ToString());
            }
            paletteAddress += (ushort)(colorNum - 1);
            return _memory.Read(paletteAddress);
        }
        
        void RenderPixel()
        {
            byte bgPixelData = GetBgPixelData();

            int spriteScanlineIndex;
            byte spritePixelData = GetSpritePixelData(out spriteScanlineIndex);
            bool isSpriteZero = _spriteIndicies[spriteScanlineIndex] == 0;

            int bgColorNum = bgPixelData & 0x03;
            int spriteColorNum = spritePixelData & 0x03;

            byte color;

            if (bgColorNum == 0)
            {
                if (spriteColorNum == 0) color = LookupBackgroundColor(bgPixelData);
                else color = LookupSpriteColor(spritePixelData);
            }
            else
            {
                if (spriteColorNum == 0) color = LookupBackgroundColor(bgPixelData);
                else
                {
                    if (isSpriteZero) _flagSpriteZeroHit = 1;
                    int priority = (_sprites[(spriteScanlineIndex * 4) + 2] >> 5) & 1;
                    if (priority == 1) color = LookupBackgroundColor(bgPixelData);
                    else color = LookupSpriteColor(spritePixelData);
                }
            }

            BitmapData[Scanline * 256 + (Cycle - 1)] = color;
        }
        
        byte GetBgPixelData()
        {
            int xPos = Cycle - 1;

            if (_flagShowBackground == 0) return 0;
            if (_flagShowBackgroundLeft == 0 && xPos < 8) return 0;

            return (byte)((_tileShiftReg >> (x * 4)) & 0xF);
        }
        
        byte GetSpritePixelData(out int spriteIndex)
        {
            int xPos = Cycle - 1;
            int yPos = Scanline - 1;

            spriteIndex = 0;

            if (_flagShowSprites == 0) return 0;
            if (_flagShowSpritesLeft == 0 && xPos < 8) return 0;
            
            ushort _currSpritePatternTableAddr = _spritePatternTableAddress;
            
            for (int i = 0; i < _numSprites * 4; i += 4)
            {
                int spriteXLeft = _sprites[i + 3];
                int offset = xPos - spriteXLeft;

                if (offset <= 7 && offset >= 0)
                {
                    int yOffset = yPos - _sprites[i];
                    byte patternIndex;
                    if (_flagSpriteSize == 1)
                    {
                        _currSpritePatternTableAddr = (ushort)((_sprites[i + 1] & 1) * 0x1000);
                        patternIndex = (byte)(_sprites[i + 1] & 0xFE);
                    }
                    else
                    {
                        patternIndex = (byte)(_sprites[i + 1]);
                    }

                    ushort patternAddress = (ushort)(_currSpritePatternTableAddr + (patternIndex * 16));

                    bool flipHoriz = (_sprites[i + 2] & 0x40) != 0;
                    bool flipVert = (_sprites[i + 2] & 0x80) != 0;
                    int colorNum = GetSpritePatternPixel(patternAddress, offset, yOffset, flipHoriz, flipVert);
                    
                    if (colorNum == 0)
                    {
                        continue;
                    }
                    else 
                    {
                        byte paletteNum = (byte)(_sprites[i + 2] & 0x03);
                        spriteIndex = i / 4;
                        return (byte)(((paletteNum << 2) | colorNum) & 0xF);
                    }
                }
            }
            return 0x00; 
        }
        
        int GetSpritePatternPixel(ushort patternAddr, int xPos, int yPos, bool flipHoriz = false, bool flipVert = false)
        {
            int h = _flagSpriteSize == 0 ? 7 : 15;
            xPos = flipHoriz ? 7 - xPos : xPos;
            yPos = flipVert ? h - yPos : yPos;
            
            ushort yAddr;
            if (yPos <= 7) yAddr = (ushort)(patternAddr + yPos);
            else yAddr = (ushort)(patternAddr + 16 + (yPos - 8)); 
            
            byte[] pattern = new byte[2];
            pattern[0] = _memory.Read(yAddr);
            pattern[1] = _memory.Read((ushort)(yAddr + 8));
            
            byte loBit = (byte)((pattern[0] >> (7 - xPos)) & 1);
            byte hiBit = (byte)((pattern[1] >> (7 - xPos)) & 1);

            return ((hiBit << 1) | loBit) & 0x03;
        }

    }
}
