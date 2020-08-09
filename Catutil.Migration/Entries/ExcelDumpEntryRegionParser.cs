using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using Proteus.Extras;
using System;
using System.Collections.Generic;
using System.IO;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Excel dumping entry region parser. This just dumps the entries of each
    /// region into a set of one or more Excel files.
    /// <para>Tag: <c>entry-region-parser.excel-dump</c>.</para>
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    /// <seealso cref="IConfigurable{ExcelDumpEntryRegionParserOptions}" />
    [Tag("entry-region-parser.excel-dump")]
    public sealed class ExcelDumpEntryRegionParser : IEntryRegionParser,
        IConfigurable<ExcelDumpEntryRegionParserOptions>
    {
        private readonly DecodedEntryDataWriterOptions _writerOptions;
        private ExcelDumpEntryRegionParserOptions _options;
        private ExcelPackage _package;
        private int _fileNr;
        private int _entryCount;
        private DecodedEntryDumper _dumper;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelDumpEntryRegionParser"/> class.
        /// </summary>
        public ExcelDumpEntryRegionParser()
        {
            _writerOptions = new DecodedEntryDataWriterOptions
            {
                HasFlags = false,
                HasLocation = false
            };
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(ExcelDumpEntryRegionParserOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Determines whether this parser is applicable to the specified
        /// region. Typically, the applicability is determined via a configurable
        /// nested object, having parameters like region tag(s) and paths.
        /// </summary>
        /// <param name="set">The entries set.</param>
        /// <param name="regions">The regions.</param>
        /// <param name="regionIndex">Index of the region.</param>
        /// <returns>
        ///   <c>true</c> if applicable; otherwise, <c>false</c>.
        /// </returns>
        public bool IsApplicable(EntrySet set, IReadOnlyList<EntryRegion> regions,
            int regionIndex)
        {
            return true;
        }

        /// <summary>
        /// Parses the region of entries at <paramref name="regionIndex" />
        /// in the specified <paramref name="regions" />.
        /// </summary>
        /// <param name="set">The entries set.</param>
        /// <param name="regions">The regions.</param>
        /// <param name="regionIndex">Index of the region in the set.</param>
        /// <param name="context">The context object.</param>
        /// <returns>
        /// The index to the next region to be parsed.
        /// </returns>
        /// <exception cref="ArgumentNullException">set or regions</exception>
        public int Parse(EntrySet set, IReadOnlyList<EntryRegion> regions,
            int regionIndex, object context)
        {
            if (set is null) throw new ArgumentNullException(nameof(set));
            if (regions is null) throw new ArgumentNullException(nameof(regions));

            if (_package == null ||
                (_options.MaxEntriesPerDumpFile > 0
                 && _entryCount >= _options.MaxEntriesPerDumpFile))
            {
                // close a pending file
                if (_package != null)
                {
                    _dumper.WriteTail();
                    _package.Dispose();
                }

                // create a new one
                if (!Directory.Exists(_options.OutputDirectory))
                    Directory.CreateDirectory(_options.OutputDirectory);
                string filePath = Path.Combine(
                    _options.OutputDirectory,
                    $"s{++_fileNr:00000}.xlsx");
                _package = new ExcelPackage(new FileInfo(filePath));
                _entryCount = 0;

                _dumper = new DecodedEntryDumper(
                    new ExcelDecodedEntryDataWriter(_package, _writerOptions));

                _dumper.WriteHead();
            }

            // dump
            int start = regions[regionIndex].Range.Start.Entry;
            int end = regions[regionIndex].Range.End.Entry;

            _dumper.DumpEntries(set.Context.Number, set.Entries, regions,
                start, end + 1 - start, regionIndex == 0);
            _entryCount += set.Entries.Count;

            return regionIndex + 1;
        }
    }

    /// <summary>
    /// Options for <see cref="ExcelDumpEntryRegionParser"/>.
    /// </summary>
    public sealed class ExcelDumpEntryRegionParserOptions
    {
        /// <summary>
        /// Gets or sets the maximum count of entries per dump file
        /// (0=unlimited). Each dump file will anyway exceed this limit
        /// to avoid splitting a set between two files.
        /// </summary>
        public int MaxEntriesPerDumpFile { get; set; }

        /// <summary>
        /// Gets or sets the output directory.
        /// </summary>
        public string OutputDirectory { get; set; }
    }
}
