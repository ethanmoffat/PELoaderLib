// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace PELoaderLib
{
    //Much of this file was ported to C# from examples at:
    //	http://www.csn.ul.ie/~caolan/publink/winresdump/winresdump/doc/pefile.html
    //This specification is for a version of Windows from 1993 (Windows NT 3.1).
    //Doesn't seem like much has changed since then though, since everything seems to work!

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
        private readonly Dictionary<DataDirectoryEntry, ImageSectionHeader> _sectionMap;

        private readonly Dictionary<ResourceType, ResourceDirectoryEntry> _levelOneCache;
        private readonly Dictionary<int, ResourceDirectoryEntry> _levelTwoCache;
        private readonly Dictionary<int, Dictionary<int, ResourceDirectoryEntry>> _levelThreeCache;

        public PEFile(string filename)
        {
            FileName = filename;
            CreateFileStreams();
            _sectionHeaders = new List<ImageSectionHeader>();
            _sectionMap = new Dictionary<DataDirectoryEntry, ImageSectionHeader>();
            _levelOneCache = new Dictionary<ResourceType, ResourceDirectoryEntry>();
            _levelTwoCache = new Dictionary<int, ResourceDirectoryEntry>();
            _levelThreeCache = new Dictionary<int, Dictionary<int, ResourceDirectoryEntry>>();
        }

        public void Initialize()
        {
            if (_file == null || _fileStream == null ||
                !_fileStream.CanRead || !_fileStream.CanSeek)
                throw new InvalidOperationException();

            Initialized = false;
            _fileStream.Seek(0, SeekOrigin.Begin);
            _sectionHeaders.Clear();
            _sectionMap.Clear();

            DOSHeader = GetImageDOSHeader();
            if (DOSHeader.e_magic != ImageDOSHeader.DOS_MAGIC_NUMBER)
                throw new IOException("The PE file has an invalid magic number");

            HeaderType = GetHeaderType();
            ImageHeader = GetImageFileHeader();
            OptionalHeader = GetOptionalFileHeader();

            LoadSectionHeaders();
            MapDirectoryEntriesToSectionHeaders();

            BuildLevelOneCache();
            BuildLevelTwoCache();
            foreach (var level2Entry in _levelTwoCache.Values)
                BuildLevelThreeCache(level2Entry);

            Initialized = true;
        }

        public byte[] GetEmbeddedBitmapResourceByID(int intResource, BitmapVersion version = BitmapVersion.BitmapInfoHeader, int cultureID = 0)
        {
            if (!Initialized)
                throw new InvalidOperationException("The PE File must be initialized first");

            var bytes = GetBitmapResourceByID(intResource, cultureID);

            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException(string.Format("Error loading the resource: could not find the specified resource for ID {0} and Culture {1}", intResource, cultureID));

            return PrependBitmapFileHeaderToResourceBytes(version, bytes);
        }

        #region Initialize Helpers

        private void CreateFileStreams()
        {
            if (_file != null || _fileStream != null)
                throw new InvalidOperationException();

            _file = MemoryMappedFile.CreateFromFile(FileName);
            _fileStream = _file.CreateViewStream();
        }

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

        private void MapDirectoryEntriesToSectionHeaders()
        {
            var directoryEntryTypes = (DataDirectoryEntry[])Enum.GetValues(typeof (DataDirectoryEntry));
            foreach (var type in directoryEntryTypes)
            {
                ImageSectionHeader section;
                if (!TryGetSectionHeaderFromDirectoryEntry(type, out section))
                    continue;

                _sectionMap.Add(type, section);
            }
        }

        private bool TryGetSectionHeaderFromDirectoryEntry(DataDirectoryEntry entry, out ImageSectionHeader retSectionHeader)
        {
            var directoryEntry = OptionalHeader.DataDirectory[(int) entry];

            foreach (var sectionHeader in _sectionHeaders)
            {
                if (sectionHeader.VirtualAddress <= directoryEntry.VirtualAddress &&
                    sectionHeader.VirtualAddress + sectionHeader.SizeOfRawData > directoryEntry.VirtualAddress)
                {
                    retSectionHeader = sectionHeader;
                    return true;
                }
            }

            retSectionHeader = new ImageSectionHeader();
            return false;
        }

        #endregion

        #region Resource Helpers

        private byte[] PrependBitmapFileHeaderToResourceBytes(BitmapVersion version, byte[] array)
        {
            var totalFileSize = (uint)(array.Length + BitmapFileHeader.BMP_FILE_HEADER_SIZE);
            var retArray = new byte[totalFileSize];

            new BitmapFileHeader(version, totalFileSize).ToByteArray().CopyTo(retArray, 0);
            array.CopyTo(retArray, BitmapFileHeader.BMP_FILE_HEADER_SIZE);

            return retArray;
        }

        private void BuildLevelOneCache()
        {
            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];

            SetStreamToStartOfResourceSection(resourceSectionHeader);
            var resourceTableHeader = GetResourceDirectoryHeaderTable();

            //skip over named entries in resource section (since this is explicitly by resource ID)
            for (int i = 0; i < resourceTableHeader.NumberOfNamedEntries; ++i)
            {
                GetResourceDirectoryEntryAtCurrentFilePosition();
            }

            for (int i = 0; i < resourceTableHeader.NumberOfIdEntries; ++i)
            {
                var level1Entry = GetResourceDirectoryEntryAtCurrentFilePosition();
                _levelOneCache[level1Entry.NameAsResourceType] = level1Entry;
            }
        }

        private void BuildLevelTwoCache()
        {
            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];
            var resourceSectionFileOffset = resourceSectionHeader.PointerToRawData;
            var resourceDirectoryFileOffset = resourceSectionFileOffset + ResourceDirectory.RESOURCE_DIRECTORY_SIZE;

            var level1Entry = _levelOneCache[ResourceType.Bitmap];
            _fileStream.Seek(resourceDirectoryFileOffset + (level1Entry.OffsetToData & 0x7FFFFFFF), SeekOrigin.Begin);

            ResourceDirectoryEntry level2Entry;
            do
            {
                level2Entry = GetResourceDirectoryEntryAtCurrentFilePosition();
                _levelTwoCache[(int)level2Entry.Name] = level2Entry;
            } while (level2Entry.Name != 0);
        }

        private void BuildLevelThreeCache(ResourceDirectoryEntry level2Entry)
        {
            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];

            var resourceDirectoryFileOffset = resourceSectionHeader.PointerToRawData + ResourceDirectory.RESOURCE_DIRECTORY_SIZE;

            _fileStream.Seek(resourceDirectoryFileOffset + (level2Entry.OffsetToData & 0x7FFFFFFF), SeekOrigin.Begin);

            ResourceDirectoryEntry level3Entry;
            do
            {
                level3Entry = GetResourceDirectoryEntryAtCurrentFilePosition();
                if (!_levelThreeCache.ContainsKey((int)level2Entry.Name))
                    _levelThreeCache.Add((int)level2Entry.Name, new Dictionary<int, ResourceDirectoryEntry>());

                _levelThreeCache[(int)level2Entry.Name][(int)level3Entry.Name] = level3Entry;
            } while (level3Entry.Name != 0);
        }

        private byte[] GetBitmapResourceByID(int resourceID, int cultureID)
        {
            if (!_levelOneCache.ContainsKey(ResourceType.Bitmap))
                return new byte[0];

            return FindMatchingLevel2ResourceEntry(resourceID, cultureID);
        }

        private byte[] FindMatchingLevel2ResourceEntry(int resourceID, int cultureID)
        {
            if (!_levelTwoCache.ContainsKey(resourceID))
                return new byte[0];

            return GetResourceDataForCulture(_levelTwoCache[resourceID], resourceID, cultureID);
        }

        private byte[] GetResourceDataForCulture(ResourceDirectoryEntry level2Entry, int resourceID, int cultureID)
        {
            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];

            if (!_levelThreeCache.ContainsKey(resourceID) || !_levelThreeCache[resourceID].ContainsKey(cultureID))
                return new byte[0];

            var resourceDataEntry = GetResourceDataEntryAtOffset(_levelThreeCache[resourceID][cultureID].OffsetToData);
            var bitmapDataOffset = resourceSectionHeader.PointerToRawData + resourceDataEntry.OffsetToData - resourceSectionHeader.VirtualAddress;

            _fileStream.Seek(bitmapDataOffset, SeekOrigin.Begin);
            var bytes = new byte[resourceDataEntry.Size];
            _fileStream.Read(bytes, 0, bytes.Length);

            return bytes;
        }

        private ResourceDirectory GetResourceDirectoryHeaderTable()
        {
            var resourceInfo = new byte[ResourceDirectory.RESOURCE_DIRECTORY_SIZE];
            _fileStream.Read(resourceInfo, 0, resourceInfo.Length);
            var resourceTableHeader = ResourceDirectory.CreateFromBytes(resourceInfo);
            return resourceTableHeader;
        }

        private ResourceDirectoryEntry GetResourceDirectoryEntryAtCurrentFilePosition()
        {
            var directoryEntryArray = new byte[ResourceDirectoryEntry.ENTRY_SIZE];
            _fileStream.Read(directoryEntryArray, 0, directoryEntryArray.Length);
            return new ResourceDirectoryEntry(BitConverter.ToUInt32(directoryEntryArray, 0),
                                              BitConverter.ToUInt32(directoryEntryArray, 4));
        }

        private ResourceDataEntry GetResourceDataEntryAtOffset(uint offset)
        {
            var resourceSectionFileOffset = _sectionMap[DataDirectoryEntry.Resource].PointerToRawData;
            _fileStream.Seek(resourceSectionFileOffset + offset, SeekOrigin.Begin);

            var dataEntryArray = new byte[ResourceDataEntry.ENTRY_SIZE];
            _fileStream.Read(dataEntryArray, 0, dataEntryArray.Length);
            return ResourceDataEntry.CreateFromBytes(dataEntryArray);
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

        private void SetStreamToStartOfResourceSection(ImageSectionHeader resourceHeader)
        {
            _fileStream.Seek((int)resourceHeader.PointerToRawData, SeekOrigin.Begin);
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
            GC.SuppressFinalize(this);
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
