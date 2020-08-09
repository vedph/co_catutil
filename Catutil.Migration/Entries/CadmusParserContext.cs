using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using System;
using System.Globalization;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Context for parsers targeting Cadmus models.
    /// </summary>
    public class CadmusParserContext
    {
        private int _fragmentId;
        private string _fid;

        private int _entryId;
        private string _eid;

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
        #endregion

        /// <summary>
        /// Gets the apparatus part.
        /// </summary>
        public TiledTextLayerPart<ApparatusLayerFragment> ApparatusPart
            { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CadmusParserContext"/> class.
        /// </summary>
        public CadmusParserContext()
        {
            UserId = "zeus";
        }

        /// <summary>
        /// Adds the specified apparatus entry to this context. The entry will
        /// get the current entry ID as its tag, and will replace an existing
        /// entry with the same tag if any. The apparatus layer part and the
        /// container fragment will be created as needed.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="entry">The apparatus entry.</param>
        /// <param name="partCompleted">The part completed callback. This is
        /// called whenever a part gets completed and a new one starts in this
        /// context.</param>
        /// <exception cref="ArgumentNullException">itemId or entry</exception>
        public void AddEntry(string itemId, ApparatusEntry entry,
            Action<TiledTextLayerPart<ApparatusLayerFragment>> partCompleted)
        {
            if (itemId == null) throw new ArgumentNullException(nameof(itemId));
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (partCompleted == null)
                throw new ArgumentNullException(nameof(partCompleted));

            // ensure there is the item's part
            if (ApparatusPart == null || ApparatusPart.ItemId != itemId)
            {
                partCompleted(ApparatusPart);
                ApparatusPart = new TiledTextLayerPart<ApparatusLayerFragment>
                {
                    ItemId = itemId,
                    CreatorId = UserId,
                    UserId = UserId
                };
            }

            // ensure there is the container fragment
            ApparatusLayerFragment fr = ApparatusPart.Fragments.Find(f => f.Tag == _fid);
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
        }
    }
}
