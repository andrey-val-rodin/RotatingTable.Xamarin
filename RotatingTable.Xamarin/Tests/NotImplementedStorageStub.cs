using RotatingTable.Xamarin.Models;
using System.Threading.Tasks;

namespace Tests
{
    internal class NotImplementedStorageStub : IStorage
    {
        public Task<string> GetAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public Task SetAsync(string key, string value)
        {
            throw new System.NotImplementedException();
        }
    }
}
