using System.Text;
using ImageMagick;

namespace Stego.Core;

public sealed class StegoImageService
{
    private const int MaxExifBytes = 65000;

    public static bool IsSupportedFormat(string filePath)
    {
        var supported = new[] { "jpg", "jpeg", "png", "bmp", "tif", "tiff" };
        var extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        return supported.Contains(extension);
    }

    public StegoEncodeResult Encode(string inputPath, string outputPath, string message)
    {
        if (!File.Exists(inputPath))
        {
            return StegoEncodeResult.Fail(
                StegoErrorCode.FileNotFound,
                $"Image not found: {inputPath}");
        }

        if (!IsSupportedFormat(inputPath) || !IsSupportedFormat(outputPath))
        {
            return StegoEncodeResult.Fail(
                StegoErrorCode.UnsupportedFormat,
                "Supported formats: JPG, PNG, BMP, TIFF.");
        }

        var bytes = Encoding.UTF8.GetBytes(message);
        if (bytes.Length > MaxExifBytes)
        {
            return StegoEncodeResult.Fail(
                StegoErrorCode.MessageTooLarge,
                $"Message is too large ({bytes.Length} bytes). Maximum: {MaxExifBytes} bytes.");
        }

        try
        {
            using var image = new MagickImage(inputPath);
            var profile = image.GetExifProfile() ?? new ExifProfile();
            profile.SetValue(ExifTag.UserComment, bytes);
            image.SetProfile(profile);
            image.Write(outputPath);

            using var verify = new MagickImage(outputPath);
            var verifyProfile = verify.GetExifProfile();
            if (verifyProfile == null || verifyProfile.GetValue(ExifTag.UserComment) == null)
            {
                return StegoEncodeResult.Fail(
                    StegoErrorCode.SaveFailed,
                    "EXIF data was not saved. Try JPG, PNG, BMP, or TIFF output.");
            }

            return StegoEncodeResult.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return StegoEncodeResult.Fail(StegoErrorCode.UnknownError, ex.Message);
        }
    }

    public StegoDecodeResult Decode(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            return StegoDecodeResult.Fail(
                StegoErrorCode.FileNotFound,
                $"Image not found: {inputPath}");
        }

        if (!IsSupportedFormat(inputPath))
        {
            return StegoDecodeResult.Fail(
                StegoErrorCode.UnsupportedFormat,
                "Supported formats: JPG, PNG, BMP, TIFF.");
        }

        try
        {
            using var image = new MagickImage(inputPath);
            var profile = image.GetExifProfile();

            if (profile == null)
            {
                return StegoDecodeResult.Fail(
                    StegoErrorCode.NoExifData,
                    "No EXIF data found.");
            }

            var value = profile.GetValue(ExifTag.UserComment);
            if (value == null || value.Value == null)
            {
                return StegoDecodeResult.Fail(
                    StegoErrorCode.EmptyExif,
                    "EXIF is empty.");
            }

            return StegoDecodeResult.Ok(Encoding.UTF8.GetString(value.Value));
        }
        catch (Exception ex)
        {
            return StegoDecodeResult.Fail(StegoErrorCode.UnknownError, ex.Message);
        }
    }
}