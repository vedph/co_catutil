using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using Fusi.Tools.Config;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Proteus.Core;
using Proteus.Entries;
using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Context for CO apparatus parsers targeting Cadmus parts to be written
    /// into a set of JSON files. This context accumulates the fragments and
    /// entries of an apparatus part until its item ID changes. Then, it
    /// saves the whole part in JSON format, using as output a set of files
    /// so that no more than <see cref="ApparatusParserContextOptions.MaxPartsPerFile"/>
    /// parts are found in each file.
    /// <para>Tag: <c>parser-context.co-apparatus</c>.</para>
    /// </summary>
    [Tag("parser-context.co-apparatus")]
    public class ApparatusParserContext : EntrySetReaderContext,
        IParserContext, IHasLogger,
        IConfigurable<ApparatusParserContextOptions>
    {
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly FragmentLocator _locator;

        private ILogger _logger;
        private string _outputDir;
        private int _maxPartsPerFile;
        private int _filePartCount;
        private int _fileNr;
        private TextWriter _writer;
        private DbConnection _connection;
        private DbCommand _cmdGetLine;

        #region Properties        
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
                _locator.Logger = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string to the source database.
        /// This is used to fetch lines text and detecting apparatus fragments
        /// locations.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the user identifier to assign to parsed parts.
        /// The default value is <c>zeus</c>.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets the apparatus part.
        /// </summary>
        public TiledTextLayerPart<ApparatusLayerFragment> ApparatusPart
            { get; private set; }

        /// <summary>
        /// Gets the current entry.
        /// </summary>
        public ApparatusEntry CurrentEntry { get; private set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ApparatusParserContext"/> class.
        /// </summary>
        public ApparatusParserContext()
        {
            UserId = "zeus";

            _maxPartsPerFile = 100;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            };
            _locator = new FragmentLocator(GetLine);
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(ApparatusParserContextOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            ConnectionString = options.ConnectionString;
            _outputDir = options.OutputDirectory;
            _maxPartsPerFile = options.MaxPartsPerFile;

            _connection?.Close();
            _connection = new MySqlConnection(ConnectionString);
            _cmdGetLine = _connection.CreateCommand();

            _cmdGetLine.CommandText = "SELECT `value` FROM `line` WHERE `id`=@id";
            DbParameter p = _cmdGetLine.CreateParameter();
            p.ParameterName = "@id";
            p.DbType = DbType.String;
            p.Direction = ParameterDirection.Input;
            _cmdGetLine.Parameters.Add(p);
        }

        private string GetLine(string id)
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection not configured");

            if (_connection.State != ConnectionState.Open) _connection.Open();
            _cmdGetLine.Parameters[0].Value = id;
            using (DbDataReader reader = _cmdGetLine.ExecuteReader())
            {
                if (!reader.Read()) return null;
                return reader.GetString(0);
            }
        }

        private void AdjustPart()
        {
            foreach (ApparatusLayerFragment fr in ApparatusPart.Fragments)
            {
                foreach (ApparatusEntry entry in fr.Entries)
                {
                    // replacement with value=null is a deletion,
                    // so remove also the normValue, which was kept
                    // to allow detection
                    if (entry.Type == ApparatusEntryType.Replacement
                        && entry.Value == null)
                    {
                        entry.NormValue = null;
                    }
                }
            }
        }

        private void SavePart()
        {
            // try locating fragments
            _locator.LocateFragments(ApparatusPart.Fragments);

            // adjust part
            AdjustPart();

            // create new output file if required
            if (_writer == null
                || (_maxPartsPerFile > 0 && ++_filePartCount > _maxPartsPerFile))
            {
                if (_writer != null) CloseOutputFile();
                string path = Path.Combine(_outputDir,
                    $"app_{++_fileNr:00000}.json");

                _writer = new StreamWriter(new FileStream(path,
                    FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8);
                _writer.WriteLine("[");

                _filePartCount = 0;
            }

            string json = JsonConvert.SerializeObject(ApparatusPart, _jsonSettings);

            // this will output a , also for the last JSON array item
            // but usually we can ignore this in JSON parsers
            _writer.WriteLine(json + ",");
        }

        private void CloseOutputFile()
        {
            if (_writer == null) return;

            _writer.WriteLine("]");
            _writer.Flush();
            _writer.Close();
        }

        /// <summary>
        /// Starts the parsing, creating the output directory if required
        /// and resetting the context status.
        /// </summary>
        public void Start()
        {
            CloseOutputFile();

            if (!string.IsNullOrEmpty(_outputDir) && !Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);
            _fileNr = 0;
            _filePartCount = 0;
        }

        /// <summary>
        /// Ends the parsing, saving any pending part and closing the output
        /// file if any.
        /// </summary>
        public void End()
        {
            if (ApparatusPart != null) SavePart();
            CloseOutputFile();
        }

        /// <summary>
        /// Adds the specified apparatus entry to this context. The entry will
        /// get the current entry ID as its tag, and will replace an existing
        /// entry with the same tag if any. The apparatus layer part and the
        /// container fragment will be created as needed.
        /// </summary>
        /// <remarks>Each fragment gets as its
        /// <see cref="ApparatusLayerFragment.Tag"/> a value built of the
        /// fragment's numeric ID in the database plus <c>@</c> plus the ID
        /// of the line the entry refers to. Its
        /// <see cref="ApparatusLayerFragment.Location"/> is set to
        /// Y=<paramref name="lineOrdinal"/> and X=1000 + the fragment ordinal number.
        /// The X value is fake, and is used only to ensure that no fragment
        /// overlaps when we are collecting them here. Later, when saving the
        /// whole part we try to locate the fragments as a whole.
        /// <para>Each entry gets its numeric ID in the source database in its
        /// <see cref="ApparatusEntry.Tag"/>, too.</para>
        /// </remarks>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="lineId">The line indentifier.</param>
        /// <param name="lineOrdinal">The line ordinal number in the poem.</param>
        /// <param name="fragmentId">The ID of the fragment the entry belongs
        /// to.</param>
        /// <param name="entry">The apparatus entry. It is assumed that its
        /// <see cref="ApparatusEntry.Tag"/> is equal to the entry's ID in the
        /// source database.</param>
        /// <exception cref="ArgumentNullException">itemId or entry</exception>
        public void AddEntry(string itemId, string lineId, int lineOrdinal,
            int fragmentId, ApparatusEntry entry)
        {
            if (itemId == null) throw new ArgumentNullException(nameof(itemId));
            if (lineId == null)
                throw new ArgumentNullException(nameof(lineId));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            // ensure there is the item's part
            if (ApparatusPart == null || ApparatusPart.ItemId != itemId)
            {
                if (ApparatusPart != null)
                {
                    SavePart();
                }
                ApparatusPart = new TiledTextLayerPart<ApparatusLayerFragment>
                {
                    ItemId = itemId,
                    CreatorId = UserId,
                    UserId = UserId
                };
            }

            // ensure there is the container fragment
            string tagPrefix = fragmentId + "@";
            ApparatusLayerFragment fr =
                ApparatusPart.Fragments.Find(f => f.Tag.StartsWith(tagPrefix));
            if (fr == null)
            {
                fr = new ApparatusLayerFragment
                {
                    // just assign a fake location, ensuring it's unique
                    // for each added fragment. The true location will be set
                    // by later analysis when saving the complete part
                    Location = $"{lineOrdinal}.{1000 + ApparatusPart.Fragments.Count + 1}",
                    // keep the source fragment ID + @ + line ID in its tag
                    Tag = tagPrefix + lineId
                };
                ApparatusPart.AddFragment(fr);
            }

            // add the entry
            ApparatusEntry targetEntry = fr.Entries.Find(e => e.Tag == entry.Tag);
            // if it exists, it will be replaced -- anyway this should never happen
            if (targetEntry != null)
            {
                Logger?.LogWarning($"Entry with ID {entry.Tag} " +
                    $"in fragment with ID {fragmentId} " +
                    $"in line {lineId} is overwritten");
                fr.Entries.Remove(targetEntry);
            }
            fr.Entries.Add(entry);

            // update the current entry
            CurrentEntry = entry;
        }
    }

    /// <summary>
    /// Options for <see cref="ApparatusParserContext"/>.
    /// </summary>
    public sealed class ApparatusParserContextOptions
    {
        /// <summary>
        /// Gets or sets the connection string to the source MySql database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the output directory, where all the Cadmus models
        /// will be saved in JSON files.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of parts per file.
        /// </summary>
        public int MaxPartsPerFile { get; set; }
    }
}
