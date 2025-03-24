using System.Xml.Linq;

namespace RoslynPad.Build;

internal static class MSBuildHelper
{
    public const string ReferencesFile = "references.txt";
    public const string AnalyzersFile = "analyzers.txt";
    private const string Sdk = "Microsoft.NET.Sdk";

    public static XDocument CreateCsxCsproj(bool isDotNet, string targetFramework, IEnumerable<LibraryRef> references) =>
        new(new XElement("Project",
            ImportSdkProject(Sdk, "Sdk.props"),
            BuildProperties(targetFramework, copyBuildOutput: false),
            Reference(references),
            ReferenceAssemblies(isDotNet),
            ImportSdkProject(Sdk, "Sdk.targets"),
            CoreCompileTarget()));

    public static XDocument CreateCsproj(string targetFramework, IEnumerable<LibraryRef> referenceItems, IEnumerable<string> usingItems) =>
       new(new XElement("Project",
            new XAttribute("Sdk", Sdk),
            BuildProperties(targetFramework, copyBuildOutput: true),
            Reference(referenceItems),
            Using(usingItems),
            CopySQLiteInteropTarget())); //copy SQLite.Interop.dll

    private static XElement ReferenceAssemblies(bool isDotNet) =>
        isDotNet ? new XElement("ItemGroup") : new XElement("ItemGroup",
            new XElement("PackageReference",
                new XAttribute("Include", "Microsoft.NETFramework.ReferenceAssemblies"),
                new XAttribute("Version", "*")));

    private static XElement Reference(IEnumerable<LibraryRef> referenceItems) =>
        new("ItemGroup",
            referenceItems.Select(Reference).ToArray());

    private static XElement Reference(LibraryRef reference)
    {
        var element = new XElement(reference.Kind.ToString(),
            new XAttribute("Include", reference.Value));

        if (!string.IsNullOrEmpty(reference.Version))
        {
            element.Add(new XAttribute("Version", reference.Version));
        }

        if (reference.embedInteropTypes)
        {
            element.Add(new XElement("EmbedInteropTypes", "True"));
        }

        return element;
    }

    private static XElement Compile(IEnumerable<string> compileItems) =>
        new("ItemGroup",
            compileItems.Select(c => new XElement("Compile", new XAttribute("Include", c))));

    private static XElement Using(IEnumerable<string> usingItems) =>
        new("ItemGroup",
            usingItems.Select(c => new XElement("Using", new XAttribute("Include", c))));

    private static XElement BuildProperties(string targetFramework, bool copyBuildOutput) =>
        new("PropertyGroup",
            new XElement("TargetFramework", targetFramework),
            new XElement("OutputType", "Exe"),
            new XElement("OutputPath", "bin"),
            new XElement("UseAppHost", false),
            new XElement("AllowUnsafeBlocks", true),
            new XElement("LangVersion", "preview"),
            new XElement("Nullable", "enable"),
            new XElement("AppendTargetFrameworkToOutputPath", false),
            new XElement("AppendRuntimeIdentifierToOutputPath", false),
            new XElement("AppendPlatformToOutputPath", false),
            new XElement("CopyBuildOutputToOutputDirectory", copyBuildOutput),
            new XElement("GenerateAssemblyInfo", false));

    private static XElement CoreCompileTarget() =>
        new("Target",
            new XAttribute("Name", "CoreCompile"),
            WriteLinesToFile(ReferencesFile, "@(ReferencePathWithRefAssemblies)"),
            WriteLinesToFile(AnalyzersFile, "@(Analyzer)"));

    private static XElement WriteLinesToFile(string file, string lines) =>
        new("WriteLinesToFile",
            new XAttribute("File", file),
            new XAttribute("Lines", lines),
            new XAttribute("Overwrite", true));

    private static XElement ImportSdkProject(string sdk, string project) =>
        new("Import",
            new XAttribute("Sdk", sdk),
            new XAttribute("Project", project));
    private static XElement CopySQLiteInteropTarget() =>
      new("Target",
          new XAttribute("Name", "CopySQLiteInterop"),
          new XAttribute("AfterTargets", "AfterBuild"),
          // x86 copy
          new XElement("PropertyGroup",
              new XElement("SourceFile_x86", $"{AppContext.BaseDirectory}\\x86\\SQLite.Interop.dll"),
              new XElement("TargetDir_x86", "$(OutputPath)\\x86"),
              new XElement("TargetFile_x86", "$(TargetDir_x86)\\SQLite.Interop.dll")),
          new XElement("MakeDir",
              new XAttribute("Directories", "$(TargetDir_x86)"),
              new XAttribute("Condition", "!Exists('$(TargetDir_x86)')")),
          new XElement("Copy",
              new XAttribute("SourceFiles", "$(SourceFile_x86)"),
              new XAttribute("DestinationFiles", "$(TargetFile_x86)"),
              new XAttribute("Condition", "Exists('$(SourceFile_x86)')"),
              new XAttribute("SkipUnchangedFiles", "true")),
          new XElement("Message",
              new XAttribute("Text", "Copied SQLite.Interop.dll from $(SourceFile_x86) to $(TargetFile_x86)"),
              new XAttribute("Importance", "High")),
          // x64 copy
          new XElement("PropertyGroup",
              new XElement("SourceFile_x64", $"{AppContext.BaseDirectory}\\x64\\SQLite.Interop.dll"),
              new XElement("TargetDir_x64", "$(OutputPath)\\x64"),
              new XElement("TargetFile_x64", "$(TargetDir_x64)\\SQLite.Interop.dll")),
          new XElement("MakeDir",
              new XAttribute("Directories", "$(TargetDir_x64)"),
              new XAttribute("Condition", "!Exists('$(TargetDir_x64)')")),
          new XElement("Copy",
              new XAttribute("SourceFiles", "$(SourceFile_x64)"),
              new XAttribute("DestinationFiles", "$(TargetFile_x64)"),
              new XAttribute("Condition", "Exists('$(SourceFile_x64)')"),
              new XAttribute("SkipUnchangedFiles", "true")),
          new XElement("Message",
              new XAttribute("Text", "Copied SQLite.Interop.dll from $(SourceFile_x64) to $(TargetFile_x64)"),
              new XAttribute("Importance", "High")));
}
