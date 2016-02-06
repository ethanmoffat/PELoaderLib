using System;
using System.Drawing;
using System.IO;
using System.Text;
using PELoaderLib;

namespace TestApp
{
	static class Program
	{
		private static IPEFile _file;
		private static void Main(string[] args)
		{
			using (_file = new PEFile(@"G:\Programs\EndlessOnline\gfx\gfx008.egf"))
			{
				_file.Initialize();

				var bytes = _file.GetEmbeddedBitmapResourceByID(101);
				bytes = AppendFileHeader(bytes);

				using (var ms = new MemoryStream(bytes))
				{
					var img = Image.FromStream(ms);
					img.Save("test.bmp");
				}
			}
		}

		private static byte[] AppendFileHeader(byte[] array)
		{
			var type = BitConverter.ToInt16(Encoding.ASCII.GetBytes("BM"), 0);
			var size = (uint)(array.Length + BitmapInfoHeader.STRUCT_SIZE);
			var retArray = new byte[size];

			var fileHeader = new BitmapFileHeader(type, size);
			fileHeader.ToByteArray().CopyTo(retArray, 0);
			array.CopyTo(retArray, BitmapFileHeader.STRUCT_SIZE);
			return retArray;
		}

		private static BitmapInfoHeader GetBitmapInfoHeader(byte[] array)
		{
			var sz = BitConverter.ToUInt32(array, 0);
			if (sz != BitmapInfoHeader.STRUCT_SIZE)
				throw new Exception();

			return new BitmapInfoHeader(BitConverter.ToInt32(array, 4),
										BitConverter.ToInt32(array, 8),
										BitConverter.ToInt16(array, 12),
										BitConverter.ToInt16(array, 14),
										BitConverter.ToUInt32(array, 16),
										BitConverter.ToUInt32(array, 20),
										BitConverter.ToInt32(array, 24),
										BitConverter.ToInt32(array, 28),
										BitConverter.ToUInt32(array, 32),
										BitConverter.ToUInt32(array, 36));
		}

		private struct BitmapFileHeader
		{
			public const int STRUCT_SIZE = 14;

			public short bfType;
			public uint bfSize;
			public short bfReserved1;
			public short bfReserved2;
			public uint bfOffBits;

			public BitmapFileHeader(short type, uint size)
			{
				bfType = type;
				bfSize = size;
				bfReserved1 = bfReserved2 = 0;
				bfOffBits = STRUCT_SIZE + BitmapInfoHeader.STRUCT_SIZE;
			}

			public byte[] ToByteArray()
			{
				var bytes = new byte[STRUCT_SIZE];
				BitConverter.GetBytes(bfType).CopyTo(bytes, 0);
				BitConverter.GetBytes(bfSize).CopyTo(bytes, 2);
				BitConverter.GetBytes(bfReserved1).CopyTo(bytes, 6);
				BitConverter.GetBytes(bfReserved2).CopyTo(bytes, 8);
				BitConverter.GetBytes(bfOffBits).CopyTo(bytes, 10);
				return bytes;
			}
		}

		private struct BitmapInfoHeader
		{
			public const int STRUCT_SIZE = 40;

			public int biWidth;
			public int biHeight;
			public short biPlanes;
			public short biBitCount;
			public uint biCompression;
			public uint biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public uint biClrUsed;
			public uint biClrImportant;
			
			public BitmapInfoHeader(int width, int height, short planes, short bitcount, uint compression,
									uint sizeImage, int xPelsPerMeter, int yPelsPerMeter, uint clrUsed, uint clrImportant)
			{
				biWidth = width;
				biHeight = height;
				biPlanes = planes;
				biBitCount = bitcount;
				biCompression = compression;
				biSizeImage = sizeImage;
				biXPelsPerMeter = xPelsPerMeter;
				biYPelsPerMeter = yPelsPerMeter;
				biClrUsed = clrUsed;
				biClrImportant = clrImportant;
			}
		}
	}
}
