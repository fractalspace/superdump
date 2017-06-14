﻿using SuperDump.Analyzer.Linux;
using SuperDump.Doubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using System.Collections.Generic;
using SuperDump.Analyzer.Linux.Analysis;
using System.IO;
using Moq;
using SuperDump.Analyzer.Linux.Boundary;
using Thinktecture.IO;

namespace SuperDump.Analyzer.Linux.Test {
	[TestClass]
	public class DebugSymbolResolverTest {

		private DebugSymbolResolver resolver;
		private Mock<IFilesystem> filesystem;
		private RequestHandlerDouble requestHandler;

		private List<SDModule> modules;
		private SDCDModule module;

		[TestInitialize]
		public void Init() {
			this.filesystem = new Mock<IFilesystem>();
			this.requestHandler = new RequestHandlerDouble();
			this.resolver = new DebugSymbolResolver(filesystem.Object, requestHandler);

			this.modules = new List<SDModule>();
			this.module = new SDCDModule() {
				LocalPath = $"./lib/ruxit/somelib.so",
				FileName = "somelib.so",
				FilePath = $"/lib/ruxit/somelib.so"
			};
			this.modules.Add(module);
		}

		[TestMethod]
		public void TestWithoutModules() {
			resolver.Resolve(new List<SDModule>());
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestWithoutBinary() {
			module.LocalPath = null;
			resolver.Resolve(this.modules);
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestWithIrrelevantBinary() {
			module.FilePath = $"some{Path.DirectorySeparatorChar}path";
			module.FileName = "somelib.so";
			resolver.Resolve(this.modules);
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestDebugFilePresent() {
			SetDebugFileExists(true);
			resolver.Resolve(this.modules);
			Assert.IsTrue(module.DebugSymbolPath.EndsWith($"some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg"), $"Invalid DebugSymbol path: {module.DebugSymbolPath}");
			AssertNoRequestsMade();
		}

		[TestMethod]
		public void TestDownloadDebugSymbolFail() {
			SetDebugFileExists(false);
			requestHandler.Return = false;
			resolver.Resolve(this.modules);
			Assert.IsNull(module.DebugSymbolPath);
			AssertValidRequestDone();
		}

		[TestMethod]
		public void TestDownloadDebugSymbol() {
			SetDebugFileExists(false);
			requestHandler.Return = true;
			resolver.Resolve(this.modules);
			Assert.IsTrue(module.DebugSymbolPath.EndsWith($"some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg"), $"Invalid DebugSymbol path: {module.DebugSymbolPath}");
			AssertValidRequestDone();
		}

		private void SetDebugFileExists(bool exists) {
			filesystem.Setup(fs => fs.Md5FromFile(this.module.LocalPath)).Returns("some-md5-hash");
			var debugFileInfo = new Mock<IFileInfo>();
			debugFileInfo.Setup(fi => fi.Exists).Returns(exists);
			filesystem.Setup(fs => fs.GetFile(Path.Combine(Constants.DEBUG_SYMBOL_PATH, "some-md5-hash", "somelib.dbg"))).Returns(debugFileInfo.Object);
		}

		private void AssertNoRequestsMade() {
			Assert.IsNull(requestHandler.FromUrl);
			Assert.IsNull(requestHandler.ToFile);
		}

		private void AssertValidRequestDone() {
			Assert.IsNotNull(requestHandler.FromUrl);
			Assert.AreNotEqual("", requestHandler.FromUrl);
			Assert.IsTrue(requestHandler.ToFile.StartsWith(Constants.DEBUG_SYMBOL_PATH), "Invalid DebugSymbol target: " + requestHandler.ToFile);
			Assert.IsTrue(requestHandler.ToFile.EndsWith($"some-md5-hash{Path.DirectorySeparatorChar}somelib.dbg"), $"Invalid DebugSymbol target: {requestHandler.ToFile}");
		}
	}
}
