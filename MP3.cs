using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareLib5
{

    public class MP3
    {

        TagLib.File mp3Tag;

        public bool valid
        {
            get
            {
                return mp3Tag != null;
            }
        }

        public MP3(String FileName)
        {
            try
            {
                mp3Tag = TagLib.File.Create(FileName);
            }
            catch { }
        }

        public TimeSpan TimeDuration()
        {
            if (!valid)
                return new TimeSpan();
            return mp3Tag.Properties.Duration;
        }
    }

}
