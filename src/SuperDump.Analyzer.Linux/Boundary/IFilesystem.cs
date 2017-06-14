﻿using System.Collections.Generic;
using System.Net.Http;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Boundary {
	public interface IFilesystem {
		IFileInfo GetFile(string path);
		void CreateSymbolicLink(string targetDebugFile, string debugSymbolPath);
		List<string> FilesInDirectory(string directory);
		string Md5FromFile(string path);
		void HttpContentToFile(HttpContent stream, string targetFile);
		void WriteToFile(string filepath, string content);
		IEnumerable<string> ReadLines(IFileInfo file);
	}
}