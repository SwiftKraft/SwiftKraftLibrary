// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Cloud;
    using Coherence.Tests;
    using NUnit.Framework;

    public class LobbyFilterTests : CoherenceTest
    {
        [Test]
        [TestCase(-2)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(8)]
        [Description("Filter string matches a single max player value")]
        public void Filter_Contains_MaxPlayers(int maxPlayers)
        {
            var lobbyFilter = new LobbyFilter().WithMaxPlayers(FilterOperator.Equals, maxPlayers);

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.maxPlayers.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(maxPlayers));
        }

        [Test]
        [TestCase(-1, -16)]
        [TestCase(10, -2)]
        [TestCase(-10, 2)]
        [TestCase(0, 0)]
        [TestCase(1, 4)]
        [TestCase(8, 16)]
        [TestCase(16, 32)]
        [TestCase(32, 48)]
        [Description("Filter contains max players range matching")]
        public void Filter_Contains_MaxPlayers_And_Group(int lower, int upper)
        {
            var lobbyFilter = new LobbyFilter().WithAnd()
                .WithMaxPlayers(FilterOperator.GreaterOrEqualThan, lower)
                .WithMaxPlayers(FilterOperator.LessThan, upper);

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(FilterKey.maxPlayers.ToString()));
            Assert.That(lhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterOperatorToString(FilterOperator.GreaterOrEqualThan)));
            Assert.That(lhs.Values.Count, Is.EqualTo(1));
            Assert.That(lhs.Values[0], Is.EqualTo(lower));

            Assert.That(rhs.Key, Is.EqualTo(FilterKey.maxPlayers.ToString()));
            Assert.That(rhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterOperatorToString(FilterOperator.LessThan)));
            Assert.That(rhs.Values.Count, Is.EqualTo(1));
            Assert.That(rhs.Values[0], Is.EqualTo(upper));
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(5, 5)]
        [Description("Filter contains given max player range matches with group")]
        public void MaxPlayers_Filter_Contains_Given_MaxPlayers_WithGroup(int lower, int upper)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithTag(FilterOperator.Any, new List<string>
                {
                    "eu",
                    "us"
                })
                .WithOr()
                .WithMaxPlayers(FilterOperator.GreaterOrEqualThan, lower)
                .WithMaxPlayers(FilterOperator.LessThan, upper)
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator,
                Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(FilterKey.tag.ToString()));
            Assert.That(lhs.Values.Count, Is.EqualTo(2));

            Assert.That(rhs.Key, Is.Null);
            Assert.That(rhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.Or)));
            Assert.That(rhs.Values.Count, Is.EqualTo(2));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(FilterKey.maxPlayers.ToString()));
            Assert.That(orRhs.Key, Is.EqualTo(FilterKey.maxPlayers.ToString()));

            Assert.That(orLhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterOperatorToString(FilterOperator.GreaterOrEqualThan)));
            Assert.That(orRhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterOperatorToString(FilterOperator.LessThan)));

            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo(lower));

            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo(upper));
        }

        [Test]
        [TestCase(-2)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [Description("Number of players filter set correctly")]
        public void NumPlayers_Filter_Contains_NumPlayers_Value(int numPlayers)
        {
            var lobbyFilter = new LobbyFilter().WithNumPlayers(FilterOperator.Equals, numPlayers);

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.numPlayers.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(numPlayers));
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(5, 5)]
        [Description("Filter contains given number of players")]
        public void NumPlayers_Filter_Contains_Given_NumPlayers_WithGroup(int first, int second)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithTag(FilterOperator.Any, new List<string>
                {
                    "eu",
                    "us"
                })
                .WithOr()
                .WithNumPlayers(FilterOperator.GreaterOrEqualThan, first)
                .WithNumPlayers(FilterOperator.LessThan, second)
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(FilterKey.tag.ToString()));
            Assert.That(lhs.LogicOperator, Is.EqualTo("any"));
            Assert.That(lhs.Values.Count, Is.EqualTo(2));
            Assert.That(lhs.Values[0], Is.EqualTo("eu"));
            Assert.That(lhs.Values[1], Is.EqualTo("us"));

            Assert.That(rhs.Key, Is.Null);
            Assert.That(rhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.Or)));
            Assert.That(rhs.Values.Count, Is.EqualTo(2));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(FilterKey.numPlayers.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo(first));

            Assert.That(orRhs.Key, Is.EqualTo(FilterKey.numPlayers.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo(second));
        }

        [Test]
        [TestCase("eu")]
        [TestCase("us")]
        [TestCase("ca")]
        [Description("Filter with a single region")]
        public void Region_Filter_Contains_Given_Region(string region)
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Equals, new[]
            {
                region
            });

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.region.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(region));
        }

        [Test]
        [TestCase("region0")]
        [TestCase("region1", "region2")]
        [TestCase("region3", "region4", "region5")]
        [Description("Filter contains given region(s)")]
        public void Region_Filter_Contains_Given_Region(params string[] region)
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Equals, region);

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.region.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(region.Length));
            for (var i = 0; i < region.Length; i++)
            {
                Assert.That(lobbyFilter.Values[i], Is.EqualTo(region[i]));
            }
        }

        [Test]
        [TestCase(-4)]
        [TestCase(-2)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [Description("Available slots filter generates correct string")]
        public void AvailableSlots_String_Generated(int availableSlots)
        {
            var lobbyFilter = new LobbyFilter().WithAvailableSlots(FilterOperator.Equals, availableSlots);

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.availableSlots.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(availableSlots));
        }

        [Test]
        [TestCase("slug-a")]
        [TestCase("slug-b")]
        [TestCase("slug-c")]
        [Description("Simulator slug filter generates correct string")]
        public void SimulatorSlug_String_Generated(string simSlug)
        {
            var lobbyFilter = new LobbyFilter().WithSimulatorSlug(FilterOperator.Equals, simSlug);

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.simSlug.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(simSlug));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        [Description("Private lobby filter generates correct string")]
        public void PrivateLobby_String_Generated(bool @private)
        {
            var lobbyFilter = new LobbyFilter().WithIsPrivateLobby(FilterOperator.Equals, @private);

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.Private.ToString().ToLowerInvariant()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(@private));
        }

        [Test]
        [TestCase(FilterOperator.LessThan)]
        [TestCase(FilterOperator.LessOrEqualThan)]
        [TestCase(FilterOperator.GreaterThan)]
        [TestCase(FilterOperator.GreaterOrEqualThan)]
        [TestCase(FilterOperator.Any)]
        [TestCase(FilterOperator.Between)]
        [Description("Incorrect filter operator for WithIsPrivateLobby used throws exception")]
        public void PrivateLobby_Throws_On_InvalidOperator(FilterOperator filterOperator)
        {
            Assert.Throws<ArgumentException>(() => _ = new LobbyFilter().WithIsPrivateLobby(filterOperator, true));
        }

        [Test]
        [TestCase(FilterOperator.Equals)]
        [TestCase(FilterOperator.NotEquals)]
        [Description("Correct filter operator for WithIsPrivateLobby does not throw")]
        public void PrivateLobby_Noes_Not_Throw_On_ValidOperator(FilterOperator filterOperator)
        {
            Assert.DoesNotThrow(() => _ = new LobbyFilter().WithIsPrivateLobby(filterOperator, true));
        }

        [Test]
        [TestCase("region1", "region2")]
        [TestCase("region3", "region4")]
        [Description("Filter contains given region matches")]
        public void Region_Filter_Contains_Given_Region_WithGroup(string firstRegion, string secondRegion)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithRegion(FilterOperator.Any, new List<string>
                {
                    firstRegion,
                    secondRegion
                })
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map1")
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map2")
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo("region"));
            Assert.That(lhs.Values.Count, Is.EqualTo(2));
            Assert.That(lhs.Values[0], Is.EqualTo(firstRegion));
            Assert.That(lhs.Values[1], Is.EqualTo(secondRegion));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo("map1"));

            Assert.That(orRhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo("map2"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [Description("Filter contains given region matches")]
        public void Available_Filter_Contains_Given_AvailableSlot_WithGroup(int availableSlots)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithAvailableSlots(FilterOperator.Equals, availableSlots)
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map1")
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map2")
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo("availableSlots"));
            Assert.That(lhs.Values.Count, Is.EqualTo(1));
            Assert.That(lhs.Values[0], Is.EqualTo(availableSlots));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo("map1"));

            Assert.That(orRhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo("map2"));
        }

        [Test]
        [TestCase("slug-a")]
        [TestCase("slug-b")]
        [Description("Filter contains given region matches")]
        public void SimulatorSlug_Filter_Contains_Given_SimSlug_WithGroup(string simSlug)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithSimulatorSlug(FilterOperator.Equals, simSlug)
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map1")
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map2")
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(FilterKey.simSlug.ToString()));
            Assert.That(lhs.Values.Count, Is.EqualTo(1));
            Assert.That(lhs.Values[0], Is.EqualTo(simSlug));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo("map1"));

            Assert.That(orRhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo("map2"));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        [Description("Filter contains given private lobby matches")]
        public void PrivateLobby_Filter_Contains_Given_PrivateLobby_WithGroup(bool @private)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithIsPrivateLobby(FilterOperator.Equals, @private)
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map1")
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map2")
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(FilterKey.Private.ToString().ToLowerInvariant()));
            Assert.That(lhs.Values.Count, Is.EqualTo(1));
            Assert.That(lhs.Values[0], Is.EqualTo(@private));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo("map1"));

            Assert.That(orRhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo("map2"));
        }

        [Test]
        [TestCase(IntAttributeIndex.n1, 1)]
        [TestCase(IntAttributeIndex.n2, 2)]
        [TestCase(IntAttributeIndex.n3, 3)]
        [TestCase(IntAttributeIndex.n4, 4)]
        [TestCase(IntAttributeIndex.n5, 5)]
        [Description("Filter contains given integer attribute matches")]
        public void IntAttribute_Filter_Contains_Given_IntAttribute(IntAttributeIndex index, int value)
        {
            var lobbyFilter = new LobbyFilter().WithIntAttribute(FilterOperator.Equals, index, value);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo("="));

            Assert.That(lobbyFilter.Key, Is.EqualTo(index.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(value));
        }

        [Test]
        [TestCase(IntAttributeIndex.n1, 1)]
        [TestCase(IntAttributeIndex.n2, 2)]
        [TestCase(IntAttributeIndex.n3, 3)]
        [TestCase(IntAttributeIndex.n4, 4)]
        [TestCase(IntAttributeIndex.n5, 5)]
        [Description("Filter contains given integer attribute matches")]
        public void IntAttribute_Filter_Contains_Given_IntAttributes_WithGroup(IntAttributeIndex index, int value)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithIntAttribute(FilterOperator.Equals, index, value)
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map1")
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map2")
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(2));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(index.ToString()));
            Assert.That(lhs.Values.Count, Is.EqualTo(1));
            Assert.That(lhs.Values[0], Is.EqualTo(value));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo("map1"));

            Assert.That(orRhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo("map2"));
        }

        [Test]
        [TestCase("tag1", "tag2")]
        [TestCase("tag3", "tag4")]
        [Description("Filter contains given tag matches")]
        public void Tag_Filter_Contains_Given_Tag_WithGroup(string firstTag, string secondTag)
        {
            var lobbyFilter = new LobbyFilter()
                .WithAnd()
                .WithTag(FilterOperator.Any, new List<string>
                {
                    firstTag,
                    secondTag
                })
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map1")
                .WithStringAttribute(FilterOperator.Equals, StringAttributeIndex.s1, "map2")
                .End();

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.And)));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(FilterKey.tag.ToString()));
            Assert.That(lhs.Values.Count, Is.EqualTo(2));
            Assert.That(lhs.Values[0], Is.EqualTo(firstTag));
            Assert.That(lhs.Values[1], Is.EqualTo(secondTag));

            Assert.That(rhs.Key, Is.Null);
            Assert.That(rhs.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.Or)));
            Assert.That(rhs.Values.Count, Is.EqualTo(2));

            var orLhs = (LobbyFilter)rhs.Values[0];
            var orRhs = (LobbyFilter)rhs.Values[1];

            Assert.That(orLhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orLhs.Values.Count, Is.EqualTo(1));
            Assert.That(orLhs.Values[0], Is.EqualTo("map1"));

            Assert.That(orRhs.Key, Is.EqualTo(StringAttributeIndex.s1.ToString()));
            Assert.That(orRhs.Values.Count, Is.EqualTo(1));
            Assert.That(orRhs.Values[0], Is.EqualTo("map2"));
        }

        [Test]
        [TestCase("tag1")]
        [TestCase("tag2")]
        [TestCase("tag3")]
        [Description("Filter with a single tag")]
        public void Tag_Filter_Contains_Given_Tag(string tag)
        {
            var lobbyFilter = new LobbyFilter().WithTag(FilterOperator.Equals, new List<string>
            {
                tag
            });

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.tag.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(tag));
        }

        [Test]
        [TestCase("tag0")]
        [TestCase("tag1", "tag2")]
        [TestCase("tag3", "tag4", "tag5")]
        [Description("Filter contains given tag(s)")]
        public void Tag_Filter_Contains_Given_Tags(params string[] tag)
        {
            var lobbyFilter = new LobbyFilter().WithTag(FilterOperator.Equals, new List<string>(tag));

            Assert.That(lobbyFilter.Key, Is.EqualTo(FilterKey.tag.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(tag.Length));

            for (var i = 0; i < tag.Length; i++)
            {
                Assert.That(lobbyFilter.Values[i], Is.EqualTo(tag[i]));
            }
        }

        [Test]
        [TestCase(StringAttributeIndex.s1, "s1")]
        [TestCase(StringAttributeIndex.s2, "s2")]
        [TestCase(StringAttributeIndex.s3, "s3")]
        [TestCase(StringAttributeIndex.s4, "s4")]
        [TestCase(StringAttributeIndex.s5, "s5")]
        [Description("Filter with a single string attribute")]
        public void StringAttribute_Filter_Contains_Given_Attribute(StringAttributeIndex index, string value)
        {
            var lobbyFilter = new LobbyFilter().WithStringAttribute(FilterOperator.Equals, index, value);

            Assert.That(lobbyFilter.Key, Is.EqualTo(index.ToString()));
            Assert.That(lobbyFilter.Values.Count, Is.EqualTo(1));
            Assert.That(lobbyFilter.Values[0], Is.EqualTo(value));
        }

        [Test]
        [TestCase(StringAttributeIndex.s1, "attr1", StringAttributeIndex.s1, "attr2")]
        [TestCase(StringAttributeIndex.s2, "attr3", StringAttributeIndex.s3, "attr4")]
        [TestCase(StringAttributeIndex.s4, "attr5", StringAttributeIndex.s5, "attr6")]
        [Description("Or operator correctly generates filter")]
        public void Or_Operator_Correctly_Generates_Filter(StringAttributeIndex firstAttrIndex, string firstString, StringAttributeIndex secondAttrIndex, string secondString)
        {
            var lobbyFilter = new LobbyFilter()
                .WithOr()
                .WithStringAttribute(FilterOperator.Equals, firstAttrIndex, firstString)
                .WithStringAttribute(FilterOperator.Equals, secondAttrIndex, secondString);

            Assert.That(lobbyFilter.Key, Is.Null);
            Assert.That(lobbyFilter.LogicOperator, Is.EqualTo(lobbyFilter.FilterGroupOperatorToString(FilterGroupOperator.Or)));

            var lhs = (LobbyFilter)lobbyFilter.Values[0];
            var rhs = (LobbyFilter)lobbyFilter.Values[1];

            Assert.That(lhs.Key, Is.EqualTo(firstAttrIndex.ToString()));
            Assert.That(lhs.Values.Count, Is.EqualTo(1));
            Assert.That(lhs.Values[0], Is.EqualTo(firstString));

            Assert.That(rhs.Key, Is.EqualTo(secondAttrIndex.ToString()));
            Assert.That(rhs.Values.Count, Is.EqualTo(1));
            Assert.That(rhs.Values[0], Is.EqualTo(secondString));
        }

        [Test]
        [Description("End is called outside of a nested filter group")]
        public void End_Called_Outside_Nested_Filter_Group()
        {
            var lobbyFilter = new LobbyFilter()
                .WithRegion(FilterOperator.Any, new []
                {
                    "eu",
                    "us"
                });

            Assert.Throws<InvalidOperationException>(() => _ = lobbyFilter.End());
        }

        [Test]
        [Description("Only filter groups can be used with 'WithOr'")]
        public void NonFilterGroup_InvalidOperation_WhenAddingWithOr()
        {
            var lobbyFilter = new LobbyFilter();
            lobbyFilter.WithTag(FilterOperator.Any, new List<string>
            {
                "tag1",
                "tag2"
            });

            Assert.Throws<InvalidOperationException>(() => _ = lobbyFilter.WithOr());
        }

        [Test]
        [Description("Only filter groups can be used with 'WithAnd'")]
        public void NonFilterGroup_InvalidOperation_WhenAddingWithAnd()
        {
            var lobbyFilter = new LobbyFilter();
            lobbyFilter.WithTag(FilterOperator.Any, new List<string>
            {
                "tag1",
                "tag2"
            });

            Assert.Throws<InvalidOperationException>(() => _ = lobbyFilter.WithAnd());
        }

        [Test]
        [Description("String attribute condition cannot be added to a non-filter group")]
        public void StringAttribute_Added_To_NonFilterGroup()
        {
            var lobbyFilter = new LobbyFilter().WithTag(FilterOperator.Equals, new List<string>
            {
                "tag1"
            });
            Assert.Throws<InvalidOperationException>(() =>
                lobbyFilter.WithStringAttribute(FilterOperator.Any, StringAttributeIndex.s1, "attrValue"));
        }

        [Test]
        [Description("Region condition cannot be added to a non-filter group")]
        public void Region_Added_To_NonFilterGroup()
        {
            var lobbyFilter = new LobbyFilter().WithTag(FilterOperator.Equals, new List<string>
            {
                "tag1"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            }));
        }

        [Test]
        [Description("Tag condition cannot be added to a non-filter group")]
        public void Tag_Added_To_NonFilterGroup()
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithTag(FilterOperator.Equals, new List<string>
            {
                "tag1"
            }));
        }

        [Test]
        [Description("Max players condition cannot be added to a non-filter group")]
        public void MaxPlayers_Added_To_NonFilterGroup()
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithMaxPlayers(FilterOperator.Equals, 10));
        }

        [Test]
        [Description("Num players condition cannot be added to a non-filter group")]
        public void NumPlayers_Added_To_NonFilterGroup()
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithNumPlayers(FilterOperator.Equals, 10));
        }

        [Test]
        [TestCase(-8)]
        [TestCase(-4)]
        [TestCase(-2)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [Description("Available slots condition cannot be added to a non-filter group")]
        public void WithAvailableSlots_Added_To_NonFilterGroup(int slots)
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithAvailableSlots(FilterOperator.Equals, slots));
        }

        [Test]
        [TestCase(IntAttributeIndex.n1, 1)]
        [TestCase(IntAttributeIndex.n2, 2)]
        [TestCase(IntAttributeIndex.n3, 3)]
        [TestCase(IntAttributeIndex.n4, 4)]
        [TestCase(IntAttributeIndex.n5, 5)]
        [Description("Integer attribute condition cannot be added to a non-filter group")]
        public void IntAttribute_Added_To_NonFilterGroup(IntAttributeIndex index, int value)
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithIntAttribute(FilterOperator.Equals, index, value));
        }

        [Test]
        [TestCase("slug-1")]
        [TestCase("slug-2")]
        [Description("Sim slug condition cannot be added to a non-filter group")]
        public void SimSlug_Added_To_NonFilterGroup(string simSlug)
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithSimulatorSlug(FilterOperator.Equals, simSlug));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        [Description("Private lobby condition cannot be added to a non-filter group")]
        public void PrivateLobby_Added_To_NonFilterGroup(bool isPrivateLobby)
        {
            var lobbyFilter = new LobbyFilter().WithRegion(FilterOperator.Any, new[]
            {
                "eu"
            });
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.WithIsPrivateLobby(FilterOperator.Equals, isPrivateLobby));
        }

        [Test]
        [Description("Calling End with throw if it is not used within a filter group")]
        public void Calling_End_Throws_Outside_Of_FilterGroup()
        {
            var lobbyFilter = new LobbyFilter().WithAnd().WithSimulatorSlug(FilterOperator.Any, "slug1");
            Assert.Throws<InvalidOperationException>(() => lobbyFilter.End());
        }
    }
}
