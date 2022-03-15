﻿using System;
using System.Threading.Tasks;

namespace RotatingTable.Xamarin.Models
{
    public interface IConfig
    {
        Task<Guid> GetDeviceIdAsync();
        Task SetDeviceIdAsync(Guid id);
        Task<int> GetStepsAsync();
        Task SetStepsAsync(int steps);
        Task<int> GetAccelerationAsync();
        Task SetAccelerationAsync(int acceleration);
        Task<int> GetDelayAsync();
        Task SetDelayAsync(int delay);
        Task<int> GetExposureAsync();
        Task SetExposureAsync(int exposure);
    }
}