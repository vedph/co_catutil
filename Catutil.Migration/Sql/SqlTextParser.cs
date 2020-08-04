using Cadmus.Core;
using Cadmus.Parts.General;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Proteus.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Sql
{
    /// <summary>
    /// SQL-based CO text parser. This reads lines of every poem from the MySql
    /// database, and for each poem outputs 1 or more items, each having a
    /// <see cref="TiledTextPart"/> with the corresponding lines of text.
    /// </summary>
    /// <seealso cref="Proteus.Core.IHasLogger" />
    public sealed class SqlTextParser : IHasLogger
    {
        private string _facetId;
        private string _userId;
        private readonly IItemSortKeyBuilder _sortKeyBuilder;
        private readonly char[] _tileSepChars;
        private readonly Regex _breakRegex;
        private readonly Regex _titleTailRegex;
        private readonly string _cs;
        private readonly Queue<IItem> _itemQueue;
        private DbConnection _connection;
        private DbCommand _cmd;
        private bool _end;
        private List<string> _poems;
        private int _poemIndex;
        private int _minTreshold;
        private int _maxTreshold;

        #region Properties
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the facet to be assigned to items.
        /// The default value is <c>default</c>.
        /// </summary>
        public string FacetId
        {
            get { return _facetId; }
            set
            {
                _facetId = value
                    ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the user identifier to be assigned to data being
        /// imported. The default value is <c>zeus</c>.
        /// </summary>
        public string UserId
        {
            get { return _userId; }
            set
            {
                _userId = value
                    ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the minimum treshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value</exception>
        public int MinTreshold
        {
            get { return _minTreshold; }
            set
            {
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _minTreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum treshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value</exception>
        public int MaxTreshold
        {
            get { return _maxTreshold; }
            set
            {
                if (value < 1 || value > 1000)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _maxTreshold = value;
            }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlTextParser" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <exception cref="ArgumentNullException">connectionString</exception>
        public SqlTextParser(string connectionString)
        {
            _cs = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));

            _facetId = "default";
            _userId = "zeus";
            _sortKeyBuilder = new StandardItemSortKeyBuilder();
            _tileSepChars = new[] { ' ', '\t' };
            _breakRegex = new Regex(@"[\u037e.?!][^\p{L}]*$");
            _titleTailRegex = new Regex(@"\s*[,.;]\s*$");
            _minTreshold = 20;
            _maxTreshold = 50;
            _itemQueue = new Queue<IItem>();
        }

        private void EnsureConnected()
        {
            if (_connection != null) return;

            _connection = new MySqlConnection(_cs);
            _connection.Open();
        }

        private List<string> GetPoems()
        {
            EnsureConnected();
            DbCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT `poem` FROM `line` ORDER BY poem";
            List<string> poems = new List<string>();

            using (var reader = cmd.ExecuteReader())
            {
                poems.Add(reader.GetString(0));
            }
            return poems;
        }

        private IList<TextTileRow> GetPoemRows(string poem)
        {
            List<TextTileRow> rows = new List<TextTileRow>();
            _cmd.Parameters[0].Value = poem;

            using (var reader = _cmd.ExecuteReader())
            {
                int y = 0;
                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string line = reader.GetString(1);

                    TextTileRow row = new TextTileRow
                    {
                        Y = ++y
                    };
                    row.Data["_id"] = id;
                    rows.Add(row);

                    // tiles in row
                    foreach (string token in line.Split(_tileSepChars,
                        StringSplitOptions.RemoveEmptyEntries))
                    {
                        int x = 0;
                        var tile = new TextTile
                        {
                            X = ++x,
                        };
                        tile.Data[TextTileRow.TEXT_DATA_NAME] = token;
                        row.Tiles.Add(tile);
                    }
                }
            }
            return rows;
        }

        private bool IsBreakPoint(TextTileRow row, int ordinal)
        {
            if (row.Tiles.Count == 0) return false;

            TextTile lastTile = row.Tiles[row.Tiles.Count - 1];
            if (_breakRegex.IsMatch(lastTile.Data[TextTileRow.TEXT_DATA_NAME])
                && ordinal >= _minTreshold)
            {
                return true;
            }

            if (ordinal < _maxTreshold) return false;

            double excessRatio = ordinal / _maxTreshold;
            return excessRatio > 1.1;
        }

        private IList<Tuple<int, int>> GetRowPartitions(IList<TextTileRow> rows)
        {
            List<Tuple<int, int>> ranges = new List<Tuple<int, int>>();

            if (rows.Count <= MaxTreshold)
            {
                ranges.Add(Tuple.Create(0, rows.Count));
            }

            int start = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                if (IsBreakPoint(rows[i], i + 1))
                {
                    ranges.Add(Tuple.Create(start, i + 1));
                    start = i + 1;
                }
            }
            if (start < rows.Count)
                ranges.Add(Tuple.Create(start, rows.Count - start));

            return ranges;
        }

        private IItem CreateItem(string poem, int partition, string title)
        {
            IItem item = new Item
            {
                FacetId = _facetId,
                CreatorId = _userId,
                UserId = _userId,
                GroupId = poem,
                Title = $"{poem} {partition:00} {title}"
            };
            item.SortKey = _sortKeyBuilder.BuildKey(item, null);
            return item;
        }

        private string FilterTitle(string title) =>
            _titleTailRegex.Replace(title, "").Trim();

        private bool Init()
        {
            // collect poems
            _poems = GetPoems();
            _poemIndex = 0;

            // corner case - no poem
            if (_poems.Count == 0)
            {
                _end = true;
                return false;
            }

            // create the poem's lines query command
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "SELECT `id`,`value` FROM `line` " +
                "WHERE `poem`=@poem ORDER BY `ordinal`";
            var p = _cmd.CreateParameter();
            p.ParameterName = "@poem";
            p.DbType = DbType.String;
            p.Direction = ParameterDirection.Input;
            _cmd.Parameters.Add(p);

            _itemQueue.Clear();

            return true;
        }

        /// <summary>
        /// Reads the next item if any.
        /// </summary>
        /// <returns>The next item or null if no more items.</returns>
        public IItem Read()
        {
            // nope if end reached
            if (_end) return null;

            // if any queued item, just dequeue and return the first one.
            // The queue is used when partitioning a poem, so that each
            // item contains a single portion of the text.
            if (_itemQueue.Count > 0) return _itemQueue.Dequeue();

            // the first time read the list of poems with their lines count
            if (_poems == null && !Init()) return null;

            // fetch all the poem's rows (lines)
            IList<TextTileRow> rows = GetPoemRows(_poems[_poemIndex]);
            string title = FilterTitle(rows[0].GetText());

            // apply partitioning if required
            IList<Tuple<int, int>> ranges = GetRowPartitions(rows);
            IItem retItem = null;

            for (int i = 0; i < ranges.Count; i++)
            {
                // item
                IItem item = CreateItem(_poems[_poemIndex], i + 1, title);

                // tiled text part
                int start = ranges[i].Item1;
                int len = ranges[i].Item2;
                string firstRowId = rows[start].Data["_id"];
                string lastRowId = rows[start + len - 1].Data["_id"];

                TiledTextPart part = new TiledTextPart
                {
                    ItemId = item.Id,
                    CreatorId = _userId,
                    UserId = _userId,
                    Citation = $"{_poems[_poemIndex]}.{firstRowId}-{lastRowId}",
                    Rows = rows.Skip(start).Take(len).ToList()
                };
                item.Parts.Add(part);

                if (i == 0) retItem = item;
                else _itemQueue.Enqueue(item);
            }

            // this poem was completed, move forward for the next Read
            if (++_poemIndex >= _poems.Count) _end = true;

            return retItem;
        }
    }
}