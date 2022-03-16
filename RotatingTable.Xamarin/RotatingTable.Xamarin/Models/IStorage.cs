using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Models
{
    public interface IStorage
    {
        Task<string> GetAsync(string key);
        Task SetAsync(string key, string value);
    }
}
