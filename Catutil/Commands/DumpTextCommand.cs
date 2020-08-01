using System;
using Microsoft.Extensions.CommandLineUtils;
using System.Threading.Tasks;
using Catutil.Migration.Xls;
using System.IO;
using System.Linq;
using NPOI.SS.Formula.Functions;

namespace Catutil.Commands
{
    public sealed class DumpTextCommand : ICommand
    {
        private readonly string _inputDir;
        private readonly string _fileMask;

        public DumpTextCommand(AppOptions options, string inputDir,
            string fileMask)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _inputDir = inputDir ??
                throw new ArgumentNullException(nameof(inputDir));
            _fileMask = fileMask ??
                throw new ArgumentNullException(nameof(fileMask));
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Dump Excel texts.";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputDirArgument = command.Argument("[input-dir]",
                "The input files directory");
            CommandArgument fileMaskArgument = command.Argument("[file-mask]",
                "The input files mask");

            command.OnExecute(() =>
            {
                options.Command = new DumpTextCommand(
                    options,
                    inputDirArgument.Value,
                    fileMaskArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("DUMP TEXT\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input dir: {_inputDir}\n" +
                $"Input mask: {_fileMask}\n");

            int[] counts = new int[3];
            foreach (string filePath in Directory.GetFiles(
                _inputDir, _fileMask)
                .OrderBy(s => s))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(filePath);
                Console.ResetColor();

                using (XlsTextReader reader = new XlsTextReader(filePath))
                {
                    foreach (XlsTextReaderItem item in reader.Read())
                    {
                        Console.WriteLine(item);
                        counts[item.Level]++;

                        foreach (string key in item.Data.Keys)
                        {
                            Console.WriteLine($"  {key}={item.Data[key]}");
                        }
                    }
                }
            }

            for (int i = 0; i < 3; i++)
                Console.WriteLine($"L{i}={counts[i]}");
            
            return Task.CompletedTask;
        }
    }
}
