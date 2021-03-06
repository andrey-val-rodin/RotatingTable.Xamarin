namespace RotatingTable.Xamarin.Models
{
    public static class Commands
    {
        public const string Status          = "STATUS";
        public const string Ready           = "READY";
        public const string Running         = "RUNNING";
        public const string Busy            = "BUSY";
        public const string End             = "END";
        public const string OK              = "OK";
        public const string Error           = "ERR";
        public const string GetSteps        = "GET STEPS";
        public const string Step            = "STEP ";
        public const string GetAcceleration = "GET ACC";
        public const string GetExposure     = "GET EXP";
        public const string GetDelay        = "GET DELAY";
        public const string GetVideoPWM     = "GET VPWM";
        public const string GetNFrequency   = "GET NFREQ";
        public const string SetAcceleration = "SET ACC";
        public const string SetSteps        = "SET STEPS";
        public const string SetExposure     = "SET EXP";
        public const string SetDelay        = "SET DELAY";
        public const string SetVideoPWM     = "SET VPWM";
        public const string SetNFrequency   = "SET NFREQ";
        public const string Position        = "POS ";
        public const string GetMode         = "GET MODE";
        public const string RunAutoMode     = "RUN AUTO";
        public const string RunManualMode   = "RUN MANUAL";
        public const string RunNonStopMode  = "RUN NS";
        public const string RunVideoMode    = "RUN VIDEO";
        public const string RunFreeMovement = "RUN FM";
        public const string FreeMovement    = "FM ";
        public const string Shutter         = "SHUTTER";
        public const string Next            = "NEXT";
        public const string Stop            = "STOP";
        public const string SoftStop        = "SOFTSTOP";
        public const string IncreasePWM     = "INCPWM";
        public const string DecreasePWM     = "DECPWM";
        public const string Undefined       = "UNDEF";
    }
}
