using Cadmus.Philology.Parts.Layers;
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
    /// Parser for the <c>del-lem</c> region.
    /// <para>Tag: <c>entry-region-parser.co-del-lem</c>.</para>
    /// </summary>
    [Tag("entry-region-parser.co-del-lem")]
    public sealed class DelLemEntryRegionParser : IEntryRegionParser
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

            return regions[regionIndex].Tag == "del-lem";
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

            if (set.Entries[regions[regionIndex].Range.Start.Entry]
                is DecodedTextEntry entry)
            {
                ApparatusParserContext ctx = (ApparatusParserContext)context;
                ctx.CurrentEntry.Type = ApparatusEntryType.Replacement;
                ctx.CurrentEntry.Value = null;
                // keep NormValue as it will be later used for locating
                // (in ApparatusParserContext)
                ctx.CurrentEntry.Note = entry.Value.Trim();

                Logger?.LogInformation($">del-lem: Note={ctx.CurrentEntry.Note}");
            }
            else
            {
                Logger?.LogError("Expected text entry in del-lem region at " +
                    $"{regionIndex} not found");
            }

            return regionIndex + 1;
        }
    }
}
