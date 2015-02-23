using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;

namespace ParadoxCraft
{
    [DataContract]
    public class AtmosphereSettings
    {
        public float GroundHeight = 6.36e6f;
        public float TopHeight = 6.42e6f;
        public float HeightLimit = 6.421e6f;

        public Size2 TransmittanceSize = new Size2(256, 64);
        public Size2 SkySize = new Size2(64, 16);

        public int AltitudeResolution = 32;
        public int ViewZenithResolution = 128;
        public int SunZenithResolution = 32;
        public int ViewSunResolution = 32;
    }
}
