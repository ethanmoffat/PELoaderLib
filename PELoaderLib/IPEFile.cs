// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;

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

        /// <summary>
        /// Initialize the PE file. This must be called prior to getting resources.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Get an embedded bitmap resource, prepending a bitmap file header to the resource data
        /// </summary>
        /// <param name="intResource">The integer ID of the resource to get</param>
        /// <param name="cultureID">The culture ID of the resource to get, or -1 to get the first culture available</param>
        /// <returns>Byte array of resource data including a prepended bitmap file header</returns>
        byte[] GetEmbeddedBitmapResourceByID(int intResource, int cultureID = -1);

        /// <summary>
        /// Get an embedded resource from this PE file
        /// </summary>
        /// <param name="resourceType">The type of the resource to get</param>
        /// <param name="intResource">The integer ID of the resource to get</param>
        /// <param name="cultureID">The culture ID of the resource to get, or -1 to get the first culture available</param>
        /// <returns>Byte array of resource data</returns>
        byte[] GetResourceByID(ResourceType resourceType, int intResource, int cultureID = -1);
    }
}
