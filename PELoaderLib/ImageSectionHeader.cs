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
    public struct ImageSectionHeader
    {
        private const int NAME_SIZE_IN_BYTES = 8;
        public const int SECTION_HEADER_SIZE = 40;

        public string  Name { get; private set; }

        private uint   _miscUnion;
        public uint    PhysicalAddress { get { return _miscUnion; } }
        public uint    VirtualSize { get { return _miscUnion; } }

        public uint    VirtualAddress { get; private set; }
        public uint    SizeOfRawData { get; private set; }
        public uint    PointerToRawData { get; private set; }
        public uint    PointerToRelocations { get; private set; }
        public uint    PointerToLinenumbers { get; private set; }
        public ushort  NumberOfRelocations { get; private set; }
        public ushort  NumberOfLinenumbers { get; private set; }
        public uint    Characteristics { get; private set; }

        public static ImageSectionHeader CreateFromBytes(byte[] array)
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
