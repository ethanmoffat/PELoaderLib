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
		byte[] GetEmbeddedBitmapResourceByID(int intResource, int cultureID = 0);
	}
}
