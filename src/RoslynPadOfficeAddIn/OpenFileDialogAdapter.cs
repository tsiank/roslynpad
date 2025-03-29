using System.Composition;
using System.Windows;
using Microsoft.Win32;
using RoslynPad.UI;

namespace OfficeSharp;

[Export(typeof(IOpenFileDialog))]
internal class OpenFileDialogAdapter : IOpenFileDialog
{
    private readonly OpenFileDialog _dialog;

    public OpenFileDialogAdapter()
    {
        _dialog = new OpenFileDialog();
    }

    public bool AllowMultiple
    {
        get => _dialog.Multiselect;
        set => _dialog.Multiselect = value;
    }

    public FileDialogFilter Filter
    {
        set => _dialog.Filter = value + string.Empty;
    }

    public string InitialDirectory
    {
        get => _dialog.InitialDirectory;
        set => _dialog.InitialDirectory = value;
    }

    public string FileName
    {
        get => _dialog.FileName;
        set => _dialog.FileName = value;
    }

    public Task<string[]?> ShowAsync()
    {
        if (_dialog.ShowDialog() == true)
        {
            return Task.FromResult<string[]?>(_dialog.FileNames);
        }

        return Task.FromResult<string[]?>(null);
    }
}
