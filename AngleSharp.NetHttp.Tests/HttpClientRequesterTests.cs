﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Network;
using AngleSharp.Services.Default;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetHttpMethod = System.Net.Http.HttpMethod;
using AngleSharpHttpMethod = AngleSharp.Network.HttpMethod;

namespace Knapcode.AngleSharp.NetHttp.Tests
{
    [TestClass]
    public class HttpClientRequesterTests
    {
        [TestMethod, TestCategory("Unit")]
        public void SupportsHttp()
        {
            // ARRANGE, ACT, ASSERT
            new TestState().Target.SupportsProtocol("HTTP").Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public void SupportsHttps()
        {
            // ARRANGE, ACT, ASSERT
            new TestState().Target.SupportsProtocol("HTTPS").Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public async Task RequestWithoutContent()
        {
            // ARRANGE
            var ts = new TestState { Request = { Content = null, Method = AngleSharpHttpMethod.Get } };

            // ACT
            await ts.Target.RequestAsync(ts.Request, CancellationToken.None);

            // ASSERT
            ts.HttpRequestMessage.Version.Should().Be(new Version(1, 1));
            ts.HttpRequestMessage.Method.Should().Be(NetHttpMethod.Get);
            ts.HttpRequestMessage.RequestUri.Should().Be(new Uri("http://example/path?query=value"));
            ts.HttpRequestMessage.Content.Should().BeNull();
            ts.HttpRequestMessage.Properties.Should().BeEmpty();
            ts.HttpRequestMessage.Headers.Select(p => p.Key).ShouldBeEquivalentTo(new[] { "User-Agent", "Cookie" });
            ts.HttpRequestMessage.Headers.UserAgent.ToString().Should().Be("Foo/2.0");
            ts.HttpRequestMessage.Headers.Single(p => p.Key == "Cookie").Value.ShouldBeEquivalentTo(new[] { "foo=bar" });
        }

        [TestMethod, TestCategory("Unit")]
        public async Task RequestWithContent()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT
            await ts.Target.RequestAsync(ts.Request, CancellationToken.None);

            // ASSERT
            ts.HttpRequestMessage.Version.Should().Be(new Version(1, 1));
            ts.HttpRequestMessage.Method.Should().Be(NetHttpMethod.Post);
            ts.HttpRequestMessage.RequestUri.Should().Be(new Uri("http://example/path?query=value"));
            Encoding.UTF8.GetString(ts.HttpRequestMessageContent).Should().Be("\"request\"");
            ts.HttpRequestMessage.Content.Headers.Select(p => p.Key).ShouldBeEquivalentTo(new[] {"Content-Type", "Content-Length"});
            ts.HttpRequestMessage.Content.Headers.ContentType.ToString().Should().Be("application/json");
            ts.HttpRequestMessage.Content.Headers.ContentLength.Should().Be(9);
            ts.HttpRequestMessage.Properties.Should().BeEmpty();
            ts.HttpRequestMessage.Headers.Select(p => p.Key).ShouldBeEquivalentTo(new[] { "User-Agent", "Cookie" });
            ts.HttpRequestMessage.Headers.UserAgent.ToString().Should().Be("Foo/2.0");
            ts.HttpRequestMessage.Headers.Single(p => p.Key == "Cookie").Value.ShouldBeEquivalentTo(new[] { "foo=bar" });
        }

        [TestMethod, TestCategory("Unit")]
        public async Task ResponseWithContent()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT
            var response = await ts.Target.RequestAsync(ts.Request, CancellationToken.None);

            // ASSERT
            response.Address.ShouldBeEquivalentTo(ts.Request.Address);
            response.StatusCode.Should().Be(ts.HttpResponseMessage.StatusCode);
            response.Headers.Keys.ShouldBeEquivalentTo(new[] {"Server", "X-Powered-By", "X-CSV", "Content-Type", "Content-Length"});
            response.Headers["Server"].Should().Be("Fake");
            response.Headers["X-Powered-By"].Should().Be("Magic");
            response.Headers["X-CSV"].Should().Be("foo, bar");
            response.Headers["Content-Type"].Should().Be("application/json; charset=utf-8");
            response.Headers["Content-Length"].Should().Be("10");
            new StreamReader(response.Content, Encoding.UTF8).ReadToEnd().Should().Be("\"response\"");
        }

