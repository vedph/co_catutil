using Fusi.Tools.Config;
using Proteus.Core.Entries;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Filter for bibliographic references with interpolated death year.
    /// </summary>
    /// <seealso cref="EntryFilterBase" />
    [Tag("entry-filter.co.biblio-ref-death")]
    public sealed class BiblioRefDeathEntryFilter : EntryFilterBase
    {
        private readonly EntryPatternSequence _seq;

        /// <summary>
        /// Initializes a new instance of the <see cref="BiblioRefDeathEntryFilter"/>
        /// class.
        /// </summary>
        public BiblioRefDeathEntryFilter()
        {
            _seq = new EntryPatternSequence(
                "prp italic=1",
                // e.g. "Parthenius 1485 et B. Guarinus"
                @"txt$[^0-9]\s*$",
                "prp italic=0",
                // e.g. " (†"
                @"txt$^\s*\(†\s*",
                "prp italic=1",
                // e.g. "1503"
                @"txt$^\s*[0-9]+",
                "prp italic=0",
                // e.g. ") "
                @"txt$^\s*\)\s*$",
                "prp italic=1",
                // e.g. "1521 e Seruio"
                @"txt$^\s*[0-9]"
                );
        }

        private string CollectText(List<DecodedEntry> entries, int start,
            int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (entries[start + i] is DecodedTextEntry txt)
                    sb.Append(txt.Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Does the filtering.
        /// </summary>
        /// <param name="entries">decoded entries</param>
        /// <returns>
        /// True if applied to any number of entries; false
        /// if never applied.
        /// </returns>
        /// <exception cref="ArgumentNullException">entries</exception>
        protected override bool DoApply(List<DecodedEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            bool applied = false;
            for (int i = entries.Count - 10; i > -1; i--)
            {
                if (_seq.IsMatch(entries, i))
                {
                    // get merged text
                    string text = CollectText(entries, i, 10);

                    // keep initial italic=1 and append merged text as a single txt
                    entries.RemoveRange(i + 1, 9);
                    entries.Insert(i + 1, new DecodedTextEntry(0, 0, text));
                    applied = true;
                }
            }
            return applied;
        }
    }
}
