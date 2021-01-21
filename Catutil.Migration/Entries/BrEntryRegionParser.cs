using Cadmus.Philology.Parts;
using Cadmus.Philology.Parts.Layers;
using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Parser for <c>br</c> region. This adds to the current entry the content
    /// of the <c>br</c> region as an author reference.
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    [Tag("entry-region-parser.co-br")]
    public sealed class BrEntryRegionParser : IEntryRegionParser,
        IConfigurable<BrEntryRegionParserOptions>
    {
        private readonly List<Tuple<Regex, string>> _replacements;
        private readonly Regex _nameAndSuffixesRegex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrEntryRegionParser"/>
        /// class.
        /// </summary>
        public BrEntryRegionParser()
        {
            _replacements = new List<Tuple<Regex, string>>();
            _nameAndSuffixesRegex = new Regex(
                @"^(?<n>[^0-9]+)(?:\s*(?<s>[0-9]+[^\s]*))+");
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public void Configure(BrEntryRegionParserOptions options)
        {
            _replacements.Clear();
            if (!string.IsNullOrEmpty(options.ReplacementsPath))
            {
                Regex r = new Regex("^([^=]+)=(.*)");

                using (StreamReader reader = new StreamReader(
                    options.ReplacementsPath, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match m = r.Match(line);
                        if (m.Success)
                        {
                            _replacements.Add(Tuple.Create(
                                new Regex(m.Groups[1].Value),
                                m.Groups[2].Value));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether this parser is applicable to the specified
        /// region. Typically, the applicability is determined via a configurable
        /// nested object, having parameters like region tag(s) and paths.
        /// </summary>
        /// <param name="set">The entries set.</param>
        /// <param name="regions">The regions.</param>
        /// <param name="regionIndex">Index of the region.</param>
        /// <returns><c>true</c> if applicable; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">set or regions</exception>
        public bool IsApplicable(EntrySet set, IReadOnlyList<EntryRegion> regions,
            int regionIndex)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (regions == null)
                throw new ArgumentNullException(nameof(regions));

            return regions[regionIndex].Tag == "br";
        }

        private string GetReplacedValue(string value)
        {
            if (_replacements.Count == 0) return value;
            foreach (var replacement in _replacements)
            {
                if (replacement.Item1.IsMatch(value))
                {
                    return replacement.Item1.Replace(value, replacement.Item2);
                }
            }
            return value;
        }

        private IList<string> ExpandValue(string value)
        {
            List<string> values = new List<string>();
            Match m = _nameAndSuffixesRegex.Match(value);

            // type "Fruterius"
            if (!m.Success) values.Add(GetReplacedValue(value));
            else
            {
                // type "Fruterius 1701 1702"
                if (m.Groups["s"].Captures.Count > 1)
                {
                    string name = GetReplacedValue(m.Groups["n"].Value);
                    for (int i = 0; i < m.Groups["s"].Captures.Count; i++)
                    {
                        values.Add(name + " " + m.Groups["s"].Captures[i].Value);
                    }
                }
                // type "Fruterius 1701"
                else
                {
                    values.Add(GetReplacedValue(value));
                }
            }
            return values;
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
        /// <exception cref="ArgumentNullException">set or regions or target
        /// </exception>
        public int Parse(EntrySet set,
            IReadOnlyList<EntryRegion> regions, int regionIndex,
            object context)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (regions == null)
                throw new ArgumentNullException(nameof(regions));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            EntryRange range = regions[regionIndex].Range;
            if (set.Entries[range.Start.Entry] is DecodedTextEntry entry)
            {
                ApparatusParserContext ctx = (ApparatusParserContext)context;

                string value = entry.Value.Substring(range.Start.Character,
                        range.End.Character + 1 - range.Start.Character);

                foreach (string author in ExpandValue(value))
                {
                    ctx.CurrentEntry.Authors.Add(new LocAnnotatedValue
                    {
                        Value = author
                    });
                }

                Logger?.LogInformation($">auth: Author={value}");
            }
            else
            {
                Logger?.LogError("Expected text entry including br region at " +
                    $"{regionIndex} not found");
            }

            return regionIndex + 1;
        }
    }

    /// <summary>
    /// Options for <see cref="BrEntryRegionParser"/>.
    /// </summary>
    public sealed class BrEntryRegionParserOptions
    {
        /// <summary>
        /// Gets or sets the path to the file with replacements to be applied
        /// to references. Each line in the file is a string with format
        /// source=target, e.g. <c>Mureto=Muretus</c>.
        /// </summary>
        public string ReplacementsPath { get; set; }
    }
}
