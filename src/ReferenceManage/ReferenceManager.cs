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
    public static List<string> FwDefaultReferences => GetDefaultReferences(isDotNet: false, needOfficeRef: true, needGUI: true);
    public static List<MetadataReference> FwDefaultMetadataReferences => GetMetadataReferences(FwDefaultReferences);
    public static List<MetadataReference> FwGUIMetadataReferences => GetMetadataReferences(FwGUIDefaultReferences);
    public static List<string> FwGUIDefaultReferences =>
        [
            "System.Windows.Forms",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework",
            "System.Xaml",
            "Microsoft.CSharp"
        ];

    public static List<string> DotNetDefaultReferences => GetDefaultReferences(isDotNet: true, needOfficeRef: true, needGUI: true);
    public static List<MetadataReference> DotNetDefaultMetadataReferences => GetMetadataReferences(DotNetDefaultReferences);

    public static List<string> AdditionalImports => [
                                                    "OfficeMacroExt",
                                                    "ExcelDna.Integration",
                                                    "Microsoft.Office.Interop.Excel",
                                                    "System.Windows"
                                                 ];


    public static List<string> ScriptingAdditionalImports => [
                                                    "System",
                                                    "System.Linq",
                                                    "System.Reflection",
                                                    "System.Collections.Generic",
                                                    "System.Text.RegularExpressions",
                                                    "System.IO",
                                                    "OfficeMacroExt",
                                                    "ExcelDna.Integration",
                                                    "Microsoft.Office.Interop.Excel",
                                                    "System.Windows"
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


    public static List<string> GetDefaultReferences(bool isDotNet, bool needOfficeRef, bool needGUI = false)
    {
        List<string> result = [];

        List<string> gacResult = [];
        List<string> appBaseResult = [];

        if (!isDotNet)
        {
            if (needOfficeRef)
            {
                gacResult.AddRange(GetOfficeReletedReferences());

                appBaseResult.AddRange(GetFwAdditionalReferences(isDotNet));
            }
        }
        else
        {
            if (needOfficeRef)
            {
                gacResult.AddRange(GetOfficeReletedReferences());

                appBaseResult.AddRange(GetFwAdditionalReferences(isDotNet));
            }
        }

        foreach (var r in gacResult)
        {
            result.Add(ResolvePathInGAC(r));
        }

        foreach (var r in appBaseResult)
        {
            result.Add(ResolvePathInAppDir(r));
        }

        return result;

    }

    public static List<string> GetOfficeReletedReferences()
    {
        List<string> result = [
            "Microsoft.Vbe.Interop",
            "OFFICE",
            "Microsoft.Office.Interop.Excel"
            ];

        return result;
    }

    public static List<string> GetFwAdditionalReferences(bool isDotNet)
	{
		List<string> result = [
            "ExcelDna.Integration.dll",
            "OfficeMacroExt.dll"
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

    private static string ResolvePathInAppDir(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, fileName);
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
