using Cadmus.Philology.Parts.Layers;
using Catutil.Migration.Entries;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class LemmaLocatorTest
    {
        private static LemmaLocator GetLocator(double treshold = 0.8)
        {
            return new LemmaLocator
            {
                Treshold = treshold
            };
        }

        [Fact]
        public void Locate_NoMatch_0()
        {
            LemmaLocator locator = GetLocator();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.123"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "tactus",
                NormValue = "tactus"
            });

            string loc = locator.Locate(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Null(loc);
        }

        [Fact]
        public void Locate_FuzzyMatch_1()
        {
            LemmaLocator locator = GetLocator();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.123"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "nouum",
                NormValue = "nouum"
            });

            string loc = locator.Locate(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Equal("1.4", loc);
        }

        [Fact]
        public void Locate_ExactMatch_2()
        {
            LemmaLocator locator = GetLocator();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.123"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });

            string loc = locator.Locate(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Equal("1.1", loc);
        }
    }
}
