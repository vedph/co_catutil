using Cadmus.Philology.Parts.Layers;
using Catutil.Migration.Entries;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class LemmaLocatorTest
    {
        private static FragmentLocator GetLocator(double treshold = 0.8)
        {
            return new FragmentLocator(id =>
            {
                return id switch
                {
                    "1" => "cui dono lepidum nouom libellum",
                    _ => null,
                };
            })
            {
                Treshold = treshold
            };
        }

        [Fact]
        public void LocateFragment_NoMatch_0()
        {
            FragmentLocator locator = GetLocator();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.123"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "tactus",
                NormValue = "tactus"
            });

            string loc = locator.LocateFragment(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Null(loc);
        }

        [Fact]
        public void LocateFragment_FuzzyMatch_1()
        {
            FragmentLocator locator = GetLocator();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.123"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "nouum",
                NormValue = "nouum"
            });

            string loc = locator.LocateFragment(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Equal("1.4", loc);
        }

        [Fact]
        public void LocateFragment_ExactMatch_2()
        {
            FragmentLocator locator = GetLocator();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.123"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });

            string loc = locator.LocateFragment(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Equal("1.1", loc);
        }
    }
}
