// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
using System.Linq;

namespace PELoaderLib
{
    #pragma warning disable CS1591

    public struct OptionalFileHeader
    {
        public const int OPTIONAL_FILE_HEADER_SIZE = 224;
        public const int IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;
        //
        // Standard fields.
        //
        public ushort  Magic { get; private set; }
        public byte    MajorLinkerVersion { get; private set; }
        public byte    MinorLinkerVersion { get; private set; }
        public uint    SizeOfCode { get; private set; }
        public uint    SizeOfInitializedData { get; private set; }
        public uint    SizeOfUninitializedData { get; private set; }
        public uint    AddressOfEntryPoint { get; private set; }
        public uint    BaseOfCode { get; private set; }
        public uint    BaseOfData { get; private set; }
        //
        // NT additional fields.
        //
        public uint    ImageBase { get; private set; }
        public uint    SectionAlignment { get; private set; }
        public uint    FileAlignment { get; private set; }
        public ushort  MajorOperatingSystemVersion { get; private set; }
        public ushort  MinorOperatingSystemVersion { get; private set; }
        public ushort  MajorImageVersion { get; private set; }
        public ushort  MinorImageVersion { get; private set; }
        public ushort  MajorSubsystemVersion { get; private set; }
        public ushort  MinorSubsystemVersion { get; private set; }
        public uint    Reserved1 { get; private set; }
        public uint    SizeOfImage { get; private set; }
        public uint    SizeOfHeaders { get; private set; }
        public uint    CheckSum { get; private set; }
        public ushort  Subsystem { get; private set; }
        public ushort  DllCharacteristics { get; private set; }
        public uint    SizeOfStackReserve { get; private set; }
        public uint    SizeOfStackCommit { get; private set; }
        public uint    SizeOfHeapReserve { get; private set; }
        public uint    SizeOfHeapCommit { get; private set; }
        public uint    LoaderFlags { get; private set; }
        public uint    NumberOfRvaAndSizes { get; private set; }
        public ImageDataDirectory[] DataDirectory { get; private set; }

        public static OptionalFileHeader CreateFromBytes(byte[] array)
        {
            if (array.Length != OPTIONAL_FILE_HEADER_SIZE)
                throw new ArgumentException("Array is not the correct size", "array");

            return new OptionalFileHeader
            {
                Magic = BitConverter.ToUInt16(array, 0),
                MajorLinkerVersion = array[2],
                MinorLinkerVersion = array[3],
                SizeOfCode = BitConverter.ToUInt32(array, 4),
                SizeOfInitializedData = BitConverter.ToUInt32(array, 8),
                SizeOfUninitializedData = BitConverter.ToUInt32(array, 12),
                AddressOfEntryPoint = BitConverter.ToUInt32(array, 16),
                BaseOfCode = BitConverter.ToUInt32(array, 20),
                BaseOfData = BitConverter.ToUInt32(array, 24),

                ImageBase = BitConverter.ToUInt32(array, 28),
                SectionAlignment = BitConverter.ToUInt32(array, 32),
                FileAlignment = BitConverter.ToUInt32(array, 36),
                MajorOperatingSystemVersion = BitConverter.ToUInt16(array, 40),
                MinorOperatingSystemVersion = BitConverter.ToUInt16(array, 42),
                MajorImageVersion = BitConverter.ToUInt16(array, 44),
                MinorImageVersion = BitConverter.ToUInt16(array, 46),
                MajorSubsystemVersion = BitConverter.ToUInt16(array, 48),
                MinorSubsystemVersion = BitConverter.ToUInt16(array, 50),
                Reserved1 = BitConverter.ToUInt32(array, 52),
                SizeOfImage = BitConverter.ToUInt32(array, 56),
                SizeOfHeaders = BitConverter.ToUInt32(array, 60),
                CheckSum = BitConverter.ToUInt32(array, 64),
                Subsystem = BitConverter.ToUInt16(array, 68),
                DllCharacteristics = BitConverter.ToUInt16(array, 70),
                SizeOfStackReserve = BitConverter.ToUInt32(array, 72),
                SizeOfStackCommit = BitConverter.ToUInt32(array, 76),
                SizeOfHeapReserve = BitConverter.ToUInt32(array, 80),
                SizeOfHeapCommit = BitConverter.ToUInt32(array, 84),
                LoaderFlags = BitConverter.ToUInt32(array, 88),
                NumberOfRvaAndSizes = BitConverter.ToUInt32(array, 92),
                DataDirectory = new ImageDataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES]
                    .Select((directory, i) =>
                        new ImageDataDirectory(BitConverter.ToUInt32(array, 96 + i*ImageDataDirectory.SIZE_OF_IMAGE_DATA_DIRECTORY),
                                               BitConverter.ToUInt32(array, 100 + i*ImageDataDirectory.SIZE_OF_IMAGE_DATA_DIRECTORY)))
                    .ToArray()
            };
        }
    }
}
