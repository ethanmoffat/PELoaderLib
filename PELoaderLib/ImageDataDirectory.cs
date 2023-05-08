// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

namespace PELoaderLib
{
    #pragma warning disable CS1591

    public struct ImageDataDirectory
    {
        public const int SIZE_OF_IMAGE_DATA_DIRECTORY = 8;

        public uint VirtualAddress { get; }
        public uint Size { get; }

        public ImageDataDirectory(uint address, uint size) : this()
        {
            VirtualAddress = address;
            Size = size;
        }
    }
}
