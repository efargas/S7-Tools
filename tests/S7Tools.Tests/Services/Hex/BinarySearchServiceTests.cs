using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaHex.Document;
using S7Tools.Services.Hex;
using Xunit;

namespace S7Tools.Tests.Services.Hex
{
    public class BinarySearchServiceTests
    {
        private class TestBinaryDocument : IBinaryDocument
        {
            private readonly byte[] _data;

            public TestBinaryDocument(byte[] data)
            {
                _data = data;
            }

            public ulong Length => (ulong)_data.Length;
            public bool IsReadOnly => true;
            public bool CanInsert => false;
            public bool CanRemove => false;
            public IReadOnlyBitRangeUnion ValidRanges => throw new NotImplementedException();
            public event EventHandler<BinaryDocumentChange>? Changed;

            public void Dispose() { }
            public void Flush() { }
            public void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer) => throw new NotImplementedException();
            public void RemoveBytes(ulong offset, ulong length) => throw new NotImplementedException();
            public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer) => throw new NotImplementedException();

            public void ReadBytes(ulong offset, Span<byte> buffer)
            {
                if (offset >= Length) return;
                
                int count = Math.Min(buffer.Length, (int)(Length - offset));
                _data.AsSpan((int)offset, count).CopyTo(buffer);
            }
        }

        [Fact]
        public async Task FindAllAsync_SimpleMatch_ReturnsCorrectOffsets()
        {
            var data = new byte[] { 0x01, 0x02, 0xAA, 0xBB, 0x03, 0xAA, 0xBB, 0x04 };
            var doc = new TestBinaryDocument(data);
            var service = new BinarySearchService();
            var pattern = new byte[] { 0xAA, 0xBB };

            var results = await service.FindAllAsync(doc, pattern);

            Assert.Equal(new long[] { 2, 5 }, results);
        }
        
        [Fact]
        public async Task FindNextAsync_SimpleMatch_ReturnsCorrectOffset()
        {
             var data = new byte[] { 0x01, 0x02, 0xAA, 0xBB, 0x03, 0xAA, 0xBB, 0x04 };
             var doc = new TestBinaryDocument(data);
             var service = new BinarySearchService();
             var pattern = new byte[] { 0xAA, 0xBB };

             var result1 = await service.FindNextAsync(doc, pattern, 0);
             Assert.Equal(2, result1);

             var result2 = await service.FindNextAsync(doc, pattern, 3);
             Assert.Equal(5, result2);
        }

        [Fact]
        public async Task FindAllAsync_NoMatch_ReturnsEmpty()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };
            var doc = new TestBinaryDocument(data);
            var service = new BinarySearchService();
            var pattern = new byte[] { 0xFF };

            var results = await service.FindAllAsync(doc, pattern);

            Assert.Empty(results);
        }

        [Fact]
        public async Task FindAllAsync_PatternLargerThanDoc_ReturnsEmpty()
        {
            var data = new byte[] { 0x01 };
            var doc = new TestBinaryDocument(data);
            var service = new BinarySearchService();
            var pattern = new byte[] { 0x01, 0x02 };

            var results = await service.FindAllAsync(doc, pattern);

            Assert.Empty(results);
        }
    }
}
