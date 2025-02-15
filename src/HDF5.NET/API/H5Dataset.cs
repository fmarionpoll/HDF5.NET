using System;
using System.Reflection;

namespace HDF5.NET
{
    public partial class H5Dataset : H5AttributableObject
    {
        #region Properties

        public H5File File { get; }

        public H5Dataspace Space
        {
            get
            {
                if (_space is null)
                    _space = new H5Dataspace(this.InternalDataspace);

                return _space;
            }
        }

        public H5DataType Type
        {
            get
            {
                if (_type is null)
                    _type = new H5DataType(this.InternalDataType);

                return _type;
            }
        }

        public H5DataLayout Layout
        {
            get
            {
                if (_layout is null)
                    _layout = new H5DataLayout(this.InternalDataLayout);

                return _layout;
            }
        }

        public H5FillValue FillValue
        {
            get
            {
                if (_fillValue is null)
                    _fillValue = new H5FillValue(this.InternalFillValue);

                return _fillValue;
            }
        }

        #endregion

        #region Public

        public byte[] Read(
            Selection? fileSelection = default,
            Selection? memorySelection = default,
            ulong[]? memoryDims = default,
            H5DatasetAccess datasetAccess = default)
        {
            var result = this.Read<byte>(
                null,
                fileSelection,
                memorySelection,
                memoryDims,
                datasetAccess,
                skipShuffle: false);

            if (result is null)
                throw new Exception("The buffer is null. This should never happen.");

            return result;
        }

        public T[] Read<T>(
            Selection? fileSelection = default,
            Selection? memorySelection = default,
            ulong[]? memoryDims = default,
            H5DatasetAccess datasetAccess = default) where T : unmanaged
        {
            var result = this.Read<T>(
                null,
                fileSelection,
                memorySelection,
                memoryDims,
                datasetAccess,
                skipShuffle: false);

            if (result is null)
                throw new Exception("The buffer is null. This should never happen.");

            return result;
        }

        public void Read<T>(
            Memory<T> buffer,
            Selection? fileSelection = default,
            Selection? memorySelection = default,
            ulong[]? memoryDims = default,
            H5DatasetAccess datasetAccess = default) where T : unmanaged
        {
            this.Read(
                buffer,
                fileSelection,
                memorySelection,
                memoryDims,
                datasetAccess,
                skipShuffle: false);
        }

        public T[] ReadCompound<T>(
           Func<FieldInfo, string>? getName = default,
           Selection? fileSelection = default,
           Selection? memorySelection = default,
           ulong[]? memoryDims = default,
           H5DatasetAccess datasetAccess = default) where T : struct
        {
            var data = this.Read<byte>(
                null,
                fileSelection,
                memorySelection,
                memoryDims,
                datasetAccess,
                skipShuffle: false);

            if (data is null)
                throw new Exception("The buffer is null. This should never happen.");

            if (getName is null)
                getName = fieldInfo => fieldInfo.Name;

            return H5Utils.ReadCompound<T>(this.InternalDataType, this.InternalDataspace, this.Context.Superblock, data, getName);
        }

        public string[] ReadString(
            Selection? fileSelection = default,
            Selection? memorySelection = default,
            ulong[]? memoryDims = default,
            H5DatasetAccess datasetAccess = default)
        {
            var data = this.Read<byte>(
                null,
                fileSelection,
                memorySelection,
                memoryDims,
                datasetAccess,
                skipShuffle: false,
                skipTypeCheck: true);

            if (data is null)
                throw new Exception("The buffer is null. This should never happen.");

            return H5Utils.ReadString(this.InternalDataType, data, this.Context.Superblock);
        }

        #endregion
    }
}
