using Fusi.Tools;
using MySql.Data.MySqlClient;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// Excel (XLS) text and apparatus importer. This imports text and apparatus
    /// from Excel files (columns ABC=id, text, apparatus) into a MySql database
    /// designed for analysis purposes.
    /// </summary>
    public sealed class XlsTextImporter
    {
        private readonly string _connectionString;
        private readonly Regex _poemRegex;
        private readonly Regex _wsRegex;

        /// <summary>
        /// Gets or sets the name of the sheet to read.
        /// The default value is <c>Foglio1</c>.
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dry run is enabled.
        /// </summary>
        public bool IsDryRunEnabled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XlsTextImporter"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <exception cref="ArgumentNullException">connectionString</exception>
        public XlsTextImporter(string connectionString)
        {
            _connectionString = connectionString ??
                throw new ArgumentNullException(nameof(connectionString));
            SheetName = "Foglio1";

            // the poem "number" in the line ID is all what precedes the first
            // dot (or sometimes - seemingly by mistake - comma)
            _poemRegex = new Regex(@"^(?<pn>\d+)(?<pa>[^.,]*)");
            _wsRegex = new Regex(@"\s+");
        }

        /// <summary>
        /// Gets the target database SQL schema.
        /// </summary>
        /// <returns>SQL code representing the schema definition</returns>
        public static string GetTargetSchema()
        {
            using (StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Catutil.Migration.Assets.Schema.sql"),
                Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void AddParameter(DbCommand command, string name,
            DbType type)
        {
            DbParameter p = command.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            command.Parameters.Add(p);
        }

        private static DbCommand GetInsLineCommand(MySqlConnection connection)
        {
            DbCommand cmd =
                new MySqlCommand(
                    "INSERT INTO `line`(`id`,`poem`,`ordinal`,`value`)\n" +
                    "VALUES(@id,@poem,@ordinal,@value);", connection);
            AddParameter(cmd, "@id", DbType.String);
            AddParameter(cmd, "@poem", DbType.String);
            AddParameter(cmd, "@ordinal", DbType.Int32);
            AddParameter(cmd, "@value", DbType.String);
            return cmd;
        }

        private static DbCommand GetInsFragmentCommand(MySqlConnection connection)
        {
            MySqlCommand cmd =
                new MySqlCommand(
                    "INSERT INTO `fragment`(`lineId`,`ordinal`,`value`)\n" +
                    "VALUES(@lineId,@ordinal,@value);", connection);
            AddParameter(cmd, "@lineId", DbType.String);
            AddParameter(cmd, "@ordinal", DbType.Int16);
            AddParameter(cmd, "@value", DbType.String);
            return cmd;
        }

        private static DbCommand GetInsEntryCommand(MySqlConnection connection)
        {
            MySqlCommand cmd =
                new MySqlCommand(
                    "INSERT INTO `entry`(`fragmentId`,`ordinal`,`value`)\n" +
                    "VALUES(@fragmentId,@ordinal,@value);", connection);
            AddParameter(cmd, "@fragmentId", DbType.Int32);
            AddParameter(cmd, "@ordinal", DbType.Int16);
            AddParameter(cmd, "@value", DbType.String);
            return cmd;
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
        /// <param name="filePath">The file path.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
        /// <exception cref="InvalidDataException">Invalid text line ID</exception>
        public void Import(string filePath, CancellationToken cancel,
            IProgress<ProgressReport> progress = null)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            ProgressReport report = progress != null ? new ProgressReport() : null;

            using (MySqlConnection connection = new MySqlConnection(
                _connectionString))
            {
                connection.Open();
                DbCommand insLineCommand = GetInsLineCommand(connection);
                DbCommand insFragmentCommand = GetInsFragmentCommand(connection);
                DbCommand insEntryCommand = GetInsEntryCommand(connection);

                // https://stackoverflow.com/questions/5855813/how-to-read-file-using-npoi
                HSSFWorkbook wbk;
                using (FileStream file = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read))
                {
                    wbk = new HSSFWorkbook(file);
                }

                ISheet sheet = wbk.GetSheet(SheetName);
                string currentPoem = null;
                string prevLineId = null;
                short frOrdinal = 0;
                bool continued = false;
                short lineOrdinal = 0;

                for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row == null) continue;  // skip empty row

                    // [0]=id: note that occasionally the cell's type is not
                    // a string as expected, so here we are more tolerant
                    string lineId;
                    continued = false;

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
                            if (prevLineId == null)
                            {
                                throw new InvalidDataException(
                                    $"Blank ID without preceding line at row {rowIndex + 1}");
                            }
                            continued = true;
                            lineId = prevLineId;
                            break;
                        default:
                            throw new InvalidDataException(
                                $"Invalid cell 0 type at row {rowIndex + 1}");
                    }

                    if (!continued)
                    {
                        prevLineId = lineId;
                        frOrdinal = 0;
                        Match m = _poemRegex.Match(lineId);
                        if (!m.Success)
                        {
                            throw new InvalidDataException(
                                $"Invalid text line ID: {lineId}");
                        }

                        string poem = m.Groups["pn"].Value + m.Groups["pa"].Value;
                        int n = int.Parse(m.Groups["pn"].Value,
                            CultureInfo.InvariantCulture);
                        poem = $"{n:000}{m.Groups["pa"].Value}";
                        if (poem != currentPoem)
                        {
                            lineOrdinal = 0;
                            currentPoem = poem;
                        }
                        lineOrdinal++;

                        // [1]=line
                        string text = row.Cells[1].StringCellValue.Trim();
                        // insert line
                        insLineCommand.Parameters["@id"].Value = lineId;
                        insLineCommand.Parameters["@poem"].Value = currentPoem;
                        insLineCommand.Parameters["@ordinal"].Value = lineOrdinal;
                        insLineCommand.Parameters["@value"].Value = text;
                        if (!IsDryRunEnabled)
                        {
                            insLineCommand.ExecuteNonQuery();
                        }
                    }

                    // [2]=app with formatting
                    if (row.Cells.Count > 2)
                    {
                        HSSFRichTextString rts = (HSSFRichTextString)
                            row.Cells[2].RichStringCellValue;
                        IList<FormattingRun> runs = GetFormattingRuns(rts);
                        string appText = BuildItalicizedText(wbk, rts.String, runs);

                        // split and insert fragments
                        foreach (string frText in GetAppFragments(appText))
                        {
                            frOrdinal++;
                            insFragmentCommand.Parameters["@lineId"].Value = lineId;
                            insFragmentCommand.Parameters["@ordinal"].Value = frOrdinal;
                            insFragmentCommand.Parameters["@value"].Value = frText;
                            int frId = 0;
                            if (!IsDryRunEnabled)
                            {
                                insFragmentCommand.ExecuteNonQuery();
                                frId = (int)((MySqlCommand)insFragmentCommand)
                                    .LastInsertedId;
                            }

                            // split and insert entries
                            short entryOrdinal = 0;
                            foreach (string entryText in GetAppEntries(frText))
                            {
                                entryOrdinal++;
                                insEntryCommand.Parameters["@fragmentId"].Value =
                                    frId;
                                insEntryCommand.Parameters["@ordinal"].Value =
                                    entryOrdinal;
                                insEntryCommand.Parameters["@value"].Value =
                                    entryText;
                                if (!IsDryRunEnabled)
                                {
                                    insEntryCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    if (cancel.IsCancellationRequested) return;
                    if (progress != null && rowIndex % 10 == 0)
                    {
                        report.Percent = (rowIndex + 1) * 100 / sheet.LastRowNum;
                        progress.Report(report);
                    }
                }
                if (progress != null)
                {
                    report.Percent = 100;
                    progress.Report(report);
                }
            }
        }
    }
}
