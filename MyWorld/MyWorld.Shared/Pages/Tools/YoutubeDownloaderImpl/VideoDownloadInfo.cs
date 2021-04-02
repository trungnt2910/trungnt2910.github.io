using System;
using System.Collections.Generic;
using System.Text;
using YoutubeExplode.Videos.Streams;

namespace MyWorld.Pages.Tools.YoutubeDownloaderImpl
{
    public class VideoDownloadInfo
    {
        public IVideoStreamInfo Video { get; set; }
        public IAudioStreamInfo Audio { get; set; }
        public VideoDownloadInfo(IVideoStreamInfo video, IAudioStreamInfo audio)
        {
            Video = video;
            Audio = audio;
        }
        public override string ToString()
        {
            if (Video == null)
            {
                return $"mp3, {Audio.Bitrate}, {Audio.Size}";
            }
            string quality = Video.VideoQualityLabel;
            long size = 0;
            string container = Video.Container.ToString();

            if (Video is MuxedStreamInfo)
            {
                size = Video.Size.TotalBytes;
            }
            else if (Video is VideoOnlyStreamInfo)
            {
                size = Video.Size.TotalBytes + Audio.Size.TotalBytes;
            }

            return $"{container}, {quality}, {new FileSize(size)}";
        }
    }
}
