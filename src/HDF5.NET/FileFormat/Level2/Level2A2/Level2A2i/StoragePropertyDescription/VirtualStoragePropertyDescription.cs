﻿namespace HDF5.NET
{
    internal class VirtualStoragePropertyDescription : StoragePropertyDescription
    {
        #region Constructors

        public VirtualStoragePropertyDescription(H5BinaryReader reader, Superblock superblock) : base(reader)
        {
            // address
            this.Address = superblock.ReadOffset(reader);

            // index
            this.Index = reader.ReadUInt32();
        }

        #endregion

        #region Properties

        public uint Index { get; set; }

        #endregion
    }
}
