using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace RotatingTable.Xamarin.Models
{
    public class Config : IConfig
    {
        private Guid _idBackingStore = Guid.Empty;
        private int _stepsBackingStore = ConfigValidator.DefaultStepsValue;
        private int _accelerationBackingStore = ConfigValidator.DefaultAccelerationValue;
        private int _exposureBackingStore = ConfigValidator.DefaultExposureValue;
        private int _delayBackingStore = ConfigValidator.DefaultDelayValue;

        public async Task<Guid> GetDeviceIdAsync()
        {
            try
            {
                return Guid.Parse(await SecureStorage.GetAsync("DeviceId"));
            }
            catch
            {
                return _idBackingStore;
            }
        }

        public async Task SetDeviceIdAsync(Guid id)
        {
            try
            {
                // {00000000-0000-0000-0000-e684736e04f7}
                await SecureStorage.SetAsync("DeviceId", id.ToString());
            }
            catch
            {
                _idBackingStore = id;
            }
        }

        public async Task<int> GetStepsAsync()
        {
            try
            {
                var steps = await SecureStorage.GetAsync("Steps");
                return int.Parse(steps.ToString());
            }
            catch
            {
                return _stepsBackingStore;
            }
        }

        public async Task SetStepsAsync(int steps)
        {
            try
            {
                await SecureStorage.SetAsync("Steps", steps.ToString());
            }
            catch
            {
                _stepsBackingStore = steps;
            }
        }

        public async Task<int> GetAccelerationAsync()
        {
            try
            {
                var acceleration = await SecureStorage.GetAsync("Acceleration");
                return int.Parse(acceleration.ToString());
            }
            catch
            {
                return _accelerationBackingStore;
            }
        }

        public async Task SetAccelerationAsync(int acceleration)
        {
            try
            {
                await SecureStorage.SetAsync("Acceleration", acceleration.ToString());
            }
            catch
            {
                _accelerationBackingStore = acceleration;
            }
        }

        public async Task<int> GetDelayAsync()
        {
            try
            {
                var delay = await SecureStorage.GetAsync("Delay");
                return int.Parse(delay.ToString());
            }
            catch
            {
                return _delayBackingStore;
            }
        }

        public async Task SetDelayAsync(int delay)
        {
            try
            {
                await SecureStorage.SetAsync("Delay", delay.ToString());
            }
            catch
            {
                _delayBackingStore = delay;
            }
        }

        public async Task<int> GetExposureAsync()
        {
            try
            {
                var exposure = await SecureStorage.GetAsync("Exposure");
                return int.Parse(exposure.ToString());
            }
            catch
            {
                return _exposureBackingStore;
            }
        }

        public async Task SetExposureAsync(int exposure)
        {
            try
            {
                await SecureStorage.SetAsync("Exposure", exposure.ToString());
            }
            catch
            {
                _exposureBackingStore = exposure;
            }
        }
    }
}
