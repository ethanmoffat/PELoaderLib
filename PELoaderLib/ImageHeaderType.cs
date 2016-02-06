﻿namespace PELoaderLib
{
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
