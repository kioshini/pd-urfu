using System;
using System.Runtime.InteropServices;
using System.Windows;
using Stego.Core;

namespace Stego;

public partial class App : Application
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Если есть аргументы → CLI режим
        if (e.Args.Length > 0)
        {
            // Привязываемся к консоли родительского процесса
            AttachConsole(ATTACH_PARENT_PROCESS);
            HandleCliMode(e.Args);
            Shutdown(0);
            return;
        }

        // Если аргументов нет → GUI режим
        base.OnStartup(e);
        // No startup diagnostics popup
        // Ensure Stego.GUI resource dictionaries (themes, languages) are merged into the host Application
        try
        {
            var guiResourcePaths = new[]
            {
                "Languages/russian.xaml",
                "Languages/english.xaml",
                "Themes/Light.xaml",
                "Themes/Light.xaml"
            };

            foreach (var relative in guiResourcePaths)
            {
                try
                {
                    var rd = new System.Windows.ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Stego.GUI;component/{relative}", UriKind.Absolute)
                    };

                    // Add in the same order as Stego.GUI App.xaml so indexes match
                    Application.Current.Resources.MergedDictionaries.Add(rd);
                }
                catch
                {
                    // ignore missing resources
                }
            }
        }
        catch
        {
            // ignore merging failures
        }

        MainWindow = new Stego.GUI.MainWindow();
        MainWindow.Show();
    }

    private static void HandleCliMode(string[] args)
    {
        var service = new StegoImageService();

        try
        {
            if (args[0].Equals("encode", StringComparison.OrdinalIgnoreCase))
            {
                string? inputPath = GetArgValue(args, "-i");
                string? outputPath = GetArgValue(args, "-o");
                string? message = GetArgValue(args, "-m");

                if (inputPath == null || outputPath == null || message == null)
                {
                    Console.Error.WriteLine("✗ Ошибка: используйте -i <input> -o <output> -m <message>");
                    Console.Error.Flush();
                    Environment.ExitCode = 1;
                    return;
                }

                var result = service.Encode(inputPath, outputPath, message);

                if (result.Success)
                {
                    Console.WriteLine($"✓ Готово: {result.OutputPath}");
                    Console.Out.Flush();
                    Environment.ExitCode = 0;
                }
                else
                {
                    Console.Error.WriteLine($"✗ Ошибка: {result.ErrorMessage}");
                    Console.Error.Flush();
                    Environment.ExitCode = (int)result.ErrorCode;
                }
            }
            else if (args[0].Equals("decode", StringComparison.OrdinalIgnoreCase))
            {
                string? inputPath = GetArgValue(args, "-i");

                if (inputPath == null)
                {
                    Console.Error.WriteLine("✗ Ошибка: используйте -i <input>");
                    Console.Error.Flush();
                    Environment.ExitCode = 1;
                    return;
                }

                var result = service.Decode(inputPath);

                if (result.Success)
                {
                    Console.WriteLine(result.Message);
                    Console.Out.Flush();
                    Environment.ExitCode = 0;
                }
                else
                {
                    Console.Error.WriteLine($"✗ Ошибка: {result.ErrorMessage}");
                    Console.Error.Flush();
                    Environment.ExitCode = (int)result.ErrorCode;
                }
            }
            else
            {
                Console.Error.WriteLine($"✗ Неизвестная команда: {args[0]}");
                Console.Error.WriteLine("Используйте: stego encode -i <input> -o <output> -m <message>");
                Console.Error.WriteLine("             stego decode -i <input>");
                Console.Error.Flush();
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Ошибка: {ex.Message}");
            Console.Error.Flush();
            Environment.ExitCode = 1;
        }
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == key)
                return args[i + 1];
        }
        return null;
    }
}
