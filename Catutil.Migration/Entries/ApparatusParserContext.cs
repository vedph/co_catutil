using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using Fusi.Tools.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Context for CO apparatus parsers targeting Cadmus parts to be written
    /// into a set of JSON files.
    /// <para>Tag: <c>parser-context.co-apparatus</c>.</para>
    /// </summary>
    [Tag("parser-context.co-apparatus")]
    public class ApparatusParserContext : IParserContext,
        IConfigurable<CadmusParserContextOptions>
    {
        private readonly JsonSerializerSettings _jsonSettings;
        private int _fragmentId;
        private string _fid;

        private int _entryId;
        private string _eid;

        private string _outputDir;
        private int _maxPartsPerFile;
        private int _filePartCount;
        private int _fileNr;
        private TextWriter _writer;

        #region Properties
        /// <summary>
        /// Gets or sets the current fragment identifier in the source database.
        /// </summary>
        public int FragmentId
        {
            get { return _fragmentId; }
            set
            {
                if (_fragmentId == value) return;
                _fragmentId = value;
                _fid = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets or sets the current entry identifier in the source database.
        /// </summary>
        public int EntryId
        {
            get { return _entryId; }
            set
            {
                if (_entryId == value) return;
                _entryId = value;
                _eid = value.ToString(CultureInfo.InvariantCulture);
            }
        }

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
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(CadmusParserContextOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _outputDir = options.OutputDirectory;
            _maxPartsPerFile = options.MaxPartsPerFile;
        }

        private void SavePart()
        {
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
        /// <param name="itemId">The item identifier.</param>
        /// <param name="entry">The apparatus entry.</param>
        /// <exception cref="ArgumentNullException">itemId or entry</exception>
        public void AddEntry(string itemId, ApparatusEntry entry)
        {
            if (itemId == null) throw new ArgumentNullException(nameof(itemId));
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
            ApparatusLayerFragment fr =
                ApparatusPart.Fragments.Find(f => f.Tag == _fid);
            if (fr == null)
            {
                fr = new ApparatusLayerFragment
                {
                    // keep the source fragment ID in its tag
                    Tag = _fid
                };
                ApparatusPart.AddFragment(fr);
            }

            // entry
            ApparatusEntry targetEntry = fr.Entries.Find(e => e.Tag == _eid);
            // if it exists, it will be replaced -- anyway this should never happen
            if (targetEntry != null) fr.Entries.Remove(targetEntry);
            // keep the source entry ID in its tag
            entry.Tag = _eid;
            fr.Entries.Add(entry);

            // update the current entry
            CurrentEntry = entry;
        }
    }

    /// <summary>
    /// Options for <see cref="ApparatusParserContext"/>.
    /// </summary>
    public sealed class CadmusParserContextOptions
    {
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
