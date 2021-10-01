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
    public sealed partial class AppLog : Page
    {
        public AppLog()
        {
            this.InitializeComponent();
        }

        //private void ProgresywnyRing(bool sStart)
        //{
        //    if (sStart)
        //    {
        //        double dVal;
        //        dVal = Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth) / 2;
        //        uiProcesuje.Width = dVal;
        //        uiProcesuje.Height = dVal;

        //        uiProcesuje.Visibility = Visibility.Visible;
        //        uiProcesuje.IsActive = true;
        //    }
        //    else
        //    {
        //        uiProcesuje.IsActive = false;
        //        uiProcesuje.Visibility = Visibility.Collapsed;
        //    }
        //}



        private async void ReloadLogFile()
        {
            //ProgresywnyRing(true);
            p.k.ProgRingShow(true);
            var plik = await App.GetLogFile();
            if (plik != null)
            {
                string logText = await plik.ReadAllTextAsync(); // Windows.Storage.FileIO.ReadTextAsync(plik);
                uiLog.Text = logText;
            }

            // ProgresywnyRing(false);
            p.k.ProgRingShow(false);
        }
        private void uiPage_Loaded(object sender, RoutedEventArgs e)
        {
            p.k.ProgRingInit(true, false);
            ReloadLogFile();
        }

        private void uiOk_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void uiReload_Click(object sender, RoutedEventArgs e)
        {
            ReloadLogFile();
        }
        private async void uiClearLog_Click(object sender, RoutedEventArgs e)
        {
            if(await p.k.DialogBoxResYNAsync("msgSureDeleteLog"))
            {

                if(await p.k.DialogBoxResYNAsync("msgSendLogEmail"))
                {
                    try
                    {
                        Windows.ApplicationModel.Email.EmailMessage oMsg = new Windows.ApplicationModel.Email.EmailMessage();
                        oMsg.Subject = "Log Andro2UWP, " + DateTime.Now.ToString("yyyy.MM.dd HH:mm");
                        oMsg.Body = uiLog.Text;
                        await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(oMsg);
                    }
                    catch
                    {
                        p.k.DialogBoxRes("msgComposingError");
                    }
                }

                var plik = await App.GetLogFile();
                if (plik != null) await plik.DeleteAsync();
                uiLog.Text = "";
            }
        }
    }
}
