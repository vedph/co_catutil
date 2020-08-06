using Catutil.Migration.Xls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Catutil.Migration.Biblio
{
    /// <summary>
    /// Bibliographic reference finder.
    /// </summary>
    public sealed class BiblioReferenceFinder
    {
        private readonly XlsBiblioLookup _lookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="BiblioReferenceFinder"/>
        /// class.
        /// </summary>
        /// <param name="lookup">The lookup.</param>
        /// <exception cref="ArgumentNullException">lookup</exception>
        public BiblioReferenceFinder(XlsBiblioLookup lookup)
        {
            _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        }

        private static bool MatchKeyTail(string key, string text,
            int index, int minLen)
        {
            int n = key.Length - minLen;
            Debug.Assert(n >= 0);
            for (int i = 0; i < n; i++)
            {
                if (index + minLen + i >= text.Length) return false;
                if (text[index + minLen + i] != key[minLen + i])
                    return false;
            }

            int limit = index + key.Length;
            return limit == text.Length || !char.IsLetterOrDigit(text[limit]);
        }

        /// <summary>
        /// Finds all the references in the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>One tuples per match, where 1=index to text and 2=length.
        /// </returns>
        /// <exception cref="ArgumentNullException">text</exception>
        public IEnumerable<Tuple<int,int>> FindAll(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            int minLen = _lookup.GetMinReferenceLength();
            for (int index = 0; index <= text.Length - minLen; index++)
            {
                string longestMatch = null;
                foreach (string key in _lookup.FindAll(
                    text.Skip(index).Take(minLen)))
                {
                    int n = key.Length - minLen;
                    if (MatchKeyTail(key, text, index, minLen)
                        && (longestMatch == null || longestMatch.Length < key.Length))
                    {
                        longestMatch = key;
                    }
                }
                if (longestMatch != null)
                    yield return Tuple.Create(index, longestMatch.Length);
            }
        }
    }
}
