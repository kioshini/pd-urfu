using System;
using System.IO;
using Path = System.IO.Path;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ImageMagick;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.IO.Pipes;
using System.Reflection.Metadata;

namespace Stego.GUI;

public partial class MainWindow : Window
{
    private string? _encodeImagePath;
    private string? _decodeImagePath;
    List<ThemeProperties> Themes = new List<ThemeProperties>();
    List<(string parameter, string path)> Languages;

    public class ThemeProperties
    {
        public required string path {  get; set; }
        public required string textColor { get; set; }
        public required string primaryBG { get; set; }
        public required string secondaryBG { get; set; }
    }

    public MainWindow()
    {
        InitializeComponent();
        ResetEncodeFileInfo();
        ResetDecodeFileInfo();
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void DropZoneEncode_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0) SelectEncodeImage(files[0]);
        }
    }

    private void DropZoneDecode_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0) SelectDecodeImage(files[0]);
        }
    }

    private void SelectEncodeImage(string filePath)
    {
        if (!IsSupportedFormat(filePath))
        {
            MessageBox.Show(
                "Поддерживаются только JPG, PNG, BMP, TIFF.",
                "Неподдерживаемый формат",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        _encodeImagePath = filePath;
        EncodeDropText.Text = $"Выбрано: {Path.GetFileName(filePath)}";

        DisplayImagePreview(filePath, EncodeImagePreview);
        OutputPathInput.Text = BuildDefaultOutputPath(filePath);
        UpdateEncodeFileInfo(filePath);
    }

    private void SelectDecodeImage(string filePath)
    {
        if (!IsSupportedFormat(filePath))
        {
            MessageBox.Show(
                "Поддерживаются только JPG, PNG, BMP, TIFF.",
                "Неподдерживаемый формат",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        _decodeImagePath = filePath;
        DecodeDropText.Text = $"Выбрано: {Path.GetFileName(filePath)}";
        SaveDecodedTextButton.IsEnabled = false;
        DecodeResultText.Text = "Нет данных";

        DisplayImagePreview(filePath, DecodeImagePreview);
        DecodeOutputPathInput.Text = BuildDefaultDecodedTextPath(filePath);
        UpdateDecodeFileInfo(filePath);
        AutoDecodeSelectedImage();
    }

    private void EncodeBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectEncodeImage(dialog.FileName);
        }
    }

    private void DecodeBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectDecodeImage(dialog.FileName);
        }
    }

    private void OutputBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить изображение как",
            Filter = "JPEG Image (*.jpg)|*.jpg|PNG Image (*.png)|*.png|BMP Image (*.bmp)|*.bmp|TIFF Image (*.tiff)|*.tiff|All files (*.*)|*.*",
            FileName = Path.GetFileName(OutputPathInput.Text)
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPathInput.Text = dialog.FileName;
        }
    }

    private void DecodeOutputBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить текст как",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(DecodeOutputPathInput.Text)
                ? "decoded_message.txt"
                : Path.GetFileName(DecodeOutputPathInput.Text)
        };

        if (dialog.ShowDialog() == true)
        {
            DecodeOutputPathInput.Text = dialog.FileName;
        }
    }

    private void EncodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_encodeImagePath))
        {
            MessageBox.Show(
                "Сначала выберите изображение.",
                "Недостаточно данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(MessageInput.Text))
        {
            MessageBox.Show(
                "Введите текст для скрытия.",
                "Недостаточно данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPathInput.Text))
        {
            MessageBox.Show(
                "Укажите путь сохранения.",
                "Недостаточно данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        int messageSize = Encoding.UTF8.GetByteCount(MessageInput.Text);
        const int maxExifSize = 65000;

        if (messageSize > maxExifSize)
        {
            MessageBox.Show(
                $"Сообщение слишком большое.\n\n" +
                $"Текущий размер: {FormatFileSize(messageSize)}\n" +
                $"Максимум: {FormatFileSize(maxExifSize)}",
                "Ошибка размера",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            string outputPath = OutputPathInput.Text;

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            EncodeMessage(_encodeImagePath, outputPath, MessageInput.Text);
            MessageBox.Show(
                $"Текст успешно спрятан.\n\n{outputPath}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка: {ex.Message}",
                "Ошибка кодирования",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void AutoDecodeSelectedImage()
    {
        if (string.IsNullOrEmpty(_decodeImagePath))
        {
            return;
        }

        try
        {
            string message = DecodeMessage(_decodeImagePath);

            if (string.IsNullOrWhiteSpace(message) ||
                message.Equals("No EXIF data found", StringComparison.Ordinal) ||
                message.Equals("EXIF is empty", StringComparison.Ordinal))
            {
                DecodeResultText.Text = "Нет данных";
                SaveDecodedTextButton.IsEnabled = false;

                MessageBox.Show(
                    "В изображении не найдены данные EXIF (UserComment).",
                    "Нет данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            DecodeResultText.Text = message;
            SaveDecodedTextButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка: {ex.Message}",
                "Ошибка извлечения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SaveDecodedTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DecodeResultText.Text) ||
            DecodeResultText.Text.Equals("Нет данных", StringComparison.Ordinal))
        {
            MessageBox.Show(
                "Нет извлечённого текста для сохранения.",
                "Недостаточно данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(DecodeOutputPathInput.Text))
        {
            MessageBox.Show(
                "Укажите путь для сохранения текста.",
                "Недостаточно данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            string outputPath = DecodeOutputPathInput.Text;
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.WriteAllText(outputPath, DecodeResultText.Text, Encoding.UTF8);

            MessageBox.Show(
                $"Текст сохранён в файл.\n\n{outputPath}",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка: {ex.Message}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EncodeMessage(string inputPath, string outputPath, string message)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Image not found: {inputPath}");

        using var image = new MagickImage(inputPath);
        var profile = image.GetExifProfile() ?? new ExifProfile();
        var bytes = Encoding.UTF8.GetBytes(message);
        
        // Validate size before setting
        if (bytes.Length > 65000)
            throw new InvalidOperationException($"Message is too large ({FormatFileSize(bytes.Length)}). Maximum: {FormatFileSize(65000)}");
        
        profile.SetValue(ExifTag.UserComment, bytes);
        image.SetProfile(profile);
        image.Write(outputPath);

        using var verify = new MagickImage(outputPath);
        var verifyProfile = verify.GetExifProfile();
        if (verifyProfile == null)
            throw new InvalidOperationException("Failed to save EXIF data. Try JPG/PNG format.");
        
        var savedValue = verifyProfile.GetValue(ExifTag.UserComment);
        if (savedValue == null)
            throw new InvalidOperationException("EXIF data was not saved properly. Message may be too large for this image format.");
    }

    private string DecodeMessage(string inputPath)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Image not found: {inputPath}");

        using var image = new MagickImage(inputPath);
        var profile = image.GetExifProfile();

        if (profile == null)
            return "No EXIF data found";

        var value = profile.GetValue(ExifTag.UserComment);

        if (value == null || value.Value == null)
            return "EXIF is empty";

        return Encoding.UTF8.GetString(value.Value);
    }

    private void UpdateEncodeFileInfo(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        FileFormatValue.Text = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
        FileSizeValue.Text = FormatFileSize(fileInfo.Length);

        try
        {
            using var stream = File.OpenRead(filePath);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.DelayCreation,
                BitmapCacheOption.None);

            var frame = decoder.Frames[0];
            FileResolutionValue.Text = $"{frame.PixelWidth} x {frame.PixelHeight}";
        }
        catch
        {
            FileResolutionValue.Text = "-";
        }
    }

    private void ResetEncodeFileInfo()
    {
        FileFormatValue.Text = "-";
        FileSizeValue.Text = "-";
        FileResolutionValue.Text = "-";
    }

    private void UpdateDecodeFileInfo(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        DecodeFileFormatValue.Text = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
        DecodeFileSizeValue.Text = FormatFileSize(fileInfo.Length);

        try
        {
            using var stream = File.OpenRead(filePath);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.DelayCreation,
                BitmapCacheOption.None);

            var frame = decoder.Frames[0];
            DecodeFileResolutionValue.Text = $"{frame.PixelWidth} x {frame.PixelHeight}";
        }
        catch
        {
            DecodeFileResolutionValue.Text = "-";
        }
    }

    private void ResetDecodeFileInfo()
    {
        DecodeFileFormatValue.Text = "-";
        DecodeFileSizeValue.Text = "-";
        DecodeFileResolutionValue.Text = "-";
    }

    private static string BuildDefaultOutputPath(string inputPath)
    {
        string directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        string extension = Path.GetExtension(inputPath);
        return Path.Combine(directory, $"{fileName}_encoded{extension}");
    }

    private static string BuildDefaultDecodedTextPath(string inputPath)
    {
        string directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, $"{fileName}_decoded.txt");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private bool IsSupportedFormat(string filePath)
    {
        var supported = new[] { "jpg", "jpeg", "png", "bmp", "tif", "tiff" };
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        return supported.Contains(ext);
    }

    private void DisplayImagePreview(string imagePath, Image imageControl)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            imageControl.Source = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось загрузить предпросмотр: {ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            imageControl.Source = null;
        }
    }

    /// <summary>
    /// Looks through a folder and fetches XAML's parameter keyword and its path
    /// </summary>
    /// <param name="folder">where to look</param>
    /// <param name="parameter">what we looking for</param>
    /// <returns></returns>
    private List<(string parameter, string path)> FetchXAMLDataAndPath(string folder, string[] parameters)
    {
        var toReturn = new List<(string parameter, string path)> { };
        var baseDir = AppContext.BaseDirectory;
        var fullFolder = Path.Combine(baseDir, folder);

        if (!Directory.Exists(fullFolder))
        {
            return toReturn;
        }

        var paths = Directory.GetFiles(fullFolder, "*.xaml");

        foreach(var path in paths)
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    /* using filestrem with xamlReader and not uri because uri cheats by 
                    encoding data into exe and doesn't actually look at the new file

                    defeating the point of making this code being open like this
                    */
                    var currentXaml = (ResourceDictionary)XamlReader.Load(fileStream);

                    foreach(var parameter in parameters)
                    {
                        if (currentXaml.Contains(parameter) && currentXaml[parameter] != null)
                        {
                            toReturn.Add((currentXaml[parameter].ToString(), path));
                        }
                    }                        
                }
            }
            catch
            {
                continue;
            }
        }
        return toReturn;
    }

    private void LanguageCombo_DropDownOpened(object sender, EventArgs e)
    {
        LanguageCombo.Items.Clear();
        Languages = FetchXAMLDataAndPath("Languages", new string[]{ "Language"});
        for(int i = 0; i < Languages.Count; i++)
        {
            LanguageCombo.Items.Add(Languages[i].parameter);
        }
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach(var language in Languages)
        {
            if (language.parameter.Equals(LanguageCombo.SelectedItem))
            {
                using (FileStream fileStream = new FileStream(language.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    /* using filestrem with xamlReader and not uri because uri
                    doesn't actually look at the new file

                    defeating the point of making this code being open like this
                    */
                    var currentXaml = (ResourceDictionary)XamlReader.Load(fileStream);
                    Application.Current.Resources.MergedDictionaries[1] = currentXaml;
                    break;
                }
            }
        }

        Dispatcher.BeginInvoke(new Action(() => {
            LanguageCombo.Text = Application.Current.TryFindResource("Language").ToString();
            ThemeCombo.Text = Application.Current.TryFindResource("Theme").ToString();
        }));
    }

    /// <summary>
    /// do not look here
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private List<ThemeProperties> ThemesFiller(string folder, string[] parameters)
    {
        var returnList = new List<ThemeProperties>();
        var rawData = FetchXAMLDataAndPath(folder, parameters);
        if (rawData.Count == 0)
        {
            return returnList;
        }
        var piece = new ThemeProperties() { path = rawData[0].path, primaryBG = "", secondaryBG = "", textColor = "" };

        foreach(var data in rawData)
        {
            if (data.path.Equals(piece.path))
            {
                if (piece.textColor.Length == 0)
                {
                    piece.textColor = data.parameter;
                }
                else if (piece.primaryBG.Length == 0)
                {
                    piece.primaryBG = data.parameter;
                }
                else if (piece.secondaryBG.Length == 0)
                {
                    piece.secondaryBG = data.parameter;
                }
            }
            else
            {
                returnList.Add(piece);
                piece = new ThemeProperties() { path = data.path, primaryBG = "", secondaryBG = "", textColor = data.parameter };
            }
        }

        returnList.Add(piece);
        return returnList;
    }

    private void ThemeCombo_DropDownOpened(object sender, EventArgs e)
    {
        Themes.Clear();
        Themes = ThemesFiller("Themes", new string[] {"RegularText", "CardBackgroundBrush", "BodyBackground" });
        ThemeCombo.ItemsSource = Themes;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = ThemeCombo.SelectedItem as ThemeProperties;
        if (selectedItem != null) 
        {
            using (FileStream fileStream = new FileStream(selectedItem.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                /* using filestrem with xamlReader and not uri because uri
                doesn't actually look at the new file

                defeating the point of making this code being open like this
                */
                var currentXaml = (ResourceDictionary)XamlReader.Load(fileStream);
                Application.Current.Resources.MergedDictionaries[3] = currentXaml;
            }
        }

        Dispatcher.BeginInvoke(new Action(() => {
            ThemeCombo.Text = Application.Current.TryFindResource("Theme").ToString();
        }));
    }
}
