// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

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