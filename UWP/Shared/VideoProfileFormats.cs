using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum VideoProfileFormats
{
    Legacy2272x1478x30,
    Legacy2272x1478x15,
    Legacy896x504x30,
    Legacy896x504x15,
    BalancedVideoAndPhoto2272x1278x30,
    BalancedVideoAndPhoto2272x1278x15,
    BalancedVideoAndPhoto896x504x30,
    BalancedVideoAndPhoto896x504x15,
    //VideoConferencing1952x1100x60, seems to be unsupported
    VideoConferencing1952x1100x30,
    VideoConferencing1952x1100x15,
    VideoConferencing1920x1080x30,
    VideoConferencing1920x1080x15,
    VideoConferencing1504x846x60,
    VideoConferencing1504x846x30,
    VideoConferencing1504x846x15,
    VideoConferencing1504x846x5
}
/*Legacy2272x1478x30,
Legacy2272x1478x15,
Legacy896x504x30,
Legacy896x504x15,*/