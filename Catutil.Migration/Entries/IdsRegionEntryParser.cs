using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Parser for CO <c>ids</c> region.
    /// This parser assumes that the <c>ids</c> region just contains a single
    /// command entry, with arguments <c>f</c>=fragment ID and <c>e</c>=entry
    /// ID. It then reads these IDs and updates the target object accordingly.
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    [Tag("entry-region-parser.co-ids")]
    public sealed class IdsRegionEntryParser : IEntryRegionParser
    {
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

            return regions[regionIndex].Tag == "ids";
        }

        /// <summary>
        /// Parses the region of entries at <paramref name="regionIndex" />
        /// in the specified <paramref name="regions" />.
        /// </summary>
        /// <param name="set">The entries set.</param>
        /// <param name="regions">The regions.</param>
        /// <param name="regionIndex">Index of the region in the set.</param>
        /// <param name="target">The target object.</param>
        /// <returns>
        /// The index to the next region to be parsed.
        /// </returns>
        /// <exception cref="ArgumentNullException">set or regions or target
        /// </exception>
        public int Parse(EntrySet set,
            IReadOnlyList<EntryRegion> regions, int regionIndex,
            object target)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (regions == null)
                throw new ArgumentNullException(nameof(regions));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            DecodedEntry entry = set.Entries[regions[regionIndex].Range.Start.Entry];
            if (entry is DecodedCommandEntry cmd)
            {
                CadmusParserContext t = target as CadmusParserContext;
                t.FragmentId = int.Parse(cmd.GetArgument("f"), CultureInfo.InvariantCulture);
                t.EntryId = int.Parse(cmd.GetArgument("e"), CultureInfo.InvariantCulture);
            }
            else Logger?.LogError("Unexpected entry type in ids region " +
                $"at {regionIndex}: \"{entry}\"");

            return regionIndex + 1;
        }
    }
}
