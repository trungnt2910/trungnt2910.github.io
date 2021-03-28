using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MyWorld.Pages.Tools.YoutubeDownloaderImpl
{
    [Bindable(true)]
    public class YoutubeDownloaderViewModel : BindableBase
    {
        private string title = string.Empty;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private string author = string.Empty;
        public string Author
        {
            get => author;
            set => SetProperty(ref author, value);
        }

        private DateTimeOffset uploadDate;
        public DateTimeOffset UploadDate
        {
            get => uploadDate;
            set => SetProperty(ref uploadDate, value);
        }

        private TimeSpan duration;
        public TimeSpan Duration
        {
            get => duration;
            set => SetProperty(ref duration, value);
        }

        private Visibility infoVisibility = Visibility.Collapsed;
        public Visibility InfoVisibility
        {
            get => infoVisibility;
            set => SetProperty(ref infoVisibility, value);
        }

        private Visibility loadingVisibility = Visibility.Collapsed;
        public Visibility LoadingVisibility
        {
            get => loadingVisibility;
            set => SetProperty(ref loadingVisibility, value);
        }

        private Visibility qualitiesSelectorVisibility = Visibility.Collapsed;
        public Visibility QualitiesSelectorVisibility
        {
            get => qualitiesSelectorVisibility;
            set => SetProperty(ref qualitiesSelectorVisibility, value);
        }

        private Visibility downloadInfoVisibility = Visibility.Collapsed;
        public Visibility DownloadInfoVisibility
        {
            get => downloadInfoVisibility;
            set => SetProperty(ref downloadInfoVisibility, value);
        }

        private string thumbnailUrl = "ms-appx:///Assets/StoreLogo.png";
        public string ThumbnailUrl
        {
            get => thumbnailUrl;
            set => SetProperty(ref thumbnailUrl, value);
        }

        private long videoSize = 0;
        public long VideoSize
        {
            get => videoSize;
            set => SetProperty(ref videoSize, value);
        }

        private long downloaded = 0;
        public long Downloaded
        {
            get => downloaded;
            set
            {
                SetProperty(ref downloaded, value);
                if (videoSize != 0)
                    ProgressBarValue = (double)((decimal)downloaded / (decimal)(videoSize) * 100);
            }
        }

        private string downloadStatus;
        public string DownloadStatus
        {
            get => downloadStatus;
            set => SetProperty(ref downloadStatus, value);
        }

        private double progressBarValue;
        public double ProgressBarValue
        {
            get => progressBarValue;
            set => SetProperty(ref progressBarValue, value);
        }

        public Visibility Negate(Visibility visibility) => (visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;

        private ObservableCollection<object> qualities = new ObservableCollection<object>() { "Placeholder1", "Placeholder2" };
        public ObservableCollection<object> Qualities { get => qualities; }

        private YoutubeClient client = new YoutubeClient(PlatformSpecific.Http.DefaultClient);

        private StreamManifest currentManifest;
        private Video currentVideo;

        public async Task ShowInfoAsync(string Url)
        {
            DownloadInfoVisibility = Visibility.Collapsed;
            InfoVisibility = Visibility.Collapsed;
            LoadingVisibility = Visibility.Visible;
            var video = await client.Videos.GetAsync(Url);

            Title = video.Title;
            Author = video.Author;
            Duration = video.Duration;
            UploadDate = video.UploadDate;
            ThumbnailUrl = video.Thumbnails.StandardResUrl;

            Qualities.Clear();

            currentVideo = video;

            currentManifest = await client.Videos.Streams.GetManifestAsync(video.Id);

            QualitiesSelectorVisibility = Visibility.Collapsed;
            LoadingVisibility = Visibility.Collapsed;
            InfoVisibility = Visibility.Visible;

            var videoList = from item in currentManifest.GetVideo()
                            orderby item.VideoQuality descending
                            select item;

            foreach (var item in videoList)
            {
                Qualities.Add(item);
            }

            QualitiesSelectorVisibility = Visibility.Visible;
        }

        public async Task DownloadVideoAsync(IVideoStreamInfo videoStreamInfo, EventHandler callback = null)
        {
            if (videoStreamInfo is MuxedStreamInfo)
            {
                System.Diagnostics.Debug.WriteLine(videoStreamInfo.Url);
                System.Diagnostics.Debug.WriteLine(videoStreamInfo.Container);
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                savePicker.FileTypeChoices.Add("Video", new List<string>() { $".{videoStreamInfo.Container}" });
                savePicker.SuggestedFileName = currentVideo.Title;
                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Windows.Storage.CachedFileManager.DeferUpdates(file);

                    var fileStream = await file.OpenStreamForWriteAsync();

                    Downloaded = 0;
                    VideoSize = videoStreamInfo.Size.TotalBytes;

                    DownloadInfoVisibility = Visibility.Visible;

                    const int bufferSize = 128 << 10;

                    var url = videoStreamInfo.Url;

                    while (Downloaded < VideoSize)
                    {
                        byte[] buffer = await PlatformSpecific.Http.FetchRange(url, downloaded, Math.Min(VideoSize, Downloaded + bufferSize) - 1);
                        fileStream.Write(buffer, 0, buffer.Length);
                        downloaded = Math.Min(VideoSize, Downloaded + bufferSize);
                        DownloadStatus = ($"Downloaded: {Downloaded}/{VideoSize}");
                        callback?.Invoke(null, null);
                    }

                    fileStream.Dispose();

                    DownloadStatus = "Download completed.";

                    Windows.Storage.Provider.FileUpdateStatus status =
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
            else
            {
                var audio = Path.GetTempFileName();
                var video = Path.GetTempFileName();

                AudioOnlyStreamInfo audioInfo = (AudioOnlyStreamInfo)currentManifest.GetAudioOnly().WithHighestBitrate();

                var audioTask = client.Videos.Streams.DownloadAsync(audioInfo, audio);
                var videoTask = client.Videos.Streams.DownloadAsync(videoStreamInfo, video);

                await audioTask;
                await videoTask;


            }
        }
    }
}
