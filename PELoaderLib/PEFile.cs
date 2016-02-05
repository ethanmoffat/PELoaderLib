

using System.IO.MemoryMappedFiles;

namespace PELoaderLib
{
	public class PEFile : IPEFile
	{
		public string FileName { get; private set; }

		private MemoryMappedFile _file;
		private MemoryMappedViewStream _fileStream;

		public PEFile(string filename)
		{
			FileName = filename;
			CreateFileStreams();
		}

		public bool Initialize()
		{
			return false;
		}

		private void CreateFileStreams()
		{
			_file = MemoryMappedFile.CreateFromFile(FileName);
			_fileStream = _file.CreateViewStream();
		}

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
