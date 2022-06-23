namespace GraphicProcessingUnit.Mappers
{
    public abstract class Mapper
    {
        public enum VramMirroring
        {
            Horizontal,
            Vertical,
            SingleLower,
            SingleUpper,
        }
        
        protected Console _console;
        protected VramMirroring _vramMirroringType;
        
        public int VramAddressToIndex(ushort address)
        {
            int index = (address - 0x2000) % 0x1000;
            switch(_vramMirroringType)
            {
                case VramMirroring.Vertical:
                    if (index >= 0x800) index -= 0x800;
                    break;
                case VramMirroring.Horizontal:
                    if (index > 0x800) index = ((index - 0x800) % 0x400) + 0x400; //2 B regions
                    else index %= 0x400; //2 A regions
                    break;
                case VramMirroring.SingleLower:
                    index %= 0x400;
                    break;
                case VramMirroring.SingleUpper:
                    index = (index % 400) + 0x400;
                    break;
            }
            return index;
        }
        
        public virtual void Step()
        {
            
        }
        public abstract byte Read(ushort address);
        public abstract void Write(ushort address, byte data);
    }
}