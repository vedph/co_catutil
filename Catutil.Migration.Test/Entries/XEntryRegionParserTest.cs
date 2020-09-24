using Cadmus.Philology.Parts.Layers;
using Catutil.Migration.Entries;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using Proteus.Entries.Regions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class XEntryRegionParserTest
    {
        private const string RES_NAME = "XEntryRegionParser.txt";
        private readonly ExplicitRegionDetector _detector;

        public XEntryRegionParserTest()
        {
            _detector = new ExplicitRegionDetector();
            _detector.Configure(new ExplicitRegionDetectorOptions
            {
                PairedCommandNames = new[] { "x" }
            });
        }

        [Fact]
        public void Parse_NoSub_NotEmpty()
        {
            List<DecodedEntry> entries =
                TestHelper.LoadResourceEntries(RES_NAME, "x-with-ms");

            EntryRegionSet regionSet = new EntryRegionSet();
            _detector.Detect(entries, regionSet);

            ApparatusParserContext context = new ApparatusParserContext();
            context.AddEntry(Guid.NewGuid().ToString(), "1.1", 1, 1,
                new ApparatusEntry());

            EntrySet entrySet = new EntrySet(entries, context);

            XEntryRegionParser parser = new XEntryRegionParser();

            int i = parser.Parse(entrySet, regionSet.Regions, 0, context);

            Assert.Equal(1, i);
            Assert.Equal("MS 123, MS 124.", context.CurrentEntry.Note);
        }

        [Fact]
        public void Parse_Sub_NotEmpty()
        {
            List<DecodedEntry> entries =
                TestHelper.LoadResourceEntries(RES_NAME, "x-with-ms");

            EntryRegionSet regionSet = new EntryRegionSet();
            _detector.Detect(entries, regionSet);

            // add sub-region for MS 123
            regionSet.AddNewRegion(EntryRange.Parse("1.0-1.5"), "ms");

            ApparatusParserContext context = new ApparatusParserContext();
            context.AddEntry(Guid.NewGuid().ToString(), "1.1", 1, 1,
                new ApparatusEntry());

            EntrySet entrySet = new EntrySet(entries, context);

            XEntryRegionParser parser = new XEntryRegionParser();

            int i = parser.Parse(entrySet, regionSet.Regions, 0, context);

            Assert.Equal(1, i);
            Assert.Equal(", MS 124.", context.CurrentEntry.Note);
        }

        [Fact]
        public void Parse_Subs_Empty()
        {
            List<DecodedEntry> entries =
                TestHelper.LoadResourceEntries(RES_NAME, "x-with-ms");

            EntryRegionSet regionSet = new EntryRegionSet();
            _detector.Detect(entries, regionSet);

            // add sub-regions for MS 123 and MS 124:
            // MS 123, MS 124.
            // 0123456789-1234
            regionSet.AddNewRegion(EntryRange.Parse("1.0-1.5"), "ms");
            regionSet.AddNewRegion(EntryRange.Parse("1.8-1.13"), "ms");

            ApparatusParserContext context = new ApparatusParserContext();
            context.AddEntry(Guid.NewGuid().ToString(), "1.1", 1, 1,
                new ApparatusEntry());

            EntrySet entrySet = new EntrySet(entries, context);

            XEntryRegionParser parser = new XEntryRegionParser();

            int i = parser.Parse(entrySet, regionSet.Regions, 0, context);

            Assert.Equal(1, i);
            Assert.Null(context.CurrentEntry.Note);
        }

        [Fact]
        public void Parse_SubToEnd_Empty()
        {
            List<DecodedEntry> entries =
                TestHelper.LoadResourceEntries(RES_NAME, "x-with-ms");

            EntryRegionSet regionSet = new EntryRegionSet();
            _detector.Detect(entries, regionSet);

            // add fake sub-regions from MS 123 up to next entry
            regionSet.AddNewRegion(EntryRange.Parse("1.0-2.0"), "ms");

            ApparatusParserContext context = new ApparatusParserContext();
            context.AddEntry(Guid.NewGuid().ToString(), "1.1", 1, 1,
                new ApparatusEntry());

            EntrySet entrySet = new EntrySet(entries, context);

            XEntryRegionParser parser = new XEntryRegionParser();

            int i = parser.Parse(entrySet, regionSet.Regions, 0, context);

            Assert.Equal(1, i);
            Assert.Null(context.CurrentEntry.Note);
        }
    }
}
