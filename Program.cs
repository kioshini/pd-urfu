using System;
using ImageMagick;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("encode <input> <output> <message>");
            Console.WriteLine("decode <input>");
            return;
        }

        var command = args[0];

        if (command == "encode" && args.Length == 4)
        {
            var input = args[1];
            var output = args[2];
            var message = args[3];

            WriteExif(input, output, message);
            Console.WriteLine("Done");
        }
        else if (command == "decode" && args.Length == 2)
        {
            var input = args[1];

            var result = ReadExif(input);
            Console.WriteLine(result);
        }
        else
        {
            Console.WriteLine("Invalid arguments");
        }
    }

    static void WriteExif(string input, string output, string message)
    {
        using var image = new MagickImage(input);

        var profile = image.GetExifProfile() ?? new ExifProfile();
        var bytes = System.Text.Encoding.UTF8.GetBytes(message);

        profile.SetValue(ExifTag.UserComment, bytes);

        image.SetProfile(profile);
        image.Write(output);
    }

    static string ReadExif(string input)
    {
        using var image = new MagickImage(input);
        var profile = image.GetExifProfile();

        if (profile == null) return "No Exif";

        var value = profile.GetValue(ExifTag.UserComment);
        
        if (value == null || value.Value == null)
            return "Exif is Empty";

        return System.Text.Encoding.UTF8.GetString(value.Value);
    }
}