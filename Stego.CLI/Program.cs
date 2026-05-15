using System;
using System.Text;
using CommandLine;
using Stego.Core;

namespace Stego.CLI;

class Program
{
    private static readonly StegoImageService Service = new();

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
                var result = Service.Encode(opts.Input, opts.Output, opts.Message);
                if (result.Success)
                {
                    Console.WriteLine($"Done: {result.OutputPath}");
                    Environment.ExitCode = 0;
                    return;
                }

                Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                Environment.ExitCode = (int)result.ErrorCode;
            })
            .WithParsed<DecodeOptions>(opts => 
            {
                var result = Service.Decode(opts.Input);
                if (result.Success)
                {
                    Console.WriteLine(result.Message);
                    Environment.ExitCode = 0;
                    return;
                }

                Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                Environment.ExitCode = (int)result.ErrorCode;
            });
    }
}