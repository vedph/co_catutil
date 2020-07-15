using Catutil.Migration.Entries;
using Catutil.Migration.Sql;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Configuration;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries.Filters;
using SimpleInjector;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class EntryParserFactoryTest
    {
        private static EntryParserFactory _factory;

        private static string LoadTextResource(string name)
        {
            using (StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"Catutil.Migration.Test.Assets.{name}"),
                Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void Init()
        {
            if (_factory != null) return;

            // get the connection string
            const string cs = "Server=localhost;Database=catullus;Uid=root;Pwd=mysql;";

            // load the pipeline configuration
            string json = LoadTextResource("Pipeline.json");
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryJson(json);
            IConfiguration pipelineCfg = builder.Build();

            // create the pipeline
            Container container = new Container();
            EntryParserFactory.ConfigureServices(container);
            _factory = new EntryParserFactory(container, pipelineCfg)
            {
                ConnectionString = cs
            };
        }

        [Fact]
        public void GetEntryReader_Ok()
        {
            Init();
            IEntryReader reader = _factory.GetEntryReader();
            Assert.NotNull(reader);
            Assert.IsType<SqlEntryReader>(reader);
        }

        // TODO: GetRegionDetectors

        [Fact]
        public void GetEntryFilters_Ok()
        {
            Init();
            IList<IEntryFilter> filters = _factory.GetEntryFilters();
            Assert.Equal(1, filters.Count);
            Assert.IsType<EscapeEntryFilter>(filters[0]);
        }

        [Fact]
        public void GetRegionFilters_Ok()
        {
            Init();
            IList<IEntryRegionFilter> filters = _factory.GetRegionFilters();
            Assert.Equal(1, filters.Count);
            Assert.IsType<UnmappedEntryRegionFilter>(filters[0]);
        }

        [Fact]
        public void GetRegionParsers_Ok()
        {
            Init();
            IList<IEntryRegionParser> parsers = _factory.GetRegionParsers();
            Assert.Equal(1, parsers.Count);
            Assert.IsType<ExcelDumpEntryRegionParser>(parsers[0]);
        }
    }
}
