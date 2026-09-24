using System;
using System.IO;
using System.Linq;
using System.Text;
using QRevMS.Parsing;
using Xunit;

namespace QRevMS.Tests
{
    public class QRevMSParserTests
    {
        private const string MeanSectionFile = "SampleQRevMS-MeanSection.xml";
        private const string MidSectionFile = "SampleQRevMS-MidSection.xml";

        [Fact]
        public void CanParse_ReturnsTrue_ForRealQRevMSFile()
        {
            using var stream = File.OpenRead(MeanSectionFile);

            Assert.True(QRevMSParser.CanParse(stream));
        }

        [Fact]
        public void CanParse_ReturnsFalse_ForNonQRevMSXml()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<NotQRevMS/>"));

            Assert.False(QRevMSParser.CanParse(stream));
        }

        [Fact]
        public void CanParse_ResetsStreamPositionForSubsequentParse()
        {
            using var stream = File.OpenRead(MeanSectionFile);

            QRevMSParser.CanParse(stream);
            var document = QRevMSParser.Parse(stream);

            Assert.Equal("QRevMS 1.40", document.QRevVersion);
        }

        [Fact]
        public void Parse_MeanSectionFile_ProducesExpectedTotals()
        {
            using var stream = File.OpenRead(MeanSectionFile);
            var document = QRevMSParser.Parse(stream);

            Assert.Equal("QRevMS 1.40", document.QRevVersion);
            Assert.Equal("Mean-Section", document.Processing.DischargeMethod);
            Assert.Equal(1.72318, document.ChannelSummary.ChannelTotalQ.Value, 5);
            Assert.Equal(23, document.Stations.Count);
            Assert.Equal(24, document.Verticals.Count);
            Assert.Equal("RM, MAD", document.SiteInformation.Persons);
        }

        [Fact]
        public void Parse_MidSectionFile_ProducesExpectedTotals()
        {
            using var stream = File.OpenRead(MidSectionFile);
            var document = QRevMSParser.Parse(stream);

            Assert.Equal("Mid-Section", document.Processing.DischargeMethod);
            Assert.Equal(1.73443, document.ChannelSummary.ChannelTotalQ.Value, 5);
            Assert.Equal(24, document.Stations.Count);
            Assert.Equal(24, document.Verticals.Count);
            Assert.Equal("EN063", document.SiteInformation.SiteId);
        }

        [Fact]
        public void Parse_MeanSectionFile_HasExactlyTwoEdgeVerticals()
        {
            using var stream = File.OpenRead(MeanSectionFile);
            var document = QRevMSParser.Parse(stream);

            var edgeCount = document.Verticals.Count(v => v.IsEdge);

            Assert.Equal(2, edgeCount);
        }

        [Fact]
        public void Parse_MidSectionFile_HasExactlyTwoEdgeVerticals()
        {
            using var stream = File.OpenRead(MidSectionFile);
            var document = QRevMSParser.Parse(stream);

            var edgeCount = document.Verticals.Count(v => v.IsEdge);

            Assert.Equal(2, edgeCount);
        }

        [Fact]
        public void Parse_StationTotalQValues_SumToChannelTotalQ()
        {
            using var stream = File.OpenRead(MeanSectionFile);
            var document = QRevMSParser.Parse(stream);

            var sum = document.Stations.Sum(s => s.StationTotalQ ?? 0.0);

            Assert.Equal(document.ChannelSummary.ChannelTotalQ.Value, sum, 3);
        }

        [Fact]
        public void Parse_UnexpectedUnitsCode_Throws()
        {
            var xml = File.ReadAllText(MeanSectionFile).Replace("unitsCode=\"cms\"", "unitsCode=\"cfs\"");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Assert.Throws<FormatException>(() => QRevMSParser.Parse(stream));
        }
    }
}