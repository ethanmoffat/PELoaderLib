// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

namespace PELoaderLib
{
    #pragma warning disable CS1591

    //#define IMAGE_DOS_SIGNATURE             0x5A4D      // MZ
    //#define IMAGE_OS2_SIGNATURE             0x454E      // NE
    //#define IMAGE_OS2_SIGNATURE_LE          0x454C      // LE
    //#define IMAGE_NT_SIGNATURE              0x00004550  // PE00

    public enum ImageHeaderType
    {
        SignatureDOS    = 0x5A4D,
        SignatureOS2    = 0x454E,
        SignatureOS2_LE = 0x454C,
        SignatureNT     = 0x00004550
    }
}
