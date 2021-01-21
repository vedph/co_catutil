using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// XLS text reader.
    /// </summary>
    public class XlsTextReader : IDisposable
    {
        private readonly Regex _poemRegex;
        private readonly Regex _wsRegex;
        private readonly FileStream _stream;
        private HSSFWorkbook _wbk;
        private ISheet _sheet;
        private string _currentPoem;
        private string _prevLineId;
        private short _frOrdinal;
        private bool _continued;
        private short _lineOrdinal;
        private int _rowIndex;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the name of the sheet to read.
        /// The default value is <c>Foglio1</c>.
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XlsTextReader"/> class.
        /// </summary>
        /// <param name="filePath">The XLS file path.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
        public XlsTextReader(string filePath)
        {
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));

            SheetName = "Foglio1";
            // the poem "number" in the line ID is all what precedes the first
            // dot (or sometimes - seemingly by mistake - comma)
            _poemRegex = new Regex(@"^(?<pn>\d+)(?<pa>[^.,]*)");
            _wsRegex = new Regex(@"\s+");

            // https://stackoverflow.com/questions/5855813/how-to-read-file-using-npoi
            _stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read);
            _wbk = new HSSFWorkbook(_stream);
            _sheet = _wbk.GetSheet(SheetName);
        }

        private IList<FormattingRun> GetFormattingRuns(HSSFRichTextString rts)
        {
            List<FormattingRun> runs = new List<FormattingRun>();

            for (int i = 0; i < rts.NumFormattingRuns; i++)
            {
                int runIndex = rts.GetIndexOfFormattingRun(i);
                short fontIndex = rts.GetFontAtIndex(runIndex);
                int length = i + 1 < rts.NumFormattingRuns
                    ? rts.GetIndexOfFormattingRun(i + 1) - runIndex
                    : rts.Length - runIndex;

                runs.Add(new FormattingRun
                {
                    Start = runIndex,
                    Length = length,
                    FontIndex = fontIndex
                });
            }
            return runs;
        }

        private string BuildItalicizedText(HSSFWorkbook wbk,
            string text, IList<FormattingRun> runs)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;

            foreach (FormattingRun run in runs)
            {
                if (index < run.Start)
                {
                    AppTextItalicHelper.AppendAndSanitize(
                        text, index, run.Start - index, sb);
                }

                IFont font = wbk.GetFontAt(run.FontIndex);
                if (font.IsItalic) sb.Append('{');

                AppTextItalicHelper.AppendAndSanitize(
                    text, run.Start, run.Length, sb);

                if (font.IsItalic) sb.Append('}');
                index = run.Start + run.Length;
            }

            if (index < text.Length) sb.Append(text, index, text.Length - index);

            // remove eventual redundancies
            AppTextItalicHelper.ReduceSequencesOf('{', sb);
            AppTextItalicHelper.ReduceSequencesOf('}', sb);

            // normalize whitespaces and trim
            string result = sb.ToString();
            result = _wsRegex.Replace(result, " ").Trim();

            // adjust italicized spaces
            return AppTextItalicHelper.AdjustItalicSpaces(result);
        }

        private static IEnumerable<string> GetAppFragments(string text)
        {
            return Regex.Split(text, @"\s+\|\s+");
        }

        private static IEnumerable<string> GetAppEntries(string text)
        {
            return Regex.Split(text, @"\s+\:\s+");
        }

        /// <summary>
        /// Imports text and apparatus entries from the specified file.
        /// </summary>
        /// <exception cref="ArgumentNullException">filePath</exception>
        /// <exception cref="InvalidDataException">Invalid text line ID</exception>
        public IEnumerable<XlsTextReaderItem> Read()
        {
            if (_rowIndex > _sheet.LastRowNum) yield break;

            while (_rowIndex <= _sheet.LastRowNum)
            {
                IRow row = _sheet.GetRow(_rowIndex);
                // skip empty row
                if (row == null)
                {
                    _rowIndex++;
                    continue;
                }

                // [0]=id: note that occasionally the cell's type is not
                // a string as expected, so here we are more tolerant
                string lineId;
                _continued = false;

                switch (row.Cells[0].CellType)
                {
                    case CellType.Numeric:
                        lineId = row.Cells[0].NumericCellValue
                            .ToString(CultureInfo.InvariantCulture);
                        if (lineId.Length == 0) goto case CellType.Blank;
                        break;
                    case CellType.String:
                        lineId = row.Cells[0].StringCellValue.Trim();
                        if (lineId.Length == 0) goto case CellType.Blank;
                        break;
                    case CellType.Blank:
                        // this continues the previous line
                        if (_prevLineId == null)
                        {
                            throw new InvalidDataException(
                                $"Blank ID without preceding line at row {_rowIndex + 1}");
                        }
                        _continued = true;
                        lineId = _prevLineId;
                        break;
                    default:
                        throw new InvalidDataException(
                            $"Invalid cell 0 type at row {_rowIndex + 1}");
                }

                if (!_continued)
                {
                    _prevLineId = lineId;
                    _frOrdinal = 0;
                    Match m = _poemRegex.Match(lineId);
                    if (!m.Success)
                    {
                        throw new InvalidDataException(
                            $"Invalid text line ID: {lineId}");
                    }

                    int n = int.Parse(m.Groups["pn"].Value,
                        CultureInfo.InvariantCulture);
                    string poem = $"{n:000}{m.Groups["pa"].Value}";
                    if (poem != _currentPoem)
                    {
                        _lineOrdinal = 0;
                        _currentPoem = poem;
                    }
                    _lineOrdinal++;

                    // [1]=line
                    string text = row.Cells[1].StringCellValue.Trim();

                    // insert line
                    XlsTextReaderItem item0 = new XlsTextReaderItem
                    {
                        Level = 0
                    };
                    item0.Data["id"] = lineId;
                    item0.Data["poem"] = _currentPoem;
                    item0.Data["ordinal"] = _lineOrdinal;
                    item0.Data["value"] = text;
                    yield return item0;
                }

                // [2]=app with formatting
                if (row.Cells.Count > 2)
                {
                    HSSFRichTextString rts = (HSSFRichTextString)
                        row.Cells[2].RichStringCellValue;
                    IList<FormattingRun> runs = GetFormattingRuns(rts);
                    string appText = BuildItalicizedText(_wbk, rts.String, runs);

                    // split and insert fragments
                    foreach (string frText in GetAppFragments(appText))
                    {
                        _frOrdinal++;
                        XlsTextReaderItem item1 = new XlsTextReaderItem
                        {
                            Level = 1
                        };
                        item1.Data["lineId"] = lineId;
                        item1.Data["ordinal"] = _frOrdinal;
                        item1.Data["value"] = frText;
                        yield return item1;

                        // split and insert entries
                        short entryOrdinal = 0;
                        foreach (string entryText in GetAppEntries(frText))
                        {
                            entryOrdinal++;
                            XlsTextReaderItem item2 = new XlsTextReaderItem
                            {
                                Level = 2
                            };
                            item2.Data["fragment"] = item1;
                            item2.Data["ordinal"] = entryOrdinal;
                            item2.Data["value"] = entryText;
                            yield return item2;
                        }
                    }
                }
                _rowIndex++;
            } // row
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

    #region XslTextReaderItem
    /// <summary>
    /// An item read by <see cref="XlsTextReader"/>.
    /// </summary>
    public sealed class XlsTextReaderItem
    {
        /// <summary>
        /// Gets or sets the level: 0=line, 1=fragment, 2=entry.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets the data.
        /// </summary>
        public Dictionary<string, object> Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XlsTextReaderItem"/> class.
        /// </summary>
        public XlsTextReaderItem()
        {
            Data = new Dictionary<string, object>();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"L{Level}: {Data.Count}";
        }
    }
    #endregion
}
