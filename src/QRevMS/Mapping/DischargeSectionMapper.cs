using System;
using System.Linq;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.DischargeActivities;
using QRevMS.Parsing;
using QRevMS.Units;

namespace QRevMS.Mapping
{
    public static class DischargeSectionMapper
    {
        public static ManualGaugingDischargeSection CreateSection(QRevMSDocument source, UnitConverter unitConverter, DischargeActivity dischargeActivity, DischargeMethodType dischargeMethod)
        {
            var factory = new ManualGaugingDischargeSectionFactory(unitConverter.ToUnitSystem());

            var section = factory.CreateManualGaugingDischargeSection(dischargeActivity.MeasurementPeriod, dischargeActivity.Discharge.Value);

            section.WidthValue = unitConverter.ConvertDistance(source.ChannelSummary.ChannelWidth ?? 0.0);
            section.AreaValue = unitConverter.ConvertArea(source.ChannelSummary.ChannelArea ?? 0.0);
            section.VelocityAverageValue = unitConverter.ConvertVelocity(source.ChannelSummary.MeanNormalVelocity ?? 0.0);
            section.Party = dischargeActivity.Party;
            section.Comments = dischargeActivity.Comments;
            section.MeterSuspension = MeterSuspensionType.Unspecified;
            section.StartPoint = DetermineStartPoint(source);
            section.DischargeMethod = dischargeMethod;
            section.DeploymentMethod = DeploymentMethodType.Unspecified;
            // TODO: VelocityObservationMethod intentionally left unset, revisit after testing

            return section;
        }

        public static DischargeMethodType DetermineDischargeMethod(QRevMSDocument source)
        {
            if (string.Equals(source.Processing.DischargeMethod, "Mid-Section", StringComparison.OrdinalIgnoreCase))
                return DischargeMethodType.MidSection;

            if (string.Equals(source.Processing.DischargeMethod, "Mean-Section", StringComparison.OrdinalIgnoreCase))
                return DischargeMethodType.MeanSection;

            throw new FormatException(
                $"Unrecognized QRevMS discharge method '{source.Processing.DischargeMethod}' - expected 'Mid-Section' or 'Mean-Section'.");
        }

        private static StartPointType DetermineStartPoint(QRevMSDocument source)
        {
            var measuredPoints = source.Verticals.Where(v => !v.IsEdge).ToList();

            var startedAtFirstEdge = measuredPoints.Count < 2
                || measuredPoints.First().StartDateTime <= measuredPoints.Last().StartDateTime;

            var startingBank = startedAtFirstEdge
                ? source.Verticals.FirstOrDefault()?.Bank
                : source.Verticals.LastOrDefault()?.Bank;

            return startingBank == "Left"
                ? StartPointType.LeftEdgeOfWater
                : StartPointType.RightEdgeOfWater;
        }
    }
}