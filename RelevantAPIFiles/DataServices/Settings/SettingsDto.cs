using System;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Settings
{
    public class SettingsDto
    {
        public int PassMark { get; set; }
        public double TimeToRoll { get; set; }
        public double TimeToForceNextRoll { get; set; }
        public string MinimumAcceptableApiVersion { get; set; }
        public DateTime EnabledAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
