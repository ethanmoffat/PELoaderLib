// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;

namespace PELoaderLib
{
    public struct ResourceDirectory
    {
        public const int RESOURCE_DIRECTORY_SIZE = 16;

        public uint  Characteristics { get; private set; }
        public uint  TimeDateStamp { get; private set; }
        public ushort MajorVersion { get; private set; }
        public ushort MinorVersion { get; private set; }
        public ushort NumberOfNamedEntries { get; private set; }
        public ushort NumberOfIdEntries { get; private set; }

        public static ResourceDirectory CreateFromBytes(byte[] array)
        {
            if (array.Length != RESOURCE_DIRECTORY_SIZE)
                throw new ArgumentException("Array is not the correct size", "array");

            return new ResourceDirectory
            {
                Characteristics = BitConverter.ToUInt32(array, 0),
                TimeDateStamp = BitConverter.ToUInt32(array, 4),
                MajorVersion = BitConverter.ToUInt16(array, 8),
                MinorVersion = BitConverter.ToUInt16(array, 10),
                NumberOfNamedEntries = BitConverter.ToUInt16(array, 12),
                NumberOfIdEntries = BitConverter.ToUInt16(array, 14)
            };
        }
    }
}
