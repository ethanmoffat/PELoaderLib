// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;

namespace PELoaderLib
{
    #pragma warning disable CS1591

    public struct ImageFileHeader
    {
        public const int IMAGE_FILE_HEADER_SIZE = 20;

        public ushort Machine { get; private set; }
        public ushort NumberOfSections { get; private set; }
        public uint TimeDateStamp { get; private set; }
        public uint PointerToSymbolTable { get; private set; }
        public uint NumberOfSymbols { get; private set; }
        public ushort SizeOfOptionalHeader { get; private set; }
        public ushort Characteristics { get; private set; }

        public static ImageFileHeader CreateFromBytes(byte[] array)
        {
            if (array.Length != IMAGE_FILE_HEADER_SIZE)
                throw new ArgumentException("Array is not the correct size", "array");

            return new ImageFileHeader
            {
                Machine = BitConverter.ToUInt16(array, 0),
                NumberOfSections = BitConverter.ToUInt16(array, 2),
                TimeDateStamp = BitConverter.ToUInt16(array, 4),
                PointerToSymbolTable = BitConverter.ToUInt16(array, 8),
                NumberOfSymbols = BitConverter.ToUInt16(array, 12),
                SizeOfOptionalHeader = BitConverter.ToUInt16(array, 16),
                Characteristics = BitConverter.ToUInt16(array, 18)
            };
        }
    }
}
