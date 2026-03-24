using System.Collections.Generic;

namespace TheBirdOfHermes
{
    public class AudioLane
    {
        public List<AudioTrack> Tracks { get; } = new List<AudioTrack>();
        public float Volume { get; set; } = 1f;
        public bool IsMuted { get; set; }
    }
}