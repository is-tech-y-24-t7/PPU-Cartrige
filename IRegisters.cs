namespace GraphicProcessingUnit
{

    public interface IRegisters
    {
        public byte ReadFromRegister(ushort address);

        public void WriteToRegister(ushort address, byte data);

        // $2000
        void WritePpuCtrl(byte data);

        // $2001
        void WritePpuMask(byte data);

        // $2002
        byte ReadPpuStatus();

        // $2004
        byte ReadOamData();

        /// $2004
        void WriteOamData(byte data);

        // $2005
        void WritePpuScroll(byte data);

        // $2006
        void WritePpuAddr(byte data);

        // $2007
        byte ReadPpuData();

        // $2007
        void WritePpuData(byte data);

        // $4014
        void WriteOamAddr(byte data);

        // $4014
        void WriteOamDma(byte data);
    }
}