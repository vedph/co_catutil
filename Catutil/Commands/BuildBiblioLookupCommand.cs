using Catutil.Migration.Xls;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Catutil.Commands
{
    public sealed class BuildBiblioLookupCommand : ICommand
    {
        private readonly string _xlsFilePath;
        private readonly string _jsonFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildBiblioLookupCommand"/> class.
        /// </summary>
        /// <param name="xlsFilePath">The XLS path.</param>
        /// <param name="jsonFilePath">The JSON path.</param>
        /// <exception cref="ArgumentNullException">xlsFilePath or
        /// jsonFilePath</exception>
        public BuildBiblioLookupCommand(string xlsFilePath, string jsonFilePath)
        {
            _xlsFilePath = xlsFilePath
                ?? throw new ArgumentNullException(nameof(xlsFilePath));
            _jsonFilePath = jsonFilePath
                ?? throw new ArgumentNullException(nameof(jsonFilePath));
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
            CommandArgument jsonPathArgument = command.Argument("[json-path]",
                "The output JSON file path");

            command.OnExecute(() =>
            {
                options.Command = new BuildBiblioLookupCommand(
                    xlsPathArgument.Value,
                    jsonPathArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("BUILD BIBLIOGRAPHY LOOKUP\n");
            Console.ResetColor();
            Console.WriteLine(
                $"XLS path : {_xlsFilePath}\n" +
                $"JSON path: {_jsonFilePath}\n");

            XlsBiblioLookup lookup = new XlsBiblioLookup();
            lookup.ExtractJson(_xlsFilePath, _jsonFilePath, CancellationToken.None,
                new Progress<ProgressReport>(r => Console.WriteLine(r.Message)));

            return Task.CompletedTask;
        }
    }
}
