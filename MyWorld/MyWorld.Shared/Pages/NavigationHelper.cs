using System;
using System.Collections.Generic;
using System.Text;

namespace MyWorld.Pages
{
    public static class NavigationHelper
    {
        private static Dictionary<string, Type> PageMap = new Dictionary<string, Type>
        {
            { "Home", typeof(HomePage) },
            { "Stories", typeof(StoriesPage) },
            { "Stories/EarlyLife", typeof(Stories.EarlyLife) },
            { "Tools/CodeHashifier", typeof(Tools.CodeHashifier) }
        };

        public static bool NavigateToLocation(this Windows.UI.Xaml.Controls.Frame frame, string location)
        {
            try
            {
                return frame.Navigate(PageMap[location]);
            }
            catch (KeyNotFoundException)
            {
                return frame.Navigate(typeof(NotAvailablePage));
            }
        }
    }
}
