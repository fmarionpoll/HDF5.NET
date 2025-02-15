﻿namespace HDF5.NET
{
    internal class FloatingPointPropertyDescription : DatatypePropertyDescription
    {
        #region Constructors

        public FloatingPointPropertyDescription(H5BinaryReader reader) : base(reader)
        {
            this.BitOffset = reader.ReadUInt16();
            this.BitPrecision = reader.ReadUInt16();
            this.ExponentLocation = reader.ReadByte();
            this.ExponentSize = reader.ReadByte();
            this.MantissaLocation = reader.ReadByte();
            this.MantissaSize = reader.ReadByte();
            this.ExponentBias = reader.ReadUInt32();
        }

        #endregion

        #region Properties

        public ushort BitOffset { get; set; }
        public ushort BitPrecision { get; set; }
        public byte ExponentLocation { get; set; }
        public byte ExponentSize { get; set; }
        public byte MantissaLocation { get; set; }
        public byte MantissaSize { get; set; }
        public uint ExponentBias { get; set; }

        #endregion
    }
}