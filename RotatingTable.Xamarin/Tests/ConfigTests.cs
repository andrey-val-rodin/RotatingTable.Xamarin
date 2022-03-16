using RotatingTable.Xamarin.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class ConfigTests
    {
        [Fact]
        public async Task GetId_ExceptionDeviceInStorage_DefaultResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            var id = await config.GetDeviceIdAsync();

            Assert.Equal(ConfigValidator.DefaultDeviceIdValue, id);
        }

        [Fact]
        public async Task SetAndThenGetDeviceId_ExceptionInStorage_StillValidResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            var value = Guid.NewGuid();
            await config.SetDeviceIdAsync(value);
            var id = await config.GetDeviceIdAsync();

            Assert.Equal(value, id);
        }

        [Fact]
        public async Task SetAndThenGetDeviceId_CorrectStorage_StillValidResult()
        {
            var config = new Config { Storage = new StorageStub() };
            var value = Guid.NewGuid();
            await config.SetDeviceIdAsync(value);
            var id = await config.GetDeviceIdAsync();

            Assert.Equal(value, id);
        }

        [Fact]
        public async Task GetSteps_ExceptionInStorage_DefaultResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            var steps = await config.GetStepsAsync();

            Assert.Equal(ConfigValidator.DefaultStepsValue, steps);
        }

        [Fact]
        public async Task SetAndThenGetSteps_ExceptionInStorage_StillValidResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            await config.SetStepsAsync(5);
            var steps = await config.GetStepsAsync();

            Assert.Equal(5, steps);
        }

        [Fact]
        public async Task SetAndThenGetSteps_CorrectStorage_StillValidResult()
        {
            var config = new Config { Storage = new StorageStub() };
            await config.SetStepsAsync(5);
            var steps = await config.GetStepsAsync();

            Assert.Equal(5, steps);
        }

        [Fact]
        public async Task GetAcceleration_ExceptionInStorage_DefaultResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            var acceleration = await config.GetAccelerationAsync();

            Assert.Equal(ConfigValidator.DefaultAccelerationValue, acceleration);
        }

        [Fact]
        public async Task SetAndThenGetAcceleration_ExceptionInStorage_StillValidResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            await config.SetAccelerationAsync(5);
            var acceleration = await config.GetAccelerationAsync();

            Assert.Equal(5, acceleration);
        }

        [Fact]
        public async Task SetAndThenGetAcceleration_CorrectStorage_StillValidResult()
        {
            var config = new Config { Storage = new StorageStub() };
            await config.SetAccelerationAsync(5);
            var acceleration = await config.GetAccelerationAsync();

            Assert.Equal(5, acceleration);
        }

        [Fact]
        public async Task GetExposure_ExceptionInStorage_DefaultResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            var exposure = await config.GetExposureAsync();

            Assert.Equal(ConfigValidator.DefaultExposureValue, exposure);
        }

        [Fact]
        public async Task SetAndThenGetExposure_ExceptionInStorage_StillValidResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            await config.SetExposureAsync(5);
            var exposure = await config.GetExposureAsync();

            Assert.Equal(5, exposure);
        }

        [Fact]
        public async Task SetAndThenGetExposure_CorrectStorage_StillValidResult()
        {
            var config = new Config { Storage = new StorageStub() };
            await config.SetExposureAsync(5);
            var exposure = await config.GetExposureAsync();

            Assert.Equal(5, exposure);
        }

        [Fact]
        public async Task GetDelay_ExceptionInStorage_DefaultResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            var delay = await config.GetDelayAsync();

            Assert.Equal(ConfigValidator.DefaultDelayValue, delay);
        }

        [Fact]
        public async Task SetAndThenGetDelay_ExceptionInStorage_StillValidResult()
        {
            var config = new Config { Storage = new NotImplementedStorageStub() };
            await config.SetDelayAsync(5);
            var delay = await config.GetDelayAsync();

            Assert.Equal(5, delay);
        }

        [Fact]
        public async Task SetAndThenGetDelay_CorrectStorage_StillValidResult()
        {
            var config = new Config { Storage = new StorageStub() };
            await config.SetDelayAsync(5);
            var delay = await config.GetDelayAsync();

            Assert.Equal(5, delay);
        }
    }
}
