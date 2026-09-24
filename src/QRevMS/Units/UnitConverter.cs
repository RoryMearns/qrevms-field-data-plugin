using FieldDataPluginFramework.Units;

namespace QRevMS.Units
{
    public class UnitConverter
    {
        private const double MetersPerFoot = 0.3048;
        private const double SquareMetersPerSquareFoot = 0.09290304;
        private const double CubicMetersPerSecondPerCubicFootPerSecond = 0.028316846592;

        private bool IsImperial { get; }

        public UnitConverter(bool isImperial)
        {
            IsImperial = isImperial;
        }

        public string DistanceUnitId => IsImperial ? "ft" : "m";
        public string AreaUnitId => IsImperial ? "ft^2" : "m^2";
        public string VelocityUnitId => IsImperial ? "ft/s" : "m/s";
        public string DischargeUnitId => IsImperial ? "ft^3/s" : "m^3/s";

        public double ConvertDistance(double metersValue) => IsImperial ? metersValue / MetersPerFoot : metersValue;
        public double ConvertArea(double squareMetersValue) => IsImperial ? squareMetersValue / SquareMetersPerSquareFoot : squareMetersValue;
        public double ConvertVelocity(double metersPerSecondValue) => IsImperial ? metersPerSecondValue / MetersPerFoot : metersPerSecondValue;
        public double ConvertDischarge(double cubicMetersPerSecondValue) => IsImperial ? cubicMetersPerSecondValue / CubicMetersPerSecondPerCubicFootPerSecond : cubicMetersPerSecondValue;

        public UnitSystem ToUnitSystem()
        {
            return new UnitSystem
            {
                DistanceUnitId = DistanceUnitId,
                AreaUnitId = AreaUnitId,
                VelocityUnitId = VelocityUnitId,
                DischargeUnitId = DischargeUnitId,
            };
        }
    }
}
