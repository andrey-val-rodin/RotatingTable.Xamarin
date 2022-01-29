using System.Threading.Tasks;
using Xamarin.Essentials;

namespace RotatingTable.Xamarin.Services
{
    public class ConfigService : IConfigService
    {
        public async Task<string> GetMacAddressAsync()
        {
            return await SecureStorage.GetAsync("Address");
        }

        public async Task SetMacAddressAsync(string address)
        {
            await SecureStorage.SetAsync("Address", address);
        }
    }
}
