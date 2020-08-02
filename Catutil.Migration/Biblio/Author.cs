using System;
using System.Collections.Generic;
using System.Text;

namespace Catutil.Migration.Biblio
{
    /// <summary>
    /// Bibliography author. This model reflects data which can be parsed
    /// from the original XLS based bibliography.
    /// </summary>
    public class Author
    {
        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Gets the full name, built from last name plus comma plus
        /// first name, when present.
        /// </summary>
        /// <returns>The full name.</returns>
        public string GetFullName()
        {
            return string.IsNullOrEmpty(FirstName) ?
                LastName : $"{LastName}, {FirstName}";
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
            sb.Append(LastName).Append(", ").Append(FirstName);

            if (!string.IsNullOrEmpty(Note))
                sb.Append(" (").Append(Note).Append(')');

            return sb.ToString();
        }
    }
}
