using System;
using System.Collections.Generic;
using System.Linq;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.Meters;
using FieldDataPluginFramework.DataModel.Verticals;
using QRevMS.Parsing;
using QRevMS.Units;

namespace QRevMS.Mapping
{
    public static class VerticalMapper
    {
        public static IEnumerable<Vertical> BuildVerticals(QRevMSDocument source, DischargeMethodType dischargeMethod, UnitConverter unitConverter, TimeSpan utcOffset)
        {
            return dischargeMethod == DischargeMethodType.MidSection
                ? BuildMidSectionVerticals(source, unitConverter, utcOffset)
                : BuildMeanSectionVerticals(source, unitConverter, utcOffset);
        }

        private static IEnumerable<Vertical> BuildMidSectionVerticals(QRevMSDocument source, UnitConverter unitConverter, TimeSpan utcOffset)
        {
            if (source.Stations.Count != source.Verticals.Count)
                throw new FormatException(
                    $"Mid-Section QRevMS file has {source.Stations.Count} stations but {source.Verticals.Count} verticals - expected these to match exactly, with each station centered on its corresponding vertical.");

            for (var i = 0; i < source.Stations.Count; i++)
                yield return CreateVertical(source, source.Stations[i], source.Verticals[i], i, source.Stations.Count, unitConverter, utcOffset);
        }

        private static IEnumerable<Vertical> BuildMeanSectionVerticals(QRevMSDocument source, UnitConverter unitConverter, TimeSpan utcOffset)
        {
            var measuredPoints = source.Verticals.Where(v => !v.IsEdge).ToList();

            if (source.Stations.Count != source.Verticals.Count - 1)
                throw new FormatException(
                    $"Mean-Section QRevMS file has {source.Stations.Count} stations but {source.Verticals.Count} verticals - expected exactly one fewer station than vertical (each station is the gap between two consecutive points).");

            for (var i = 0; i < source.Stations.Count; i++)
            {
                var station = source.Stations[i];
                var referenceVertical = measuredPoints[Math.Min(i, measuredPoints.Count - 1)];

                yield return CreateVertical(source, station, referenceVertical, i, source.Stations.Count, unitConverter, utcOffset);
            }
        }

        private static Vertical CreateVertical(QRevMSDocument source, StationDischarge station, VerticalDetail referenceVertical, int index, int stationCount, UnitConverter unitConverter, TimeSpan utcOffset)
        {
            var depth = unitConverter.ConvertDistance(station.StationDepth ?? referenceVertical.Depth ?? 0.0);
            var velocityMagnitude = unitConverter.ConvertVelocity(referenceVertical.VelocityMagnitude ?? 0.0);

            return new Vertical
            {
                SequenceNumber = index + 1,
                TaglinePosition = unitConverter.ConvertDistance(station.StationLocation ?? referenceVertical.Location ?? 0.0),
                VerticalType = DetermineVerticalType(index, stationCount),
                MeasurementConditionData = DetermineMeasurementConditionData(source),
                FlowDirection = FlowDirectionType.Normal,
                MeasurementTime = referenceVertical.StartDateTime.HasValue
                    ? new DateTimeOffset(referenceVertical.StartDateTime.Value.AddSeconds((referenceVertical.DurationSeconds ?? 0.0) / 2.0), utcOffset)
                    : null,
                EffectiveDepth = depth,
                SoundedDepth = depth,
                Segment = new Segment
                {
                    Width = unitConverter.ConvertDistance(station.StationWidth ?? 0.0),
                    Area = unitConverter.ConvertArea(station.StationArea ?? 0.0),
                    Discharge = unitConverter.ConvertDischarge(station.StationTotalQ ?? 0.0),
                    Velocity = unitConverter.ConvertVelocity(station.StationNormalVelocity ?? referenceVertical.NormalVelocity ?? 0.0),
                    IsDischargeEstimated = false,
                    TotalDischargePortion = station.StationPercentQ ?? 0.0,
                },
                VelocityObservation = CreateVelocityObservation(source, referenceVertical, depth, velocityMagnitude, unitConverter),
            };
        }

        private static VerticalType DetermineVerticalType(int index, int stationCount)
        {
            if (index == 0)
                return VerticalType.StartEdgeNoWaterBefore;

            if (index == stationCount - 1)
                return VerticalType.EndEdgeNoWaterAfter;

            return VerticalType.MidRiver;
        }

        private static MeasurementConditionData DetermineMeasurementConditionData(QRevMSDocument source)
        {
            var condition = source.Processing.WaterSurfaceCondition;

            // IndexOf: Contains(string, StringComparison) isn't available on net472.
            if (condition != null && condition.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new IceCoveredData
                {
                    IceThickness = 0.0,
                    WaterSurfaceToBottomOfIce = 0.0,
                    WaterSurfaceToBottomOfSlush = 0.0,
                };
            }

            return new OpenWaterData();
        }

        private static VelocityObservation CreateVelocityObservation(QRevMSDocument source, VerticalDetail referenceVertical, double depth, double velocityMagnitude, UnitConverter unitConverter)
        {
            return new VelocityObservation
            {
                MeanVelocity = velocityMagnitude,
                DeploymentMethod = DeploymentMethodType.Unspecified,
                MeterCalibration = new MeterCalibration
                {
                    SerialNumber = source.Instrument.SerialNumber ?? "Unknown",
                    Manufacturer = source.Instrument.Manufacturer ?? "SonTek",
                    Model = source.Instrument.Model ?? "RS5",
                    FirmwareVersion = source.Instrument.FirmwareVersion,
                    SoftwareVersion = source.Processing.SoftwareVersion,
                    MeterType = MeterType.Adcp,
                    Configuration = BuildMeterConfiguration(source),
                    Equations = { new MeterCalibrationEquation { InterceptUnitId = unitConverter.VelocityUnitId } },
                },
                Observations =
                {
                    new VelocityDepthObservation
                    {
                        Depth = depth,
                        Velocity = velocityMagnitude,
                        ObservationInterval = referenceVertical.DurationSeconds,
                        IsVelocityEstimated = false,
                        RevolutionCount = 0,
                    }
                },
            };
        }

        private static string BuildMeterConfiguration(QRevMSDocument source)
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(source.Processing.VelocityMethod))
                lines.Add($"Velocity Method: {source.Processing.VelocityMethod}");

            if (!string.IsNullOrWhiteSpace(source.Processing.VelocityReference))
                lines.Add($"Velocity Reference: {source.Processing.VelocityReference}");

            if (source.Processing.TaglineAzimuthDegrees.HasValue)
                lines.Add($"Tagline Azimuth: {source.Processing.TaglineAzimuthDegrees} deg");

            if (!string.IsNullOrWhiteSpace(source.Processing.WaterSurfaceCondition))
                lines.Add($"Water Surface Condition: {source.Processing.WaterSurfaceCondition}");

            return lines.Any() ? string.Join(Environment.NewLine, lines) : null;
        }
    }
}