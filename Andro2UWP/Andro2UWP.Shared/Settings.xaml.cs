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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Andro2UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
        }

        private void uiPage_Loaded(object sender, RoutedEventArgs e)
        {
            // uiVersion.Text = p.k.GetAppVers();
            p.k.GetAppVers(uiVersion);
            uiCounter.Text = App.giCurrentNumber.ToString();
            uiDeviceName.Text = App.gsDeviceName;
#if __ANDROID__
            if (string.IsNullOrEmpty(App.gsDeviceName) || App.gsDeviceName == "default")
                uiDeviceName.Text = Android.OS.Build.Model;
#endif
            p.k.GetSettingsBool(uiCreateToasts,"createToasts");
            p.k.GetSettingsBool(uiSortListMode,"sortDescending", true);
            p.k.GetSettingsBool(uiDebugLog, "debugLog");
            uiCreateToasts.IsEnabled = false;   // jako ze jeszcze bez obslugi tego
            //uiHowMany.Text = p.k.GetSettingsInt("howMany", 10).ToString();
        }

        private async void uiPermissAccess_Click(object sender, RoutedEventArgs e)
        {
            if (p.k.GetPlatform("uwp"))
            {
                await p.k.DialogBoxAsync("jakim cudem nacisnales to nie bedac na Androidzie?");
                return;
            }
#if __ANDROID__
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionAccessibilitySettings);
            Uno.UI.BaseActivity.Current.StartActivity(intent);
#endif
        }

        private async void uiPermissBattery_Click(object sender, RoutedEventArgs e)
        {
            if (p.k.GetPlatform("uwp"))
            {
                await p.k.DialogBoxAsync("jakim cudem nacisnales to nie bedac na Androidzie?");
                return;
            }

#if __ANDROID__
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
            Uno.UI.BaseActivity.Current.StartActivity(intent);
#endif
        // prosba o wlaczenie od Android M:
        // https://stackoverflow.com/questions/39256501/check-if-battery-optimization-is-enabled-or-not-for-an-app
        }

        //private async void uiShowRenames_Click(object sender, RoutedEventArgs e)
        //{
        //}

        //private async void uiShowFilters_Click(object sender, RoutedEventArgs e)
        //{
        //}

        private async void uiResetList_Click(object sender, RoutedEventArgs e)
        { // Visibility="Collapsed"
            if (await p.k.DialogBoxYNAsync("Na pewno wyzerowac aktualną listę?"))
            {

            }
        }
        private void uiSave_Click(object sender, RoutedEventArgs e)
        {
            App.giCurrentNumber = int.Parse(uiCounter.Text);
            p.k.SetSettingsInt("currentFileNum", App.giCurrentNumber);
            App.gsDeviceName = uiDeviceName.Text;
            p.k.SetSettingsString("deviceName", App.gsDeviceName);

            p.k.SetSettingsBool(uiCreateToasts, "createToasts");
            p.k.SetSettingsBool(uiSortListMode, "sortDescending");
            p.k.SetSettingsBool(uiDebugLog, "debugLog");
            //p.k.SetSettingsInt("howMany", int.Parse(uiHowMany.Text));

            Frame.GoBack();
        }
    }
}