using Catutil.Migration.Entries;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Entries;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Catutil.Commands
{
    public sealed class ParseApparatusCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;
        private readonly string _pipelineCfgPath;

        public ParseApparatusCommand(AppOptions options, string dbName,
            string pipelineCfgPath)
        {
            _config = options?.Configuration ??
                throw new ArgumentNullException(nameof(options));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _pipelineCfgPath = pipelineCfgPath ??
                throw new ArgumentNullException(nameof(pipelineCfgPath));
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Parse apparatus from MySql database " +
                "using a Proteus-based pipeline.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbNameArgument = command.Argument("[db-name]",
                "The database name");
            CommandArgument pipelineCfgArgument = command.Argument("[pipeline-cfg]",
                "The pipeline configuration path");

            command.OnExecute(() =>
            {
                options.Command = new ParseApparatusCommand(
                    options,
                    dbNameArgument.Value,
                    pipelineCfgArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARSE APPARATUS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"DB name: {_dbName}\n" +
                $"Pipeline config: {_pipelineCfgPath}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("PARSE APPARATUS");

            // get the connection string
            string csTemplate = _config.GetConnectionString("Catullus");
            string cs = string.Format(csTemplate, _dbName);

            // load the pipeline configuration
            Console.WriteLine("Building the pipeline...");
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(_pipelineCfgPath);
            IConfiguration pipelineCfg = builder.Build();

            // create the pipeline
            EntryPipeline pipeline = new EntryPipeline();
            Container container = new Container();
            EntryParserFactory.ConfigureServices(container);
            EntryParserFactory factory = new EntryParserFactory(container, pipelineCfg)
            {
                ConnectionString = cs,
                Logger = loggerFactory.CreateLogger("parse-app"),
            };
            pipeline.Configure(factory);

            IEntryReader entryReader = factory.GetEntryReader();
            List<DecodedEntry> entries = new List<DecodedEntry>();
            EntrySetReaderContext readerContext = new EntrySetReaderContext();
            DecodedEntry entry;
            int count = 0;

            Console.WriteLine("Reading entries: ");
            IParserContext parserContext = factory.GetParserContext();
            parserContext?.Start();

            try
            {
                while ((entry = entryReader.Read()) != null)
                {
                    if (++count % 10 == 0) Console.Write('.');

                    entries.Clear();
                    entries.Add(entry);
                    EntrySet set = new EntrySet(entries, readerContext);
                    readerContext.Number++;

                    pipeline.Execute<object>(set, parserContext);
                }
            }
            finally
            {
                parserContext?.End();
            }
            Console.WriteLine($"\nEntries read: {count}");

            return Task.CompletedTask;
        }
    }
}
