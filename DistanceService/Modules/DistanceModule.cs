using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DistanceService.Models;
using Microsoft.Extensions.Configuration;
using Nancy;
using Newtonsoft.Json;
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
            
            Get("api/distance/{from:iata}-{to:iata}", async (args, ct) =>
            {
                var airportFrom = await GetAirportInfo((string)args.from, ct);
                var airportTo = await GetAirportInfo((string)args.to, ct);
                ThrowIfDataProblems(airportFrom, airportTo);



                return 200;
            });
        }

        private async Task<AirportModel> GetAirportInfo(string iata, CancellationToken ct)
        {
            var httpClient = new HttpClient();
            var request = $"{AirportServiceUri.AbsoluteUri}/{iata}";

            var response = await Registry.Get<IAsyncPolicy<HttpResponseMessage>>("StandardHttpResilience")
                .ExecuteAsync(() => httpClient.GetAsync(request, ct));

            var stringContent = await response.Content.ReadAsStringAsync();
            ct.ThrowIfCancellationRequested();

            return JsonConvert.DeserializeObject<AirportModel>(stringContent);
        }

        private void CalculateDistance()
        {

        }

        private void ThrowIfDataProblems(AirportModel from, AirportModel to)
        {

        }
    }
}
