namespace PELoaderLib
{
	public struct ImageDataDirectory
	{
		public const int SIZE_OF_IMAGE_DATA_DIRECTORY = 8;

		public uint VirtualAddress { get; private set; }
		public uint Size { get; private set; }

		public ImageDataDirectory(uint address, uint size) : this()
		{
			VirtualAddress = address;
			Size = size;
		}
	}
}
