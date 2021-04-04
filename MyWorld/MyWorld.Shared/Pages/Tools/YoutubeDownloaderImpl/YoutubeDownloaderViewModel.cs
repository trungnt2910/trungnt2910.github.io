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

        public object Manifest { get => currentManifest; }

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
                Qualities.Add(new VideoDownloadInfo(
                                item, 
                                (AudioOnlyStreamInfo)currentManifest
                                    .GetAudioOnly()
                                    .Where((info) => info.Container == item.Container)
                                    .WithHighestBitrate()));
            }

            var audioList = from item in currentManifest.GetAudioOnly()
                            orderby item.Bitrate descending
                            select item;

            foreach (var item in audioList)
            {
                Qualities.Add(new VideoDownloadInfo(null, item));
            }

            QualitiesSelectorVisibility = Visibility.Visible;
        }

        public async Task DownloadVideoAsync(VideoDownloadInfo downloadInfo, EventHandler callback = null)
        {
            var videoStreamInfo = downloadInfo.Video;
            var audioStreamInfo = downloadInfo.Audio;
            IsSelectorEnabled = false;
            DownloadInfoVisibility = Visibility.Visible;

            Action<long> incrementDownloaded = (long increment) =>
            {
                Downloaded += increment;
                DownloadStatus = ($"Downloaded: {Downloaded}/{VideoSize}");
                callback?.Invoke(null, null);
            };

            Action<double> setProgressBar = (double progress) =>
            {
                ProgressBarValue = progress;
                callback?.Invoke(null, null);
            };

            setProgressBar(0);

            #region Audio Only Downloads
            // Audio-only downloads.
            if (videoStreamInfo == null)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
                savePicker.FileTypeChoices.Add("Audio", new List<string>() { $".mp3" });
                savePicker.SuggestedFileName = currentVideo.Title;
                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Windows.Storage.CachedFileManager.DeferUpdates(file);

                    if (!FFmpeg.IsLoaded())
                    {
                        DownloadStatus = "Loading FFmpeg...";
                        await FFmpeg.InitAsync();
                        DownloadStatus = "FFmpeg loaded";
                    }

                    Downloaded = 0;
                    VideoSize = audioStreamInfo.Size.TotalBytes;

                    string audioName = $"{currentVideo.Id.Value}.{audioStreamInfo.Container}";

                    DownloadStatus = "Downloading...";

                    var inputStream = FFmpeg.OpenInputFile(audioName);
                    await DownloadFileAsync(audioStreamInfo.Url, VideoSize, inputStream, incrementDownloaded);
                    inputStream.Dispose();

                    DownloadStatus = "Downloaded.";

                    DownloadStatus = "Converting audio...";

                    EventHandler<ProgressChangedEventArgs> progressChangedHandler = (sender, args) =>
                    {
                        ProgressBarValue = args.ProgressPercentage;
                        DownloadStatus = $"Converting audio: {args.ProgressPercentage}%.";
                        callback?.Invoke(null, null);
                    };

                    setProgressBar(0);
                    FFmpeg.ProgressChanged += progressChangedHandler;
                    var outputStream = await FFmpeg.RunAsync("-i", audioName, "-c:v", "copy", "-c:a", "libmp3lame", "-q:a", "4", "output.mp3");
                    FFmpeg.ProgressChanged -= progressChangedHandler;

                    DownloadStatus = "Audio converted.";

                    DownloadStatus = "Saving file...";

                    setProgressBar(0);

                    await StreamToFileAsync(outputStream, outputStream.Length, file, setProgressBar);
                    outputStream.Dispose();

                    FFmpeg.DeleteFile(audioName);
                    FFmpeg.DeleteFile("output.mp3");

                    DownloadStatus = "Download completed.";

                    Windows.Storage.Provider.FileUpdateStatus status =
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }

            }
            #endregion
            // Video downloads
            else
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                // FIXME: .mp4, .com, .exe, .bin
                savePicker.FileTypeChoices.Add("Video", new List<string>() { $".{videoStreamInfo.Container}" });
                savePicker.SuggestedFileName = currentVideo.Title;
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                // Muxed
                #region Muxed Video Download
                if (videoStreamInfo is MuxedStreamInfo)
                {
                    Windows.Storage.CachedFileManager.DeferUpdates(file);

                    var fileStream = await file.OpenStreamForWriteAsync();

                    Downloaded = 0;
                    VideoSize = videoStreamInfo.Size.TotalBytes;

                    DownloadStatus = "Downloading...";
                    await DownloadFileAsync(videoStreamInfo.Url, videoStreamInfo.Size.TotalBytes, fileStream, incrementDownloaded);
                    fileStream.Dispose();

                    DownloadStatus = "Download completed.";

                    Windows.Storage.Provider.FileUpdateStatus status =
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }
                #endregion
                // Video only, have to mix ourselves.
                else
                #region Video Only Download
                {
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

                    var audioStream = FFmpeg.OpenInputFile(audio);
                    var videoStream = FFmpeg.OpenInputFile(video);

                    VideoSize = audioStreamInfo.Size.TotalBytes + videoStreamInfo.Size.TotalBytes;
                    Downloaded = 0;

                    DownloadStatus = "Downloading...";

                    var audioTask = DownloadFileAsync(audioStreamInfo.Url, audioStreamInfo.Size.TotalBytes, audioStream, incrementDownloaded);
                    var videoTask = DownloadFileAsync(videoStreamInfo.Url, videoStreamInfo.Size.TotalBytes, videoStream, incrementDownloaded);
                    await Task.Factory.ContinueWhenAll(new Task[] { audioTask, videoTask }, (tasks) => { });

                    audioStream.Dispose();
                    videoStream.Dispose();

                    DownloadStatus = "Merging audio and video...";

                    EventHandler<ProgressChangedEventArgs> progressChangedHandler = (sender, args) =>
                    {
                        ProgressBarValue = args.ProgressPercentage;
                        DownloadStatus = $"Merging audio and video: {args.ProgressPercentage}%.";
                        callback?.Invoke(null, null);
                    };

                    setProgressBar(0);
                    FFmpeg.ProgressChanged += progressChangedHandler;
                    var outputStream = await FFmpeg.MergeToFileAsync(audio, video, output);
                    FFmpeg.ProgressChanged -= progressChangedHandler;

                    DownloadStatus = "Merging done.";

                    DownloadStatus = "Saving file...";

                    setProgressBar(0);

                    await StreamToFileAsync(outputStream, outputStream.Length, file, setProgressBar);

                    outputStream.Dispose();

                    FFmpeg.DeleteFile(audio);
                    FFmpeg.DeleteFile(video);
                    FFmpeg.DeleteFile(output);

                    DownloadStatus = "Download completed.";

                    Windows.Storage.Provider.FileUpdateStatus status =
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                }
                #endregion
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

        private async Task StreamToFileAsync(Stream source, long streamSize, StorageFile file, Action<double> callback = null)
        {
            Stream destination = await file.OpenStreamForWriteAsync();
            const int bufferSize = 128 << 10;
            byte[] buffer = new byte[bufferSize];
            long progress = 0;
            while (progress < streamSize)
            {
                int bytesRead = source.Read(buffer, 0, bufferSize);
                destination.Write(buffer, 0, bytesRead);
                progress += bytesRead;
                // Only decimal is precise enough to hold a long value.
                callback?.Invoke((double)((decimal)progress / (decimal)streamSize * 100));
            }
            destination.Dispose();
        }
    }
}
