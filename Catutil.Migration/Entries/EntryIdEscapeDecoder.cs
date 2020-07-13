using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Escapes;
using System;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Entry ID escape decoder. This decoder the escape inserted by
    /// <see cref="SqlEntryReader"/> when reading rows from the CO database.
    /// This escape resolves into a <c>set-ids</c> command having 2 arguments,
    /// <c>f</c>=fragment ID and <c>e</c>=entry ID.
    /// </summary>
    /// <seealso cref="Proteus.Core.Escapes.IEscapeDecoder" />
    [Tag("escape-decoder.entry-id")]
    public sealed class EntryIdEscapeDecoder : IEscapeDecoder
    {
        private readonly Regex _idRegex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryIdEscapeDecoder"/>
        /// class.
        /// </summary>
        public EntryIdEscapeDecoder()
        {
            _idRegex = new Regex(@"^(\d+)\.(\d+)");
        }

        /// <summary>
        /// Resets the internal state of this decoder, if any. This does nothing.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        /// Decodes the specified text.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <param name="index">The index to the input text.</param>
        /// <param name="context">An optional context object, whose type
        /// and usage depends on implementations.</param>
        /// <returns>
        /// A tuple with 1=array of entries output by processing,
        /// and 2=length of escape. If there was no escape at that location,
        /// null is returned. Note that the entries can be null
        /// if there is an escape but it has no handler.
        /// </returns>
        /// <exception cref="ArgumentNullException">text</exception>
        public Tuple<DecodedEntry[], int> Decode(string text, int index,
            IEscapeContext context = null)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));

            if (text[index] == '«')
            {
                int end = text.IndexOf('»', index + 1);
                if (end == -1) return null;

                Match m = _idRegex.Match(text, index + 1, end - 1);
                if (!m.Success) return null;

                int len = end + 1 - index;
                return Tuple.Create(new DecodedEntry[]
                    {
                        new DecodedCommandEntry(index, len, "set-ids",
                            "f", m.Groups[1].Value, "e", m.Groups[2].Value)
                    }, len);
            }
            return null;
        }
    }
}
