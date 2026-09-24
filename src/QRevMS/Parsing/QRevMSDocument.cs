using System;
using System.Collections.Generic;

namespace QRevMS.Parsing
{
    public class QRevMSDocument
    {
        public string QRevFilename { get; set; }
        public string QRevVersion { get; set; }

        public SiteInformation SiteInformation { get; set; } = new();
        public Instrument Instrument { get; set; } = new();
        public Processing Processing { get; set; } = new();
        public ChannelSummary ChannelSummary { get; set; } = new();

        public List<StationDischarge> Stations { get; set; } = [];
        public List<VerticalDetail> Verticals { get; set; } = [];

        public string UserComment { get; set; }
        public string QaMessage { get; set; }
    }

    public class SiteInformation
    {
        public string StationName { get; set; }
        public string SiteId { get; set; }
        public string Persons { get; set; }
        public string MeasurementNumber { get; set; }
    }

    public class Instrument
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
    }

    public class Processing
    {
        public string SoftwareVersion { get; set; }
        public string Type { get; set; }
        public string DischargeMethod { get; set; }
        public string VelocityReference { get; set; }
        public string VelocityMethod { get; set; }
        public double? TaglineAzimuthDegrees { get; set; }
        public string WaterSurfaceCondition { get; set; }
        public string Filename { get; set; }
    }

    public class ChannelSummary
    {
        public double? ChannelTopQ { get; set; }
        public double? ChannelMiddleQ { get; set; }
        public double? ChannelBottomQ { get; set; }
        public double? ChannelTotalQ { get; set; }
        public double? ChannelWidth { get; set; }
        public double? ChannelArea { get; set; }
        public double? MeanNormalVelocity { get; set; }
        public double? ChannelMeanDepth { get; set; }
        public double? ChannelMaximumDepth { get; set; }
        public double? MaximumNormalVelocity { get; set; }
        public string UserRating { get; set; }
        public double? UncertaintyPercent { get; set; }
        public string UncertaintyModel { get; set; }
    }

    public class StationDischarge
    {
        public double? StationLocation { get; set; }
        public double? StationDepth { get; set; }
        public double? StationWidth { get; set; }
        public double? StationArea { get; set; }
        public double? StationNormalVelocity { get; set; }
        public double? StationTopQ { get; set; }
        public double? StationMiddleQ { get; set; }
        public double? StationBottomQ { get; set; }
        public double? StationTotalQ { get; set; }
        public double? StationPercentQ { get; set; }
    }

    public class VerticalDetail
    {
        public bool IsEdge => Bank != null && StartDateTime == null;

        public string Bank { get; set; }
        public double? EdgeDepth { get; set; }
        public double? EdgeCoefficient { get; set; }

        public double? Location { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public double? Depth { get; set; }
        public double? GaugeHeight { get; set; }
        public TimeSpan? GaugeHeightTime { get; set; }
        public double? VelocityMagnitude { get; set; }
        public double? NormalVelocity { get; set; }
        public int? NumberValidEnsembles { get; set; }
        public int? NumberValidCells { get; set; }
        public double? DurationSeconds { get; set; }
        public double? MeanTemperature { get; set; }
    }
}