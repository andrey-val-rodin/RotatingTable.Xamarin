using System.Threading.Tasks;
using Xamarin.Essentials;

namespace RotatingTable.Xamarin.Models
{
    public class Storage : IStorage
    {
        public async Task<string> GetAsync(string key)
        {
            return await SecureStorage.GetAsync(key);
        }

        public async Task SetAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
        }
    }
}
