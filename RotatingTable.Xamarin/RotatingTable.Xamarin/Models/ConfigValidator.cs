using System;

namespace RotatingTable.Xamarin.Models
{
    /// <summary>
    /// Validates config values: steps, acceleration, delay and exposure.
    /// Validation corresponds to validation in sketches for rotating table,
    /// see for example class Settings here:
    /// https://github.com/andrey-val-rodin/RotatingTable.Arduino/blob/main/Program/400/400.ino
    /// </summary>
    public static class ConfigValidator
    {
        public static readonly int[] StepValues =
            { 2, 4, 5, 6, 8, 9, 10, 12, 15, 18, 20, 24, 30, 36, 40, 45, 60, 72, 90, 120, 180, 360 };

        public static Guid DefaultDeviceIdValue = Guid.Empty;
        public const int DefaultStepsValue = 24;
        public const int DefaultAccelerationValue = 7;
        public const int DefaultDelayValue = 0;
        public const int DefaultExposureValue = 100;
        public const int DefaultVideoPWMValue = 100;
        public const float DefaultNonstopFrequencyValue = 0.5F;

        public static bool IsStepsValid(int value)
        {
            return Array.Exists(StepValues, e => e == value);
        }

        public static int ValidateSteps(int value)
        {
            return IsStepsValid(value)
                ? value
                : DefaultStepsValue;
        }

        public static bool IsAccelerationValid(int value)
        {
            return 1 <= value && value <= 10;
        }
        public static int ValidateAcceleration(int value)
        {
            return IsAccelerationValid(value)
                ? value
                : DefaultAccelerationValue;
        }

        public static bool IsDelayValid(int value)
        {
            return value <= 5000 && value % 100 == 0;
        }

        public static int ValidateDelay(int value)
        {
            return IsDelayValid(value)
                ? value
                : DefaultDelayValue;
        }

        public static bool IsExposureValid(int value)
        {
            return 100 <= value && value <= 500 && value % 100 == 0;
        }

        public static int ValidateExposure(int value)
        {
            return IsExposureValid(value)
                ? value
                : DefaultExposureValue;
        }

        public static bool IsVideoPWMValid(int value)
        {
            return -255 <= value && value <= 255;
        }

        public static bool IsNonstopFrequencyValid(float value)
        {
            return 0.5F <= value && value <= 3.0F;
        }
    }
}
