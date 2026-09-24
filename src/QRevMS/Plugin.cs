using System;
using System.IO;
using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.Results;
using QRevMS.Configuration;
using QRevMS.Mapping;
using QRevMS.Parsing;
using QRevMS.Units;

namespace QRevMS
{
    public class Plugin : IFieldDataPlugin
    {
        public ParseFileResult ParseFile(Stream fileStream, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            return ParseFileInternal(fileStream, null, fieldDataResultsAppender, logger);
        }

        public ParseFileResult ParseFile(Stream fileStream, LocationInfo targetLocation, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            return ParseFileInternal(fileStream, targetLocation, fieldDataResultsAppender, logger);
        }

        private ParseFileResult ParseFileInternal(Stream fileStream, LocationInfo targetLocation, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            if (!QRevMSParser.CanParse(fileStream))
            {
                logger.Info("File does not look like a QRevMS <Channel> document. Skipping.");
                return ParseFileResult.CannotParse();
            }

            try
            {
                var document = QRevMSParser.Parse(fileStream);

                logger.Info($"Parsed QRevMS file '{document.QRevFilename}' ({document.QRevVersion}), " +
                            $"Total Q = {document.ChannelSummary.ChannelTotalQ} cms, " +
                            $"{document.Stations.Count} stations.");

                if (targetLocation == null && string.IsNullOrWhiteSpace(document.SiteInformation.SiteId))
                {
                    logger.Info("QRevMS file has no SiteID and no target location was supplied. " +
                                "Import this file via Location Manager or the Field Data Editor instead of Springboard.");
                    return ParseFileResult.SuccessfullyParsedButDataInvalid(
                        "QRevMS file does not identify a location. Re-import using Location Manager or the Field Data Editor.");
                }

                var locationIdentifier = targetLocation?.LocationIdentifier ?? document.SiteInformation.SiteId;
                var location = targetLocation ?? fieldDataResultsAppender.GetLocationByIdentifier(locationIdentifier);

                var config = ConfigLoader.Load(fieldDataResultsAppender.GetPluginConfigurations());
                var unitConverter = new UnitConverter(config.ImperialUnits);

                var dischargeActivity = DischargeActivityBuilder.Build(document, location.UtcOffset, unitConverter);
                var fieldVisitDetails = new FieldVisitDetails(dischargeActivity.MeasurementPeriod)
                {
                    Party = document.SiteInformation.Persons,
                };

                var visit = fieldDataResultsAppender.AddFieldVisit(location, fieldVisitDetails);
                fieldDataResultsAppender.AddDischargeActivity(visit, dischargeActivity);

                return ParseFileResult.SuccessfullyParsedAndDataValid();
            }
            catch (Exception exception)
            {
                logger.Error($"Error parsing QRevMS file: {exception}");
                return ParseFileResult.SuccessfullyParsedButDataInvalid(exception);
            }
        }
    }
}