// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

namespace PELoaderLib
{
    internal struct ResourceDirectoryEntry
    {
        internal const int ENTRY_SIZE = 8;

        internal uint Name { get; }

        internal ResourceType NameAsResourceType { get { return (ResourceType)Name; } }

        internal uint OffsetToData { get; }

        internal ResourceDirectoryEntry(uint name, uint offsetToSibling)
            : this()
        {
            Name = name;
            OffsetToData = offsetToSibling;
        }
    }
}