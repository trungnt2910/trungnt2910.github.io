using MyWorld.Pages.Tools.YoutubeDownloaderImpl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YoutubeExplode.Videos.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MyWorld.Pages.Tools
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class YoutubeDownloader : Page
    {
        public YoutubeDownloaderViewModel ViewModel { get; set; } = new YoutubeDownloaderViewModel();

        public YoutubeDownloader()
        {
            this.InitializeComponent();
        }

        private async void UrlTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ViewModel.ShowInfoAsync(UrlTextBox.Text);
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.DownloadVideoAsync(QualitySelector.SelectedItem as IVideoStreamInfo, DownloadProgressChanged);
        }

        private async void DownloadProgressChanged(object sender, EventArgs args)
        {
            await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => DownloadProgressBar.Value = ViewModel.ProgressBarValue);
        }
    }
}
