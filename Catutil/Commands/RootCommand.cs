using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Threading.Tasks;

namespace Catutil.Commands
{
    public sealed class RootCommand : ICommand
    {
        private readonly CommandLineApplication _app;

        public RootCommand(CommandLineApplication app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public static void Configure(CommandLineApplication app, AppOptions options)
        {
            // configure all the app commands here
            app.Command("dump-text", c => DumpTextCommand.Configure(c, options));
            app.Command("import-text", c => ImportTextCommand.Configure(c, options));
            app.Command("parse-text", c => ParseTextCommand.Configure(c, options));
            app.Command("build-biblio", c => BuildBiblioLookupCommand.Configure(c, options));

            app.OnExecute(() =>
            {
                options.Command = new RootCommand(app);
                return 0;
            });
        }

        public Task Run()
        {
            _app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}
