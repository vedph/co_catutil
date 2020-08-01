using Catutil.Migration.Biblio;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// An item derived from the XLS-based bibliography.
    /// </summary>
    public class XlsBiblioItem
    {
        static private readonly Regex _yrRegex = new Regex(@"[12]\d{3}[a-z]*");

        /// <summary>
        /// Gets or sets the authors.
        /// </summary>
        public IList<Author> Authors { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Gets the expected reference to this item. This is equal to the author
        /// plus the first full year found in the date, if any.
        /// </summary>
        /// <param name="includeDate">if set to <c>true</c> include date.</param>
        /// <returns>The reference or null.</returns>
        public string GetReference(bool includeDate)
        {
            if (Authors == null || Authors.Count == 0) return null;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Authors.Count; i++)
            {
                if (i > 0) sb.Append(" & ");
                sb.Append(Authors[i].GetFullName());
            }

            if (includeDate && !string.IsNullOrEmpty(Date))
            {
                Match m = _yrRegex.Match(Date);
                if (m.Success) sb.Append(' ').Append(m.Value);
            }

            return sb.ToString();
        }
    }
}
