# AngleSharp.NetHttp

## Install

```
Install-Package Knapcode.AngleSharp.NetHttp
```

## Example

```csharp
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
```

```html
<h1>Herman Melville - Moby-Dick</h1>
```
