using RotatingTable.Xamarin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests
{
    internal class StorageStub : IStorage
    {
        private readonly Dictionary<string, string> _dictionary = new();

        public Task<string> GetAsync(string key)
        {
            return Task.FromResult(_dictionary.TryGetValue(key, out var value) ? value : null);
        }

        public Task SetAsync(string key, string value)
        {
            _dictionary[key] = value;
            return Task.CompletedTask;
        }
    }
}
