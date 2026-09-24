using System;
using System.Linq;
using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.DataModel.DischargeActivities;
using QRevMS.Parsing;
using QRevMS.Units;

namespace QRevMS.Mapping
{
    public static class DischargeActivityBuilder
    {
        public static DischargeActivity Build(QRevMSDocument source, TimeSpan utcOffset, UnitConverter unitConverter)
        {
            var dischargeMethod = DischargeSectionMapper.DetermineDischargeMethod(source);
            var (visitStart, visitEnd) = GetVisitTimeRange(source, utcOffset);
            var measurementPeriod = new DateTimeInterval(visitStart, visitEnd);

            var dischargeActivity = CreateDischargeActivity(source, unitConverter, measurementPeriod);
            var dischargeSection = DischargeSectionMapper.CreateSection(source, unitConverter, dischargeActivity, dischargeMethod);

            foreach (var vertical in VerticalMapper.BuildVerticals(source, dischargeMethod, unitConverter, utcOffset))
                dischargeSection.Verticals.Add(vertical);

            dischargeSection.MeterCalibration = dischargeSection.Verticals
                .Select(v => v.VelocityObservation.MeterCalibration)
                .FirstOrDefault(mc => mc != null);
            dischargeSection.NumberOfVerticals = dischargeSection.Verticals.Count;

            dischargeActivity.ChannelMeasurements.Add(dischargeSection);
            AddGageHeightMeasurements(source, dischargeActivity, unitConverter, utcOffset);

            return dischargeActivity;
        }

        private static void AddGageHeightMeasurements(QRevMSDocument source, DischargeActivity dischargeActivity, UnitConverter unitConverter, TimeSpan utcOffset)
        {
            foreach (var vertical in source.Verticals)
            {
                if (!vertical.GaugeHeight.HasValue || !vertical.StartDateTime.HasValue || !vertical.GaugeHeightTime.HasValue)
                    continue;

                var gaugeHeightDateTime = vertical.StartDateTime.Value.Date + vertical.GaugeHeightTime.Value;

                dischargeActivity.GageHeightMeasurements.Add(
                    new GageHeightMeasurement(
                        new Measurement(unitConverter.ConvertDistance(vertical.GaugeHeight.Value), unitConverter.DistanceUnitId),
                        new DateTimeOffset(gaugeHeightDateTime, utcOffset)));
            }
        }

        private static DischargeActivity CreateDischargeActivity(QRevMSDocument source, UnitConverter unitConverter, DateTimeInterval measurementPeriod)
        {
            var factory = new DischargeActivityFactory(unitConverter.ToUnitSystem())
            {
                DefaultParty = source.SiteInformation.Persons,
            };

            var totalDischarge = unitConverter.ConvertDischarge(source.ChannelSummary.ChannelTotalQ ?? 0.0);
            var dischargeActivity = factory.CreateDischargeActivity(measurementPeriod, totalDischarge);

            dischargeActivity.MeasurementId = source.SiteInformation.MeasurementNumber;
            dischargeActivity.Comments = BuildComments(source);

            if (source.ChannelSummary.UncertaintyPercent.HasValue)
            {
                dischargeActivity.ActiveUncertaintyType = UncertaintyType.Quantitative;
                dischargeActivity.QuantitativeUncertainty = source.ChannelSummary.UncertaintyPercent.Value;
            }

            var qualitativeUncertainty = DetermineQualitativeUncertainty(source);
            if (qualitativeUncertainty.HasValue)
                dischargeActivity.QualitativeUncertainty = qualitativeUncertainty.Value;

            return dischargeActivity;
        }

        private static QualitativeUncertaintyType? DetermineQualitativeUncertainty(QRevMSDocument source)
        {
            var rating = source.ChannelSummary.UserRating;

            if (string.Equals(rating, "Excellent", StringComparison.OrdinalIgnoreCase))
                return QualitativeUncertaintyType.Excellent;

            if (string.Equals(rating, "Good", StringComparison.OrdinalIgnoreCase))
                return QualitativeUncertaintyType.Good;

            if (string.Equals(rating, "Fair", StringComparison.OrdinalIgnoreCase))
                return QualitativeUncertaintyType.Fair;

            if (string.Equals(rating, "Poor", StringComparison.OrdinalIgnoreCase))
                return QualitativeUncertaintyType.Poor;

            return null;
        }

        private static (DateTimeOffset start, DateTimeOffset end) GetVisitTimeRange(QRevMSDocument source, TimeSpan utcOffset)
        {
            var starts = source.Verticals
                .Where(v => v.StartDateTime.HasValue)
                .Select(v => v.StartDateTime.Value)
                .ToList();
            var ends = source.Verticals
                .Where(v => v.EndDateTime.HasValue)
                .Select(v => v.EndDateTime.Value)
                .ToList();

            var start = starts.Any() ? starts.Min() : DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var end = ends.Any() ? ends.Max() : start;

            return (new DateTimeOffset(start, utcOffset), new DateTimeOffset(end, utcOffset));
        }

        private static string BuildComments(QRevMSDocument source)
        {
            return string.Join(Environment.NewLine, new[]
                {
                    $"Imported from QRevMS file: {source.QRevFilename} ({source.QRevVersion})",
                    source.QaMessage,
                    source.UserComment,
                }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}