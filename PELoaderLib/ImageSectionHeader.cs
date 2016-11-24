// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
using System.Text;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace PELoaderLib
{
    internal struct ImageSectionHeader
    {
        private const int NAME_SIZE_IN_BYTES = 8;
        internal const int SECTION_HEADER_SIZE = 40;

        internal string Name { get; private set; }

        private uint   _miscUnion;
        internal uint PhysicalAddress { get { return _miscUnion; } }
        internal uint VirtualSize { get { return _miscUnion; } }

        internal uint VirtualAddress { get; private set; }
        internal uint SizeOfRawData { get; private set; }
        internal uint PointerToRawData { get; private set; }
        internal uint PointerToRelocations { get; private set; }
        internal uint PointerToLinenumbers { get; private set; }
        internal ushort NumberOfRelocations { get; private set; }
        internal ushort NumberOfLinenumbers { get; private set; }
        internal uint Characteristics { get; private set; }

        internal static ImageSectionHeader CreateFromBytes(byte[] array)
        {
            if (array.Length != SECTION_HEADER_SIZE)
                throw new ArgumentException("Array is not the correct size", "array");

            return new ImageSectionHeader
            {
                Name = Encoding.ASCII.GetString(array, 0, NAME_SIZE_IN_BYTES).Replace("\0", ""),
                _miscUnion = BitConverter.ToUInt32(array, 8),
                VirtualAddress = BitConverter.ToUInt32(array, 12),
                SizeOfRawData = BitConverter.ToUInt32(array, 16),
                PointerToRawData = BitConverter.ToUInt32(array, 20),
                PointerToRelocations = BitConverter.ToUInt32(array, 24),
                PointerToLinenumbers = BitConverter.ToUInt32(array, 28),
                NumberOfRelocations = BitConverter.ToUInt16(array, 32),
                NumberOfLinenumbers = BitConverter.ToUInt16(array, 34),
                Characteristics = BitConverter.ToUInt32(array, 36)
            };
        }
    }
}
