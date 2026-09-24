using System;
using System.Collections.Generic;
using QRevMS.Configuration;
using Xunit;

namespace QRevMS.Tests
{
    public class ConfigLoaderTests
    {
        [Fact]
        public void Load_NoConfigSetting_DefaultsToMetric()
        {
            var config = ConfigLoader.Load(new Dictionary<string, string>());

            Assert.False(config.ImperialUnits);
        }

        [Fact]
        public void Load_BlankConfigSetting_DefaultsToMetric()
        {
            var settings = new Dictionary<string, string> { ["Config"] = "" };

            var config = ConfigLoader.Load(settings);

            Assert.False(config.ImperialUnits);
        }

        [Fact]
        public void Load_ExplicitImperialUnits_IsRespected()
        {
            var settings = new Dictionary<string, string> { ["Config"] = "{\"ImperialUnits\": true}" };

            var config = ConfigLoader.Load(settings);

            Assert.True(config.ImperialUnits);
        }

        [Fact]
        public void Load_LowercasePropertyName_StillMatches()
        {
            var settings = new Dictionary<string, string> { ["Config"] = "{\"imperialUnits\": true}" };

            var config = ConfigLoader.Load(settings);

            Assert.True(config.ImperialUnits);
        }

        [Fact]
        public void Load_MalformedJson_Throws()
        {
            var settings = new Dictionary<string, string> { ["Config"] = "{not valid json" };

            Assert.Throws<ArgumentException>(() => ConfigLoader.Load(settings));
        }
    }
}