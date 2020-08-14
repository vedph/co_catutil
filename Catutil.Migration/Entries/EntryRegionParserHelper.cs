using Proteus.Core.Entries;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Helper functions for <see cref="IEntryRegionParser"/>'s.
    /// </summary>
    static public class EntryRegionParserHelper
    {
        /// <summary>
        /// Collects all the text entries in the specified range (inclusive),
        /// concatenating them into a unique string.
        /// </summary>
        /// <param name="entries">The entries.</param>
        /// <param name="start">The start entry index (inclusive).</param>
        /// <param name="end">The end entry index (inclusive).</param>
        /// <returns>Text.</returns>
        /// <exception cref="ArgumentNullException">entries</exception>
        static public string GetText(IList<DecodedEntry> entries,
            int start, int end)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            StringBuilder sb = new StringBuilder();
            for (int i = start; i <= end; i++)
            {
                if (entries[i] is DecodedTextEntry txt)
                    sb.Append(txt.Value);
            }
            return sb.ToString();
        }
    }
}
