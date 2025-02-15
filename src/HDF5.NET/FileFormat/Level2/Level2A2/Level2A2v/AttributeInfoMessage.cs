﻿using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace HDF5.NET
{
    internal class AttributeInfoMessage : Message
    {
        #region Fields

        private Superblock _superblock;
        private byte _version;

        #endregion

        #region Constructors

        public AttributeInfoMessage(H5BinaryReader reader, Superblock superblock) : base(reader)
        {
            _superblock = superblock;

            // version
            this.Version = reader.ReadByte();

            // flags
            this.Flags = (CreationOrderFlags)reader.ReadByte();

            // maximum creation index
            if (this.Flags.HasFlag(CreationOrderFlags.TrackCreationOrder))
                this.MaximumCreationIndex = reader.ReadUInt16();

            // fractal heap address
            this.FractalHeapAddress = superblock.ReadOffset(reader);

            // b-tree 2 name index address
            this.BTree2NameIndexAddress = superblock.ReadOffset(reader);

            // b-tree 2 creation order index address
            if (this.Flags.HasFlag(CreationOrderFlags.IndexCreationOrder))
                this.BTree2CreationOrderIndexAddress = superblock.ReadOffset(reader);
        }

        #endregion

        #region Properties

        public byte Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (value != 0)
                    throw new FormatException($"Only version 0 instances of type {nameof(AttributeInfoMessage)} are supported.");

                _version = value;
            }
        }

        public CreationOrderFlags Flags { get; set; } 
        public ushort MaximumCreationIndex { get; set; }
        public ulong FractalHeapAddress { get; set; }
        public ulong BTree2NameIndexAddress { get; set; }
        public ulong BTree2CreationOrderIndexAddress { get; set; }

        public FractalHeapHeader FractalHeap
        {
            get
            {
                this.Reader.Seek((long)this.FractalHeapAddress, SeekOrigin.Begin);
                return new FractalHeapHeader(this.Reader, _superblock);
            }
        }

        public BTree2Header<BTree2Record08> BTree2NameIndex
        {
            get
            {
                this.Reader.Seek((long)this.BTree2NameIndexAddress, SeekOrigin.Begin);
                return new BTree2Header<BTree2Record08>(this.Reader, _superblock, this.DecodeRecord08);
            }
        }

        public BTree2Header<BTree2Record09> BTree2CreationOrder
        {
            get
            {
                this.Reader.Seek((long)this.BTree2NameIndexAddress, SeekOrigin.Begin);
                return new BTree2Header<BTree2Record09>(this.Reader, _superblock, this.DecodeRecord09);
            }
        }

        #endregion

        #region Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BTree2Record08 DecodeRecord08() => new BTree2Record08(this.Reader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BTree2Record09 DecodeRecord09() => new BTree2Record09(this.Reader);

        #endregion
    }
}
