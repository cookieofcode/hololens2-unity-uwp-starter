using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using CSharp.Shared;
#endif

public class VideoProfileFormatService
{
    public VideoProfileFormatService()
    {

    }

#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
    public class VideoProfileFormat
    {
        public bool Legacy;
        public KnownVideoProfile Profile;
        public uint Width;
        public uint Height;
        public uint FrameRate;

        public VideoProfileFormat(KnownVideoProfile profile, uint width, uint height, uint frameRate, bool legacy)
        {
            Legacy = legacy;
            Profile = profile;
            Width = width;
            Height = height;
            FrameRate = frameRate;
        }
    }

    public VideoProfileFormat GetVideoProfileFormat(VideoProfileFormats videoProfileFormats)
    {
        bool legacy = false;
        KnownVideoProfile profile;
        uint width;
        uint height;
        uint frameRate;
        switch (videoProfileFormats)
        {
            case VideoProfileFormats.Legacy2272x1478x30:
                profile = KnownVideoProfile.HighFrameRate; // Profile will be ignored using legacy mode
                legacy = true;
                width = 2272;
                height = 1478;
                frameRate = 30;
                break;
            case VideoProfileFormats.Legacy2272x1478x15:
                profile = KnownVideoProfile.HighFrameRate; // Profile will be ignored using legacy mode
                legacy = true;
                width = 2272;
                height = 1478;
                frameRate = 15;
                break;
            case VideoProfileFormats.Legacy896x504x30:
                profile = KnownVideoProfile.HighFrameRate; // Profile will be ignored using legacy mode
                legacy = true;
                width = 896;
                height = 504;
                frameRate = 30;
                break;
            case VideoProfileFormats.Legacy896x504x15:
                profile = KnownVideoProfile.HighFrameRate; // Profile will be ignored using legacy mode
                legacy = true;
                width = 896;
                height = 504;
                frameRate = 30;
                break;
            case VideoProfileFormats.BalancedVideoAndPhoto2272x1278x30:
                profile = KnownVideoProfile.BalancedVideoAndPhoto;
                width = 2272;
                height = 1278;
                frameRate = 30;
                break;
            case VideoProfileFormats.BalancedVideoAndPhoto2272x1278x15:
                profile = KnownVideoProfile.BalancedVideoAndPhoto;
                width = 2272;
                height = 1278;
                frameRate = 15;
                break;
            case VideoProfileFormats.BalancedVideoAndPhoto896x504x30:
                profile = KnownVideoProfile.BalancedVideoAndPhoto;
                width = 896;
                height = 504;
                frameRate = 30;
                break;
            case VideoProfileFormats.BalancedVideoAndPhoto896x504x15:
                profile = KnownVideoProfile.BalancedVideoAndPhoto;
                width = 896;
                height = 504;
                frameRate = 15;
                break;
            case VideoProfileFormats.VideoConferencing1920x1080x30:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1920;
                height = 1080;
                frameRate = 30;
                break;
            /*case VideoProfileFormats.VideoConferencing1952x1100x60:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1952;
                height = 1100;
                frameRate = 60;
                break;*/
            case VideoProfileFormats.VideoConferencing1952x1100x30:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1952;
                height = 1100;
                frameRate = 30;
                break;
            case VideoProfileFormats.VideoConferencing1952x1100x15:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1952;
                height = 1100;
                frameRate = 15;
                break;
            case VideoProfileFormats.VideoConferencing1920x1080x15:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1920;
                height = 1080;
                frameRate = 15;
                break;
            case VideoProfileFormats.VideoConferencing1504x846x60:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1504;
                height = 846;
                frameRate = 60;
                break;
            case VideoProfileFormats.VideoConferencing1504x846x30:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1504;
                height = 846;
                frameRate = 30;
                break;
            case VideoProfileFormats.VideoConferencing1504x846x15:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1504;
                height = 846;
                frameRate = 15;
                break;
            case VideoProfileFormats.VideoConferencing1504x846x5:
                profile = KnownVideoProfile.VideoConferencing;
                width = 1504;
                height = 846;
                frameRate = 5;
                break;
            default:
                Debug.LogError("Provided Format have no defined parameters.");
                return null;
        }
        return new VideoProfileFormat(profile, width, height, frameRate, legacy);
    }
#endif
}
