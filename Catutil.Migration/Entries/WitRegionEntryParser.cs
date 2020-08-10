using Cadmus.Philology.Parts.Layers;
using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Parser for the <c>wit</c> (witnesses) entries region. This extracts
    /// all the references to the main manuscripts from each text entry inside
    /// the region, adding them to the <see cref="ApparatusEntry.Witnesses"/> of
    /// the current target entry.
    /// <para>Tag: <c>entry-region-parser.co-wit</c>.</para>
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    [Tag("entry-region-parser.co-wit")]
    public sealed class WitRegionEntryParser : IEntryRegionParser
    {
        private readonly Regex _msRegex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitRegionEntryParser"/>
        /// class.
        /// </summary>
        public WitRegionEntryParser()
        {
            _msRegex = new Regex("[TOGRm]2?");
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

            return regions[regionIndex].Tag == "wit";
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
        public int Parse(EntrySet set, IReadOnlyList<EntryRegion> regions,
            int regionIndex, object context)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (regions == null)
                throw new ArgumentNullException(nameof(regions));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ApparatusEntry appEntry =
                ((ApparatusParserContext)context).CurrentEntry;

            for (int i = regions[regionIndex].Range.Start.Entry;
                     i <= regions[regionIndex].Range.End.Entry; i++)
            {
                if (set.Entries[i] is DecodedTextEntry txt)
                {
                    foreach (Match m in _msRegex.Matches(txt.Value))
                    {
                        appEntry.Witnesses.Add(new ApparatusAnnotatedValue
                        {
                            Value = m.Value
                        });
                    }
                }
            }

            return regionIndex + 1;
        }
    }
}
