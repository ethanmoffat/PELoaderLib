

using System;

namespace PELoaderLib
{
	public interface IPEFile : IDisposable
	{
		string FileName { get; }

		bool Initialize();
	}
}
