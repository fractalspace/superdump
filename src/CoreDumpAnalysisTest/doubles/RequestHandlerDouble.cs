﻿using CoreDumpAnalysis.boundary;

namespace CoreDumpAnalysisTest.doubles {
	class RequestHandlerDouble : IHttpRequestHandler {

		public string FromUrl { get; private set; }
		public string ToFile { get; private set; }
		public bool Return { get; set; }

		public bool DownloadFromUrl(string url, string targetFile) {
			this.FromUrl= url;
			this.ToFile = targetFile;
			return Return;
		}
	}
}