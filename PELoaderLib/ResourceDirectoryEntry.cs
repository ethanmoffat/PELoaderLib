namespace PELoaderLib
{
	public struct ResourceDirectoryEntry
	{
		public const int ENTRY_SIZE = 8;

		public uint Name { get; private set; }

		public ResourceType NameAsResourceType { get { return (ResourceType) Name; } }
		
		public uint OffsetToData { get; private set; }

		public ResourceDirectoryEntry(uint name, uint offsetToSibling) : this()
		{
			Name = name;
			OffsetToData = offsetToSibling;
		}
	}
}
