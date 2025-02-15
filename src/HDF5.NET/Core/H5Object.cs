﻿using System.IO;
using System.Linq;

namespace HDF5.NET
{
    abstract partial class H5Object
    {
        #region Fields

        private ObjectHeader? _header;
        private ObjectReferenceCountMessage? _objectReferenceCount;

        #endregion

        #region Constructors

        internal H5Object(H5Context context, NamedReference reference)
        {
            this.Context = context;
            this.Reference = reference;
        }

        internal H5Object(H5Context context, NamedReference reference, ObjectHeader header)
        {
            this.Context = context;
            this.Reference = reference;
            _header = header;
        }

        #endregion

        #region Properties

        internal H5Context Context { get; }

        internal uint ReferenceCount => this.GetReferenceCount();

        internal NamedReference Reference { get; set; }

        private ObjectReferenceCountMessage? ObjectReferenceCount
        {
            get
            {
                if (_objectReferenceCount is null)
                {
                    _objectReferenceCount = this.Header
                        .GetMessages<ObjectReferenceCountMessage>()
                        .FirstOrDefault();
                }

                return _objectReferenceCount;
            }
        }

        private protected ObjectHeader Header
        {
            get
            {
                if (_header is null)
                {
                    this.Context.Reader.Seek((long)this.Reference.Value, SeekOrigin.Begin);
                    _header = ObjectHeader.Construct(this.Context);
                }

                return _header;
            }
        }

        #endregion

        #region Methods

        private uint GetReferenceCount()
        {
            var header1 = this.Header as ObjectHeader1;

            if (header1 is not null)
                return header1.ObjectReferenceCount;

            else
                return this.ObjectReferenceCount is null
                    ? 1
                    : this.ObjectReferenceCount.ReferenceCount;
        }

        #endregion
    }
}
