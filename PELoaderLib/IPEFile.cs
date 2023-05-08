// Original Work Copyright (c) Ethan Moffat 2016
// This file is subject to the MIT License
// For additional details, see the LICENSE file

using System;

namespace PELoaderLib
{
    /// <summary>
    /// Representation of a windows PE file
    /// </summary>
    public interface IPEFile : IDisposable
    {
        /// <summary>
        /// Gets the filename of the file on disk
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets a value indicating whether the file has been successfully initialized
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// Gets the DOS header of the PE file
        /// </summary>
        ImageDOSHeader DOSHeader { get; }

        /// <summary>
        /// Gets the header type of the PE file
        /// </summary>
        ImageHeaderType HeaderType { get; }

        /// <summary>
        /// Gets the image header of the PE file
        /// </summary>
        ImageFileHeader ImageHeader { get; }

        /// <summary>
        /// Gets the optional header of the PE file
        /// </summary>
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
        /// <returns>Memory segment including bitmap resource data and prepended bitmap file header</returns>
        ReadOnlyMemory<byte> GetEmbeddedBitmapResourceByID(int intResource, int cultureID = -1);

        /// <summary>
        /// Get an embedded resource from this PE file
        /// </summary>
        /// <param name="resourceType">The type of the resource to get</param>
        /// <param name="intResource">The integer ID of the resource to get</param>
        /// <param name="cultureID">The culture ID of the resource to get, or -1 to get the first culture available</param>
        /// <returns>Span of bytes representing the PE resource</returns>
        ReadOnlySpan<byte> GetResourceByID(ResourceType resourceType, int intResource, int cultureID = -1);
    }
}
