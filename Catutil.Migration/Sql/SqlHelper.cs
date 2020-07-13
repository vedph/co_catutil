using System;
using System.Text;

namespace Catutil.Migration.Sql
{
    /// <summary>
    /// Helper class for SQL query builders. This is compatible with both
    /// SQL Server and MySql syntax.
    /// </summary>
    public static class SqlHelper
    {
        /// <summary>
        /// Encodes the specified date (or date and time) value for SQL.
        /// </summary>
        /// <param name="dt">The value.</param>
        /// <param name="time">if set to <c>true</c>, include the time.</param>
        /// <returns>SQL-encoded value</returns>
        public static string SqlEncode(DateTime dt, bool time)
        {
            return time ?
                $"'{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}T{dt.Hour:00}:" +
                    $"{dt.Minute:00}:{dt.Second:00}.000Z'" :
                $"'{dt.Year:0000}{dt.Month:00}{dt.Day:00}'";
        }

        /// <summary>
        /// Encodes the specified literal text value for SQL.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="hasWildcards">if set to <c>true</c>, the text value
        /// has wildcards.</param>
        /// <param name="wrapInQuotes">if set to <c>true</c>, wrap in quotes
        /// the SQL literal.</param>
        /// <param name="unicode">if set to <c>true</c>, add the Unicode
        /// prefix <c>N</c> before a string literal. This is required in SQL
        /// Server for Unicode strings, while it's harmless in MySql. The
        /// option is meaningful only when <paramref name="wrapInQuotes"/> is
        /// true.</param>
        /// <returns>SQL-encoded value</returns>
        public static string SqlEncode(string text, bool hasWildcards = false,
            bool wrapInQuotes = false, bool unicode = true)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in text)
            {
                switch (c)
                {
                    case '\'':
                        sb.Append("''");
                        break;
                    case '_':
                        if (hasWildcards) sb.Append(c);
                        else sb.Append("\\_");
                        break;
                    case '%':
                        if (hasWildcards) sb.Append(c);
                        else sb.Append("\\%");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            if (wrapInQuotes)
            {
                sb.Insert(0, unicode ? "N'" : "'");
                sb.Append('\'');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes the specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <param name="databaseType">The type of the database.</param>
        /// <returns>Escaped keyword</returns>
        public static string EscapeKeyword(string keyword, string databaseType)
        {
            if (string.IsNullOrEmpty(keyword)) return "";

            switch (databaseType?.ToLowerInvariant())
            {
                case "mysql":
                    return $"`{keyword}`";
                default:
                    return $"[{keyword}]";
            }
        }
    }
}
