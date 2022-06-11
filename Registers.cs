using System;
using System.IO;

namespace GraphicProcessingUnit
{
    public class Registers : IRegisters
    {
        readonly PpuMemory _memory;
        readonly Console _console;

        ushort v;
        ushort t;
        byte x;
        byte w;
        byte f;

        public byte ReadFromRegister(ushort address)
        {
            
        }

        public void WriteToRegister(ushort address, byte data)
        {
            
        }

        // $2000
        public void WritePpuCtrl(byte data)
        {
            
        }

        // $2001
        public void WritePpuMask(byte data)
        {
            
        }

        // $4014
        public void WriteOamAddr(byte data)
        {
            
        }

        // $2004
        public void WriteOamData(byte data)
        {
            
        }

        // $2005
        public void WritePpuScroll(byte data)
        {
            
        }

        // $2006
        public void WritePpuAddr(byte data)
        {
            
        }

        // $2007
        public void WritePpuData(byte data)
        {
            
        }

        // $4014
        public void WriteOamDma(byte data)
        {
            
        }

        // $2002
        public byte ReadPpuStatus()
        {
            
        }

        // $2004
        public byte ReadOamData()
        {
            
        }

        // $2007
        public byte ReadPpuData()
        {
            
        }
    }
}