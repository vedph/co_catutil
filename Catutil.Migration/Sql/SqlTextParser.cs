﻿using Cadmus.Core;
using Cadmus.Parts.General;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Proteus.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Sql
{
    /// <summary>
    /// SQL-based CO text parser. This reads lines of every poem from the MySql
    /// database, and for each poem outputs 1 or more items, each having a
    /// <see cref="TiledTextPart"/> with the corresponding lines of text.
    /// </summary>
    /// <seealso cref="IHasLogger" />
    public sealed class SqlTextParser : IHasLogger
    {
        private string _facetId;
        private string _userId;
        private readonly IItemSortKeyBuilder _sortKeyBuilder;
        private readonly char[] _tileSepChars;
        private readonly Regex _breakRegex;
        private readonly Regex _titleTailRegex;
        private readonly Regex _wsRegex;
        private readonly string _cs;
        private readonly IPartitioner _partitioner;
        private readonly Queue<IItem> _itemQueue;
        private DbConnection _connection;
        private DbCommand _cmdGetPoemLines;
        private DbCommand _cmdSetItemId;
        private bool _end;
        private List<string> _poems;
        private int _poemIndex;

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
        /// Gets or sets a value indicating whether this parser should update
        /// the source database by mapping each line to its item ID. Default
        /// value is true.
        /// </summary>
        public bool IsItemIdMappingEnabled { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlTextParser" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="partitioner">The partitioner to be used.</param>
        /// <exception cref="ArgumentNullException">connectionString or
        /// partitioner</exception>
        public SqlTextParser(string connectionString, IPartitioner partitioner)
        {
            _cs = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
            _partitioner = partitioner
                ?? throw new ArgumentNullException(nameof(partitioner));
            _facetId = "default";
            _userId = "zeus";
            _sortKeyBuilder = new StandardItemSortKeyBuilder();
            _tileSepChars = new[] { ' ', '\t' };
            _breakRegex = new Regex(@"[\u037e.?!][^\p{L}]*$");
            _titleTailRegex = new Regex(@"\s*[,.;]\s*$");
            _wsRegex = new Regex(@"\s+");
            _itemQueue = new Queue<IItem>();
            IsItemIdMappingEnabled = true;
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
                while (reader.Read()) poems.Add(reader.GetString(0));
            }
            return poems;
        }

        private IList<TextTileRow> GetPoemRows(string poem)
        {
            List<TextTileRow> rows = new List<TextTileRow>();
            _cmdGetPoemLines.Parameters[0].Value = poem;

            using (var reader = _cmdGetPoemLines.ExecuteReader())
            {
                int y = 0;
                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string line = _wsRegex.Replace(reader.GetString(1), " ").Trim();

                    TextTileRow row = new TextTileRow
                    {
                        Y = ++y
                    };
                    row.Data["_id"] = id;
                    // store the ordinal for later use when updating item IDs
                    // in the source database
                    row.Data["_ord"] = reader.GetInt32(2)
                        .ToString(CultureInfo.InvariantCulture);
                    rows.Add(row);

                    // tiles in row
                    int x = 0;
                    foreach (string token in line.Split(_tileSepChars,
                        StringSplitOptions.RemoveEmptyEntries))
                    {
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

        private bool IsBreakPoint(TextTileRow row)
        {
            if (row.Tiles.Count == 0) return false;

            TextTile lastTile = row.Tiles[row.Tiles.Count - 1];
            return _breakRegex.IsMatch(lastTile.Data[TextTileRow.TEXT_DATA_NAME]);
        }

        private IItem CreateItem(string poem, int partition, string title,
            string description)
        {
            IItem item = new Item
            {
                FacetId = _facetId,
                CreatorId = _userId,
                UserId = _userId,
                GroupId = poem,
                Title = $"{poem} {partition:00} {title}",
                Description = description,
                Flags = 1   // = to revise
            };
            item.SortKey = _sortKeyBuilder.BuildKey(item, null);
            return item;
        }

        private string FilterTitle(string title) =>
            _titleTailRegex.Replace(title, "").Trim();

        private void CreateGetPoemLinesCommand()
        {
            _cmdGetPoemLines = _connection.CreateCommand();
            _cmdGetPoemLines.CommandText =
                "SELECT `id`,`value`,`ordinal` FROM `line` " +
                "WHERE `poem`=@poem ORDER BY `ordinal`";
            DbParameter p = _cmdGetPoemLines.CreateParameter();
            p.ParameterName = "@poem";
            p.DbType = DbType.String;
            p.Direction = ParameterDirection.Input;
            _cmdGetPoemLines.Parameters.Add(p);
        }

        private void CreateSetItemIdCommand()
        {
            _cmdSetItemId = _connection.CreateCommand();
            _cmdSetItemId.CommandText =
                "UPDATE `line` SET `itemId`=@itemId\n" +
                "WHERE `poem`=@poem\n" +
                "AND `ordinal`>=@firstOrdinal AND `ordinal`<=@lastOrdinal";

            DbParameter itemIdParam = _cmdSetItemId.CreateParameter();
            itemIdParam.ParameterName = "@itemId";
            itemIdParam.DbType = DbType.String;
            itemIdParam.Direction = ParameterDirection.Input;
            _cmdSetItemId.Parameters.Add(itemIdParam);

            DbParameter poemParam = _cmdSetItemId.CreateParameter();
            poemParam.ParameterName = "@poem";
            poemParam.DbType = DbType.String;
            poemParam.Direction = ParameterDirection.Input;
            _cmdSetItemId.Parameters.Add(poemParam);

            DbParameter firstOrdinalParam = _cmdSetItemId.CreateParameter();
            firstOrdinalParam.ParameterName = "@firstOrdinal";
            firstOrdinalParam.DbType = DbType.Int32;
            firstOrdinalParam.Direction = ParameterDirection.Input;
            _cmdSetItemId.Parameters.Add(firstOrdinalParam);

            DbParameter lastOrdinalParam = _cmdSetItemId.CreateParameter();
            lastOrdinalParam.ParameterName = "@lastOrdinal";
            lastOrdinalParam.DbType = DbType.Int32;
            lastOrdinalParam.Direction = ParameterDirection.Input;
            _cmdSetItemId.Parameters.Add(lastOrdinalParam);
        }

        private void ClearItemIds()
        {
            DbCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE `line` SET `itemId`=NULL";
            cmd.ExecuteNonQuery();
        }

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
            CreateGetPoemLinesCommand();

            // create the item ID setter command
            CreateSetItemIdCommand();

            // clear all the item IDs if required
            if (IsItemIdMappingEnabled) ClearItemIds();

            _itemQueue.Clear();

            return true;
        }

        private void UpdateItemId(IItem item)
        {
            _cmdSetItemId.Parameters["@itemId"].Value = item.Id;
            _cmdSetItemId.Parameters["@poem"].Value = _poems[_poemIndex];

            TiledTextPart textPart = (TiledTextPart)item.Parts[0];

            _cmdSetItemId.Parameters["@firstOrdinal"].Value =
                int.Parse(textPart.Rows[0].Data["_ord"],
                CultureInfo.InvariantCulture);

            _cmdSetItemId.Parameters["@lastOrdinal"].Value =
                int.Parse(textPart.Rows[textPart.Rows.Count - 1].Data["_ord"],
                CultureInfo.InvariantCulture);

            _cmdSetItemId.ExecuteNonQuery();
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
            if (_itemQueue.Count > 0)
            {
                IItem item = _itemQueue.Dequeue();
                if (IsItemIdMappingEnabled) UpdateItemId(item);
                if (_itemQueue.Count == 0 && ++_poemIndex >= _poems.Count)
                    _end = true;
                return item;
            }

            // the first time read the list of poems with their lines count
            if (_poems == null && !Init()) return null;

            // fetch all the poem's rows (lines)
            IList<TextTileRow> rows = GetPoemRows(_poems[_poemIndex]);
            string title = FilterTitle(rows[0].GetText());

            // apply partitioning if required
            IList<Tuple<int, int>> ranges =
                _partitioner.Partition(rows.Count, i => IsBreakPoint(rows[i]));
            IItem retItem = null;

            for (int i = 0; i < ranges.Count; i++)
            {
                int start = ranges[i].Item1;
                int len = ranges[i].Item2;
                string firstRowId = rows[start].Data["_id"];
                string lastRowId = rows[start + len - 1].Data["_id"];

                IItem item = CreateItem(_poems[_poemIndex], i + 1, title,
                    $"{firstRowId}-{lastRowId} {title} - " +
                    $"{FilterTitle(rows[rows.Count - 1].GetText())}");

                TiledTextPart part = new TiledTextPart
                {
                    ItemId = item.Id,
                    CreatorId = _userId,
                    UserId = _userId,
                    Citation = $"{firstRowId}-{lastRowId}",
                    Rows = rows.Skip(start).Take(len).ToList()
                };
                item.Parts.Add(part);

                if (i == 0) retItem = item;
                else _itemQueue.Enqueue(item);
            }

            // update the item ID for the current item if requested
            if (IsItemIdMappingEnabled) UpdateItemId(retItem);

            // unless queued items are present, this poem was completed,
            // so move forward for the next Read
            if (_itemQueue.Count == 0 && ++_poemIndex >= _poems.Count)
                _end = true;

            return retItem;
        }
    }
}