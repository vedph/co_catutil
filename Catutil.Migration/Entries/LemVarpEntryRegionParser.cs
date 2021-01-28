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
    /// Postfix lemma variant parser. The postfix lemma variant has a fixed
    /// pattern and occurs after the lemma with a note.
    /// <para>Tag: <c>entry-region-parser.co-lem-var-p</c>.</para>
    /// </summary>
    /// <seealso cref="IEntryRegionParser" />
    [Tag("entry-region-parser.co-lem-var-p")]
    public sealed class LemVarpEntryRegionParser : IEntryRegionParser
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

            return regions[regionIndex].Tag == "lem-var-p";
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

            // ...
            // "txt",  <== VARIANT
            // "prp italic=1",
            // "txt",  <== NOTE
            // "prp italic=0",
            // "txt$\\s*\\)\\s*$"
            string note = (set.Entries[regions[regionIndex].Range.End.Entry - 2]
                as DecodedTextEntry)?.Value?.Trim();
            string variant = (set.Entries[regions[regionIndex].Range.End.Entry - 4]
                as DecodedTextEntry)?.Value?.Trim();

            ApparatusParserContext ctx = (ApparatusParserContext)context;
            Logger?.LogInformation($">lem-var-p: Value=[{variant}]");

            if (ctx.CurrentEntry.Value == null)
            {
                ctx.CurrentEntry.Value = "";
                Logger?.LogError(
                    $"Lemma variant without lemma at region {regionIndex}");
            }
            ctx.CurrentEntry.Value += " | " + variant;
            if (string.IsNullOrEmpty(ctx.CurrentEntry.Note))
                ctx.CurrentEntry.Note = note;
            else ctx.CurrentEntry.Note += " | " + note;

            return regionIndex + 1;
        }
    }
}
