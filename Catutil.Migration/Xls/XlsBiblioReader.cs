using Catutil.Migration.Biblio;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// XLS bibliography reader. This assumes that the bibliography is sorted,
    /// i.e. that all the rows related to the same author follow each other,
    /// rather than being scattered around the file.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public class XlsBiblioReader : IDisposable
    {
        private readonly FileStream _stream;
        private HSSFWorkbook _wbk;
        private ISheet _sheet;
        private bool _disposed;
        private int _rowIndex;
        private readonly Regex _seeUnderRegex;
        private readonly Regex _LastFirstRegex;
        private readonly Regex _andRegex;

        /// <summary>
        /// Gets or sets the name of the sheet to read.
        /// The default value is <c>Foglio1</c>.
        /// </summary>
        public string SheetName { get; set; }

        public XlsBiblioReader(string filePath)
        {
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));

            SheetName = "Tabelle1";
            // https://stackoverflow.com/questions/5855813/how-to-read-file-using-npoi
            _stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read);
            _wbk = new HSSFWorkbook(_stream);
            _sheet = _wbk.GetSheet(SheetName);

            _seeUnderRegex = new Regex(@":\s+see\s+under\s+");
            _LastFirstRegex = new Regex(@"^(?<l>[^,(]+)(?:,\s*(?<f>[^(]+))?");
            _andRegex = new Regex(@"\s+(?:and|&)\s+");
        }

        private Author ParseSingleAuthor(string text)
        {
            Match m = _LastFirstRegex.Match(text);
            return m.Success ?
                new Author
                {
                    LastName = m.Groups["l"].Value.Trim(),
                    FirstName = m.Groups["f"].Value.Trim()
                } : null;
        }

        private IList<Author> ParseAuthors(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // is it a link?
            Match m = _seeUnderRegex.Match(text);

            // yes: parse source and target names and return the target
            // with the source as its variant. It is assumed that a link
            // always refers to a single author.
            if (m.Success)
            {
                IList<Author> srcAuthors =
                    ParseAuthors(text.Substring(0, m.Index));
                if (srcAuthors.Count != 1)
                {
                    throw new InvalidDataException(
                        "Link has no single source author: " + text);
                }

                IList<Author> dstAuthors =
                    ParseAuthors(text.Substring(m.Index + m.Length));
                if (dstAuthors.Count != 1)
                {
                    throw new InvalidDataException(
                        "Link has no single target author: " + text);
                }

                dstAuthors[0].AddVariant(srcAuthors[0].GetFullName());
                return dstAuthors;
            } // link

            List<Author> authors = new List<Author>();

            // split at " and " or " & "
            if (_andRegex.IsMatch(text))
            {
                foreach (string name in _andRegex.Split(text))
                    authors.Add(ParseSingleAuthor(name));
                return authors;
            }

            // process author
            authors.Add(ParseSingleAuthor(text));
            return authors;
        }

        /// <summary>
        /// Imports bibiliography records from the specified file.
        /// </summary>
        /// <param name="refOnly">True to read only those data used for
        /// bibliographic references, i.e. authors and date.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
        /// <exception cref="InvalidDataException">Invalid text line ID</exception>
        public IEnumerable<XlsBiblioItem> Read(bool refOnly = false)
        {
            if (_rowIndex > _sheet.LastRowNum) yield break;

            while (_rowIndex <= _sheet.LastRowNum)
            {
                IRow row = _sheet.GetRow(_rowIndex);
                // skip empty row
                if (row == null || string.IsNullOrEmpty(row.Cells[1].StringCellValue))
                {
                    _rowIndex++;
                    continue;
                }

                // A=author
                IList<Author> authors = ParseAuthors(row.Cells[0].StringCellValue);
                if (authors == null)
                {
                    throw new InvalidDataException("Missing author(s) in row "
                        + row.RowNum);
                }
                if (authors.Any(a => a == null))
                {
                    throw new InvalidDataException("Invalid author(s) in "
                        + row.Cells[0].StringCellValue);
                }

                // B=date
                string date = row.Cells[1].StringCellValue?.Trim();

                // C=work
                if (!refOnly)
                {
                    // TODO: parse title
                }

                XlsBiblioItem item = new XlsBiblioItem
                {
                    Authors = authors,
                    Date = date
                };
                yield return item;

                _rowIndex++;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and
        /// unmanaged resources; <c>false</c> to release only unmanaged
        /// resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sheet = null;
                    _stream?.Close();
                    _wbk?.Close();
                    _wbk = null;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
