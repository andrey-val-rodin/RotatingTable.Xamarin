using RotatingTable.Xamarin.Services;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests
{
    public class ListeningStreamTests
    {
        private const char N = BluetoothService.Terminator;
        private readonly ListeningStream _stream = new();

        [Fact]
        public void Append_OneToken_ValidToken()
        {
            var token = "Token 1";
            var tokens = new List<string>();

            _stream.Append(Bytes($"{token}{N}"), (a, e) =>
            {
                tokens.Add(e.Text);
            });

            Assert.Single(tokens);
            Assert.Equal(token, tokens[0]);
            Assert.Equal(0, _stream.Length);
        }

        [Fact]
        public void Append_TwoTokens_ValidTokens()
        {
            var token1 = "Token 1";
            var token2 = "Token 2";
            var tokens = new List<string>();

            _stream.Append(Bytes($"{token1}{N}{token2}{N}"), (a, e) =>
            {
                tokens.Add(e.Text);
            });

            Assert.Equal(2, tokens.Count);
            Assert.Equal(token1, tokens[0]);
            Assert.Equal(token2, tokens[1]);
            Assert.Equal(0, _stream.Length);
        }

        [Fact]
        public void Append_DisruptedChain_ValidTokens()
        {
            var chain1 = $"Token 1{N}Token 2";
            var chain2 = $"{N}Token 3{N}";
            var tokens = new List<string>();
            _stream.Append(Bytes(chain1), (a, e) =>
            {
                tokens.Add(e.Text);
            });
            _stream.Append(Bytes(chain2), (a, e) =>
            {
                tokens.Add(e.Text);
            });

            Assert.Equal(3, tokens.Count);
            Assert.Equal("Token 1", tokens[0]);
            Assert.Equal("Token 2", tokens[1]);
            Assert.Equal("Token 3", tokens[2]);
            Assert.Equal(0, _stream.Length);
        }

        [Fact]
        public void Append_BigSequence_ValidTokens()
        {
            int current = 1;
            for (int i = 0; i < 1000; i++)
            {
                var token = $"Token {current++}";
                _stream.Append(Bytes(token + N), (a, e) =>
                {
                    Assert.Equal(token, e.Text);
                });
            }
        }

        private byte[] Bytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
    }
}
