using System;
using System.Drawing;

namespace Cele
{
    public class Track
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int TrackNumber { get; set; }
        public TimeSpan Duration { get; set; }
        public Image Cover { get; set; }
    }
}