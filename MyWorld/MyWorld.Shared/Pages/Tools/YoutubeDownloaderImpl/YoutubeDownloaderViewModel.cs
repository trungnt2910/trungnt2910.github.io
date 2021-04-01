using MyWorld.PlatformSpecific;
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

        private bool isSelectorEnabled = true;
        public bool IsSelectorEnabled
        {
            get => isSelectorEnabled;
            set => SetProperty(ref isSelectorEnabled, value);
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
                // HACK: Many video players don't support the av01 codec, so we reject them.
                if (item.VideoCodec.Contains("av01")) continue;
                Qualities.Add(item);
            }

            QualitiesSelectorVisibility = Visibility.Visible;
        }

        public async Task DownloadVideoAsync(IVideoStreamInfo videoStreamInfo, EventHandler callback = null)
        {
            IsSelectorEnabled = false;
            DownloadInfoVisibility = Visibility.Visible;
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

                    const int bufferSize = 128 << 10;

                    var url = videoStreamInfo.Url;

                    // TO-DO: CLEANUP HERE.
                    while (Downloaded < VideoSize)
                    {
                        byte[] buffer = await PlatformSpecific.Http.FetchRange(url, Downloaded, Math.Min(VideoSize, Downloaded + bufferSize) - 1);
                        fileStream.Write(buffer, 0, buffer.Length);
                        Downloaded += buffer.Length;

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
                AudioOnlyStreamInfo audioStreamInfo = (AudioOnlyStreamInfo)currentManifest.GetAudioOnly().Where((info) => info.Container == videoStreamInfo.Container).WithHighestBitrate();

                if (!FFmpeg.IsLoaded())
                {
                    DownloadStatus = "Loading FFmpeg...";
                    await FFmpeg.InitAsync();
                    DownloadStatus = "FFmpeg loaded";
                }

                DownloadStatus = "Creating temporary files...";

                var audio = $"{currentVideo.Id.Value}_audio.{audioStreamInfo.Container}";
                var video = $"{currentVideo.Id.Value}_video.{videoStreamInfo.Container}";
                var output = $"{currentVideo.Id.Value}.{videoStreamInfo.Container}";

                Console.WriteLine($"Temp files: {audio} {video}");

                var audioStream = FFmpeg.GetInputFileStream(audio);
                var videoStream = FFmpeg.GetInputFileStream(video);

                VideoSize = audioStreamInfo.Size.TotalBytes + videoStreamInfo.Size.TotalBytes;
                Downloaded = 0;

                Action<long> incrementDownloaded = (long increment) =>
                {
                    Downloaded += increment;
                    DownloadStatus = ($"Downloaded: {Downloaded}/{VideoSize}");
                    callback?.Invoke(null, null);
                };

                var audioTask = DownloadFileAsync(audioStreamInfo.Url, audioStreamInfo.Size.TotalBytes, audioStream, incrementDownloaded);
                var videoTask = DownloadFileAsync(videoStreamInfo.Url, videoStreamInfo.Size.TotalBytes, videoStream, incrementDownloaded);

                await Task.Factory.ContinueWhenAll(new Task[] { audioTask, videoTask }, (tasks) => { });

                audioStream.Flush(); audioStream.Dispose();
                videoStream.Flush(); videoStream.Dispose();

                DownloadStatus = "Merging audio and video...";

                EventHandler<ProgressChangedEventArgs> progressChangedHandler = (sender, args) =>
                {
                    ProgressBarValue = args.ProgressPercentage;
                    DownloadStatus = $"Merging audio and video: {args.ProgressPercentage}%.";
                    callback?.Invoke(null, null);
                };

                FFmpeg.ProgressChanged += progressChangedHandler;

                var outputStream = await FFmpeg.MergeToFileAsync(audio, video, output);

                FFmpeg.ProgressChanged -= progressChangedHandler;

                DownloadStatus = "Merging done.";

                DownloadStatus = "Saving file...";

                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.FileTypeChoices.Add("Video", new List<string>() { $".{videoStreamInfo.Container}" });
                savePicker.SuggestedFileName = currentVideo.Title;
                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Windows.Storage.CachedFileManager.DeferUpdates(file);

                    var fileStream = await file.OpenStreamForWriteAsync();

                    const int bufferSize = 128 << 10;
                    var buffer = new byte[bufferSize];

                    long totalSize = outputStream.Length;
                    long copied = 0;

                    ProgressBarValue = 0;
                    callback?.Invoke(null, null);

                    while (copied < totalSize)
                    {
                        int read = outputStream.Read(buffer, 0, bufferSize);
                        fileStream.Write(buffer, 0, read);
                        copied += read;
                        ProgressBarValue = (double)((decimal)copied / totalSize * 100);
                        callback?.Invoke(null, null);
                    }

                    fileStream.Dispose();
                    DownloadStatus = "Download completed.";

                    Windows.Storage.Provider.FileUpdateStatus status =
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
            IsSelectorEnabled = true;
        }

        private async Task DownloadFileAsync(string url, long fileSize, Stream fileStream, Action<long> callback = null)
        {
            const int bufferSize = 128 << 10;
            long downloadProgress = 0;
            while (downloadProgress < fileSize)
            {
                byte[] buffer = await Http.FetchRange(url, downloadProgress, Math.Min(VideoSize, downloadProgress + bufferSize) - 1);
                fileStream.Write(buffer, 0, buffer.Length);
                downloadProgress += buffer.Length;
                callback?.Invoke(buffer.Length);
            }
        }
    }
}
