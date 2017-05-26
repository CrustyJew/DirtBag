using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtbagWebservice.Models {
    public enum VideoProvider {
        Unknown = 0,
        YouTube = 1,
        Vimeo = 2,
        VidMe = 3, //*
        DailyMotion = 4,
        Instagram = 5,
        SoundCloud = 6,
        Streamable = 7, //*
        Twitch = 8,
        Imgur = 9,
        Twitter = 10
    }
}
