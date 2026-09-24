using System;
using System.IO;
using System.Linq;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using QRevMS.Mapping;
using QRevMS.Units;
using QRevMS.Parsing;
using Xunit;

namespace QRevMS.Tests
{
    public class DischargeActivityBuilderTests
    {
        private static readonly TimeSpan NzUtcOffset = TimeSpan.FromHours(12);
        private static readonly UnitConverter MetricUnitConverter = new UnitConverter(false);

        [Fact]
        public void Build_MeanSectionFile_TotalDischargeMatchesChannelSummary()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);

            Assert.Equal(document.ChannelSummary.ChannelTotalQ.Value, activity.Discharge.Value, 5);
        }

        [Fact]
        public void Build_MeanSectionFile_ProducesOneVerticalPerStation()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            Assert.Equal(document.Stations.Count, section.Verticals.Count);
        }

        [Fact]
        public void Build_MidSectionFile_ProducesOneVerticalPerStation()
        {
            using var stream = File.OpenRead("SampleQRevMS-MidSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            Assert.Equal(document.Stations.Count, section.Verticals.Count);
        }

        [Fact]
        public void Build_MidSectionFile_EdgeVerticalsHaveNoMeasurementTime()
        {
            using var stream = File.OpenRead("SampleQRevMS-MidSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            Assert.Null(section.Verticals.First().MeasurementTime);
            Assert.Null(section.Verticals.Last().MeasurementTime);
        }

        [Fact]
        public void Build_UnrecognizedDischargeMethod_Throws()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);
            document.Processing.DischargeMethod = "Something-Else";

            Assert.Throws<FormatException>(() => DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter));
        }

        [Fact]
        public void Build_MeanSectionStationVerticalCountMismatch_Throws()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);
            document.Stations.RemoveAt(0);

            Assert.Throws<FormatException>(() => DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter));
        }

        [Fact]
        public void Build_MidSectionStationVerticalCountMismatch_Throws()
        {
            using var stream = File.OpenRead("SampleQRevMS-MidSection.xml");
            var document = QRevMSParser.Parse(stream);
            document.Stations.RemoveAt(0);

            Assert.Throws<FormatException>(() => DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter));
        }

        [Fact]
        public void Build_MeanSectionFile_TotalDischargePortionIsPercentageNotFraction()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            var expectedSum = document.Stations.Sum(s => s.StationPercentQ ?? 0.0);
            var actualSum = section.Verticals.Sum(v => v.Segment.TotalDischargePortion);

            Assert.Equal(expectedSum, actualSum, 5);
            Assert.InRange(actualSum, 90.0, 100.0);
        }

        [Fact]
        public void Build_MeanSectionFile_SegmentVelocityUsesNormalVelocityNotMagnitude()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            var firstStation = document.Stations[0];
            var firstVertical = section.Verticals.First();

            Assert.Equal(firstStation.StationNormalVelocity.Value, firstVertical.Segment.Velocity, 5);
        }

        [Fact]
        public void Build_MeanSectionFile_StartPointMatchesChronologicalStart()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            Assert.Equal(StartPointType.RightEdgeOfWater, section.StartPoint);
        }

        [Fact]
        public void Build_LeftBankStartFile_StartPointReflectsActualChronologyNotFileOrder()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection-LeftBankStart.xml");
            var document = QRevMSParser.Parse(stream);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, MetricUnitConverter);
            var section = (ManualGaugingDischargeSection)Assert.Single(activity.ChannelMeasurements);

            Assert.Equal(StartPointType.LeftEdgeOfWater, section.StartPoint);
        }

        [Fact]
        public void Build_ImperialUnitConverter_ConvertsDischargeValueAndUnitId()
        {
            using var stream = File.OpenRead("SampleQRevMS-MeanSection.xml");
            var document = QRevMSParser.Parse(stream);
            var imperialUnitConverter = new UnitConverter(true);

            var activity = DischargeActivityBuilder.Build(document, NzUtcOffset, imperialUnitConverter);

            Assert.Equal("ft^3/s", activity.Discharge.UnitId);
            Assert.Equal(document.ChannelSummary.ChannelTotalQ.Value / 0.028316846592, activity.Discharge.Value, 3);
        }
    }
}