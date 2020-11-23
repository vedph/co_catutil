using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Biblio
{
    /// <summary>
    /// An item derived from the XLS-based bibliography.
    /// </summary>
    public sealed class BiblioItem
    {
        static private readonly Regex _yrRegex =
            new Regex(@"[12]\d{3}(?:(?:-\d+)?|(?:[a-z]+)?)");

        /// <summary>
        /// Gets or sets the authors.
        /// </summary>
        public List<Author> Authors { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets the date assigned by the editor. This can be either
        /// an addition to <see cref="Date"/>, or the only date for this item.
        /// </summary>
        public string EditorDate { get; set; }

        /// <summary>
        /// Gets or sets the target reference. This is not null only when this
        /// item is just an alias, which points to another existing item. The
        /// reference can end with a date when the target is not a single author
        /// name, but a full item, e.g. <c>Van Buren (1942-43)</c>.
        /// </summary>
        public string TargetRef { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BiblioItem"/> class.
        /// </summary>
        public BiblioItem()
        {
            Authors = new List<Author>();
        }

        /// <summary>
        /// Determines whether this item is an alias. This is true when
        /// <see cref="TargetRef"/> is not null and contains a bracket.
        /// </summary>
        public bool IsAlias() => TargetRef?.IndexOf('(') > -1;

        /// <summary>
        /// Gets the expected reference to this item. This is equal to the author
        /// last name, plus the first full year found in the date, if any.
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
                sb.Append(Authors[i].LastName);
            }

            if (includeDate && !string.IsNullOrEmpty(Date))
            {
                Match m = _yrRegex.Match(Date);
                if (m.Success) sb.Append(' ').Append(m.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (Authors?.Count > 0) sb.Append(string.Join(" & ", Authors));

            if (!string.IsNullOrEmpty(Date))
                sb.Append(" (").Append(Date).Append(')');

            if (!string.IsNullOrEmpty(EditorDate))
                sb.Append(" [").Append(EditorDate).Append(']');

            return sb.ToString();
        }
    }
}
