﻿using System;
using System.IO;

namespace HDF5.NET
{
    internal class OffsetStream : Stream
    {
        private Stream _baseStream;
        private long _offset;

        public OffsetStream(Stream baseStream, long offset)
        {
            if (offset >= baseStream.Length)
                throw new Exception("The offset exceeds the length of the base stream.");

            _baseStream = baseStream;
            _offset = offset;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length - _offset;

        public override long Position 
        {
            get 
            {
                return _baseStream.Position - _offset;
            }
            set 
            {
                _baseStream.Position = value + _offset;
            }
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset + _offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }
    }
}
