using System;
using System.Collections.Generic;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MyWorld
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            ContentFrame.Navigate(typeof(Pages.HomePage));
        }

        private void NavigationView_PaneClosed(Microsoft.UI.Xaml.Controls.NavigationView sender, object args)
        {
            InfoFooter.Visibility = Visibility.Collapsed;
        }

        private void NavigationView_PaneOpening(Microsoft.UI.Xaml.Controls.NavigationView sender, object args)
        {
            InfoFooter.Visibility = Visibility.Visible;
        }

        #region MainPage
        private void Home_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.HomePage));
        }

        private void Stories_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.StoriesPage));
        }

        private void Activities_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Tools_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.ToolsPage));
        }
        #endregion

        #region Stories
        private void Stories_EarlyLife_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.Stories.EarlyLife));
        }

        private void Stories_CompetitiveProgramming_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.Stories.CompetitiveProgramming));
        }
        #endregion

        #region Tools
        private void Tools_CodeHashifier_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.Tools.CodeHashifier));
        }

        private void YoutubeDownloader_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Pages.Tools.YoutubeDownloader));
        }
        #endregion
    }
}
