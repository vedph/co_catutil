using Catutil.Migration.Sql;
using Catutil.Migration.Xls;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using ShellProgressBar;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catutil.Commands
{
    public sealed class ImportXlsTextCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _inputDir;
        private readonly string _fileMask;
        private readonly string _dbName;
        private readonly bool _dry;

        public ImportXlsTextCommand(AppOptions options, string inputDir,
            string fileMask, string dbName, bool dry)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _config = options.Configuration;
            _inputDir = inputDir ??
                throw new ArgumentNullException(nameof(inputDir));
            _fileMask = fileMask ??
                throw new ArgumentNullException(nameof(fileMask));
            _dbName = dbName ??
                throw new ArgumentNullException(nameof(dbName));

            _dry = dry;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Import CO Excel texts into a MySql DB.";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputDirArgument = command.Argument("[input-dir]",
                "The input files directory");
            CommandArgument fileMaskArgument = command.Argument("[file-mask]",
                "The input files mask");
            CommandArgument dbNameArgument = command.Argument("[db-name]",
                "The target database name");

            CommandOption dryOption = command.Option("-d|--dry",
                "Dry run", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new ImportXlsTextCommand(
                    options,
                    inputDirArgument.Value,
                    fileMaskArgument.Value,
                    dbNameArgument.Value,
                    dryOption.HasValue());
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("IMPORT TEXT\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input dir: {_inputDir}\n" +
                $"Input mask: {_fileMask}\n" +
                $"DB name: {_dbName}\n" +
                $"Dry run: {(_dry ? "yes" : "no")}\n");

            string csTemplate = _config.GetConnectionString("Catullus");
            string cs = string.Format(csTemplate, _dbName);
            XlsTextImporter importer = new XlsTextImporter(cs)
            {
                IsDryRunEnabled = _dry
            };

            IDbManager manager = new MySqlDbManager(csTemplate);
            if (!manager.Exists(_dbName))
            {
                manager.CreateDatabase(
                    _dbName,
                    XlsTextImporter.GetTargetSchema(),
                    null);
            }

            using (var bar = new ProgressBar(100, "Importing...",
                new ProgressBarOptions
                {
                    ProgressCharacter = '.',
                    ProgressBarOnBottom = true,
                    DisplayTimeInRealTime = false
                }))
            {
                foreach (string filePath in Directory.GetFiles(
                    _inputDir, _fileMask)
                    .OrderBy(s => s))
                {
                    bar.Message = Path.GetFileName(filePath);

                    importer.Import(filePath, CancellationToken.None,
                        new Progress<ProgressReport>(
                            r => bar.Tick(r.Percent, r.Message)));
                }
            }

            return Task.CompletedTask;
        }
    }
}
