﻿using Cadmus.Core;
using Cadmus.Core.Config;
using Cadmus.Core.Storage;
using Cadmus.Mongo;
using Catutil.Migration;
using Catutil.Services;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Catutil.Commands
{
    /// <summary>
    /// Import JSON dumps representing text and apparatus. Requires a profile
    /// for the target database.
    /// </summary>
    /// <seealso cref="Mqutil.Commands.ICommand" />
    public sealed class ImportJsonCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _txtFileDir;
        private readonly string _txtFileMask;
        private readonly string _appFileDir;
        private readonly string _appFileMask;
        private readonly string _profilePath;
        private readonly string _database;
        private readonly bool _dry;
        private readonly bool _regexMask;
        private readonly IRepositoryProvider _repositoryProvider;

        public ImportJsonCommand(AppOptions options,
            string txtFileDir,
            string txtFileMask,
            string appFileDir,
            string appFileMask,
            string profilePath,
            string database,
            bool dry,
            bool regexMask)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _txtFileDir = txtFileDir
                ?? throw new ArgumentNullException(nameof(txtFileDir));
            _txtFileMask = txtFileMask
                ?? throw new ArgumentNullException(nameof(txtFileMask));
            _appFileDir = appFileDir
                ?? throw new ArgumentNullException(nameof(appFileDir));
            _appFileMask = appFileMask
                ?? throw new ArgumentNullException(nameof(appFileMask));
            _profilePath = profilePath
                ?? throw new ArgumentNullException(nameof(profilePath));
            _database = database
                ?? throw new ArgumentNullException(nameof(database));
            _dry = dry;
            _regexMask = regexMask;

            _config = options.Configuration;
            _repositoryProvider = new StandardRepositoryProvider(_config);
        }

        /// <summary>
        /// Configures the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">command</exception>
        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Import text and layers from JSON dumps";
            command.HelpOption("-?|-h|--help");

            CommandArgument txtDirArgument = command.Argument("[txt-dir]",
                "The input JSON text files directory");
            CommandArgument txtMaskArgument = command.Argument("[txt-mask]",
                "The input JSON text files mask");
            CommandArgument appDirArgument = command.Argument("[app-dir]",
                "The input JSON apparatus files directory");
            CommandArgument appMaskArgument = command.Argument("[app-mask]",
                "The input JSON apparatus files mask");
            CommandArgument profileArgument = command.Argument("[profile]",
                "The JSON profile file path");
            CommandArgument databaseArgument = command.Argument("[database]",
                "The database name");

            CommandOption dryOption = command.Option("-d|--dry", "Dry run",
                CommandOptionType.NoValue);
            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new ImportJsonCommand(
                    options,
                    txtDirArgument.Value,
                    txtMaskArgument.Value,
                    appDirArgument.Value,
                    appMaskArgument.Value,
                    profileArgument.Value,
                    databaseArgument.Value,
                    dryOption.HasValue(),
                    regexMaskOption.HasValue());
                return 0;
            });
        }

        private static string LoadProfile(string path)
        {
            using StreamReader reader = File.OpenText(path);
            return reader.ReadToEnd();
        }

        private JsonImporter GetImporter(ILoggerFactory loggerFactory)
        {
            // create repository and importer
            ICadmusRepository repository =
                _repositoryProvider.CreateRepository(_database);

            return new JsonImporter(repository)
            {
                Logger = loggerFactory.CreateLogger("json-importer"),
                IsDry = _dry
            };
        }

        private void CreateDatabase()
        {
            string connection = string.Format(CultureInfo.InvariantCulture,
                _config.GetConnectionString("Default"),
                _database);

            IDatabaseManager manager = new MongoDatabaseManager();

            string profileContent = LoadProfile(_profilePath);
            IDataProfileSerializer serializer = new JsonDataProfileSerializer();
            DataProfile profile = serializer.Read(profileContent);

            if (!manager.DatabaseExists(connection))
            {
                Console.WriteLine("Creating database...");
                Log.Information($"Creating database {_database}...");

                manager.CreateDatabase(connection, profile);

                Console.WriteLine("Database created.");
                Log.Information("Database created.");
            }
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("IMPORT JSON TEXT AND APPARATUS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Text dir:  {_txtFileDir}\n" +
                $"Text mask: {_txtFileMask}\n" +
                $"Apparatus dir: {_appFileDir}\n" +
                $"Apparatus mask: {_appFileMask}\n" +
                $"Profile file: {_profilePath}\n" +
                $"Database: {_database}\n" +
                $"Dry run: {_dry}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("IMPORT JSON TEXT AND APPARATUS");

            if (!_dry)
            {
                // create database if not exists
                CreateDatabase();
            }
            else
            {
                if (!File.Exists(_profilePath))
                {
                    string error = "Profile path not found: " + _profilePath;
                    Console.WriteLine(error);
                    Log.Error(error);
                    return Task.CompletedTask;
                }
            }

            JsonImporter importer = GetImporter(loggerFactory);

            // 1) import text
            string[] files = FileEnumerator.Enumerate(
                _txtFileDir, _txtFileMask, _regexMask).ToArray();
            Console.WriteLine($"Importing text from {files.Length} file(s)...");

            foreach (string txtFilePath in files)
            {
                Console.WriteLine(txtFilePath);

                using (Stream stream = new FileStream(txtFilePath, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                {
                    importer.ImportText(stream);
                }
            }

            // 2) import apparatus
            files = FileEnumerator.Enumerate(
                _appFileDir, _appFileMask, _regexMask).ToArray();
            Console.WriteLine($"Importing apparatus from {files.Length} file(s)...");

            foreach (string appFilePath in files)
            {
                Console.WriteLine(appFilePath);
                using (Stream stream = new FileStream(appFilePath, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                {
                    importer.ImportApparatus(stream);
                }
            }

            return Task.CompletedTask;
        }
    }
}
