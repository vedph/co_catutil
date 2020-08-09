using Proteus.Core;
using Proteus.Core.Regions;
using Proteus.Entries;
using System.Collections.Generic;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Entry region parser.
    /// </summary>
    /// <remarks>An entry region parser is an optionally configurable object,
    /// whose task is parsing an entries region, extracting data from it
    /// (and eventually from other regions past it) into an existing item,
    /// either as metadata and as parts.</remarks>
    public interface IEntryRegionParser : IHasLogger
    {
        /// <summary>
        /// Determines whether this parser is applicable to the specified
        /// region. Typically, the applicability is determined via a configurable
        /// nested object, having parameters like region tag(s) and paths.
        /// </summary>
        /// <param name="set">The entries set.</param>
        /// <param name="regions">The regions.</param>
        /// <param name="regionIndex">Index of the region.</param>
        /// <returns><c>true</c> if applicable; otherwise, <c>false</c>.</returns>
        bool IsApplicable(EntrySet set, IReadOnlyList<EntryRegion> regions,
            int regionIndex);

        /// <summary>
        /// Parses the region of entries at <paramref name="regionIndex" />
        /// in the specified <paramref name="regions" />.
        /// </summary>
        /// <param name="set">The entries set.</param>
        /// <param name="regions">The regions.</param>
        /// <param name="regionIndex">Index of the region in the set.</param>
        /// <param name="context">The context object; usually this contains
        /// a target object which gets filled by the parser, together with
        /// any required context data.</param>
        /// <returns>
        /// The index to the next region to be parsed.
        /// </returns>
        int Parse(EntrySet set, IReadOnlyList<EntryRegion> regions,
            int regionIndex, object context);
    }
}
