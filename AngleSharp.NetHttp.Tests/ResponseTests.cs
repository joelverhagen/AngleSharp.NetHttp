using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knapcode.AngleSharp.NetHttp.Tests
{
    [TestClass]
    public class ResponseTests
    {
        [TestMethod, TestCategory("Unit")]
        public void DisposesContent()
        {
            // ARRANGE
            var stream = new DisposableStream();
            var response = new Response { Content = stream };

            // ACT
            response.Dispose();

            // ASSERT
            stream.Disposed.Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public void DisposesNothingWhenContentIsNull()
        {
            // ARRANGE
            var response = new Response { Content = null };

            // ACT
            Action action = () => response.Dispose();

            // ASSERT
            action.ShouldNotThrow();
        }

        public class DisposableStream : Stream
        {
            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override bool CanRead { get; }
            public override bool CanSeek { get; }
            public override bool CanWrite { get; }
            public override long Length { get; }
            public override long Position { get; set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }

            public bool Disposed { get; set; }
        }
    }
}
