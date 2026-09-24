using QRevMS.Units;
using Xunit;

namespace QRevMS.Tests
{
    public class UnitConverterTests
    {
        [Fact]
        public void Metric_UnitIdsAreMetric()
        {
            var converter = new UnitConverter(false);

            Assert.Equal("m", converter.DistanceUnitId);
            Assert.Equal("m^2", converter.AreaUnitId);
            Assert.Equal("m/s", converter.VelocityUnitId);
            Assert.Equal("m^3/s", converter.DischargeUnitId);
        }

        [Fact]
        public void Imperial_UnitIdsAreImperial()
        {
            var converter = new UnitConverter(true);

            Assert.Equal("ft", converter.DistanceUnitId);
            Assert.Equal("ft^2", converter.AreaUnitId);
            Assert.Equal("ft/s", converter.VelocityUnitId);
            Assert.Equal("ft^3/s", converter.DischargeUnitId);
        }

        [Fact]
        public void Metric_ValuesPassThroughUnchanged()
        {
            var converter = new UnitConverter(false);

            Assert.Equal(10.0, converter.ConvertDistance(10.0));
            Assert.Equal(10.0, converter.ConvertArea(10.0));
            Assert.Equal(10.0, converter.ConvertVelocity(10.0));
            Assert.Equal(10.0, converter.ConvertDischarge(10.0));
        }

        [Fact]
        public void Imperial_ConvertDistance_DividesByMetersPerFoot()
        {
            var converter = new UnitConverter(true);
            const double oneMeterInFeet = 3.28084;

            Assert.Equal(oneMeterInFeet, converter.ConvertDistance(1.0), 5);
        }

        [Fact]
        public void Imperial_ConvertVelocity_UsesSameFactorAsDistance()
        {
            var converter = new UnitConverter(true);

            Assert.Equal(converter.ConvertDistance(1.0), converter.ConvertVelocity(1.0), 10);
        }

        [Fact]
        public void Imperial_ConvertArea_DividesBySquareMetersPerSquareFoot()
        {
            var converter = new UnitConverter(true);

            Assert.Equal(10.7639, converter.ConvertArea(1.0), 4);
        }

        [Fact]
        public void Imperial_ConvertDischarge_DividesByCubicMetersPerSecondPerCubicFootPerSecond()
        {
            var converter = new UnitConverter(true);

            Assert.Equal(35.3147, converter.ConvertDischarge(1.0), 4);
        }

        [Fact]
        public void ToUnitSystem_MatchesIndividualUnitIdProperties()
        {
            var converter = new UnitConverter(true);
            var unitSystem = converter.ToUnitSystem();

            Assert.Equal(converter.DistanceUnitId, unitSystem.DistanceUnitId);
            Assert.Equal(converter.AreaUnitId, unitSystem.AreaUnitId);
            Assert.Equal(converter.VelocityUnitId, unitSystem.VelocityUnitId);
            Assert.Equal(converter.DischargeUnitId, unitSystem.DischargeUnitId);
        }
    }
}