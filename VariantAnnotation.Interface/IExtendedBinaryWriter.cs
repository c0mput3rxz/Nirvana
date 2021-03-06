﻿namespace VariantAnnotation.Interface
{
    public interface IExtendedBinaryWriter
    {
        void Write(bool b);
        void Write(byte b);
        void Write(ushort us);
		void Write(string s);
        void Write(uint value);
        void WriteOpt(int value);
        void WriteOpt(long value);
        void WriteOptAscii(string s);
        void WriteOptUtf8(string s);
    }
}
