## About

PELoaderLib is a library for loading compiled resources out of windows portable executable files.

Much of the implementation is based on the [description of the file format here](http://www.csn.ul.ie/~caolan/publink/winresdump/winresdump/doc/pefile.html).

## Build Status

[![Build status](https://ethanmoffat.visualstudio.com/EndlessClient/_apis/build/status/PELoaderLib%20PR%20Gate)](https://ethanmoffat.visualstudio.com/EndlessClient/_build/latest?definitionId=3)

## Usage

Operations may throw a number of exceptions. For specific exception types, consult the source code.

```C#

using (IPEFile file = new PEFile("somefile.dll"))
{
    file.Initialize();

    var fileBytes = file.GetEmbeddedBitmapResourceByID(123);

    if (fileBytes.Length == 0)
    {
        throw new Exception("Failed to load resource!");
    }

    using(var ms = new MemoryStream(fileBytes))
    using(var image = (Bitmap)Image.FromStream(ms))
    {
        //do something with image
    }
}

```

## Contributing

[Guidelines here](https://en.wikipedia.org/wiki/Don%27t_be_evil)