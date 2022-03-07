// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
using System.Text;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace PELoaderLib
{
    public enum BitmapVersion
    {
        BitmapInfoHeader = BitmapFileHeader.BMP_INFO_HEADER_SIZE,
        BitmapV3InfoHeader = BitmapFileHeader.BMP_INFO_HEADER_SIZE_V3
    }

    internal struct BitmapFileHeader
    {
        internal const int BMP_FILE_HEADER_SIZE = 14;
        internal const int BMP_INFO_HEADER_SIZE = 40;
        internal const int BMP_INFO_HEADER_SIZE_V3 = 56;

        internal short bfType { get; }
        internal uint bfSize { get; }
        internal short bfReserved1 { get; }
        internal short bfReserved2 { get; }
        internal uint bfOffBits { get { return BMP_FILE_HEADER_SIZE + (uint)Version; } }
        internal BitmapVersion Version { get; }

        internal BitmapFileHeader(BitmapVersion version, uint size)
            : this()
        {
            Version = version;
            bfSize = size;

            bfType = MakeType();
        }

        internal byte[] ToByteArray()
        {
            var bytes = new byte[BMP_FILE_HEADER_SIZE];
            BitConverter.GetBytes(bfType).CopyTo(bytes, 0);
            BitConverter.GetBytes(bfSize).CopyTo(bytes, 2);
            BitConverter.GetBytes(bfReserved1).CopyTo(bytes, 6);
            BitConverter.GetBytes(bfReserved2).CopyTo(bytes, 8);
            BitConverter.GetBytes(bfOffBits).CopyTo(bytes, 10);
            return bytes;
        }

        private short MakeType()
        {
            var typeArray = Encoding.ASCII.GetBytes("BM");
            return BitConverter.ToInt16(typeArray, 0);
        }
    }
}
