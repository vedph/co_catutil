using Fusi.Text.Unicode;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Text filter applied to apparatus lemmata and to any other text to be
    /// compared with lemmata.
    /// </summary>
    public static class LemmaFilter
    {
        private static readonly UniData _ud = new UniData();

        /// <summary>
        /// Apply this filter to the specified text, by keeping only
        /// letters/apostrophe and whitespaces. All the diacritics are removed,
        /// and uppercase letters are lowercased. Whitespaces are normalized
        /// and flattened into space, and get trimmed if initial or final.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The filtered text.</returns>
        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            StringBuilder sb = new StringBuilder();
            bool prevWS = true;

            foreach (char c in text)
            {
                switch (c)
                {
                    case '\'':
                        sb.Append('\'');
                        prevWS = false;
                        break;
                    default:
                        if (char.IsWhiteSpace(c))
                        {
                            if (prevWS) break;
                            sb.Append(' ');
                            prevWS = true;
                            break;
                        }
                        if (char.IsLetter(c))
                        {
                            char seg = _ud.GetSegment(c, true);
                            if (seg != 0) sb.Append(char.ToLower(seg));
                            prevWS = false;
                            break;
                        }
                        prevWS = false;
                        break;
                }
            }

            // right trim
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }
    }
}
