using System;
using System.Text;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace PELoaderLib
{
	public struct BitmapFileHeader
	{
		public const int BMP_FILE_HEADER_SIZE = 14;
		public const int BMP_INFO_HEADER_SIZE = 40;

		public short bfType { get; private set; }
		public uint bfSize { get; private set; }
		public short bfReserved1 { get; private set; }
		public short bfReserved2 { get; private set; }
		public uint bfOffBits { get { return BMP_FILE_HEADER_SIZE + BMP_INFO_HEADER_SIZE; } }

		public BitmapFileHeader(uint size)
			: this()
		{
			bfType = MakeType();
			bfSize = size;
		}

		public byte[] ToByteArray()
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
