using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Serilog;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Catutil.Migration.Sql;
using Cadmus.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Catutil.Migration;

namespace Catutil.Commands
{
    public sealed class ParseTextCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;
        private readonly string _outputDir;
        private readonly int _maxItemPerFile;

        public ParseTextCommand(AppOptions options, string dbName,
            string outputDir, int maxItemPerFile)
        {
            _config = options?.Configuration ??
                throw new ArgumentNullException(nameof(options));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _outputDir = outputDir
                ?? throw new ArgumentNullException(nameof(outputDir));
            _maxItemPerFile = maxItemPerFile;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            command.Description = "Parse text lines from MySql database into JSON files";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbNameArgument = command.Argument("[db-name]",
                "The source database name");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory");
            CommandOption maxItemPerFileOption = command.Option("-m|--max",
                "Max number of items per output file",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                int max = 100;
                if (maxItemPerFileOption.HasValue()
                    && int.TryParse(maxItemPerFileOption.Value(), out int n))
                {
                    max = n;
                }
                options.Command = new ParseTextCommand(
                    options,
                    dbNameArgument.Value,
                    outputDirArgument.Value,
                    max);
                return 0;
            });
        }

        private static void CloseOutputFile(TextWriter writer)
        {
            writer.WriteLine("]");
            writer.Flush();
            writer.Close();
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARSE SQL TEXT\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Database name: {_dbName}\n" +
                $"Output dir: {_outputDir}\n" +
                $"Max items per file: {_maxItemPerFile}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("PARSE SQL TEXT");

            // get the connection string
            string csTemplate = _config.GetConnectionString(_dbName);
            string cs = string.Format(csTemplate, _dbName);

            SqlTextParser parser = new SqlTextParser(cs,
                new StandardPartitioner())
            {
                Logger = loggerFactory.CreateLogger("parse-sql-text")
            };

            int itemCount = 0;
            int fileItemCount = 0, fileNr = 0;
            TextWriter writer = null;
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            };
            IItem item;

            if (!Directory.Exists(_outputDir)) Directory.CreateDirectory(_outputDir);

            while ((item = parser.Read()) != null)
            {
                itemCount++;
                fileItemCount++;
                Console.WriteLine(item.Title);

                // create new output file if required
                if (writer == null
                    || (_maxItemPerFile > 0 && fileItemCount > _maxItemPerFile))
                {
                    if (writer != null) CloseOutputFile(writer);
                    string path = Path.Combine(_outputDir,
                        $"{_dbName}_{++fileNr:00000}.json");

                    writer = new StreamWriter(new FileStream(path,
                        FileMode.Create, FileAccess.Write, FileShare.Read),
                        Encoding.UTF8);
                    writer.WriteLine("[");

                    fileItemCount = 0;
                }

                // dump item into it
                string json = JsonConvert.SerializeObject(item, jsonSettings);
                // string json = JsonSerializer.Serialize(item, typeof(object), options);

                // this will output a , also for the last JSON array item,
                // but we don't care about it -- that's just a dump, and
                // it's easy to ignore/remove it if needed.
                writer.WriteLine(json + ",");
            }

            Console.WriteLine($"Output items: {itemCount}");
            return Task.CompletedTask;
        }
    }
}
