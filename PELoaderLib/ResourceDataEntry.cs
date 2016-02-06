using System;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace PELoaderLib
{
	public struct ResourceDataEntry
	{
		public const int ENTRY_SIZE = 16;

		public uint OffsetToData { get; private set; }
		public uint Size { get; private set; }
		public uint CodePage { get; private set; }
		public uint Reserved { get; private set; }

		public static ResourceDataEntry CreateFromBytes(byte[] array)
		{
			if (array.Length != ENTRY_SIZE)
				throw new ArgumentException("Array is not the correct size", "array");

			return new ResourceDataEntry
			{
				OffsetToData = BitConverter.ToUInt32(array, 0),
				Size = BitConverter.ToUInt32(array, 4),
				CodePage = BitConverter.ToUInt32(array, 8),
				Reserved = BitConverter.ToUInt32(array, 12)
			};
		}
	}
}
