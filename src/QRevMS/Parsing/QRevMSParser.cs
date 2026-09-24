using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace QRevMS.Parsing
{
    public static class QRevMSParser
    {
        private const string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";
        private const string TimeFormat = "HH:mm:ss";

        public static bool CanParse(Stream stream)
        {
            try
            {
                var document = XDocument.Load(stream);
                var root = document.Root;

                return root is { Name.LocalName: "Channel" }
                       && root.Attribute("QRevVersion") != null
                       && root.Attribute("QRevVersion")!.Value.StartsWith("QRevMS", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (stream.CanSeek)
                    stream.Position = 0;
            }
        }

        public static QRevMSDocument Parse(Stream stream)
        {
            var document = XDocument.Load(stream);
            var root = document.Root;

            if (root == null || root.Name.LocalName != "Channel")
                throw new FormatException("Not a QRevMS <Channel> document.");

            var result = new QRevMSDocument
            {
                QRevFilename = (string)root.Attribute("QRevFilename"),
                QRevVersion = (string)root.Attribute("QRevVersion"),
                UserComment = TextOf(root, "UserComment"),
                QaMessage = TextOf(root, "QA", "QRev_Message"),
            };

            ParseSiteInformation(root, result.SiteInformation);
            ParseInstrument(root, result.Instrument);
            ParseProcessing(root, result.Processing);
            ParseChannelSummary(root, result.ChannelSummary);

            foreach (var stationElement in root.Elements("StationDischarge").Elements("Station"))
                result.Stations.Add(ParseStation(stationElement));

            foreach (var verticalElement in root.Elements("VerticalDetails").Elements("Vertical"))
                result.Verticals.Add(ParseVertical(verticalElement));

            return result;
        }

        private static void ParseSiteInformation(XElement root, SiteInformation target)
        {
            var section = root.Element("SiteInformation");
            if (section == null) return;

            target.StationName = TextOf(section, "StationName");
            target.SiteId = TextOf(section, "SiteID");
            target.Persons = TextOf(section, "Persons");
            target.MeasurementNumber = TextOf(section, "MeasurementNumber");
        }

        private static void ParseInstrument(XElement root, Instrument target)
        {
            var section = root.Element("Instrument");
            if (section == null) return;

            target.Manufacturer = TextOf(section, "Manufacturer");
            target.Model = TextOf(section, "Model");
            target.SerialNumber = TextOf(section, "SerialNumber");
            target.FirmwareVersion = TextOf(section, "FirmwareVersion");
        }

        private static void ParseProcessing(XElement root, Processing target)
        {
            var section = root.Element("Processing");
            if (section == null) return;

            target.SoftwareVersion = TextOf(section, "SoftwareVersion");
            target.Type = TextOf(section, "Type");
            target.DischargeMethod = TextOf(section, "DischargeMethod");
            target.VelocityReference = TextOf(section, "VelocityReference");
            target.VelocityMethod = TextOf(section, "VelocityMethod");
            target.TaglineAzimuthDegrees = DoubleOf(section, "TaglineAzimuth");
            target.WaterSurfaceCondition = TextOf(section, "WaterSurfaceCondition");
            target.Filename = TextOf(section, "Filename");
        }

        private static void ParseChannelSummary(XElement root, ChannelSummary target)
        {
            var section = root.Element("ChannelSummary");
            if (section == null) return;

            ThrowIfUnexpectedUnits(section, "ChannelTotalQ", "cms");
            ThrowIfUnexpectedUnits(section, "ChannelWidth", "m");
            ThrowIfUnexpectedUnits(section, "ChannelArea", "sqm");
            ThrowIfUnexpectedUnits(section, "MeanNormalVelocity", "mps");

            target.ChannelTopQ = DoubleOf(section, "ChannelTopQ");
            target.ChannelMiddleQ = DoubleOf(section, "ChannelMiddleQ");
            target.ChannelBottomQ = DoubleOf(section, "ChannelBottomQ");
            target.ChannelTotalQ = DoubleOf(section, "ChannelTotalQ");
            target.ChannelWidth = DoubleOf(section, "ChannelWidth");
            target.ChannelArea = DoubleOf(section, "ChannelArea");
            target.MeanNormalVelocity = DoubleOf(section, "MeanNormalVelocity");
            target.ChannelMeanDepth = DoubleOf(section, "ChannelMeanDepth");
            target.ChannelMaximumDepth = DoubleOf(section, "ChannelMaximumDepth");
            target.MaximumNormalVelocity = DoubleOf(section, "MaximumNormalVelocity");
            target.UserRating = TextOf(section, "UserRating");

            var uncertainty = section.Element("Uncertainty");
            if (uncertainty == null) return;
            target.UncertaintyModel = TextOf(uncertainty, "Model");
            target.UncertaintyPercent = DoubleOf(uncertainty, "Total");
        }

        private static StationDischarge ParseStation(XElement element)
        {
            return new StationDischarge
            {
                StationLocation = DoubleOf(element, "StationLocation"),
                StationDepth = DoubleOf(element, "StationDepth"),
                StationWidth = DoubleOf(element, "StationWidth"),
                StationArea = DoubleOf(element, "StationArea"),
                StationNormalVelocity = DoubleOf(element, "StationNormalVelocity"),
                StationTopQ = DoubleOf(element, "StationTopQ"),
                StationMiddleQ = DoubleOf(element, "StationMiddleQ"),
                StationBottomQ = DoubleOf(element, "StationBottomQ"),
                StationTotalQ = DoubleOf(element, "StationTotalQ"),
                StationPercentQ = DoubleOf(element, "StationPercentQ"),
            };
        }

        private static VerticalDetail ParseVertical(XElement element)
        {
            var sensor = element.Element("Sensor");

            return new VerticalDetail
            {
                Bank = TextOf(element, "Bank"),
                EdgeDepth = DoubleOf(element, "EdgeDepth"),
                EdgeCoefficient = DoubleOf(element, "EdgeCoefficient"),
                Location = DoubleOf(element, "Location"),
                StartDateTime = DateTimeOf(element, "StartDateTime"),
                EndDateTime = DateTimeOf(element, "EndDateTime"),
                Depth = DoubleOf(element, "Depth"),
                GaugeHeight = DoubleOf(element, "GaugeHeight"),
                GaugeHeightTime = TimeOf(element, "GaugeHeightTime"),
                VelocityMagnitude = DoubleOf(element, "VelocityMagnitude"),
                NormalVelocity = DoubleOf(element, "NormalVelocity"),
                NumberValidEnsembles = IntOf(element, "NumberValidEnsembles"),
                NumberValidCells = IntOf(element, "NumberValidCells"),
                DurationSeconds = DoubleOf(element, "Duration"),
                MeanTemperature = sensor != null ? DoubleOf(sensor, "MeanTemperature") : null,
            };
        }

        private static string TextOf(XElement parent, string childName)
        {
            var value = parent?.Element(childName)?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void ThrowIfUnexpectedUnits(XElement parent, string childName, string expectedUnitsCode)
        {
            var actualUnitsCode = (string)parent.Element(childName)?.Attribute("unitsCode");

            if (string.IsNullOrEmpty(actualUnitsCode))
                return;

            if (!string.Equals(actualUnitsCode, expectedUnitsCode, StringComparison.OrdinalIgnoreCase))
                throw new FormatException(
                    $"Expected units '{expectedUnitsCode}' for {childName} but found '{actualUnitsCode}' - this plugin only supports metric QRevMS files.");
        }

        private static string TextOf(XElement parent, string childName, string grandchildName)
        {
            return TextOf(parent?.Element(childName), grandchildName);
        }

        private static double? DoubleOf(XElement parent, string childName)
        {
            var text = TextOf(parent, childName);
            return text != null && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static int? IntOf(XElement parent, string childName)
        {
            var text = TextOf(parent, childName);
            return text != null && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static DateTime? DateTimeOf(XElement parent, string childName)
        {
            var text = TextOf(parent, childName);
            return text != null && DateTime.TryParseExact(text, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? value
                : null;
        }

        private static TimeSpan? TimeOf(XElement parent, string childName)
        {
            var text = TextOf(parent, childName);
            return text != null && DateTime.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? value.TimeOfDay
                : null;
        }
    }
}
