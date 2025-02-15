﻿using System;

namespace HDF5.NET
{
    internal class FixedArrayIndexingInformation : IndexingInformation
    {
        #region Constructors

        public FixedArrayIndexingInformation(H5BinaryReader reader) : base(reader)
        {
            // page bits
            this.PageBits = reader.ReadByte();

            if (this.PageBits == 0)
                throw new Exception("Invalid fixed array creation parameter.");
        }

        #endregion

        #region Properties

        public byte PageBits { get; set; }

        #endregion
    }
}
