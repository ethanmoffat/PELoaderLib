## About

PELoaderLib is a library for loading compiled resources out of windows portable executable files.

Much of the implementation is based on the [description of the file format here](https://web.archive.org/web/20220518180743/http://www.csn.ul.ie/~caolan/publink/winresdump/winresdump/doc/pefile.html).

## Build Status

[![Build Status](https://ethanmoffat.visualstudio.com/EndlessClient/_apis/build/status/PELoaderLib%20Build?branchName=master)](https://ethanmoffat.visualstudio.com/EndlessClient/_build/latest?definitionId=15&branchName=master)

## Usage

Operations may throw a number of exceptions. For specific exception types, consult the source code.

### v1.5 and earlier
```csharp

using (IPEFile file = new PEFile("somefile.dll"))
{
    file.Initialize();

    var fileBytes = file.GetEmbeddedBitmapResourceByID(123);

    if (fileBytes.Length == 0)
    {
        throw new Exception("Failed to load resource!");
    }

    using var ms = new MemoryStream(fileBytes);
    using var image = (Bitmap)Image.FromStream(ms);
    //do something with image
}

```

### v1.6 and later
```csharp

// The following Nuget package is required for high performance conversion of Memory<byte> to Stream
// using CommunityToolkit.HighPerformance;

using (IPEFile file = new PEFile("somefile.dll"))
{
    file.Initialize();

    var bitmapData = file.GetEmbeddedBitmapResourceByID(123);

    if (fileBytes.Length == 0)
    {
        throw new Exception("Failed to load resource!");
    }

    // if using CommunityToolkit.HighPerformance:
    using var ms = fileBytes.AsStream();
    // otherwise:
    // using var ms = new MemoryStream(fileBytes.ToArray());
    using var image = (Bitmap)Image.FromStream(ms);
    //do something with image

    var resourceData = file.GetResourceByID(ResourceType.RCData, 123);
    var dataAsString = Encoding.Unicode.GetString(rawMetadata);
    // do something with string RCData
}

```

## Contributing

[Guidelines here](https://en.wikipedia.org/wiki/Don%27t_be_evil)