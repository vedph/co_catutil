using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

        private static string GetNormalizedWsString(StringBuilder text)
        {
            bool prevWS = true;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    if (prevWS) continue;
                    sb.Append(' ');
                    prevWS = true;
                    continue;
                }
                sb.Append(c);
                prevWS = false;
            }

            // right trim
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
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

            // get all the regions overlapping the parsed region,
            // as we must strip from it any sub-region like ms
            EntryRegion region = regions[regionIndex];
            List<EntryRegion> overlaps = (from r in regions
                                          where r != region
                                          && r.Range.Overlaps(region.Range)
                                          select r).ToList();
            StringBuilder noteText = new StringBuilder();

            // for each entry in region:
            for (int i = region.Range.Start.Entry;
                 i <= region.Range.End.Entry; i++)
            {
                // if it's text:
                if (set.Entries[i] is DecodedTextEntry txt)
                {
                    // fill with WS any overlap from the text
                    StringBuilder sb = new StringBuilder(txt.Value);

                    foreach (var overlap in overlaps
                        .Where(r => r.Range.Start.Entry == i))
                    {
                        for (int j = overlap.Range.Start.Character;
                             j <= overlap.Range.End.Character; j++)
                        {
                            sb[j] = ' ';
                        }
                        //if (overlap.Range.End.Entry == i)
                        //{
                        //    sb.Remove(overlap.Range.Start.Character,
                        //        overlap.Range.End.Character + 1
                        //        - overlap.Range.Start.Character);
                        //}
                        //else
                        //{
                        //    sb.Remove(overlap.Range.Start.Character,
                        //        sb.Length - overlap.Range.Start.Character);
                        //}
                    }

                    // normalize WS
                    string s = GetNormalizedWsString(sb);

                    // append what remained after removing overlaps
                    // if it has at least 1 letter/digit
                    if (s.Any(char.IsLetterOrDigit))
                        noteText.Append(s);
                }
            }
            string text = noteText.ToString();

            //string text = EntryRegionParserHelper.CollectText(set.Entries,
            //    regions[regionIndex].Range.Start.Entry,
            //    regions[regionIndex].Range.End.Entry).Trim();

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
