﻿using System;
using System.IO;
using System.Text;

namespace HDF5.NET
{
    internal class FractalHeapHeader : FileBlock
    {
        #region Fields

        private byte _version;
        private Superblock _superblock;

        #endregion

        #region Constructors

        public FractalHeapHeader(H5BinaryReader reader, Superblock superblock) : base(reader)
        {
            _superblock = superblock;

            // signature
            var signature = reader.ReadBytes(4);
            H5Utils.ValidateSignature(signature, FractalHeapHeader.Signature);

            // version
            this.Version = reader.ReadByte();

            // heap ID length
            this.HeapIdLength = reader.ReadUInt16();

            // I/O filter encoder length
            this.IOFilterEncodedLength = reader.ReadUInt16();

            // flags
            this.Flags = (FractalHeapHeaderFlags)reader.ReadByte();

            /* next group */

            // managed objects maximum size
            this.ManagedObjectsMaximumSize = reader.ReadUInt32();

            // next huge object id
            this.NextHugeObjectId = superblock.ReadLength(reader);

            // huge objects BTree2 address
            this.HugeObjectsBTree2Address = superblock.ReadOffset(reader);

            // managed blocks free space amount
            this.ManagedBlocksFreeSpaceAmount = superblock.ReadLength(reader);

            // managed block free space manager address
            this.ManagedBlockFreeSpaceManagerAddress = superblock.ReadOffset(reader);

            // heap managed space amount
            this.HeapManagedSpaceAmount = superblock.ReadLength(reader);

            // heap allocated managed space amount
            this.HeapAllocatedManagedSpaceAmount = superblock.ReadLength(reader);

            // managed space direct block allocation iterator offset
            this.ManagedSpaceDirectBlockAllocationIteratorOffset = superblock.ReadLength(reader);

            // heap managed objects count
            this.HeapManagedObjectsCount = superblock.ReadLength(reader);

            // heap huge objects size
            this.HeapHugeObjectsSize = superblock.ReadLength(reader);

            // heap huge objects cound
            this.HeapHugeObjectsCount = superblock.ReadLength(reader);

            // heap tiny objects size
            this.HeapTinyObjectsSize = superblock.ReadLength(reader);

            // heap tiny objects count
            this.HeapTinyObjectsCount = superblock.ReadLength(reader);

            /* next group */

            // table width
            this.TableWidth = reader.ReadUInt16();

            // starting block size
            this.StartingBlockSize = superblock.ReadLength(reader);

            // maximum direct block size
            this.MaximumDirectBlockSize = superblock.ReadLength(reader);

            // maximum heap size
            this.MaximumHeapSize = reader.ReadUInt16();

            // root indirect block rows starting number
            this.RootIndirectBlockRowsStartingNumber = reader.ReadUInt16();

            // root block address
            this.RootBlockAddress = superblock.ReadOffset(reader);

            // root indirect block rows count
            this.RootIndirectBlockRowsCount = reader.ReadUInt16();

            /* next group */

            // filtered root direct block size, I/O filter mask and I/O filter info
            if (this.IOFilterEncodedLength > 0)
            {
                this.FilteredRootDirectBlockSize = superblock.ReadLength(reader);
                this.IOFilterMask = reader.ReadUInt32();
                this.IOFilterInfo = new FilterPipelineMessage(reader);
            }           

            // checksum
            this.Checksum = reader.ReadUInt32();

            // cache some values
            this.CalculateBlockSizeTables();
            this.CalculateHugeObjectsData();
        }

        #endregion

        #region Properties

        public static byte[] Signature { get; } = Encoding.ASCII.GetBytes("FRHP");

        public byte Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (value != 0)
                    throw new FormatException($"Only version 0 instances of type {nameof(FractalHeapHeader)} are supported.");

