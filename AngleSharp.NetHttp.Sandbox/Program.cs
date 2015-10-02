using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Network;
using AngleSharp.Services.Default;

namespace Knapcode.AngleSharp.NetHttp.Sandbox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            // setup
            var httpClient = new HttpClient();
            var requester = new HttpClientRequester(httpClient);
            var configuration = new Configuration(new[] { new LoaderService(new[] { requester }) });
            var context = BrowsingContext.New(configuration);

            // request
            var request = DocumentRequest.Get(Url.Create("http://httpbin.org/html"));
            var response = await context.Loader.LoadAsync(request, CancellationToken.None);

            // parse
            var document = await context.OpenAsync(response, CancellationToken.None);

            // interact
            Console.WriteLine(document.QuerySelector("h1").ToHtml());
        }

    }
}
