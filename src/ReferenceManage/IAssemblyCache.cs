using System;
using System.Runtime.InteropServices;

namespace ReferenceManage;

[ComImport]
[Guid("E707DCDE-D1CD-11D2-BAB9-00C04F8ECEAE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAssemblyCache
{
	[PreserveSig]
	int UninstallAssembly(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName, IntPtr pvReserved, out uint pulDisposition);

	[PreserveSig]
	int QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName, ref ASSEMBLY_INFO pAsmInfo);

	[PreserveSig]
	int CreateAssemblyCacheItem(uint dwFlags, IntPtr pvReserved, out IntPtr ppAsmItem, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName);

	[PreserveSig]
	int CreateAssemblyScavenger(out object ppAsmScavenger);

	[PreserveSig]
	int InstallAssembly(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszManifestFilePath, IntPtr pvReserved);
}
