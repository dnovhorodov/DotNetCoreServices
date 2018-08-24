using System.Text.RegularExpressions;
using Nancy.Routing.Constraints;

namespace DistanceService.Constraints
{
    public class IATARouteSegmentConstraint : RouteSegmentConstraintBase<string>
    {
        public override string Name => "iata";

        protected override bool TryMatch(string constraint, string segment, out string matchedValue)
        {
            if(Regex.IsMatch(segment, "^[A-Z]{3}$"))
            {
                matchedValue = segment;
                return true;
            }

            matchedValue = null;
            return false;
        }
    }
}
