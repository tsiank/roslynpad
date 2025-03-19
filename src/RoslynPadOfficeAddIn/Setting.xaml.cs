using System;
using System.Drawing.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using RoslynPad.UI;

#nullable disable

namespace RoslynPad.SettingsUI;

public partial class SettingsWindow : Window
{
    private readonly IApplicationSettingsValues _appSettings;
    private readonly List<string> _fontSizes = new List<string> { "8", "9", "10", "11", "12",  "13", "14", "15", "16", "18", "20", "22", "24" };
    private readonly List<string> _platforms = new List<string> { ".NET Framework x64 ", ".NET Framework x86 ", ".NET 6 ", ".NET 9 " };

    internal SettingsWindow(IApplicationSettingsValues appSettings)
    {
        if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));

        _appSettings = appSettings;

        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        InitializeComponent();
        InitializeComboBoxes();
        LoadSettings();
    }

    private void InitializeComboBoxes()
    {
        EditorFontSizeComboBox.ItemsSource = _fontSizes;
        OutputFontSizeComboBox.ItemsSource = _fontSizes;
        DefaultPlatformComboBox.ItemsSource = _platforms;
    }

    private void LoadSettings()
    {
        try
        {
            var values = _appSettings;

            SendErrorsCheckBox.IsChecked = values.SendErrors;
            EnableBraceCompletionCheckBox.IsChecked = values.EnableBraceCompletion;
            FormatDocumentOnCommentCheckBox.IsChecked = values.FormatDocumentOnComment;
            DefaultPlatformComboBox.SelectedItem = values.DefaultPlatformName;

            EditorFontFamilyTextBox.Text = values.EditorFontFamily;

            EditorFontSizeComboBox.SelectedItem = values.EditorFontSize.ToString();
            OutputFontSizeComboBox.SelectedItem = values.OutputFontSize.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings: {ex.Message}");
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var values = _appSettings;

            values.SendErrors = SendErrorsCheckBox.IsChecked ?? false;
            values.EnableBraceCompletion = EnableBraceCompletionCheckBox.IsChecked ?? false;
            values.FormatDocumentOnComment = FormatDocumentOnCommentCheckBox.IsChecked ?? false;
            values.DefaultPlatformName = DefaultPlatformComboBox.SelectedItem?.ToString() ?? string.Empty;

            values.EditorFontFamily = EditorFontFamilyTextBox.Text.Trim();

            values.EditorFontSize = double.Parse(EditorFontSizeComboBox.SelectedItem?.ToString() ?? "12");
            values.OutputFontSize = double.Parse(OutputFontSizeComboBox.SelectedItem?.ToString() ?? "12");

            // The SaveSettings method is called automatically via PropertyChanged event in ApplicationSettings
            Window.GetWindow(this).Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}");
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this).Close();
    }
}
