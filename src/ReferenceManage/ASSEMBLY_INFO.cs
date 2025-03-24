using System.Runtime.InteropServices;

namespace ReferenceManage;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct ASSEMBLY_INFO
{
	public uint cbAssemblyInfo;  // 结构大小

    public uint dwAssemblyFlags; // 标志

    public ulong ulAssemblySizeInKB; // 程序集大小

    [MarshalAs(UnmanagedType.LPWStr)]
	public string pszCurrentAssemblyPathBuf; // 路径缓冲区

    public uint cchBuf; // 缓冲区大小
}
