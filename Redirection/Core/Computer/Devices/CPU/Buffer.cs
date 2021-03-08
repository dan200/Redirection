using System;

namespace Dan200.Core.Computer.Devices.CPU
{
    public unsafe class Buffer
    {
        public readonly byte[] Data;
        public readonly int Start;
        public readonly int Length;

        public byte this[int i]
        {
            get
            {
                if (i < 0 || i >= Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return Data[Start + i];
            }
            set
            {
                if (i < 0 || i >= Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                Data[Start + i] = value;
            }
        }

        private Buffer(byte[] data, int start, int length)
        {
            Data = data;
            Start = start;
            Length = length;
        }

        public Buffer(byte fill, int length)
        {
            var data = new byte[length];
            if (fill != 0)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] = fill;
                }
            }
            Data = data;
            Start = 0;
            Length = length;
        }

        public Buffer(byte[] bytes)
        {
            Data = bytes;
            Start = 0;
            Length = bytes.Length;
        }

        public byte[] Read(int start, int length)
        {
            if (start < 0 || length < 0 || start + length > Length)
            {
                throw new InvalidOperationException();
            }
            var bytes = new byte[length];
            System.Buffer.BlockCopy(
                Data, Start + start,
                bytes, 0,
                bytes.Length
            );
            return bytes;
        }

        public Buffer Sub(int start, int length)
        {
            if (start < 0 || length < 0 || start + length > Length)
            {
                throw new InvalidOperationException();
            }
            if (start == 0 && length == Length)
            {
                return this;
            }
            else
            {
                return new Buffer(Data, Start + start, length);
            }
        }

        public void Write(int start, byte[] bytes)
        {
            if (start < 0 || start + bytes.Length > Length)
            {
                throw new InvalidOperationException();
            }
            System.Buffer.BlockCopy(
                bytes, 0,
                Data, Start + start,
                bytes.Length
            );
        }

        public void Fill(byte fill)
        {
            Fill(fill, 0, Length);
        }

        public void Fill(byte fill, int start, int length)
        {
            if (start < 0 || length < 0 || start + length > Length)
            {
                throw new InvalidOperationException();
            }

            var mStart = Start;
            fixed (byte* pData = Data)
            {
                var end = start + length;
                for (int i = start; i < end; ++i)
                {
                    pData[mStart + i] = fill;
                }
            }
        }

        public Buffer Copy()
        {
            var mLength = Length;
            var data = new byte[mLength];
            var mData = Data;
            var mStart = Start;
            System.Buffer.BlockCopy(
                mData, (mStart) * sizeof(byte),
                data, 0,
                mLength * sizeof(byte)
            );
            return new Buffer(data, 0, mLength);
        }
    }
}

