using Fusi.Tools.Config;
using MySql.Data.MySqlClient;
using Proteus.Core.Entries;
using System.Data.Common;

namespace Catutil.Migration.Sql
{
    /// <summary>
    /// SQL entry record reader. This entry reader reads each record from the
    /// <c>entry</c> table in the CO imported database, and returns it as a
    /// <see cref="DecodedTextEntry"/>, prefixed by an escape with its fragment ID
    /// and entry ID in the database.
    /// <para>Tag: <c>entry-reader.co-sql</c>.</para>
    /// </summary>
    /// <seealso cref="IEntryReader" />
    /// <seealso cref="IConfigurable{SqlEntryReaderOptions}" />
    [Tag("entry-reader.co-sql")]
    public sealed class SqlEntryReader : IEntryReader,
        IConfigurable<SqlEntryReaderOptions>
    {
        private string _cs;
        private DbConnection _connection;
        private DbDataReader _reader;
        private bool _end;

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public void Configure(SqlEntryReaderOptions options)
        {
            _cs = options?.ConnectionString;
        }

        private void EnsureConnected()
        {
            if (_connection != null) return;

            _connection = new MySqlConnection(_cs);
            _connection.Open();
            DbCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT entry.id,entry.fragmentId,entry.value," +
                "line.itemId " +
                "FROM entry " +
                "INNER JOIN fragment ON entry.fragmentId=fragment.id " +
                "INNER JOIN line ON fragment.lineId=line.id " +
                "ORDER BY entry.fragmentId,entry.ordinal;";
            _reader = cmd.ExecuteReader();
        }

        private static string BuildEntryPrefix(int id, int fragmentId, string itemId) =>
            $"«#{itemId}.{fragmentId}.{id}»";

        /// <summary>
        /// Reads the next entry if any.
        /// </summary>
        /// <returns>
        /// next entry, or null if no more entries. The entry is a
        /// <see cref="DecodedTextEntry"/> with a single text equal to the text
        /// of the CO apparatus entry, preceded by an escape with syntax
        /// <c>«#FRID.ID»</c>, where <c>FRID</c>=entry fragment's ID in source
        /// database, and <c>ID</c>-entry's ID in source database.
        /// </returns>
        public DecodedEntry Read()
        {
            if (_end) return null;

            EnsureConnected();
            if (!_reader.Read())
            {
                _end = true;
                return null;
            }

            int id = _reader.GetInt32(0);
            int fragmentId = _reader.GetInt32(1);
            string itemId = _reader.IsDBNull(3)? null : _reader.GetString(3);
            string value = BuildEntryPrefix(id, fragmentId, itemId)
                + _reader.GetString(2);

            return new DecodedTextEntry
            {
                SourceIndex = 0,
                SourceLength = value.Length,
                Value = value
            };
        }
    }

    /// <summary>
    /// Options for <see cref="SqlEntryReader"/>.
    /// </summary>
    public sealed class SqlEntryReaderOptions
    {
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
