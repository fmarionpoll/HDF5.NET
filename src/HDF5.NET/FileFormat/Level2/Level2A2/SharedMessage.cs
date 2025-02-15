﻿using System;

namespace HDF5.NET
{
    internal class SharedMessage : FileBlock
    {
        #region Fields

        private byte _version;

        #endregion

        #region Constructors

        public SharedMessage(H5BinaryReader reader, Superblock superblock) : base(reader)
        {
            // H5Oshared.c (H5O__shared_decode)

            // version
            this.Version = reader.ReadByte();

            // type
            if (this.Version == 3)
            {
                this.Type = (SharedMessageLocation)reader.ReadByte();
            }
            else
            {
                reader.ReadByte();
                this.Type = SharedMessageLocation.AnotherObjectsHeader;
            }

            // reserved
            if (this.Version == 1)
                reader.ReadBytes(6);

            // address
            if (this.Version == 1)
            {
                this.Address = superblock.ReadOffset(reader);
            }
            else
            {
                /* If this message is in the heap, copy a heap ID.
                 * Otherwise, it is a named datatype, so copy an H5O_loc_t.
                 */

                if (this.Type == SharedMessageLocation.SharedObjectHeaderMessageHeap)
                {
#warning implement this
                    throw new NotImplementedException("This code path is not yet implemented.");
                }
                else
                {
                    this.Address = superblock.ReadOffset(reader);
                }
            }
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
                if (!(1 <= value && value <= 3))
                    throw new FormatException($"Only version 1 version 2 and version 3 instances of type {nameof(SharedMessage)} are supported.");

                _version = value;
            }
        }

        public SharedMessageLocation Type { get; set; }

        public ulong Address { get; set; }

        public FractalHeapId FractalHeapId { get; set; }

        #endregion
    }
}
