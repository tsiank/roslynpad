using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ReferenceManage;

public static class ReferenceInfo
{
    public static List<string> FwDefaultReferences
    {
        get
        {
            List<string> result = [];

            result.AddRange(OfficeReletedReferences);

            var appBaseResult = GetAdditionalReferences(false);
            foreach (var r in appBaseResult)
            {
                result.Add(ResolvePathInAppDir(false, r));
            }

            return result;
        }
    }

    public static List<string> FwGUIDefaultReferences =>
    [
        "System.Windows.Forms",
        "WindowsFormsIntegration",
        "WindowsBase",
        "PresentationCore",
        "PresentationFramework",
        "System.Xaml",
        "Microsoft.CSharp"
    ];

    public static List<MetadataReference> FwDefaultMetadataReferences => GetMetadataReferences(FwDefaultReferences);
    public static List<MetadataReference> FwGUIMetadataReferences => GetMetadataReferences(FwGUIDefaultReferences);

    public static List<string> DotNetDefaultReferences
    {
        get
        {
            List<string> result = [];
            var appBaseResult = GetAdditionalReferences(true);
            foreach (var r in appBaseResult)
            {
                result.Add(ResolvePathInAppDir(true, r));
            }

            return result;
        }
    }

    public static List<string> DotNetScriptingReferences
    {
        get
        {
            List<string> result = [];

            result.AddRange(OfficeReletedReferences);

            var appBaseResult = GetAdditionalReferences(true);
            foreach (var r in appBaseResult)
            {
                result.Add(ResolvePathInAppDir(true, r));
            }

            return result;
        }
    }

    public static List<MetadataReference> DotNetDefaultMetadataReferences => GetMetadataReferences(DotNetScriptingReferences);

    public static List<string> AdditionalImports => [
                                                    "OfficeMacroExt",
                                                    "ExcelDna.Integration",
                                                    "Microsoft.Office.Interop.Excel"
                                                 ];


    public static List<string> ScriptingAdditionalImports => [
                                                    "System",
                                                    "System.Linq",
                                                    "System.Threading",
                                                    "System.Threading.Tasks",
                                                    "System.Reflection",
                                                    "System.Collections.Generic",
                                                    "System.Text.RegularExpressions",
                                                    "System.IO",
                                                    "RoslynPad.Runtime",
                                                    "OfficeMacroExt",
                                                    "ExcelDna.Integration",
                                                    "Microsoft.Office.Interop.Excel"
                                                     ];

    public static List<Assembly> ScriptingDefaultAssemblies = [
                                typeof(System.Text.RegularExpressions.Regex).Assembly,
                                typeof(System.IO.Directory).Assembly,
                                typeof(System.Reflection.MethodInfo).Assembly,
                                typeof(System.Dynamic.DynamicObject).Assembly,
                                typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly
                                ];

    public static List<MetadataReference> ScriptingDefaultRefs =>
                                [.. ScriptingDefaultAssemblies.Select(a => MetadataReference.CreateFromFile(a.Location))];

    public static List<string> OfficeReletedReferences
    {
        get
        {
            List<string> result = [];
            List<string> officeNames = [
                                        "Microsoft.Vbe.Interop",
                                        "OFFICE",
                                        "Microsoft.Office.Interop.Excel"
                                       ];

            foreach (var r in officeNames)
            {
                result.Add(ResolvePathInGAC(r));
            }

            return result;
        }
    }


    public static List<string> GetAdditionalReferences(bool isDotNet)
	{
		List<string> result = [
            "ExcelDna.Integration.dll",
            "OfficeMacroExt.dll",
            "RoslynPad.Runtime.dll"
        ];

        if(!isDotNet)
        {
            result.Add("IndexRange.dll");
        }

        return result;
	}

    public static List<MetadataReference> GetMetadataReferences(List<string> assemblies)
    {
        List<MetadataReference> metadataReferences = [];

        foreach (string assembly in assemblies)
        {
            var asmMeta = MetadataReference.CreateFromFile(assembly);
            metadataReferences.Add(asmMeta);
        }

        return metadataReferences;
    }

	private static string ResolvePathInGAC(string name)
	{
		var text = GACInfo.QueryAssemblyInfo(name);
		if (text == null)
		{
			return name;
		}
		return text;
	}

	private static bool IsInGAC(string name)
	{
		return GACInfo.QueryAssemblyInfo(name) != null;
	}

	private static string GetOfficeMacroReferencePath(string fileName)
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OfficeMacroAddIn" + fileName);
	}

    private static string ResolvePathInAppDir(bool isDotNet, string fileName)
    {
        var pathName = isDotNet ? "net" : "netfx";
        var roslynPadRuntimeFile = Path.Combine(AppContext.BaseDirectory, "runtimes", pathName, fileName);
        if(File.Exists(roslynPadRuntimeFile))
        {
            return roslynPadRuntimeFile;
        }
        else
        {
            return Path.Combine(AppContext.BaseDirectory, fileName);
        }    
    }

    public static byte[] GetAssemblyBytesInMemeory(string asmName)
    {
        var dnaAsm = Assembly.Load(asmName);
        Type type = dnaAsm.GetType();
        var pi = type.GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
        byte[] assemblyBytes = (byte[])pi.Invoke(dnaAsm, null);
        return assemblyBytes;
    }

}
