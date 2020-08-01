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
        /// Gets or sets the variants.
        /// </summary>
        public List<string> Variants { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Adds the specified variant to this author, unless already present.
        /// </summary>
        /// <param name="variant">The variant.</param>
        /// <exception cref="ArgumentNullException">variant</exception>
        public void AddVariant(string variant)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));

            if (Variants == null)
            {
                Variants = new List<string> { variant };
            }
            else
            {
                if (!Variants.Contains(variant))
                    Variants.Add(variant);
            }
        }

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

            if (Variants?.Count > 0)
                sb.Append(string.Join("; ", Variants));

            return sb.ToString();
        }
    }
}
