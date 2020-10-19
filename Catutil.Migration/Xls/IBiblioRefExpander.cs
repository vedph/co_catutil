using Fusi.Tools.Data;
using System.Collections.Generic;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// Interface implemented by modules used to expand a list of bibliographic
    /// lookup references.
    /// </summary>
    public interface IBiblioRefExpander
    {
        /// <summary>
        /// Gets all the expansions from the bibliographic lookup references
        /// found in the specified trie.
        /// </summary>
        /// <param name="trie">The trie.</param>
        /// <returns>The expansions.</returns>
        IList<string> GetExpansions(Trie trie);
    }
}
