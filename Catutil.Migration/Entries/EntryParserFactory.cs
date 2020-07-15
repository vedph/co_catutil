using Catutil.Migration.Sql;
using Fusi.Text.Unicode;
using Fusi.Tools.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Proteus.Core;
using Proteus.Core.Entries;
using Proteus.Core.Escapes;
using Proteus.Core.Regions;
using Proteus.Entries.Filters;
using Proteus.Entries.Regions;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// A parser factory for Proteus-based components, used to parse CO entries
    /// text.
    /// </summary>
    /// <seealso cref="Fusi.Tools.Config.ComponentFactoryBase" />
    public sealed class EntryParserFactory : ComponentFactoryBase
    {
        /// <summary>
        /// The name of the connection string property to be supplied
        /// in POCO option objects (<c>ConnectionString</c>).
        /// </summary>
        public const string CONNECTION_STRING_NAME = "ConnectionString";

        /// <summary>
        /// The optional general connection string to supply to any component
        /// requiring an option named <see cref="CONNECTION_STRING_NAME"/> 
        /// (=<c>ConnectionString</c>), when this option is not specified
        /// in its configuration.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the optional logger to be provided to components
        /// created by this factory.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryParserFactory" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="configuration">The configuration.</param>
        public EntryParserFactory(Container container, IConfiguration configuration)
            : base(container, configuration)
        {
        }

        private static object SupplyProperty(Type optionType,
            PropertyInfo property, object options, object defaultValue)
        {
            // if options have been loaded, supply if not specified
            if (options != null)
            {
                string value = (string)property.GetValue(options);
                if (string.IsNullOrEmpty(value))
                    property.SetValue(options, defaultValue);
            }
            // else create empty options and supply it
            else
            {
                options = Activator.CreateInstance(optionType);
                property.SetValue(options, defaultValue);
            }

            return options;
        }

        /// <summary>
        /// Does the custom configuration.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="component">The component.</param>
        /// <param name="section">The section.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="optionType">Type of the option.</param>
        /// <returns>True if custom configuration logic applied.</returns>
        protected override bool DoCustomConfiguration<T>(T component,
            IConfigurationSection section, TypeInfo targetType, Type optionType)
        {
            // get the options if specified
            object options = section?.Get(optionType);

            // if we have a default connection AND the options type
            // has a ConnectionString property, see if we should supply a value
            // for it
            PropertyInfo property;
            if (ConnectionString != null
                && (property = optionType.GetProperty(CONNECTION_STRING_NAME)) != null)
            {
                options = SupplyProperty(optionType, property, options, ConnectionString);
            } // conn

            // apply options if any
            if (options != null)
            {
                targetType.GetMethod("Configure").Invoke(component,
                    new[] { options });
            }

            return true;
        }

        /// <summary>
        /// Configures the container services to use components from
        /// <c>Proteus.Entries</c>.
        /// This is just a helper method: at any rate, the configuration of the
        /// container is external to the VSM factory. You could use this method
        /// as a model and create your own, or call this method to register the
        /// components from these two assemblies, and then further configure
        /// the container, or add more assemblies when calling this via
        /// <paramref name="additionalAssemblies" />.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="additionalAssemblies">The optional additional
        /// assemblies.</param>
        /// <exception cref="ArgumentNullException">container</exception>
        public static void ConfigureServices(Container container,
            params Assembly[] additionalAssemblies)
        {
            Assembly[] assemblies = new[]
            {
                // Proteus.Entries
                typeof(ExplicitRegionDetector).Assembly,
                // Catutil.Migration
                typeof(SqlEntryReader).Assembly
            };
            if (additionalAssemblies?.Length > 0)
                assemblies = assemblies.Concat(additionalAssemblies).ToArray();

            container.Collection.Register<IEntryReader>(assemblies);

            container.Collection.Register<IEntryFilter>(assemblies);
            container.Collection.Register<IEscapeDecoder>(assemblies);
            container.Collection.Register<IEntryRegionDetector>(assemblies);
            container.Collection.Register<IEntryRegionFilter>(assemblies);
            container.Collection.Register<IEntryRegionParser>(assemblies);

            // required for injection
            container.RegisterInstance(new UniData());
        }

        /// <summary>
        /// Gets the entry reader (from <c>/EntryReader</c>).
        /// </summary>
        /// <returns>entry reader</returns>
        /// <exception cref="ApplicationException">component not found</exception>
        public IEntryReader GetEntryReader()
        {
            return GetComponent<IEntryReader>(
                Configuration["EntryReader:Id"],
                "EntryReader:Options",
                true);
        }

        /// <summary>
        /// Gets the entry filters (from <c>/EntryFilters</c>).
        /// </summary>
        /// <returns>entry filters</returns>
        public IList<IEntryFilter> GetEntryFilters()
        {
            // read all the filters entries in configuration,
            // as entry-filter.escape is a corner case.
            // An escape filters only has EscapeDecoders.
            IList<ComponentFactoryConfigEntry> entries =
                ComponentFactoryConfigEntry.ReadComponentEntries(
                    Configuration, "EntryFilters");

            // get the standard filters
            IList<IEntryFilter> components = GetComponents<IEntryFilter>(entries,
                true, true);

            // create the special filters
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is EscapeEntryFilter filter)
                {
                    // add all the escape decoders
                    string decodersKey =
                        $"EntryFilters:{i}:Options:EscapeDecoders";
                    var decoderEntries = ComponentFactoryConfigEntry.ReadComponentEntries(
                        Configuration, decodersKey);
                    var decoders = GetComponents<IEscapeDecoder>(decoderEntries,
                        false, true);

                    filter.Initialize(decoders);
                }
            }

            // assign logger if any
            if (Logger != null)
            {
                foreach (IHasLogger component in components)
                    component.Logger = Logger;
            }

            return components;
        }

        /// <summary>
        /// Gets the region detectors (from <c>/EntryRegionDetectors</c>).
        /// </summary>
        /// <returns>detectors</returns>
        public IList<IEntryRegionDetector> GetRegionDetectors()
        {
            var entries = ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration,
                "EntryRegionDetectors");
            var components = GetComponents<IEntryRegionDetector>(
                entries, false, true);

            if (Logger != null)
            {
                foreach (IHasLogger component in components)
                    component.Logger = Logger;
            }

            return components;
        }

        /// <summary>
        /// Gets the region filters (from <c>/EntryRegionFilters</c>).
        /// </summary>
        /// <returns>The filters.</returns>
        public IList<IEntryRegionFilter> GetRegionFilters()
        {
            var entries = ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration,
                "EntryRegionFilters");

            var components = GetComponents<IEntryRegionFilter>(
                entries, false, true);

            if (Logger != null)
            {
                foreach (IHasLogger component in components)
                    component.Logger = Logger;
            }

            return components;
        }

        /// <summary>
        /// Gets the region parsers (from <c>/EntryRegionParsers</c>).
        /// </summary>
        /// <returns>parsers</returns>
        public IList<IEntryRegionParser> GetRegionParsers()
        {
            var entries = ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration,
                "EntryRegionParsers");
            var components = GetComponents<IEntryRegionParser>(
                entries, false, true);

            if (Logger != null)
            {
                foreach (IHasLogger component in components)
                    component.Logger = Logger;
            }

            return components;
        }
    }
}
