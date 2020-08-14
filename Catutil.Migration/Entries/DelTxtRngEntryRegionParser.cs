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
    /// Parser for the <c>del-txt-rng</c> region.
    /// <para>Tag: <c>entry-region-parser.co-del-txt-rng</c>.</para>
    /// </summary>
    [Tag("entry-region-parser.co-del-txt-rng")]
    public sealed class DelTxtRngEntryRegionParser : IEntryRegionParser
    {
        private readonly Regex _lemRangeRegex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelTxtRngEntryRegionParser"/>
        /// class.
        /// </summary>
        public DelTxtRngEntryRegionParser()
        {
            _lemRangeRegex = new Regex(@"^([^-]+)\s+-\s+(.+)$");
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

            return regions[regionIndex].Tag == "del-txt-rng";
        }

        private void SetLemma(EntrySet set, int start, int end,
            ApparatusParserContext context)
        {
            // the lemma is found at the first occurrence of
            // prp italic=0, txt with ... - ...
            for (int i = start; i < end; i++)
            {
                if (set.Entries[i] is DecodedPropertyEntry prp
                    && prp.Name == CommonProps.ITALIC
                    && prp.Value == "0"
                    && set.Entries[i + 1] is DecodedTextEntry txt)
                {
                    Match m = _lemRangeRegex.Match(txt.Value);
                    if (m.Success)
                    {
                        // deletion: set null for value but keep it normalized
                        // for later detection
                        context.CurrentEntry.Value = null;
                        context.CurrentEntry.NormValue =
                            LemmaFilter.Apply(m.Groups[1].Value)
                            + " - "
                            + LemmaFilter.Apply(m.Groups[2].Value);
                        return;
                    }
                }
            }

            Logger?.LogError("Expected text range in del-txt-rng region not found");
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

            ApparatusParserContext ctx = (ApparatusParserContext)context;
            ctx.CurrentEntry.Type = ApparatusEntryType.Replacement;

            // lemma = first word - last word
            SetLemma(set,
                regions[regionIndex].Range.Start.Entry,
                regions[regionIndex].Range.End.Entry,
                ctx);

            // note = full text of region
            ctx.CurrentEntry.Note = EntryRegionParserHelper.GetText(
                set.Entries,
                regions[regionIndex].Range.Start.Entry,
                regions[regionIndex].Range.End.Entry);

            return regionIndex + 1;
        }
    }
}
