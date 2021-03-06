namespace SoundFingerprinting.Configuration
{
    using SoundFingerprinting.Strides;

    public class DefaultSpectrogramConfig : SpectrogramConfig
    {
        public DefaultSpectrogramConfig()
        {
            Overlap = 64;
            WdftSize = 2048;
            FrequencyRange = new FrequencyRange { Min = 318, Max = 2000 };
            LogBase = 2;
            LogBins = 32;
            ImageLength = 128;
            UseDynamicLogBase = false;
            NormalizeSignal = false;
            Stride = new IncrementalStaticStride(5115, ImageLength * Overlap);
        }
    }
}