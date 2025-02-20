using System.Reflection;

namespace RoslynPad.Build
{
   internal static class GACAsmSearch
    {
        internal static List<string> SearchAssemblyInGacDirectory(string assemblyName)
        {
            string winDir = Environment.GetEnvironmentVariable("WINDIR")!;
            string[] gacPaths = new[]
            {
            Path.Combine(winDir, @"assembly\GAC_MSIL"),
            Path.Combine(winDir, @"Microsoft.NET\assembly\GAC_MSIL"),

            Path.Combine(winDir, @"Microsoft.NET\assembly\GAC_32"),
            Path.Combine(winDir, @"assembly\GAC_32"),

            Path.Combine(winDir, @"Microsoft.NET\assembly\GAC_64"),
            Path.Combine(winDir, @"assembly\GAC_64")
        };

            var foundAsmFiles = new List<string>();

            foreach (string gacPath in gacPaths)
            {
                if (Directory.Exists(gacPath))
                {
                    try
                    {
                        var matchingDirs = Directory.GetDirectories(gacPath, "*", SearchOption.AllDirectories)
                            .Where(d => Path.GetFileName(d) == assemblyName);

                        foreach (var dir in matchingDirs)
                        {
                            string[] dllFiles = Directory.GetFiles(dir, assemblyName + ".dll", SearchOption.AllDirectories);
                            foreach (string file in dllFiles)
                            {
                                foundAsmFiles.Add(file);
                            }
                        }
                    }
                    catch (BadImageFormatException)
                    {
                        continue;
                    }
                }
            }

            return foundAsmFiles;
        }


        static void PrintAssemblyDetails(AssemblyName asmName, string? filePath = null)
        {
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"name: {asmName.Name}");
            Console.WriteLine($"version: {asmName.Version}");

            if (asmName.GetPublicKeyToken() != null)
            {
                string publicKeyToken = Convert.ToHexStringLower(asmName.GetPublicKeyToken()!);
                Console.WriteLine($"PublicKeyToken: {publicKeyToken}");
            }

            Console.WriteLine($"Culture: {(string.IsNullOrEmpty(asmName.CultureName) ? "neutral" : asmName.CultureName)}");

            if (!string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine($"path: {filePath}");
            }

            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    Assembly asm = Assembly.LoadFrom(filePath);
                    var attributes = asm.GetCustomAttributes(false);

                    var descriptionAttribute = attributes
                        .FirstOrDefault(a => a.GetType().Name == "AssemblyDescriptionAttribute");
                    if (descriptionAttribute != null)
                    {
                        Console.WriteLine($"description: {descriptionAttribute.GetType()?.GetProperty("Description")?.GetValue(descriptionAttribute)}");
                    }

                    var companyAttribute = attributes
                        .FirstOrDefault(a => a.GetType().Name == "AssemblyCompanyAttribute");
                    if (companyAttribute != null)
                    {
                        Console.WriteLine($"company: {companyAttribute?.GetType()?.GetProperty("Company")?.GetValue(companyAttribute)}");
                    }
                }
            }
            catch
            {

            }
        }

    }
}
