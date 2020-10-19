using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// Importing bibliographic references expander. This expands references
    /// by just adding a list of imported references, unless already present
    /// in the target.
    /// </summary>
    /// <seealso cref="IBiblioRefExpander" />
    public sealed class ImportingBiblioRefExpander : IBiblioRefExpander
    {
        private readonly HashSet<string> _imported;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportingBiblioRefExpander"/>
        /// class.
        /// </summary>
        /// <param name="source">The source to load references from.</param>
        /// <exception cref="ArgumentNullException">source</exception>
        public ImportingBiblioRefExpander(Stream source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _imported = new HashSet<string>();
            using (StreamReader reader = new StreamReader(source, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)
                        || line.StartsWith("#"))
                    {
                        continue;
                    }

                    _imported.Add(line.Trim());
                }
            }
        }

        /// <summary>
        /// Gets all the expansions from the bibliographic lookup references
        /// found in the specified trie.
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

            return _imported.Where(e => trie.Get(e) == null).ToList();
        }
    }
}
