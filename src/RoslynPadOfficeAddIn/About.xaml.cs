using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ExcelDna.Integration;
using static System.Net.Mime.MediaTypeNames;

namespace OfficeSharp;

/// <summary>
/// About.xaml 的交互逻辑
/// </summary>
public partial class About : System.Windows.Window
{
    public About()
    {
        InitializeComponent();

        SetDescriptionText();

        Feature.Text = "Version：1.0.0\n" +
                       "Author：tsiank\n" +
                       "Publish date：4, 4, 2025";
    }


    private void SetDescriptionText()
    {
        DescriptionTextBlock.Inlines.Clear();

        DescriptionTextBlock.Inlines.Add(new Run("This AddIn is developed based on "));

        Hyperlink roslynPadLink = new Hyperlink(new Run("RosylnPad"))
        {
            NavigateUri = new Uri("https://github.com/roslynpad/roslynpad"),
            ToolTip = "https://github.com/roslynpad/roslynpad"
        };
        roslynPadLink.RequestNavigate += Hyperlink_RequestNavigate;
        DescriptionTextBlock.Inlines.Add(roslynPadLink);

        DescriptionTextBlock.Inlines.Add(new Run(" and "));

        Hyperlink excelDnaLink = new Hyperlink(new Run("ExcelDna"))
        {
            NavigateUri = new Uri("https://github.com/Excel-DNA"),
            ToolTip = "https://github.com/Excel-DNA"
        };
        excelDnaLink.RequestNavigate += Hyperlink_RequestNavigate;
        DescriptionTextBlock.Inlines.Add(excelDnaLink);

        DescriptionTextBlock.Inlines.Add(new Run(".\nSee "));

        Hyperlink haibaoLink = new Hyperlink(new Run("OfficeSharp "))
        {
            NavigateUri = new Uri("https://haibao.me/index.php/2025/04/04/officesharp%ef%bc%9a%e6%8a%8aroslynpad%e5%a1%9e%e8%bf%9bexcel%e9%87%8c/"),
            ToolTip = "OfficeSharp"
        };
        haibaoLink.RequestNavigate += Hyperlink_RequestNavigate;
        DescriptionTextBlock.Inlines.Add(haibaoLink);

        DescriptionTextBlock.Inlines.Add(new Run(" for more information"));
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"cannot open: {ex.Message}", "error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }

}
