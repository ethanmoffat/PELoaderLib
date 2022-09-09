// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace PELoaderLib
{
    public interface IPEFile : IDisposable
    {
        string FileName { get; }
        bool Initialized { get; }
        ImageDOSHeader DOSHeader { get; }
        ImageHeaderType HeaderType { get; }
        ImageFileHeader ImageHeader { get; }
        OptionalFileHeader OptionalHeader { get; }

        void Initialize();
        byte[] GetEmbeddedBitmapResourceByID(int intResource, int cultureID = -1);
    }
}
