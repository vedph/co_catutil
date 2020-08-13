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
        private readonly Regex _dateRegex;

        /// <summary>
        /// Gets or sets the name of the sheet to read.
        /// The default value is <c>Foglio1</c>.
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XlsBiblioReader"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
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
            _dateRegex = new Regex(
                @"\((?<d>[12][0-9]{3}[a-z]*)\)(?:\s*\[(?<e>[12][0-9]{3})\])?");
        }

        private Author ParseSingleAuthor(string text)
        {
            // corner case: avatar = identification, e.g.
            // "Barthius, Casparus = Caspar von Barth (1587-1658)"
            // just remove the identification
            int i = text.IndexOf('=');
            if (i > -1) text = text.Substring(0, i);

            Match m = _LastFirstRegex.Match(text);
            return m.Success ?
                new Author
                {
                    LastName = m.Groups["l"].Value.Trim(),
                    FirstName = m.Groups["f"].Value.Trim()
                } : null;
        }

        private void ParseDate(string dateText, BiblioItem item)
        {
            Match m = _dateRegex.Match(dateText);
            if (!m.Success) return;

            if (m.Groups["d"].Length > 0) item.Date = m.Groups["d"].Value;
            if (m.Groups["e"].Length > 0) item.EditorDate = m.Groups["e"].Value;
        }

        private void ParseAuthors(string authorsText, string workText,
            BiblioItem item)
        {
            if (string.IsNullOrEmpty(authorsText)) return;

            // is it an alias (with empty BC)?
            Match m = _seeUnderRegex.Match(authorsText);

            // yes: parse source and target names. It is assumed that a link
            // always refers to a single author.
            if (m.Success)
            {
                item.Authors.Add(ParseSingleAuthor(authorsText.Substring(0, m.Index)));
                Author dstAuthor = ParseSingleAuthor(authorsText.Substring(m.Index + m.Length));
                item.TargetRef = dstAuthor.GetFullName();
                return;
            } // alias

            // is it an alias (with non-empty BC)?
            // yes: parse the target author and set as reference its full name
            // followed by space and date
            if (workText?.StartsWith("see ") == true)
            {
                int i = workText.LastIndexOf('(');
                string targetAuthText = i > -1 ?
                    workText.Substring(4, i - 4) : workText.Substring(4);
                item.TargetRef = ParseSingleAuthor(targetAuthText).GetFullName();
                if (i > -1) item.TargetRef += $" {workText.Substring(i)}";
            }

            // if multiple authors, split at " and " or " & "
            if (_andRegex.IsMatch(authorsText))
            {
                foreach (string name in _andRegex.Split(authorsText))
                    item.Authors.Add(ParseSingleAuthor(name));
            }
            else
            {
                // else single author
                item.Authors.Add(ParseSingleAuthor(authorsText));
            }
        }

        // TODO: full Read method

        /// <summary>
        /// Read bibliographic items for collecting references only. Note that
        /// when reading for references it makes no difference whether each item
        /// being read is an alias or not, as we are only interested in collecting
        /// the components for building the text of a bibliographic reference.
        /// At any rate, probably aliases will never be found as references in CO
        /// text, as by their nature an alias implies that the form chosen as the
        /// normal one is rather its target.
        /// </summary>
        /// <exception cref="ArgumentNullException">filePath</exception>
        /// <exception cref="InvalidDataException">Invalid text line ID</exception>
        public IEnumerable<BiblioItem> ReadForReference()
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

                BiblioItem item = new BiblioItem();

                // B=date
                ParseDate(row.Cells[1].StringCellValue, item);

                // A=author(s)
                ParseAuthors(
                    row.Cells[0].StringCellValue,
                    row.Cells[2].StringCellValue,
                    item);

                if (item.Authors.Count == 0)
                {
                    throw new InvalidDataException("Missing author(s) in row "
                        + row.RowNum);
                }
                if (item.Authors.Any(a => a == null))
                {
                    throw new InvalidDataException("Invalid author(s) in "
                        + row.Cells[0].StringCellValue);
                }

                // C=work: we are not interested in it when dealing
                // with references only

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
