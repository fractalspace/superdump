﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SuperDump.Analyzer.Linux.SharedLibs {
	[StructLayout(LayoutKind.Sequential)]
	public struct SharedLib {
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512, ArraySubType = UnmanagedType.U1)]
		public byte[] Path;
		public ulong BindingOffset;
		public ulong StartAddress;
		public ulong EndAddress;
	}
}