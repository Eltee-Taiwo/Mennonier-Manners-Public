using System.Collections.Concurrent;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Participant
{
    public static class Writers
    {
        public static ConcurrentDictionary<string, string> CurrentWriters { get; set; } 

        static Writers()
        {
            CurrentWriters = new ConcurrentDictionary<string, string>();
        }
    }
}
