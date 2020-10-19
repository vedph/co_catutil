using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// Latin names inflector for bibliographic references. This applies only
    /// to names in -us, as they are the only ones being inflected in the
    /// apparatus' text (it thus does not apply to names like Scaliger).
    /// </summary>
    /// <seealso cref="IBiblioRefExpander" />
    public sealed class InflectingBiblioRefExpander : IBiblioRefExpander
    {
        private readonly Regex _nameYearRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="InflectingBiblioRefExpander"/>
        /// class.
        /// </summary>
        public InflectingBiblioRefExpander()
        {
            _nameYearRegex = new Regex(@"^(?<n>[^0-9]+)(?<s>\s+([0-9]+.*))?$");
        }

        /// <summary>
        /// Gets all the expansions from the bibliographic lookup references
        /// found in the specified trie, by inflecting all the single-author
        /// entries whose name ends with -us. This produces the same name
        /// in its genitive, dative, accusative, and ablative forms.
        /// </summary>
        /// <param name="trie">The trie.</param>
        /// <returns>
        /// The expansions.
        /// </returns>
        /// <exception cref="ArgumentNullException">trie</exception>
        public IList<string> GetExpansions(Trie trie)
        {
            if (trie == null)
                throw new ArgumentNullException(nameof(trie));

            List<string> expansions = new List<string>();
            foreach (TrieNode node in trie.GetAll())
            {
                string key = node.GetKey();
                if (key.IndexOf('&') > -1) continue;
                Match m = _nameYearRegex.Match(key);
                if (!m.Success) continue;

                if (m.Groups["n"].Value.EndsWith("us", StringComparison.Ordinal))
                {
                    string theme = m.Groups["n"].Value.Substring(
                        0, m.Groups["n"].Value.Length - 2);
                    expansions.Add(theme + "i" + m.Groups["s"].Value);
                    expansions.Add(theme + "o" + m.Groups["s"].Value);
                    expansions.Add(theme + "um" + m.Groups["s"].Value);
                }
            }

            return expansions;
        }
    }
}