                _version = value;
            }
        }

        public ushort HeapIdLength { get; set; }
        public ushort IOFilterEncodedLength { get; set; }
        public FractalHeapHeaderFlags Flags { get; set; }
        public uint ManagedObjectsMaximumSize { get; set; }

        public ulong NextHugeObjectId { get; set; }
        public ulong HugeObjectsBTree2Address { get; set; }
        public ulong ManagedBlocksFreeSpaceAmount { get; set; }
        public ulong ManagedBlockFreeSpaceManagerAddress { get; set; }
        public ulong HeapManagedSpaceAmount { get; set; }
        public ulong HeapAllocatedManagedSpaceAmount { get; set; }
        public ulong ManagedSpaceDirectBlockAllocationIteratorOffset { get; set; }
        public ulong HeapManagedObjectsCount { get; set; }

        public ulong HeapHugeObjectsSize { get; set; }
        public ulong HeapHugeObjectsCount { get; set; }

        public ulong HeapTinyObjectsSize { get; set; }
        public ulong HeapTinyObjectsCount { get; set; }

        public ushort TableWidth { get; set; }
        public ulong StartingBlockSize { get; set; }
        public ulong MaximumDirectBlockSize { get; set; }
        public ushort MaximumHeapSize { get; set; }
        public ushort RootIndirectBlockRowsStartingNumber { get; set; }
        public ulong RootBlockAddress { get; set; }
        public ushort RootIndirectBlockRowsCount { get; set; }

        public ulong FilteredRootDirectBlockSize { get; set; }
        public uint IOFilterMask { get; set; }
        public FilterPipelineMessage? IOFilterInfo { get; set; }
        public uint Checksum { get; set; }

        public ulong[] RowBlockSizes { get; private set; }
        public ulong[] RowBlockOffsets { get; private set; }

        public uint StartingBits { get; private set; }
        public uint FirstRowBits { get; private set; }
        public uint MaxDirectRows { get; private set; }

        public bool HugeIdsAreDirect { get; private set; }
        public byte HugeIdsSize { get; private set; }

        public bool TinyObjectsAreExtended { get; private set; }

        #endregion

        #region Methods

        // from H5HF__man_op_real
        public ulong GetAddress(ManagedObjectsFractalHeapId heapId)
        {
            FractalHeapDirectBlock directBlock;
            ulong directBlockSize;
            ulong directBlockAddress;

            /* Check for root direct block */
            var isDirectBlock = this.RootIndirectBlockRowsCount == 0;

            if (isDirectBlock)
            {
                /* Set direct block info */
                directBlockSize = this.StartingBlockSize;
                directBlockAddress = this.RootBlockAddress;
            }
            else
            {
                /* Look up indirect block containing direct block */
                var (indirectBlock, entry) = this.Locate(heapId.Offset);

                /* Set direct block info */
                directBlockSize = this.RowBlockSizes[entry / this.TableWidth];
                directBlockAddress = indirectBlock.Entries[entry].Address;
            }

            this.Reader.Seek((long)directBlockAddress, SeekOrigin.Begin);
            directBlock = new FractalHeapDirectBlock(this, this.Reader, _superblock);

            /* Compute offset of object within block */
            if (heapId.Offset >= directBlock.BlockOffset + directBlockSize)
                throw new Exception("Object start offset overruns end of direct block.");

            var blockOffset = heapId.Offset - directBlock.BlockOffset;

            /* Check for object's offset in the direct block prefix information */
            if (blockOffset < directBlock.HeaderSize)
                throw new Exception("Object located in prefix of direct block.");

            /* Check for object's length overrunning the end of the direct block */
            if (blockOffset + heapId.Length > directBlockSize)
                throw new Exception("Object overruns end of direct block.");

            return directBlockAddress + blockOffset;
        }

        // from H5HF__man_dblock_locate
        private (FractalHeapIndirectBlock IndirectBlock, ulong entry) Locate(ulong offset)
        {
            var (row, column) = this.Lookup(offset);

            this.Reader.Seek((long)this.RootBlockAddress, SeekOrigin.Begin);
            var indirectBlock = new FractalHeapIndirectBlock(this, this.Reader, _superblock, this.RootIndirectBlockRowsCount);

            uint entry;

            while (row >= this.MaxDirectRows)
            {
                /* Compute # of rows in child indirect block */
                var nrows = (uint)Math.Log(this.RowBlockSizes[row], 2) - this.FirstRowBits + 1;

                if (nrows >= indirectBlock.RowCount)
                    throw new Exception("Child fractal heap block must be smaller than its parent.");

                /* Compute indirect block's entry */
                entry = row * this.TableWidth + column;

                /* Locate child indirect block */
                var indirectBlockEntry = indirectBlock.Entries[entry];
 
                /* Use new indirect block */
                this.Reader.Seek((long)indirectBlockEntry.Address, SeekOrigin.Begin);
                indirectBlock = new FractalHeapIndirectBlock(this, this.Reader, _superblock, nrows);

                /* Look up row & column in new indirect block for object */
                (row, column) = this.Lookup(offset - indirectBlock.BlockOffset);

                if (row >= indirectBlock.RowCount)
                    throw new Exception("Child fractal heap block must be smaller than its parent.");
            }

            entry = row * this.TableWidth + column;

            return (indirectBlock, entry);
        }

        // from H5HF_dtable_lookup
        private (uint Row, uint Column) Lookup(ulong offset)
        {
            uint row;
            uint column;

            if (offset < this.StartingBlockSize * this.TableWidth)
            {
                row = 0;
                column = (uint)(offset / this.StartingBlockSize);
            }
            else
            {
                var highBit = (uint)Math.Log(offset, 2);
                ulong offMask = (ulong)(1 << (int)highBit);
                row = highBit - this.FirstRowBits + 1;
                column = (uint)((offset - offMask) / this.RowBlockSizes[row]);
            }

            return (row, column);
        }

        private void CalculateBlockSizeTables()
        {
            // from H5HFdtable.c
            this.StartingBits = (uint)Math.Log(this.StartingBlockSize, 2);
            this.FirstRowBits = (uint)(this.StartingBits + Math.Log(this.TableWidth, 2));

            var maxDirectBits = (uint)Math.Log(this.MaximumDirectBlockSize, 2);
            this.MaxDirectRows = maxDirectBits - this.StartingBits + 2;

            var maxRootRows = this.MaximumHeapSize - this.FirstRowBits;

            this.RowBlockSizes = new ulong[maxRootRows];
            this.RowBlockOffsets = new ulong[maxRootRows];

            var tmpBlockSize = this.StartingBlockSize;
            var accumulatedBlockOffset = this.StartingBlockSize * this.TableWidth;

            this.RowBlockSizes[0] = tmpBlockSize;
            this.RowBlockOffsets[0] = 0;

            for (ulong i = 1; i < maxRootRows; i++)
            {
                this.RowBlockSizes[i] = tmpBlockSize;
                this.RowBlockOffsets[i] = accumulatedBlockOffset;
                tmpBlockSize *= 2;
                accumulatedBlockOffset *= 2;
            }
        }

        private void CalculateHugeObjectsData()
        {
            // H5HFhuge.c (H5HF_huge_init)

            // with filter
            if (this.IOFilterEncodedLength > 0)
            {
                // length of fractal heap id for huge objects (sub-type 4)
                var actualLength = _superblock.OffsetsSize + _superblock.LengthsSize + 4 + _superblock.LengthsSize;

                if ((this.HeapIdLength - 1) >= actualLength)
                {
                    /* Indicate that v2 B-tree doesn't have to be used to locate object */
                    this.HugeIdsAreDirect = true;

                    /* Set the size of 'huge' object IDs */
#warning Correct? Why is here not "+4"?
                    this.HugeIdsSize = (byte)(_superblock.OffsetsSize + _superblock.LengthsSize + _superblock.LengthsSize);
                }
                else
                {
                    /* Indicate that v2 B-tree must be used to access object */
                    this.HugeIdsAreDirect = false;
                }
            }
            // without filter
            else
            {
                // length of fractal heap id for huge objects (sub-type 3)
                var actualLength = _superblock.OffsetsSize + _superblock.LengthsSize;

                if ((this.HeapIdLength - 1) >= actualLength)
                {
                    /* Indicate that v2 B-tree doesn't have to be used to locate object */
                    this.HugeIdsAreDirect = true;

                    /* Set the size of 'huge' object IDs */
                    this.HugeIdsSize = (byte)actualLength;
                }
                else
                {
                    /* Indicate that v2 B-tree must be used to access object */
                    this.HugeIdsAreDirect = false;
                }
            }

            // set huge id size for indirect access
            if (!this.HugeIdsAreDirect)
            {
                /* Set the size of 'huge' object ID */
                if ((this.HeapIdLength - 1) < sizeof(ulong))
                    this.HugeIdsSize = (byte)(this.HeapIdLength - 1);
                else
                    this.HugeIdsSize = sizeof(ulong);
            }
        }

        private void CalculateTinyObjectsData()
        {
            // H5HFtiny.c (H5HF_tiny_init)

            /* Compute information about 'tiny' objects for the heap */

            /* Check if tiny objects need an extra byte for their length
             * (account for boundary condition when length of an object would need an
             *  extra byte, but using that byte means that the extra length byte is
             *  unnecessary)
             */
            if ((this.HeapIdLength - 1) <= 16)
            {
                this.TinyObjectsAreExtended = false;
            }
            else if ((this.HeapIdLength - 1) <= (16 + 1))
            {
                this.TinyObjectsAreExtended = false;
            }
            else
            {
                this.TinyObjectsAreExtended = true;
            }
        }

        #endregion
    }
}
