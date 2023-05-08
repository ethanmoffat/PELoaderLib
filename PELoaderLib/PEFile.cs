// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;

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
        private MemoryMappedViewAccessor _fileAccessor;

        private readonly List<ImageSectionHeader> _sectionHeaders;
        private readonly Dictionary<DataDirectoryEntry, ImageSectionHeader> _sectionMap;

        private readonly Dictionary<ResourceType, ResourceDirectoryEntry> _levelOneCache;
        private readonly Dictionary<ResourceType, Dictionary<int, ResourceDirectoryEntry>> _levelTwoCache;
        private readonly Dictionary<ResourceType, Dictionary<int, List<(int CultureID, ResourceDirectoryEntry Entry)>>> _levelThreeCache;

        public PEFile(string filename)
        {
            FileName = filename;
            CreateFileStreams();
            _sectionHeaders = new List<ImageSectionHeader>();
            _sectionMap = new Dictionary<DataDirectoryEntry, ImageSectionHeader>();
            _levelOneCache = new Dictionary<ResourceType, ResourceDirectoryEntry>();
            _levelTwoCache = new Dictionary<ResourceType, Dictionary<int, ResourceDirectoryEntry>>();
            _levelThreeCache = new Dictionary<ResourceType, Dictionary<int, List<(int, ResourceDirectoryEntry)>>>();
        }

        /// <inheritdoc />
        public void Initialize()
        {
            if (_file == null || _fileAccessor == null || !_fileAccessor.CanRead)
                throw new InvalidOperationException();

            Initialized = false;
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

            var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
            foreach (var resourceType in resourceTypes)
            {
                BuildLevelTwoCache(resourceType);

                if (_levelTwoCache.ContainsKey(resourceType))
                {
                    foreach (var level2Entry in _levelTwoCache[resourceType].Values)
                        BuildLevelThreeCache(resourceType, level2Entry);
                }
            }

            Initialized = true;
        }

        /// <inheritdoc />
        public ReadOnlyMemory<byte> GetEmbeddedBitmapResourceByID(int intResource, int cultureID = -1)
        {
            if (!Initialized)
                throw new InvalidOperationException("The PE File must be initialized first");

            var bytes = GetResourceByID(ResourceType.Bitmap, intResource, cultureID);

            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException(string.Format("Error loading the resource: could not find the specified resource for ID {0} and Culture {1}", intResource, cultureID));

            return PrependBitmapFileHeaderToResourceBytes(bytes);
        }

        /// <inheritdoc />
        public ReadOnlySpan<byte> GetResourceByID(ResourceType resourceType, int intResource, int cultureID = -1)
        {
            if (!Initialized)
                throw new InvalidOperationException("The PE File must be initialized first");

            if (!_levelOneCache.ContainsKey(resourceType))
                return new byte[0];

            return FindMatchingLevel2ResourceEntry(resourceType, intResource, cultureID);
        }

        #region Initialize Helpers

        private void CreateFileStreams()
        {
            if (_file != null || _fileAccessor != null)
                throw new InvalidOperationException();

            _file = MemoryMappedFile.CreateFromFile(FileName);
            _fileAccessor = _file.CreateViewAccessor();
        }

        private ImageDOSHeader GetImageDOSHeader()
        {
            var headerArray = new byte[ImageDOSHeader.DOS_HEADER_LENGTH];
            _fileAccessor.ReadArray(0, headerArray, 0, headerArray.Length);
            return ImageDOSHeader.CreateFromBytes(headerArray);
        }

        private ImageHeaderType GetHeaderType()
        {
            var offset = DOSHeader.e_lfanew;

            var typeArray = new byte[sizeof(uint)];
            _fileAccessor.ReadArray(offset, typeArray, 0, typeArray.Length);

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
            var offset = DOSHeader.e_lfanew + SIZE_OF_NT_SIGNATURE;

            var headerArray = new byte[ImageFileHeader.IMAGE_FILE_HEADER_SIZE];
            _fileAccessor.ReadArray(offset, headerArray, 0, headerArray.Length);
            return ImageFileHeader.CreateFromBytes(headerArray);
        }

        private OptionalFileHeader GetOptionalFileHeader()
        {
            var offset = DOSHeader.e_lfanew + SIZE_OF_NT_SIGNATURE + ImageFileHeader.IMAGE_FILE_HEADER_SIZE;

            var headerArray = new byte[OptionalFileHeader.OPTIONAL_FILE_HEADER_SIZE];
            _fileAccessor.ReadArray(offset, headerArray, 0, headerArray.Length);
            return OptionalFileHeader.CreateFromBytes(headerArray);
        }

        private void LoadSectionHeaders()
        {
            var offset = DOSHeader.e_lfanew + SIZE_OF_NT_SIGNATURE + ImageFileHeader.IMAGE_FILE_HEADER_SIZE + OptionalFileHeader.OPTIONAL_FILE_HEADER_SIZE;

            for (int i = 0; i < ImageHeader.NumberOfSections; ++i)
            {
                var sectionHeaderArray = new byte[ImageSectionHeader.SECTION_HEADER_SIZE];
                _fileAccessor.ReadArray(offset, sectionHeaderArray, 0, sectionHeaderArray.Length);
                _sectionHeaders.Add(ImageSectionHeader.CreateFromBytes(sectionHeaderArray));

                offset += ImageSectionHeader.SECTION_HEADER_SIZE;
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

        private unsafe Memory<byte> PrependBitmapFileHeaderToResourceBytes(ReadOnlySpan<byte> resourceBytes)
        {
            var headerSize = BitConverter.ToInt32(resourceBytes.Slice(0, 4).ToArray(), 0);
            var bitmapHeaderBytes = resourceBytes.Slice(0, headerSize).ToArray();

            var totalFileSize = (uint)(resourceBytes.Length + BitmapFileHeader.BMP_FILE_HEADER_SIZE);
            var bitmapFileHeader = new BitmapFileHeader(totalFileSize, bitmapHeaderBytes).ToByteArray();

            var retArray = new byte[totalFileSize];
            fixed (byte* headerSource = bitmapFileHeader)
            fixed (byte* source = resourceBytes)
            fixed (byte* target = retArray)
            {
                Unsafe.CopyBlock(target, headerSource, BitmapFileHeader.BMP_FILE_HEADER_SIZE);
                Unsafe.CopyBlock(target + BitmapFileHeader.BMP_FILE_HEADER_SIZE, source, (uint)resourceBytes.Length);
            }

            return retArray;
        }

        private void BuildLevelOneCache()
        {
            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];

            var offset = resourceSectionHeader.PointerToRawData;
            var resourceTableHeader = GetResourceDirectoryHeaderTable(offset);
            offset += ResourceDirectory.RESOURCE_DIRECTORY_SIZE;

            //skip over named entries in resource section (since this is explicitly by resource ID)
            for (int i = 0; i < resourceTableHeader.NumberOfNamedEntries; ++i)
            {
                GetResourceDirectoryEntryAtOffset(offset);
                offset += ResourceDirectoryEntry.ENTRY_SIZE;
            }

            for (int i = 0; i < resourceTableHeader.NumberOfIdEntries; ++i)
            {
                var level1Entry = GetResourceDirectoryEntryAtOffset(offset);
                _levelOneCache[level1Entry.NameAsResourceType] = level1Entry;

                offset += ResourceDirectoryEntry.ENTRY_SIZE;
            }
        }

        private void BuildLevelTwoCache(ResourceType resourceType)
        {
            if (!_levelOneCache.ContainsKey(resourceType))
                return;

            _levelTwoCache[resourceType] = new Dictionary<int, ResourceDirectoryEntry>();

            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];
            var resourceSectionFileOffset = resourceSectionHeader.PointerToRawData;
            var resourceDirectoryFileOffset = resourceSectionFileOffset + ResourceDirectory.RESOURCE_DIRECTORY_SIZE;

            var level1Entry = _levelOneCache[resourceType];
            var offset = resourceDirectoryFileOffset + (level1Entry.OffsetToData & 0x7FFFFFFF);

            ResourceDirectoryEntry level2Entry;
            do
            {
                level2Entry = GetResourceDirectoryEntryAtOffset(offset);
                _levelTwoCache[resourceType][(int)level2Entry.Name] = level2Entry;
                offset += ResourceDirectoryEntry.ENTRY_SIZE;
            } while (level2Entry.Name != 0);
        }

        private void BuildLevelThreeCache(ResourceType resourceType, ResourceDirectoryEntry level2Entry)
        {
            if (!_levelTwoCache.ContainsKey(resourceType))
                return;

            if (!_levelThreeCache.ContainsKey(resourceType))
                _levelThreeCache[resourceType] = new Dictionary<int, List<(int CultureID, ResourceDirectoryEntry Entry)>>();

            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];

            var resourceDirectoryFileOffset = resourceSectionHeader.PointerToRawData + ResourceDirectory.RESOURCE_DIRECTORY_SIZE;

            var offset = resourceDirectoryFileOffset + (level2Entry.OffsetToData & 0x7FFFFFFF);

            var l3CacheRef = _levelThreeCache[resourceType];

            ResourceDirectoryEntry level3Entry;
            do
            {
                level3Entry = GetResourceDirectoryEntryAtOffset(offset);
                if (!l3CacheRef.ContainsKey((int)level2Entry.Name))
                    l3CacheRef.Add((int)level2Entry.Name, new List<(int, ResourceDirectoryEntry)>());

                l3CacheRef[(int)level2Entry.Name].Add(((int)level3Entry.Name, level3Entry));
                offset += ResourceDirectoryEntry.ENTRY_SIZE;
            } while (level3Entry.Name != 0);
        }

        private ReadOnlySpan<byte> FindMatchingLevel2ResourceEntry(ResourceType resourceType, int resourceID, int cultureID)
        {
            if (!_levelTwoCache.ContainsKey(resourceType))
                return new byte[0];

            return GetResourceDataForCulture(resourceType, resourceID, cultureID);
        }

        private ReadOnlySpan<byte> GetResourceDataForCulture(ResourceType resourceType, int resourceID, int cultureID)
        {
            var resourceSectionHeader = _sectionMap[DataDirectoryEntry.Resource];
            var l3CacheRef = _levelThreeCache[resourceType];

            if (!l3CacheRef.ContainsKey(resourceID) || (cultureID >= 0 && !l3CacheRef[resourceID].Any(x => x.CultureID == cultureID)))
                return new byte[0];

            if (cultureID < 0)
            {
                cultureID = l3CacheRef[resourceID].First().CultureID;
            }

            var resourceDataEntry = GetResourceDataEntryAtOffset(l3CacheRef[resourceID].First(x => x.CultureID == cultureID).Entry.OffsetToData);
            var resourceDataOffset = resourceSectionHeader.PointerToRawData + resourceDataEntry.OffsetToData - resourceSectionHeader.VirtualAddress;

            unsafe
            {
                byte* filePointer = null;
                _fileAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref filePointer);
                return new ReadOnlySpan<byte>((void*)((ulong)filePointer + resourceDataOffset), (int)resourceDataEntry.Size);
            }
        }

        private ResourceDirectory GetResourceDirectoryHeaderTable(uint offset)
        {
            var resourceInfo = new byte[ResourceDirectory.RESOURCE_DIRECTORY_SIZE];
            _fileAccessor.ReadArray(offset, resourceInfo, 0, resourceInfo.Length);
            var resourceTableHeader = ResourceDirectory.CreateFromBytes(resourceInfo);
            return resourceTableHeader;
        }

        private ResourceDirectoryEntry GetResourceDirectoryEntryAtOffset(uint offset)
        {
            var directoryEntryArray = new byte[ResourceDirectoryEntry.ENTRY_SIZE];
            _fileAccessor.ReadArray(offset, directoryEntryArray, 0, directoryEntryArray.Length);
            return new ResourceDirectoryEntry(BitConverter.ToUInt32(directoryEntryArray, 0),
                                              BitConverter.ToUInt32(directoryEntryArray, 4));
        }

        private ResourceDataEntry GetResourceDataEntryAtOffset(uint offset)
        {
            var resourceSectionFileOffset = _sectionMap[DataDirectoryEntry.Resource].PointerToRawData;
            //_fileStream.Seek(resourceSectionFileOffset + offset, SeekOrigin.Begin);

            var dataEntryArray = new byte[ResourceDataEntry.ENTRY_SIZE];
            _fileAccessor.ReadArray(resourceSectionFileOffset + offset, dataEntryArray, 0, dataEntryArray.Length);
            return ResourceDataEntry.CreateFromBytes(dataEntryArray);
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
                _fileAccessor?.Dispose();
                _fileAccessor= null;

                _file?.Dispose();
                _file = null;
            }
        }

        #endregion
    }
}
