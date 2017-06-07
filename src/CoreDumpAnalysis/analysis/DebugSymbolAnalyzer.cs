﻿using SuperDump.Analyzer.Linux.Boundary;
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SuperDump.Analyzer.Linux.Analysis {
    public class DebugSymbolAnalyzer {

		private static Regex addr2lineRegex = new Regex("([^:]+):(\\d+)", RegexOptions.Compiled);

		private readonly IFilesystem filesystem;
		private readonly IProcessHandler processHandler;

		private readonly String coredump;
		private readonly SDResult analysisResult;

		public DebugSymbolAnalyzer(IFilesystem filesystem, IProcessHandler processHandler, String coredump, SDResult result) {
			this.filesystem = filesystem ?? throw new ArgumentNullException("Filesystem must not be null!");
			this.processHandler = processHandler ?? throw new ArgumentNullException("ProcessHandler must not be null!");
			this.analysisResult = result ?? throw new ArgumentNullException("SD Result must not be null!");
			this.coredump = coredump ?? throw new ArgumentNullException("Coredump Path must not be null!");
		}

		public void DebugAndSetResultFields() {
			if(this.analysisResult?.ThreadInformation == null) {
				throw new ArgumentNullException("Debug symbol analysis can only be executed when thread information is set!");
			}
			if(this.analysisResult?.SystemContext?.Modules == null) {
				throw new ArgumentNullException("Debug symbol analysis can only be executed when modules are set!");
			}
			Analyze();
		}

		private void Analyze() {
			foreach (var threadInfo in this.analysisResult.ThreadInformation) {
				foreach (var stackFrame in threadInfo.Value.StackTrace) {
					SDCDModule module = FindModuleAtAddress(this.analysisResult.SystemContext.Modules, stackFrame.InstructionPointer);
					if (module?.LocalPath != null) {
						stackFrame.ModuleName = module.FileName;
						AddSourceInfo(stackFrame, module);
					}
				}
			}
		}

		private void AddSourceInfo(SDCombinedStackFrame stackFrame, SDCDModule module) {
			Tuple<SDFileAndLineNumber, string> methodSource = Address2MethodSource(stackFrame.InstructionPointer, module);
			SDFileAndLineNumber sourceInfo = methodSource.Item1;
			string methodName = methodSource.Item2;
			if (methodName != "??") {
				stackFrame.MethodName = methodName;
				if (sourceInfo.File != null && sourceInfo.File != "??") {
					stackFrame.SourceInfo = sourceInfo;
				}
			}
		}

		private SDCDModule FindModuleAtAddress(IList<SDModule> modules, ulong instrPtr) {
			foreach (SDModule module in modules) {
				if (module.GetType() != typeof(SDCDModule)) {
					throw new InvalidCastException("Plain SDModule found in module list. SDCDModule expected.");
				}
				SDCDModule cdModule = (SDCDModule)module;
				if (cdModule.StartAddress < instrPtr && cdModule.EndAddress > instrPtr) {
					return cdModule;
				}
			}
			return null;
		}

		private Tuple<SDFileAndLineNumber, string> Address2MethodSource(ulong instrPtr, SDCDModule module) {
			ulong relativeIp = instrPtr;

			if (module.DebugSymbolPath != null && module.DebugSymbolPath != "") {
				// If there is a debug file, link it (required for addr2line to find the dbg file)
				LinkDebugFile(module.LocalPath, module.DebugSymbolPath);
			}
			StreamReader reader = processHandler.StartProcessAndRead("addr2line", "-f -C -e " + module.LocalPath + " 0x" + relativeIp.ToString("X"));
			string methodName = reader.ReadLine();
			string fileLine = reader.ReadLine();
			SDFileAndLineNumber sourceInfo = RetrieveSourceInfo(fileLine);
			return Tuple.Create(sourceInfo, methodName);
		}

		private void LinkDebugFile(string localPath, string debugPath) {
			string targetDebugFile = Path.GetDirectoryName(localPath) + "/" + DebugSymbolResolver.DebugFileName(localPath);
			if(filesystem.FileExists(targetDebugFile)) {
				return;
			}
			Console.WriteLine("Creating symbolic link: " + debugPath + ", " + targetDebugFile);
			filesystem.CreateSymbolicLink(debugPath, targetDebugFile);
		}

		private SDFileAndLineNumber RetrieveSourceInfo(string output) {
			Match match = addr2lineRegex.Match(output);
			if(match.Success) {
				SDFileAndLineNumber sourceInfo = new SDFileAndLineNumber();
				sourceInfo.File = match.Groups[1].Value;
				sourceInfo.Line = Int32.Parse(match.Groups[2].Value);
				return sourceInfo;
			}
			return new SDFileAndLineNumber();
		}
	}
}
