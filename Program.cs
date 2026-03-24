using System;
using ImageMagick;
using CommandLine;

class Program
{
    [Verb("encode", HelpText = "Encode message into image")]
    public class EncodeOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input image")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output image")]
        public string Output { get; set; }

        [Option('m', "message", Required = true, HelpText = "Message")]
        public string Message { get; set; }
    }

    [Verb("decode", HelpText = "Decode message from image")]
    public class DecodeOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input image")]
        public string Input { get; set; }
    }

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<EncodeOptions, DecodeOptions>(args)
            .WithParsed<EncodeOptions>(opts => 
            {
                WriteExif(opts.Input, opts.Output, opts.Message);
                Console.WriteLine("Done");
            })
            .WithParsed<DecodeOptions>(opts => 
            {
                Console.WriteLine(ReadExif(opts.Input));
            });
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
        if (value == null || value.Value == null) return "Exif is Empty";
        return System.Text.Encoding.UTF8.GetString(value.Value);
    }
}