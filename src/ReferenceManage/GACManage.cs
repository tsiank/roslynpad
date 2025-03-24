using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

#nullable disable

namespace ReferenceManage;

public class GACInfo
{
    public static string QueryAssemblyInfo(string assemblyName)
    {
        IAssemblyCache ppAsmCache = null;
        if (CreateAssemblyCache(out ppAsmCache, 0u) != 0)
        {
            return null;
        }

        var pAsmInfo = default(ASSEMBLY_INFO);
        pAsmInfo.cchBuf = 260u;
        pAsmInfo.pszCurrentAssemblyPathBuf = new string('\0', (int)pAsmInfo.cchBuf);
        if (ppAsmCache.QueryAssemblyInfo(1u, assemblyName, ref pAsmInfo) != 0)
        {
            return null;
        }
        return pAsmInfo.pszCurrentAssemblyPathBuf;
    }

    //CreateAssemblyCache:创建全局程序集缓存（GAC）的接口对象 IAssemblyCache
    [DllImport("fusion.dll")]
    internal static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);
}
