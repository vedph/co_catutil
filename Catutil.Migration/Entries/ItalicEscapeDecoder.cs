using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Escapes;
using System;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Italic escape decoder for CO apparatus text. The italic escapes are
    /// <c>{</c>=italic on and <c>}</c>=italic off.
    /// </summary>
    /// <seealso cref="IEscapeDecoder" />
    [Tag("escape-decoder.co-italic")]
    public sealed class ItalicEscapeDecoder : IEscapeDecoder
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Resets the internal state of this decoder, if any. This should
        /// be called before starting a new conversion.
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

            switch (text[index])
            {
                case '{':
                    return Tuple.Create(
                        new DecodedEntry[]
                        {
                            new DecodedPropertyEntry(index, 1, CommonProps.ITALIC, "1")
                        }, 1);
                case '}':
                    return Tuple.Create(
                        new DecodedEntry[]
                        {
                            new DecodedPropertyEntry(index, 1, CommonProps.ITALIC, "0")
                        }, 1);
            }

            return null;
        }
    }
}
