using System;
using System.Device.Location;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DistanceService.Models;
using Microsoft.Extensions.Configuration;
using Nancy;
using Polly;
using Polly.Registry;

namespace DistanceService.Modules
{
    public class DistanceModule: NancyModule
    {
        private readonly Uri AirportServiceUri;
        private readonly IReadOnlyPolicyRegistry<string> Registry;

        public DistanceModule(IConfiguration configuration, IReadOnlyPolicyRegistry<string> registry)
        {
            AirportServiceUri = new Uri(configuration["ExternalServices:0:AirportServiceUri"]);
            Registry = registry;

            Get("api/distance/{from:iata}-{to:iata}", GetDistance);
        }

        private async Task<Response> GetDistance(dynamic args, CancellationToken ct)
        {
            var airportOneResponse = await GetAirportResponseAsync((string)args.from, ct);
            if (airportOneResponse.StatusCode != HttpStatusCode.OK)
                return airportOneResponse;

            var airportTwoResponse = await GetAirportResponseAsync((string)args.to, ct);
            if (airportTwoResponse.StatusCode != HttpStatusCode.OK)
                return airportTwoResponse;

            var airportOneModel = airportOneResponse.GetModel<AirportModel>();
            var airportTwoModel = airportTwoResponse.GetModel<AirportModel>();
            var distance = CalculateDistance(airportOneModel, airportTwoModel);

            dynamic model = new ExpandoObject();
            model.AirportOne = airportOneModel.iata;
            model.AirportTwo = airportTwoModel.iata;
            model.Distance = $"{(int)distance} miles";

            return Response.AsJson((object)model);
        }

        private async Task<Response> GetAirportResponseAsync(string iata, CancellationToken ct)
        {
            var httpClient = new HttpClient();
            var request = $"{AirportServiceUri.AbsoluteUri}/{iata}";

            var httpResponse = await Registry.Get<IAsyncPolicy<HttpResponseMessage>>("StandardHttpResilience")
                .ExecuteAsync(() => httpClient.GetAsync(request, ct));

            ct.ThrowIfCancellationRequested();

            return await httpResponse.CreateResponse();
        }

        private double CalculateDistance(AirportModel from, AirportModel to)
        {
            var fromCoord = new GeoCoordinate(from.location.lat, from.location.lon);
            var toCoord = new GeoCoordinate(to.location.lat, to.location.lon);

            return fromCoord.GetDistanceTo(toCoord) / 1609.344; // meters to miles
        }
    }
}
