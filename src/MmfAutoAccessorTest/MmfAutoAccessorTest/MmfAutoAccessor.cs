using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MmfAutoAccessorTest
{
    class EndianHelper
    {
        static EndianHelper()
        {
            IsBigEndian = Endianness();
        }

        public static bool IsBigEndian { get; private set; }

        private static bool Endianness()
        {
            int a = 0x12345678;
            var bytes = BitConverter.GetBytes(a);
            if (bytes[0] == (byte)0x12)
                return true;
            else return false;
        }
    }

    /// <summary>
    /// This class is used for auto create view accessor 
    /// for mmf. It will auto load chunks for large mmf
    /// </summary>
    class MmfAutoAccessor : IDisposable
    {
        #region const

        /// <summary>
        /// 20M bytes for each chunk
        /// </summary>
        private const long DEFALT_CHUNK_SIZE = 1024 * 1024 * 20;

        #endregion

        public MmfAutoAccessor(MemoryMappedFile mmf, long mmfSize, long chunkSize = DEFALT_CHUNK_SIZE)
        {
            if (mmf == null)
                throw new ArgumentNullException("mmf");

            if (mmfSize < 0)
                throw new ArgumentException("mmfSize");

            _mmf = mmf;
            _mmfSize = mmfSize;
            ChunkSize = chunkSize;

            Init();
        }

        private readonly MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _viewAccessor;
        private readonly long _mmfSize;
        private readonly long ChunkSize;

        //TODO: 可优化，实现读和写不同的viewAccessor，实现并发
        private readonly object _rwLock = new object();  //读写锁，读操作和写操作不能同时进行，否则如果在不同CHUNK中读，写会出现问题。
        public void Dispose()
        {
            DisposeViewAccessor();

            //if (_mmf != null)
            //    _mmf.Dispose();

        }

        public byte ReadByte(long offset)
        {
            ValidateOffset(offset);

            lock (_rwLock)
            {
                var mChunkNum = (offset) / ChunkSize;
                var mOffset = (offset) % ChunkSize;

                if (mChunkNum != _curChunkIdx)
                    LoadChunk(mChunkNum);

                return _viewAccessor.ReadByte(mOffset);
            }
        }

        public short ReadInt16(long offset)
        {
            ValidateOffset(offset, 2);

            var b1 = ReadByte(offset);
            var b2 = ReadByte(offset + 1);

            if (EndianHelper.IsBigEndian)
                return (short)((b1 << 8) + b2);
            else
                return (short)((b2 << 8) + b1);
        }

        public void WriteByte(long offset, byte data)
        {
            ValidateOffset(offset);

            lock (_rwLock)
            {
                var mChunkNum = offset / ChunkSize;
                var mOffset = offset % ChunkSize;

                if (mChunkNum != _curChunkIdx)
                    LoadChunk(mChunkNum);

                _viewAccessor.Write(mOffset, data);
            }
        }

        public void WriteInt16(long offset, short data)
        {
            ValidateOffset(offset, 2);

            var bytes = BitConverter.GetBytes(data);
            var idx = 0;
            foreach (var item in bytes)
                WriteByte(offset + (idx++), item);
        }

        public int Read(long offset, out byte[] ret, int byteNumToRead)
        {
            var num = byteNumToRead;
            if (offset + byteNumToRead > _mmfSize)
                num = (int)(_mmfSize - offset);

            ret = new byte[num];
            for (int i = 0; i < num; i++)
            {
                ret[i] = ReadByte(offset + i);
            }
            return num;
        }

        public int Write(long offset, byte[] ret, int byteNumToWrite)
        {
            if (ret == null || ret.Length < byteNumToWrite)
                throw new ArgumentException("ret");

            var num = byteNumToWrite;
            if (offset + byteNumToWrite > _mmfSize)
                num = (int)(_mmfSize - offset);

            for (int i = 0; i < num; i++)
            {
                WriteByte(offset + i, ret[i]);
            }
            return num;
        }

        #region private methods

        private long _chunkNum;
        private long _lastChunkSize;
        private long _curChunkIdx;

        private void ValidateOffset(long offset)
        {
            if (offset < 0 || offset > _mmfSize)
                throw new ArgumentOutOfRangeException("offset");
        }

        private void ValidateOffset(long offset, int num)
        {
            ValidateOffset(offset);
            if (num < 0)
                throw new ArgumentOutOfRangeException("num");

            if (offset + num > _mmfSize)
                throw new ArgumentOutOfRangeException("offset+num");
        }

        private void Init()
        {
            _chunkNum = _mmfSize / ChunkSize;
            _lastChunkSize = _mmfSize % ChunkSize;

            _curChunkIdx = -1;
            //LoadChunk(0);
        }

        private void LoadChunk(long chunkIdx)
        {
            if (chunkIdx < 0 || chunkIdx > _chunkNum)
                throw new ArgumentOutOfRangeException("chunkIdx");

            DisposeViewAccessor();

            if (chunkIdx != _chunkNum)
            {
                _viewAccessor = _mmf.CreateViewAccessor(chunkIdx * ChunkSize, ChunkSize);
            }
            else
            {
                _viewAccessor = _mmf.CreateViewAccessor(chunkIdx * ChunkSize, _lastChunkSize);
            }

            _curChunkIdx = chunkIdx;
        }

        private void DisposeViewAccessor()
        {
            if (_viewAccessor != null)
                _viewAccessor.Dispose();
        }

        #endregion
    }
}
