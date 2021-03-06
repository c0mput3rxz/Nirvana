using System;
using System.IO;
using System.Text;
using ErrorHandling.Exceptions;

namespace VariantAnnotation.FileHandling.Binary
{
    public sealed class ExtendedBinaryReader : BinaryReader
    {
        #region members

        private readonly Stream _stream;

        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        public ExtendedBinaryReader(Stream s) : this(s, new UTF8Encoding()) { }

        /// <summary>
        /// constructor
        /// </summary>
        public ExtendedBinaryReader(Stream input, Encoding encoding, bool leaveOpen = false)
            : base(input, encoding, leaveOpen)
        {
            _stream = input;
        }

        /// <summary>
        /// returns an integer from the binary reader
        /// </summary>
        public int ReadOptInt32()
        {
            if (_stream == null) throw new GeneralException("File not open");

            int count = 0;
            int shift = 0;

            while (shift != 35)
            {
                byte b = ReadByte();
                count |= (b & sbyte.MaxValue) << shift;
                shift += 7;

                if ((b & 128) == 0) return count;
            }

            throw new FormatException("Unable to read the 7-bit encoded integer");
        }

        /// <summary>
        /// returns a long from the binary reader
        /// </summary>
        public long ReadOptInt64()
        {
            if (_stream == null) throw new GeneralException("File not open");

            long count = 0;
            int shift = 0;

            while (shift != 70)
            {
                byte b = ReadByte();
                count |= (long)(b & sbyte.MaxValue) << shift;
                shift += 7;

                if ((b & 128) == 0) return count;
            }

            throw new FormatException("Unable to read the 7-bit encoded long");
        }

        public T[] ReadOptArray<T>(Func<T> readOptFunc)
        {
            if (_stream == null) throw new GeneralException("File not open");

            var count = ReadOptInt32();
            if (count == 0) return null;

            var values = new T[count];
            for (var i = 0; i < count; i++) values[i] = readOptFunc();
            return values;
        }

		/// <summary>
		/// returns an ASCII string from the binary reader
		/// </summary>
		public string ReadAsciiString()
        {
            if (_stream == null) throw new GeneralException("File not open");

            int numBytes = ReadOptInt32();

            // grab the ASCII characters
            // ReSharper disable once AssignNullToNotNullAttribute
            return numBytes == 0 ? null : Encoding.ASCII.GetString(ReadBytes(numBytes));
        }
    }
}