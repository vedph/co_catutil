using Cadmus.Philology.Parts.Layers;
using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Parser for the <c>auth</c> region.
    /// <para>Tag: <c>entry-region-parser.co-auth</c>.</para>
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    [Tag("entry-region-parser.co-auth")]
    public sealed class AuthEntryRegionParser : IEntryRegionParser
    {
        private readonly Regex _authTailRegex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthEntryRegionParser"/>
        /// class.
        /// </summary>
        public AuthEntryRegionParser()
        {
            _authTailRegex = new Regex(@"[,.\s]+$");
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

            return regions[regionIndex].Tag == "auth";
        }

        private string FilterAuthPortion(string text) =>
            _authTailRegex.Replace(text, "").Trim();

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

            StringBuilder sb = new StringBuilder();
            int n = 0;
            for (int i = regions[regionIndex].Range.Start.Entry;
                 i <= regions[regionIndex].Range.End.Entry;
                 i++)
            {
                if (set.Entries[i] is DecodedTextEntry txt)
                {
                    n++;
                    if (n > 3)
                    {
                        Logger?.LogError(
                            $"More than 3 text entries in auth region at {regionIndex}");
                    }
                    if (n > 1) sb.Append(", ");
                    sb.Append(FilterAuthPortion(txt.Value));
                }
            }

            if (n < 3)
            {
                Logger?.LogError(
                    $"Less than 3 text entries in auth region at {regionIndex}");
            }
            if (n == 3)
            {
                ApparatusParserContext ctx = (ApparatusParserContext)context;
                ctx.CurrentEntry.Authors.Add(new ApparatusAnnotatedValue
                {
                    // : is a commodity conventional prefix for ancient authors
                    Value = ":"+ sb.ToString()
                });
            }

            return regionIndex + 1;
        }
    }
}
