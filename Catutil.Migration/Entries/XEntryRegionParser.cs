﻿using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Region parser for unknown (x) region.
    /// </summary>
    [Tag("entry-region-parser.co-x")]
    public sealed class XEntryRegionParser : IEntryRegionParser
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

            return regions[regionIndex].Tag == "x";
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

            string text = EntryRegionParserHelper.CollectText(set.Entries,
                regions[regionIndex].Range.Start.Entry,
                regions[regionIndex].Range.End.Entry).Trim();
            if (text.Any(c => char.IsLetterOrDigit(c)))
            {
                ApparatusParserContext ctx = (ApparatusParserContext)context;
                Logger?.LogInformation($">x: Note=[{text}] " +
                    $"(previous=[{ctx.CurrentEntry.Note}])");
                ctx.CurrentEntry.Note = text.Trim();
            }

            return regionIndex + 1;
        }
    }
}