// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace PELoaderLib
{
    internal struct ResourceDataEntry
    {
        internal const int ENTRY_SIZE = 16;

        internal uint OffsetToData { get; private set; }
        internal uint Size { get; private set; }
        internal uint CodePage { get; private set; }
        internal uint Reserved { get; private set; }

        internal static ResourceDataEntry CreateFromBytes(byte[] array)
        {
            if (array.Length != ENTRY_SIZE)
                throw new ArgumentException("Array is not the correct size", "array");

            return new ResourceDataEntry
            {
                OffsetToData = BitConverter.ToUInt32(array, 0),
                Size = BitConverter.ToUInt32(array, 4),
                CodePage = BitConverter.ToUInt32(array, 8),
                Reserved = BitConverter.ToUInt32(array, 12)
            };
        }
    }
}
