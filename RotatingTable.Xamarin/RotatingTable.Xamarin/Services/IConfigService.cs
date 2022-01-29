using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Services
{
    public interface IConfigService
    {
        Task<string> GetMacAddressAsync();
        Task SetMacAddressAsync(string address);
    }
}