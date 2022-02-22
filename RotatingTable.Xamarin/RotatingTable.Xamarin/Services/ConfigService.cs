using RotatingTable.Xamarin.Models;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace RotatingTable.Xamarin.Services
{
    public class ConfigService : IConfigService
    {
        public async Task<Guid> GetDeviceIdAsync()
        {
            if (Guid.TryParse(await SecureStorage.GetAsync("DeviceId"), out var id))
                return id;
            else
                return Guid.Empty;
        }

        public async Task SetDeviceIdAsync(Guid id)
        {
            // {00000000-0000-0000-0000-e684736e04f7}
            await SecureStorage.SetAsync("DeviceId", id.ToString());
        }

        public async Task<int> GetStepsAsync()
        {
            var steps = await SecureStorage.GetAsync("Steps");
            if (int.TryParse(steps, out int result))
                return ConfigValidator.ValidateSteps(result);
            
            return ConfigValidator.DefaultStepsValue;
        }

        public async Task SetStepsAsync(int steps)
        {
            await SecureStorage.SetAsync("Steps", steps.ToString());
        }

        public async Task<int> GetAccelerationAsync()
        {
            var acceleration = await SecureStorage.GetAsync("Acceleration");
            if (int.TryParse(acceleration, out int result))
                return ConfigValidator.ValidateAcceleration(result);

            return ConfigValidator.DefaultAccelerationValue;
        }

        public async Task SetAccelerationAsync(int acceleration)
        {
            await SecureStorage.SetAsync("Acceleration", acceleration.ToString());
        }

        public async Task<int> GetDelayAsync()
        {
            var delay = await SecureStorage.GetAsync("Delay");
            if (int.TryParse(delay, out int result))
                return ConfigValidator.ValidateDelay(result);

            return ConfigValidator.DefaultDelayValue;
        }

        public async Task SetDelayAsync(int delay)
        {
            await SecureStorage.SetAsync("Delay", delay.ToString());
        }

        public async Task<int> GetExposureAsync()
        {
            var exposure = await SecureStorage.GetAsync("Exposure");
            if (int.TryParse(exposure, out int result))
                return ConfigValidator.ValidateExposure(result);

            return ConfigValidator.DefaultExposureValue;
        }

        public async Task SetExposureAsync(int exposure)
        {
            await SecureStorage.SetAsync("Exposure", exposure.ToString());
        }
    }
}
