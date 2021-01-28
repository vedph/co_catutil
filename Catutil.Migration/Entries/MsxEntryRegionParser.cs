using Cadmus.Philology.Parts;
using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Parser for <c>ms-x</c> region. This adds to the current entry the content
    /// of the <c>ms-x</c> region as a witness.
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    [Tag("entry-region-parser.co-ms-x")]
    public sealed class MsxEntryRegionParser : IEntryRegionParser
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

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

            return regions[regionIndex].Tag == "ms-x";
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
                string value = entry.Value.Substring(range.Start.Character,
                        range.End.Character + 1 - range.Start.Character).Trim();

                ApparatusParserContext ctx = (ApparatusParserContext)context;
                ctx.CurrentEntry.Witnesses.Add(new LocAnnotatedValue
                {
                    Value = value
                });

                Logger?.LogInformation($">ms-x: Witness={value}");
            }
            else
            {
                Logger?.LogError("Expected text entry including ms-x region at " +
                    $"{regionIndex} not found");
            }

            return regionIndex + 1;
        }
    }
}
