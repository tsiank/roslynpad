using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using RoslynPad.UI;
using static RoslynPad.UI.ApplicationSettings;

#nullable disable

namespace OfficeSharp.SettingsUI;

public partial class SettingsWindow : Window
{
    private SerializableValues _appSettings;

    private string _settingsPath;
    //private Settings _settings;

    private readonly List<string> _fontSizes = ["8", "9", "10", "11", "12",  "13", "14", "15", "16", "18", "20", "22", "24"];
    private readonly List<string> _platforms = [".NET Framework x64 ", ".NET 6 ", ".NET 8 ", ".NET 9 "];
    private readonly List<string> _themeTypes = ["Light", "Dark"];

    private string _addinConfigPath;
    private AddInConfig _addinConfig;
    private Dictionary<string, CheckBox> _addinCheckBoxes = new Dictionary<string, CheckBox>();

    internal SettingsWindow()
    {
        Title = "OfficeSharp Settings";
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        InitializeComponent();
        InitializeComboBoxes();
        LoadSettings();
        LoadAddInSettings();
    }

    private void InitializeComboBoxes()
    {
        EditorFontSizeComboBox.ItemsSource = _fontSizes;
        OutputFontSizeComboBox.ItemsSource = _fontSizes;
        DefaultPlatformComboBox.ItemsSource = _platforms;
    }

    private void LoadSettings()
    {
        var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var configPath = Path.Combine(documentPath, "OfficeSharpConfig");
        var jsonPath = Path.Combine(configPath, "OfficeSharp.json");
        
        if (!File.Exists(jsonPath)) throw new ArgumentNullException(nameof(jsonPath));

        _settingsPath = jsonPath;
        var settingsJson = File.ReadAllText(jsonPath);

        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.PropertyNameCaseInsensitive = true;
        var settingConfig = JsonSerializer.Deserialize<SerializableValues>(settingsJson, jsonOptions);

        try
        {
            _appSettings = settingConfig;
            SendErrorsCheckBox.IsChecked = _appSettings.SendErrors;
            EnableBraceCompletionCheckBox.IsChecked = _appSettings.EnableBraceCompletion;
            FormatDocumentOnCommentCheckBox.IsChecked = _appSettings.FormatDocumentOnComment;
            DefaultPlatformComboBox.SelectedItem = _appSettings.DefaultPlatformName;

            EditorFontFamilyTextBox.Text = _appSettings.EditorFontFamily;

            EditorFontSizeComboBox.SelectedItem = _appSettings.EditorFontSize.ToString();
            OutputFontSizeComboBox.SelectedItem = _appSettings.OutputFontSize.ToString();

            CustomThemeName.ItemsSource = GetThemeNameList();
            CustomThemeName.SelectedItem = _appSettings.CustomThemeName;

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings: {ex.Message}");
        }

        List<string> GetThemeNameList()
        {
            var themeNameList = new List<string>();

            var themePath = Path.Combine(configPath, "Themes");
            if (!Directory.Exists(themePath))
            {
                Directory.CreateDirectory(themePath);
                var sourceThemePath = Path.Combine(AppContext.BaseDirectory, "Themes");
                foreach (string file in Directory.GetFiles(sourceThemePath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(themePath, fileName);
                    File.Copy(file, destFile, true);

                    var themeName = Path.GetFileName(file).Replace(".json", "");
                    themeNameList.Add(themeName);
                }

                _appSettings.CustomThemePath = themePath;
                return themeNameList;
            }

            var customeThemePath = settingConfig.CustomThemePath;
            var themeFiles = Directory.GetFiles(customeThemePath);

            foreach (var themeFile in themeFiles)
            {
                var themeName = Path.GetFileName(themeFile)?.Replace(".json", "");
                 themeNameList.Add(themeName);
            }

            return themeNameList;
        }
    }

    private void LoadAddInSettings()
    {
        try
        {
            var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var configPath = Path.Combine(documentPath, "OfficeSharpConfig");
            _addinConfigPath = Path.Combine(configPath, "OfficeSharpMacroAddIn.json");

            string json = File.ReadAllText(_addinConfigPath);
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.PropertyNameCaseInsensitive = true;
            _addinConfig = JsonSerializer.Deserialize<AddInConfig>(json, jsonOptions);

            // 获取 excelmacroAddinPath 下的子目录
            var subDirectories = Directory.Exists(_addinConfig.ExcelMacroAddinPath)
                ? Directory.GetDirectories(_addinConfig.ExcelMacroAddinPath).Select(Path.GetFileName).ToList()
                : new List<string>();

            // 清空之前的 CheckBox
            AddInCheckBoxPanel.Children.Clear();
            _addinCheckBoxes.Clear();

            // 生成 CheckBox
            foreach (var dir in subDirectories)
            {
                bool isChecked = _addinConfig.AddInList != null && _addinConfig.AddInList.TryGetValue(dir, out bool value) && value;
                var checkBox = new CheckBox
                {
                    Content = dir,
                    IsChecked = isChecked,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                AddInCheckBoxPanel.Children.Add(checkBox);
                _addinCheckBoxes[dir] = checkBox;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载加载项设置时出错: {ex.Message}");
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

            _appSettings.CustomThemeName = CustomThemeName.SelectedItem.ToString();

            var jsonContent = JsonSerializer.Serialize(values, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = new JsonLowerCaseNamingPolicy()
            });

            File.WriteAllText(_settingsPath, jsonContent);
            MessageBox.Show($"Saved settings file: {_settingsPath}");

            // 保存 AddIn 设置
            if (_addinConfig != null)
            {
                _addinConfig.AddInList = _addinCheckBoxes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.IsChecked ?? false
                );

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                File.WriteAllText(_addinConfigPath, JsonSerializer.Serialize(_addinConfig, options));
            }

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

public class JsonLowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return char.ToLower(name[0]) + name[1..];
    }
}

public class AddInConfig
{
    public string ExcelMacroAddinPath { get; set; } = string.Empty;
    public Dictionary<string, bool> AddInList { get; set; }
}
