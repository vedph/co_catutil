using Fusi.Tools;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// Excel (XLS) text and apparatus importer. This imports text and apparatus
    /// from Excel files (columns ABC=id, text, apparatus) into a MySql database
    /// designed for analysis purposes.
    /// </summary>
    public sealed class XlsTextImporter2
    {
        private readonly string _connectionString;

        /// <summary>
        /// Gets or sets a value indicating whether dry run is enabled.
        /// </summary>
        public bool IsDryRunEnabled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XlsTextImporter"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <exception cref="ArgumentNullException">connectionString</exception>
        public XlsTextImporter2(string connectionString)
        {
            _connectionString = connectionString ??
                throw new ArgumentNullException(nameof(connectionString));
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
            int frId = 0;

            using (XlsTextReader reader = new XlsTextReader(filePath))
            using (MySqlConnection connection = new MySqlConnection(
                _connectionString))
            {
                connection.Open();
                DbCommand insLineCommand = GetInsLineCommand(connection);
                DbCommand insFragmentCommand = GetInsFragmentCommand(connection);
                DbCommand insEntryCommand = GetInsEntryCommand(connection);

                foreach (var item in reader.Read())
                {
                    if (progress != null) report.Count++;

                    switch (item.Level)
                    {
                        case 0:
                            // insert line
                            insLineCommand.Parameters["@id"].Value = item.Data["id"];
                            insLineCommand.Parameters["@poem"].Value = item.Data["poem"];
                            insLineCommand.Parameters["@ordinal"].Value = item.Data["ordinal"];
                            insLineCommand.Parameters["@value"].Value = item.Data["value"];
                            if (!IsDryRunEnabled) insLineCommand.ExecuteNonQuery();
                            break;
                        case 1:
                            insFragmentCommand.Parameters["@lineId"].Value =
                                item.Data["lineId"];
                            insFragmentCommand.Parameters["@ordinal"].Value =
                                item.Data["ordinal"];
                            insFragmentCommand.Parameters["@value"].Value =
                                item.Data["value"];
                            frId = 0;
                            if (!IsDryRunEnabled)
                            {
                                insFragmentCommand.ExecuteNonQuery();
                                frId = (int)((MySqlCommand)insFragmentCommand)
                                    .LastInsertedId;
                            }
                            break;
                        case 2:
                            insEntryCommand.Parameters["@fragmentId"].Value = frId;
                            insEntryCommand.Parameters["@ordinal"].Value =
                                item.Data["ordinal"];
                            insEntryCommand.Parameters["@value"].Value =
                                item.Data["value"];
                            if (!IsDryRunEnabled) insEntryCommand.ExecuteNonQuery();
                            break;
                    }
                    if (cancel.IsCancellationRequested) return;
                    if (progress != null && report.Count % 10 == 0)
                    {
                        report.Message = report.Count.ToString(
                            CultureInfo.InvariantCulture);
                        progress.Report(report);
                    }
                }
            }
        }
    }
}
