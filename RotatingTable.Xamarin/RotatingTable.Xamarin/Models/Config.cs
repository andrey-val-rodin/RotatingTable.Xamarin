using System;
using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Models
{
    public class Config : IConfig
    {
        private Guid _idBackingStore = ConfigValidator.DefaultDeviceIdValue;
        private int _stepsBackingStore = ConfigValidator.DefaultStepsValue;
        private int _accelerationBackingStore = ConfigValidator.DefaultAccelerationValue;
        private int _exposureBackingStore = ConfigValidator.DefaultExposureValue;
        private int _delayBackingStore = ConfigValidator.DefaultDelayValue;

        public IStorage Storage { get; set; } = new Storage();

        public async Task<Guid> GetDeviceIdAsync()
        {
            try
            {
                var id = await Storage.GetAsync("DeviceId");
                return string.IsNullOrEmpty(id) ? _idBackingStore : Guid.Parse(id);
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
                await Storage.SetAsync("DeviceId", id.ToString());
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
                var steps = await Storage.GetAsync("Steps");
                return string.IsNullOrEmpty(steps) ? _stepsBackingStore : int.Parse(steps);
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
                await Storage.SetAsync("Steps", steps.ToString());
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
                var acceleration = await Storage.GetAsync("Acceleration");
                return string.IsNullOrEmpty(acceleration) ? _accelerationBackingStore : int.Parse(acceleration);
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
                await Storage.SetAsync("Acceleration", acceleration.ToString());
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
                var delay = await Storage.GetAsync("Delay");
                return string.IsNullOrEmpty(delay) ? _delayBackingStore : int.Parse(delay);
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
                await Storage.SetAsync("Delay", delay.ToString());
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
                var exposure = await Storage.GetAsync("Exposure");
                return string.IsNullOrEmpty(exposure) ? _exposureBackingStore : int.Parse(exposure);
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
                await Storage.SetAsync("Exposure", exposure.ToString());
            }
            catch
            {
                _exposureBackingStore = exposure;
            }
        }
    }
}
