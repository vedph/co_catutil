using Catutil.Migration.Xls;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Catutil.Commands
{
    public sealed class BuildBiblioLookupCommand : ICommand
    {
        private readonly string _xlsFilePath;
        private readonly string _outputDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildBiblioLookupCommand"/>
        /// class.
        /// </summary>
        /// <param name="xlsFilePath">The XLS path.</param>
        /// <param name="outputDir">The output directory.</param>
        /// <exception cref="ArgumentNullException">xlsFilePath or outputDir
        /// </exception>
        public BuildBiblioLookupCommand(string xlsFilePath, string outputDir)
        {
            _xlsFilePath = xlsFilePath
                ?? throw new ArgumentNullException(nameof(xlsFilePath));
            _outputDir = outputDir
                ?? throw new ArgumentNullException(nameof(outputDir));
        }

        /// <summary>
        /// Configures the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="options">The options.</param>
        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Build bibliography lookup data.";
            command.HelpOption("-?|-h|--help");

            CommandArgument xlsPathArgument = command.Argument("[xls-path]",
                "The source XLS file path");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory");

            command.OnExecute(() =>
            {
                options.Command = new BuildBiblioLookupCommand(
                    xlsPathArgument.Value,
                    outputDirArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("BUILD BIBLIOGRAPHY LOOKUP\n");
            Console.ResetColor();
            Console.WriteLine(
                $"XLS path: {_xlsFilePath}\n" +
                $"output dir: {_outputDir}\n");

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            XlsBiblioLookup lookup = new XlsBiblioLookup();
            lookup.ExtractIndex(_xlsFilePath, _outputDir, CancellationToken.None,
                new Progress<ProgressReport>(r => Console.WriteLine(r.Message)));

            return Task.CompletedTask;
        }
    }
}
