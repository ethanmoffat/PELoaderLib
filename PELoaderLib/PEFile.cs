using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace PELoaderLib
{
	//Much of this file was ported to C# from examples at 
	//	This specification is for a version of Windows from 1993 (Windows NT 3.1).
	//	http://www.csn.ul.ie/~caolan/publink/winresdump/winresdump/doc/pefile.html

	public class PEFile : IPEFile
	{
		private const int SIZE_OF_NT_SIGNATURE = 4;

		public string FileName { get; private set; }
		public bool Initialized { get; private set; }
		public ImageDOSHeader DOSHeader { get; private set; }
		public ImageHeaderType HeaderType { get; private set; }
		public ImageFileHeader ImageHeader { get; private set; }
		public OptionalFileHeader OptionalHeader { get; private set; }

		private MemoryMappedFile _file;
		private MemoryMappedViewStream _fileStream;
		private readonly List<ImageSectionHeader> _sectionHeaders;

		public PEFile(string filename)
		{
			FileName = filename;
			CreateFileStreams();
			_sectionHeaders = new List<ImageSectionHeader>();
		}

		public void Initialize()
		{
			if (_file == null || _fileStream == null ||
			    !_fileStream.CanRead || !_fileStream.CanSeek)
				throw new InvalidOperationException();

			Initialized = false;
			_fileStream.Seek(0, SeekOrigin.Begin);
			_sectionHeaders.Clear();

			DOSHeader = GetImageDOSHeader();
			if (DOSHeader.e_magic != ImageDOSHeader.DOS_MAGIC_NUMBER)
				throw new IOException("The PE file has an invalid magic number");
			
			HeaderType = GetHeaderType();
			ImageHeader = GetImageFileHeader();
			OptionalHeader = GetOptionalFileHeader();

			LoadSectionHeaders();

			Initialized = true;
		}

		public byte[] GetEmbeddedBitmapResourceByID(int intResource)
		{
			if (!Initialized)
				throw new InvalidOperationException("The PE File must be initialized first");

			var offset = GetOffsetForSectionFromDirectoryEntry(DataDirectoryEntry.Resource);
			return GetBitmapResource(offset, intResource);
		}

		private void CreateFileStreams()
		{
			if (_file != null || _fileStream != null)
				throw new InvalidOperationException();

			_file = MemoryMappedFile.CreateFromFile(FileName);
			_fileStream = _file.CreateViewStream();
		}

		#region Load Helpers

		private ImageDOSHeader GetImageDOSHeader()
		{
			var headerArray = new byte[ImageDOSHeader.DOS_HEADER_LENGTH];
			_fileStream.Read(headerArray, 0, headerArray.Length);
			return ImageDOSHeader.CreateFromBytes(headerArray);
		}

		private ImageHeaderType GetHeaderType()
		{
			SetStreamToDOSHeaderOffset();

			var typeArray = new byte[sizeof (UInt32)];
			_fileStream.Read(typeArray, 0, typeArray.Length);

			var type = BitConverter.ToInt32(typeArray, 0);
			var lowWordOfType = (ushort) (type & 0x0000FFFF);

			if (lowWordOfType == (int) ImageHeaderType.SignatureOS2 ||
			    lowWordOfType == (int) ImageHeaderType.SignatureOS2_LE)
				return (ImageHeaderType) lowWordOfType;

			if (type == (int) ImageHeaderType.SignatureNT)
				return ImageHeaderType.SignatureNT;

			return ImageHeaderType.SignatureDOS;
		}

		private ImageFileHeader GetImageFileHeader()
		{
			SetStreamToStartOfImageFileHeader();

			var headerArray = new byte[ImageFileHeader.IMAGE_FILE_HEADER_SIZE];
			_fileStream.Read(headerArray, 0, headerArray.Length);
			return ImageFileHeader.CreateFromBytes(headerArray);
		}

		private OptionalFileHeader GetOptionalFileHeader()
		{
			SetStreamToStartOfOptionalFileHeader();

			var headerArray = new byte[OptionalFileHeader.OPTIONAL_FILE_HEADER_SIZE];
			_fileStream.Read(headerArray, 0, headerArray.Length);
			return OptionalFileHeader.CreateFromBytes(headerArray);
		}

		private void LoadSectionHeaders()
		{
			SetStreamToStartOfSectionHeaders();

			for (int i = 0; i < ImageHeader.NumberOfSections; ++i)
			{
				var sectionHeaderArray = new byte[ImageSectionHeader.SECTION_HEADER_SIZE];
				_fileStream.Read(sectionHeaderArray, 0, sectionHeaderArray.Length);
				_sectionHeaders.Add(ImageSectionHeader.CreateFromBytes(sectionHeaderArray));
			}
		}

		private uint GetOffsetForSectionFromDirectoryEntry(DataDirectoryEntry entry)
		{
			var directoryEntry = OptionalHeader.DataDirectory[(int) entry];

			foreach (var sectionHeader in _sectionHeaders)
			{
				if (sectionHeader.VirtualAddress <= directoryEntry.VirtualAddress &&
					sectionHeader.VirtualAddress + sectionHeader.SizeOfRawData > directoryEntry.VirtualAddress)
				{
					return sectionHeader.PointerToRawData;
				}
			}

			throw new KeyNotFoundException("The directory entry is not present in the section header table");
		}

		private byte[] GetBitmapResource(uint offsetIntoFile, int resourceID)
		{
			_fileStream.Seek((int)offsetIntoFile, SeekOrigin.Begin);

			var resourceInfo = new byte[ResourceDirectory.RESOURCE_DIRECTORY_SIZE];
			_fileStream.Read(resourceInfo, 0, resourceInfo.Length);
			var resourceTableHeader = ResourceDirectory.CreateFromBytes(resourceInfo);

			var startOfResourceSection = offsetIntoFile + ResourceDirectory.RESOURCE_DIRECTORY_SIZE;
			for (int i = 0; i < resourceTableHeader.NumberOfIdEntries; ++i)
			{
				var level1Entry = GetResourceDirectoryEntryAtCurrentFilePosition();
				if (level1Entry.NameAsResourceType == ResourceType.Bitmap)
				{
					_fileStream.Seek(startOfResourceSection + (level1Entry.OffsetToData & 0x7FFFFFFF), SeekOrigin.Begin);

					ResourceDirectoryEntry level2Entry;
					do
					{
						level2Entry = GetResourceDirectoryEntryAtCurrentFilePosition();
						if (level2Entry.Name == resourceID)
						{
							_fileStream.Seek(startOfResourceSection + (level2Entry.OffsetToData & 0x7FFFFFFF), SeekOrigin.Begin);
							var level3Entry = GetResourceDirectoryEntryAtCurrentFilePosition();

							_fileStream.Seek(offsetIntoFile + (level3Entry.OffsetToData & 0x7FFFFFFF), SeekOrigin.Begin);
							var resourceDataEntry = GetResourceDataEntryAtCurrentFilePosition();

							_fileStream.Seek(offsetIntoFile + resourceDataEntry.OffsetToData - 0xF000, SeekOrigin.Begin);
							var bytes = new byte[resourceDataEntry.Size];
							_fileStream.Read(bytes, 0, bytes.Length);
							return bytes;
						}
					} while (level2Entry.Name != 0);

					break;
				}
			}

			return new byte[0];
		}

		private ResourceDirectoryEntry GetResourceDirectoryEntryAtCurrentFilePosition()
		{
			var entryArray = new byte[ResourceDirectoryEntry.ENTRY_SIZE];
			_fileStream.Read(entryArray, 0, entryArray.Length);
			return new ResourceDirectoryEntry(BitConverter.ToUInt32(entryArray, 0),
											  BitConverter.ToUInt32(entryArray, 4));
		}

		private ResourceDataEntry GetResourceDataEntryAtCurrentFilePosition()
		{
			var entryArray = new byte[ResourceDataEntry.ENTRY_SIZE];
			_fileStream.Read(entryArray, 0, entryArray.Length);
			return ResourceDataEntry.CreateFromBytes(entryArray);
		}

		#endregion

		#region Stream Manipulation

		private void SetStreamToDOSHeaderOffset()
		{
			_fileStream.Seek(DOSHeader.e_lfanew, SeekOrigin.Begin);
		}

		private void SetStreamToStartOfImageFileHeader()
		{
			SetStreamToDOSHeaderOffset();
			_fileStream.Seek(SIZE_OF_NT_SIGNATURE, SeekOrigin.Current);
		}

		private void SetStreamToStartOfOptionalFileHeader()
		{
			SetStreamToStartOfImageFileHeader();
			_fileStream.Seek(ImageFileHeader.IMAGE_FILE_HEADER_SIZE, SeekOrigin.Current);
		}

		private void SetStreamToStartOfSectionHeaders()
		{
			SetStreamToStartOfOptionalFileHeader();
			_fileStream.Seek(OptionalFileHeader.OPTIONAL_FILE_HEADER_SIZE, SeekOrigin.Current);
		}

		#endregion

		#region IDisposable

		~PEFile()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_fileStream != null)
					_fileStream.Dispose();
				_fileStream = null;

				if (_file != null)
					_file.Dispose();
				_file = null;
			}
		}

		#endregion
	}
}
