using Cadmus.Philology.Parts.Layers;
using Catutil.Migration.Entries;
using System;
using System.Collections.Generic;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class FragmentLocatorTest
    {
        private static FragmentLocator GetLocator(double treshold = 0.8)
        {
            return new FragmentLocator(id =>
            {
                return id switch
                {
                    "1.1" => "cui dono lepidum nouom libellum",
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
                Location = "1.1234"
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
                Location = "1.1234"
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
                Location = "1.1234"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });

            string loc = locator.LocateFragment(fr, 1, "cui dono lepidum nouom libellum");

            Assert.Equal("1.1", loc);
        }

        [Fact]
        public void LocateFragments_SingleUnresolved_Ok()
        {
            FragmentLocator locator = GetLocator();

            var fragments = new List<ApparatusLayerFragment>();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.1234",
                Tag = "1@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "tactus",
                NormValue = "tactus"
            });
            fragments.Add(fr);

            locator.LocateFragments(fragments);

            Assert.Single(fragments);
            Assert.Equal("1.1234", fragments[0].Location);
        }

        [Fact]
        public void LocateFragments_SingleLoc_Ok()
        {
            FragmentLocator locator = GetLocator();

            var fragments = new List<ApparatusLayerFragment>();
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.1234",
                Tag = "1@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });
            fragments.Add(fr);

            locator.LocateFragments(fragments);

            Assert.Single(fragments);
            Assert.Equal("1.1", fragments[0].Location);
        }

        [Fact]
        public void LocateFragments_LocAndUnresolved_Ok()
        {
            FragmentLocator locator = GetLocator();

            var fragments = new List<ApparatusLayerFragment>();
            // loc
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.1234",
                Tag = "1@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });
            fragments.Add(fr);
            // unresolved
            fr = new ApparatusLayerFragment
            {
                Location = "1.1235",
                Tag = "2@1.1"
            };
            fr.Entries.Add(new ApparatusEntry());
            fragments.Add(fr);

            locator.LocateFragments(fragments);

            Assert.Equal(2, fragments.Count);
            Assert.Equal("1.1", fragments[0].Location);
            Assert.Equal("1.1235", fragments[1].Location);
        }

        [Fact]
        public void LocateFragments_SameLoc_Ok()
        {
            FragmentLocator locator = GetLocator();

            var fragments = new List<ApparatusLayerFragment>();
            // loc
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.1234",
                Tag = "1@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });
            fragments.Add(fr);
            // same loc
            fr = new ApparatusLayerFragment
            {
                Location = "1.1235",
                Tag = "2@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "cui",
                NormValue = "cui"
            });
            fragments.Add(fr);

            locator.LocateFragments(fragments);

            Assert.Single(fragments);
            fr = fragments[0];
            Assert.Equal("1.1", fr.Location);
            Assert.Equal("1-2@1.1-1.1", fr.Tag);
            Assert.Equal(2, fr.Entries.Count);
        }

        [Fact]
        public void LocateFragments_SameLocWithUnresolved_Ok()
        {
            FragmentLocator locator = GetLocator();

            var fragments = new List<ApparatusLayerFragment>();
            // loc
            ApparatusLayerFragment fr = new ApparatusLayerFragment
            {
                Location = "1.1234",
                Tag = "1@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "Cui",
                NormValue = "cui"
            });
            fragments.Add(fr);
            // unresolved
            fr = new ApparatusLayerFragment
            {
                Location = "1.1235",
                Tag = "2@1.1"
            };
            fr.Entries.Add(new ApparatusEntry());
            fragments.Add(fr);
            // same loc
            fr = new ApparatusLayerFragment
            {
                Location = "1.1236",
                Tag = "3@1.1"
            };
            fr.Entries.Add(new ApparatusEntry
            {
                Value = "cui",
                NormValue = "cui"
            });
            fragments.Add(fr);

            locator.LocateFragments(fragments);

            Assert.Single(fragments);
            fr = fragments[0];
            Assert.Equal("1.1", fr.Location);
            Assert.Equal("1-2-3@1.1-1.1235-1.1", fr.Tag);
            Assert.Equal(3, fr.Entries.Count);
        }

        [Fact]
        public void LocateFragments_SameLocWithLoc_Ok()
        {
            throw new NotImplementedException();
        }
    }
}
