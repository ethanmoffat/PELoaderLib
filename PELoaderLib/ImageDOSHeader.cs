// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace PELoaderLib
{
    #pragma warning disable CS1591

    /// <summary>
    /// Structure that mimics IMAGE_DOS_HEADER (see winnt.h)
    /// </summary>
    public struct ImageDOSHeader
    {
        public const int DOS_HEADER_LENGTH = 64;
        public const ushort DOS_MAGIC_NUMBER = 0x5A4D;

        public ushort e_magic { get; private set; }         // Magic number
        public ushort e_cblp { get; private set; }          // Bytes on last page of file
        public ushort e_cp { get; private set; }            // Pages in file
        public ushort e_crlc { get; private set; }          // Relocations
        public ushort e_cparhdr { get; private set; }       // Size of header in paragraphs
        public ushort e_minalloc { get; private set; }      // Minimum extra paragraphs needed
        public ushort e_maxalloc { get; private set; }      // Maximum extra paragraphs needed
        public ushort e_ss { get; private set; }            // Initial (relative) SS value
        public ushort e_sp { get; private set; }            // Initial SP value
        public ushort e_csum { get; private set; }          // Checksum
        public ushort e_ip { get; private set; }            // Initial IP value
        public ushort e_cs { get; private set; }            // Initial (relative) CS value
        public ushort e_lfarlc { get; private set; }        // File address of relocation table
        public ushort e_ovno { get; private set; }          // Overlay number
        public ushort[] e_res { get; private set; }         // Reserved words
        public ushort e_oemid { get; private set; }         // OEM identifier (for e_oeminfo)
        public ushort e_oeminfo { get; private set; }       // OEM information; e_oemid specific
        public ushort[] e_res2 { get; private set; }        // Reserved words
        public uint e_lfanew { get; private set; }          // File address of new exe header

        public static ImageDOSHeader CreateFromBytes(byte[] array)
        {
            if (array.Length != DOS_HEADER_LENGTH)
                throw new ArgumentException("Array is not the correct size", "array");

            return new ImageDOSHeader
            {
                e_magic = BitConverter.ToUInt16(array, 0),
                e_cblp = BitConverter.ToUInt16(array, 2),
                e_cp = BitConverter.ToUInt16(array, 4),
                e_crlc = BitConverter.ToUInt16(array, 6),
                e_cparhdr = BitConverter.ToUInt16(array, 8),
                e_minalloc = BitConverter.ToUInt16(array, 10),
                e_maxalloc = BitConverter.ToUInt16(array, 12),
                e_ss = BitConverter.ToUInt16(array, 14),
                e_sp = BitConverter.ToUInt16(array, 16),
                e_csum = BitConverter.ToUInt16(array, 18),
                e_ip = BitConverter.ToUInt16(array, 20),
                e_cs = BitConverter.ToUInt16(array, 22),
                e_lfarlc = BitConverter.ToUInt16(array, 24),
                e_ovno = BitConverter.ToUInt16(array, 26),
                e_res = new[]
                {
                    BitConverter.ToUInt16(array, 28),
                    BitConverter.ToUInt16(array, 30),
                    BitConverter.ToUInt16(array, 32),
                    BitConverter.ToUInt16(array, 34)
                },
                e_oemid = BitConverter.ToUInt16(array, 36),
                e_oeminfo = BitConverter.ToUInt16(array, 38),
                e_res2 = new[]
                {

                    BitConverter.ToUInt16(array, 40),
                    BitConverter.ToUInt16(array, 42),
                    BitConverter.ToUInt16(array, 44),
                    BitConverter.ToUInt16(array, 46),
                    BitConverter.ToUInt16(array, 48),
                    BitConverter.ToUInt16(array, 50),
                    BitConverter.ToUInt16(array, 52),
                    BitConverter.ToUInt16(array, 54),
                    BitConverter.ToUInt16(array, 56),
                    BitConverter.ToUInt16(array, 58)
                },
                e_lfanew = BitConverter.ToUInt32(array, 60)
            };
        }
    }
}
