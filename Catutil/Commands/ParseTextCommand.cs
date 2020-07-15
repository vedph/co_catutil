using Catutil.Migration.Entries;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Proteus.Core.Entries;
using Proteus.Entries;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Catutil.Commands
{
    public sealed class ParseTextCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;
        private readonly string _pipelineCfgPath;

        public ParseTextCommand(AppOptions options, string dbName,
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
            command.Description = "Parse CO text.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbNameArgument = command.Argument("[db-name]",
                "The database name");
            CommandArgument pipelineCfgArgument = command.Argument("[pipeline-cfg]",
                "The pipeline configuration path");

            command.OnExecute(() =>
            {
                options.Command = new ParseTextCommand(
                    options,
                    dbNameArgument.Value,
                    pipelineCfgArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARSE TEXT\n");
            Console.ResetColor();
            Console.WriteLine(
                $"DB name: {_dbName}\n" +
                $"Pipeline config: {_pipelineCfgPath}\n");

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
                ConnectionString = cs
            };
            pipeline.Configure(factory);

            IEntryReader entryReader = factory.GetEntryReader();
            List<DecodedEntry> entries = new List<DecodedEntry>();
            EntrySetReaderContext context = new EntrySetReaderContext();
            DecodedEntry entry;
            int count = 0;

            Console.WriteLine("Reading entries: ");
            while ((entry = entryReader.Read()) != null)
            {
                if (++count % 10 == 0) Console.Write('.');
                entries.Clear();
                entries.Add(entry);
                EntrySet set = new EntrySet(entries, context);
                pipeline.Execute<object>(set, null);
            }
            Console.WriteLine($"\nEntries read: {count}");

            return Task.CompletedTask;
        }
    }
}