        [TestMethod, TestCategory("Unit")]
        public async Task ResponseWithoutContent()
        {
            // ARRANGE
            var ts = new TestState {HttpResponseMessage = {Content = null}};

            // ACT
            var response = await ts.Target.RequestAsync(ts.Request, CancellationToken.None);

            // ASSERT
            response.Address.ShouldBeEquivalentTo(ts.Request.Address);
            response.StatusCode.Should().Be(ts.HttpResponseMessage.StatusCode);
            response.Headers.Keys.ShouldBeEquivalentTo(new[] { "Server", "X-Powered-By", "X-CSV" });
            response.Headers["Server"].Should().Be("Fake");
            response.Headers["X-Powered-By"].Should().Be("Magic");
            response.Headers["X-CSV"].Should().Be("foo, bar");
            response.Content.Should().BeNull();
        }

        [TestMethod, TestCategory("Integration")]
        public async Task EndToEnd()
        {
            // ARRANGE
            var httpClient = new HttpClient();
            var requester = new HttpClientRequester(httpClient);
            var configuration = new Configuration(new[] { new LoaderService(new[] { requester }) });
            var context = BrowsingContext.New(configuration);
            var request = DocumentRequest.Get(Url.Create("http://httpbin.org/html"));

            // ACT
            var response = await context.Loader.LoadAsync(request, CancellationToken.None);
            var document = await context.OpenAsync(response, CancellationToken.None);

            // ASSERT
            document.QuerySelector("h1").ToHtml().Should().Be("<h1>Herman Melville - Moby-Dick</h1>");
        }

        private class TestState
        {
            public TestState()
            {
                // dependencies

                TestHandler = new TestHandler(this);
                HttpClient = new HttpClient(TestHandler);

                // data
                Request = new Request
                {
                    Method = AngleSharpHttpMethod.Post,
                    Address = new Url("http://example/path?query=value"),
                    Headers = new Dictionary<string, string>
                    {
                        { "User-Agent", "Foo/2.0" },
                        { "Cookie", "foo=bar" },
                        { "Content-Type", "application/json" },
                        { "Content-Length", "9" }
                    },
                    Content = new MemoryStream(Encoding.UTF8.GetBytes("\"request\""))
                };
                HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("\"response\"", Encoding.UTF8, "application/json"),
                    Headers =
                    {
                        { "Server", "Fake" },
                        { "X-Powered-By", "Magic" },
                        { "X-CSV", new[] { "foo", "bar" } }
                    }
                };

                // setup
                Target = new HttpClientRequester(HttpClient);
            }

            public Request Request { get; set; }

            public HttpClientRequester Target { get; set; }
            public HttpClient HttpClient { get; }
            public TestHandler TestHandler { get; }
            public HttpResponseMessage HttpResponseMessage { get; }
            public HttpRequestMessage HttpRequestMessage { get; set; }
            public byte[] HttpRequestMessageContent { get; set; }
        }

        private class TestHandler : DelegatingHandler
        {
            private readonly TestState _testState;

            public TestHandler(TestState testState)
            {
                _testState = testState;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _testState.HttpRequestMessage = request;
                if (request.Content != null)
                {
                    _testState.HttpRequestMessageContent = await request.Content.ReadAsByteArrayAsync();
                }

                _testState.HttpResponseMessage.RequestMessage = request;
                return _testState.HttpResponseMessage;
            }
        }

        private class Request : IRequest
        {
            public AngleSharpHttpMethod Method { get; set; }
            public Url Address { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public Stream Content { get; set; }
        }
    }
}