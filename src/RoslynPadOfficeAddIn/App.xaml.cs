using System.Runtime;
using System.Windows;

namespace RoslynPad;

public partial class App : Application
{
    private const string ProfileFileName = "RoslynPad.jitprofile";

    public App()
    {
        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile(ProfileFileName);
    }
}
