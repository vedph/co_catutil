using Catutil.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System;

namespace Catutil
{
    public sealed class AppOptions
    {
        public ICommand Command { get; set; }
        public IConfiguration Configuration { get; private set; }

        public AppOptions()
        {
            BuildConfiguration();
        }

        private void BuildConfiguration()
        {
            ConfigurationBuilder cb = new ConfigurationBuilder();
            Configuration = cb
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        public static AppOptions Parse(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            AppOptions options = new AppOptions();
            CommandLineApplication app = new CommandLineApplication
            {
                Name = "Catutil",
                FullName = "App CLI"
            };
            app.HelpOption("-?|-h|--help");

            // app-level options
            RootCommand.Configure(app, options);

            int result = app.Execute(args);
            return result != 0 ? null : options;
        }
    }
}
