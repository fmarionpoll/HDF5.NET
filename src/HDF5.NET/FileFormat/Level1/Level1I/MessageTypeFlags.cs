﻿using System;

namespace HDF5.NET
{
    [Flags]
    internal enum MessageTypeFlags : ushort
    {
        Dataspace = 1,
        Datatype = 2,
        FillValue = 4,
        FilterPipeline = 8,
        Attribute = 16
    }
}
