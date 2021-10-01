using System;
using System.Collections.Generic;
using System.Text;

/*

    ### zakładam już poprawne Uno własne, bez resetu funkcjonalnego

2019.10.25
* Clipboard dla UWP idzie jak poprzednio, dla UWP - bez NuGet (żeby nie trzeba było dodawać Reference jak app jest tylko UWP)
* nowa funkcja: DialogBoxInput (odkomentowana)
* nowa funkcja: SetSettingsInt(double) - z konwersją (bo C# samo nie konwertuje)
  
2019.09.10
 * nowa funkcja: GetPlatform (android, uwp, ios, wasm, other) - także jako bool, int, string
    
2019.09.03
 * dodałem MakeToast (dzięki Nugetowi)
 * .. dzięki czemu dodałem CrashMessageAdd
 * dodałem CrashMessageExit
    
2019.08.31
 * włączyłem pełną code analysis, i dodałem:
    * .ConfigureAwait(true) [czyli default, można byłoby zablokować ten warning]
    * ToString i int.Parse (i podobne): cultureinvariant, albo currentculture
    * trochę testów na null 

 2019.08.27
* nowa funkcja: string GetAppVers() - działa teoretycznie dla UWP, Android oraz iOS
* przerobienie ClipBoard na wersje uniwersalną (wymaga Nugeta)
* IsMobile: UWP jak do tej pory, macOS zawsze NIE, reszta (Android, iOS, WASM) zawsze TAK
* zmiana sposobu uzyskiwania nazwy komputera (może za to uniwersalna)

 2019.08.26
* migracja do VC
* wykomentowanie tego co nie jest crossplatform (przerzucenie tego do pkarmodule.cs w UWP)

*/


/*
 
    Wersje minimalne:

    UWP:
        Lumia 532: 15063.1805, 1703
        Lumia 650: 15254.582, 1709
        Lumia Aska: 14393, 1607
        Lumia mama: 15063, 1703
    Android:
        Aśki tablet: Android 6.0, Marshmallow, API level 23

*/

// do Strings:
// "errAnyError", resDlgYes, resDlgNo

namespace p 
{
    public static partial class k
    {
        #region "CrashMessage"

        public async static System.Threading.Tasks.Task CrashMessageShow()
        {
            string sTxt = GetSettingsString("appFailData");
            if (string.IsNullOrEmpty(sTxt))
                return;
            await DialogBox("Fail message:\n" + sTxt).ConfigureAwait(true);
            SetSettingsString("appFailData", "");
        }

        public static void CrashMessageAdd(string sTxt, string exMsg)
        {
            string sAdd = DateTime.Now.ToString("HH:mm:ss") + " " + sTxt + "\n" + exMsg + "\n";
            /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElseDirectiveTrivia */
            if (GetSettingsBool("crashShowToast"))
                MakeToast(sAdd);
            /* TODO ERROR: Skipped EndIfDirectiveTrivia */
            SetSettingsString("appFailData", GetSettingsString("appFailData") + sAdd);
        }

        public static void CrashMessageAdd(string sTxt, Exception ex)
        {
            string sMsg = ex.Message;
            if (!string.IsNullOrEmpty(ex.StackTrace))
                sMsg = sMsg + "\n" + ex.StackTrace;

            CrashMessageAdd(sTxt, sMsg);
        }

        public static void CrashMessageExit(string sTxt, string exMsg)
        {
            CrashMessageAdd(sTxt, exMsg);
#if NETFX_CORE || __ANDROID__
            Windows.UI.Xaml.Application.Current.Exit();
            // Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif __IOS__
            System.Threading.Thread.CurrentThread.Abort();
#endif
        }
        #endregion

        #region "Clipboard"
        // -- CLIPBOARD ---------------------------------------------
    
        public static void ClipPut(string sTxt)
        {
            // WYMAGA https://github.com/stavroskasidis/XamarinClipboardPlugin

            Windows.ApplicationModel.DataTransfer.DataPackage oClipCont = new Windows.ApplicationModel.DataTransfer.DataPackage();
            oClipCont.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            oClipCont.SetText(sTxt);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(oClipCont);

        }

        public async static System.Threading.Tasks.Task<string> ClipGet()
        {
            Windows.ApplicationModel.DataTransfer.DataPackageView oClipCont = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent(); 
            return await oClipCont.GetTextAsync(); 
        }

#if false
#if __ANDROID__ || __IOS__
            return await Plugin.Clipboard.CrossClipboard.Current.GetTextAsync().ConfigureAwait(true);
#elif __WASM__
            return "WASM vers";
#elif __MACOS__
            return "macOS vers";
#else
            return "unkn vers";
#endif
#endif 

#endregion

#region "Get/Set Settings"
        // -- Get/Set Settings ---------------------------------------------

#region "string"

        // odwołanie się do zmiennych
        public static string GetSettingsString(string sName, string sDefault = "")
        {
            string sTmp;
            sTmp = sDefault;

            //if (Acr.Settings.CrossSettings.Current.Contains(sName))
            //    sTmp = Acr.Settings.CrossSettings.Current.Get<string>(sName);
            if (Windows.Storage.ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName))
                sTmp = Windows.Storage.ApplicationData.Current.RoamingSettings.Values[sName].ToString();
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(sName))
                sTmp = Windows.Storage.ApplicationData.Current.LocalSettings.Values[sName].ToString();

            return sTmp;
        }

        public static void SetSettingsString(string sName, string sValue, bool bRoam)
        {
            if (bRoam)
            {
                Windows.Storage.ApplicationData.Current.RoamingSettings.Values[sName] = sValue;
                //Acr.Settings.CrossSettings.Current.SetValue(sName, sValue);
            }
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[sName] = sValue;
        }

        // obsługa ekranowa i inne typ podobne
        public static string GetSettingsString(Windows.UI.Xaml.Controls.TextBlock oTBox, string sName, string sDefault = "")
        {
            if (oTBox is null) return "";
            string sTmp = GetSettingsString(sName, sDefault);
            oTBox.Text = sTmp;
            return sTmp;
        }

        public static string GetSettingsString(Windows.UI.Xaml.Controls.TextBox oTBox, string sName, string sDefault = "")
        {
            if (oTBox is null) return "";
            string sTmp = GetSettingsString(sName, sDefault);
            oTBox.Text = sTmp;
            return sTmp;
        }



        public static void SetSettingsString(string sName, string sValue)
        {
            SetSettingsString(sName, sValue, false);
        }


        public static void SetSettingsString(string sName, Windows.UI.Xaml.Controls.TextBox sValue, bool bRoam)
        {
            if (sValue is null) return;
            SetSettingsString(sName, sValue.Text, bRoam);
        }

        public static void SetSettingsString(string sName, Windows.UI.Xaml.Controls.TextBox sValue)
        {
            if (sValue is null) return;
            SetSettingsString(sName, sValue.Text, false);
        }

#endregion

        public static int GetSettingsInt(string sName, int iDefault = 0)
        {
            int sTmp;

            sTmp = iDefault;

            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (withBlock.RoamingSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToInt32(withBlock.RoamingSettings.Values[sName].ToString(),System.Globalization.CultureInfo.InvariantCulture);
                if (withBlock.LocalSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToInt32(withBlock.LocalSettings.Values[sName].ToString(),System.Globalization.CultureInfo.InvariantCulture);
            }

            return sTmp;
        }

        public static void SetSettingsInt(string sName, int sValue)
        {
            SetSettingsInt(sName, sValue, false);
        }

        public static void SetSettingsInt(string sName, double dValue)
        {
            SetSettingsInt(sName, (int)dValue, false);
        }

        public static void SetSettingsInt(string sName, double dValue, bool bRoam)
        {
            SetSettingsInt(sName, (int)dValue, bRoam);
        }

        public static void SetSettingsInt(string sName, int sValue, bool bRoam)
        {
            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (bRoam)
                    withBlock.RoamingSettings.Values[sName] = sValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                withBlock.LocalSettings.Values[sName] = sValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        public static bool GetSettingsBool(string sName, bool iDefault = false)
        {
            bool sTmp;

            sTmp = iDefault;
            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (withBlock.RoamingSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToBoolean(withBlock.RoamingSettings.Values[sName].ToString(),System.Globalization.CultureInfo.InvariantCulture);
                if (withBlock.LocalSettings.Values.ContainsKey(sName))
                    sTmp = System.Convert.ToBoolean(withBlock.LocalSettings.Values[sName].ToString(),System.Globalization.CultureInfo.InvariantCulture);
            }

            return sTmp;
        }

        public static bool GetSettingsBool(Windows.UI.Xaml.Controls.ToggleSwitch oSwitch, string sName, bool iDefault = false)
        {
            if (oSwitch is null) return iDefault ;
            bool sTmp;
            sTmp = GetSettingsBool(sName, iDefault);
            oSwitch.IsOn = sTmp;
            return sTmp;
        }

        public static void SetSettingsBool(string sName, bool sValue)
        {
            SetSettingsBool(sName, sValue, false);
        }

        public static void SetSettingsBool(string sName, bool sValue, bool bRoam)
        {
            {
                var withBlock = Windows.Storage.ApplicationData.Current;
                if (bRoam)
                    withBlock.RoamingSettings.Values[sName] = sValue.ToString();
                withBlock.LocalSettings.Values[sName] = sValue.ToString();
            }
        }

        public static void SetSettingsBool(string sName, bool? sValue, bool bRoam = false)
        {
            if (sValue.HasValue && sValue.Value)
                SetSettingsBool(sName, true, bRoam);
            else
                SetSettingsBool(sName, false, bRoam);
        }

        public static void SetSettingsBool(Windows.UI.Xaml.Controls.ToggleSwitch sValue, string sName, bool bRoam = false)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsOn, bRoam);
        }

        public static void SetSettingsBool(string sName, Windows.UI.Xaml.Controls.ToggleSwitch sValue, bool bRoam)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsOn, bRoam);
        }

        public static void SetSettingsBool(string sName, Windows.UI.Xaml.Controls.ToggleSwitch sValue)
        {
            if (sValue is null) return;
            SetSettingsBool(sName, sValue.IsOn, false);
        }

#endregion

#region "testy sieciowe"
        // -- Testy sieciowe ---------------------------------------------


        public static bool IsDevMobile()
        { // Brewiarz: wymuszanie zmiany dark/jasne
          // GrajCyganie: zmiana wielkosci okna
          // pociagi: ile rzadkow ma pokazac (rozmiar ekranu)
          // kamerki: full screen wlacz/wylacz tylko dla niego
          // sympatia...
          // TODO: WASM w zależności od rozmiaru ekranu?
          // poprzednio: Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily - ale to bylo jako 'windows.mobile', wiec musialyby byc oddzielne testy dla nie-Windows
            return Windows.System.Profile.AnalyticsInfo.DeviceForm.ToLower().Contains("mobile");
        }

        public static bool NetIsIPavailable(bool bMsg)
        {

            if (GetSettingsBool("offline"))
                return false;

            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return true;
            if (bMsg)
                /* TODO ERROR: Skipped WarningDirectiveTrivia */
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                DialogBox("ERROR: no IP network available");
#pragma warning restore CS4014
            return false;
        }

        public static bool NetIsCellInet()
        {
            //var connectionCost = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().GetConnectionCost();
            //if (connectionCost.NetworkCostType == Windows.Networking.Connectivity.NetworkCostType.Unknown
            //        || connectionCost.NetworkCostType == Windows.Networking.Connectivity.NetworkCostType.Unrestricted)
            //{
            //    //Connection cost is unknown/unrestricted
            //}
            //else
            //{ // metered connection
            //    return true;
            //}

            // iOS: SystemConfiguration.NetworkReachabilityFlags flags;
#if NETFX_CORE
            return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile;
#elif __IOS__
            SystemConfiguration.NetworkReachability oNR =
                new SystemConfiguration.NetworkReachability(new System.Net.IPAddress(0));
            SystemConfiguration.NetworkReachabilityFlags oNRfl;
            oNR.GetFlags(out oNRfl);
            if (oNRfl.HasFlag(SystemConfiguration.NetworkReachabilityFlags.IsWWAN))
                return true;

            return false;
#elif __ANDROID__
            var oContext = Android.App.Application.Context;
            Android.Net.ConnectivityManager cm = 
                (Android.Net.ConnectivityManager)oContext.GetSystemService(Android.Content.Context.ConnectivityService);
            Android.Net.NetworkInfo info = cm.ActiveNetworkInfo;
            if (info == null) return false;
            if (!info.IsConnected) return false;
            
            //if (info.Subtype.HasFlag(Android.Net.ConnectivityType.Ethernet)) return false;
            //if (info) == Android.Net.ConnectivityType.Wifi ) return false;
            return true;
#else
            return false; // reszta: (WASM) przyjmij że WiFi
#endif
        }


        public static string GetHostName()
        {
            string sNazwa = System.Net.Dns.GetHostName();
            return sNazwa;
            //IReadOnlyList<Windows.Networking.HostName> hostNames = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();
            //foreach (Windows.Networking.HostName oItem in hostNames)
            //{
            //    if (oItem.DisplayName.Contains(".local"))
            //        return oItem.DisplayName.Replace(".local", "");
            //}
            //return "";
        }


        public static bool IsThisMoje()
        {
            string sTmp = GetHostName().ToLower();
            if ((sTmp ?? "") == "home-pkar")
                return true;
            if ((sTmp ?? "") == "lumia_pkar")
                return true;
            if ((sTmp ?? "") == "kuchnia_pk")
                return true;
            if ((sTmp ?? "") == "ppok_pk")
                return true;
            // If sTmp.Contains("pkar") Then Return True
            // If sTmp.EndsWith("_pk") Then Return True
            return false;
        }

        //public async static System.Threading.Tasks.Task<bool> NetWiFiOffOn()
        //{

        //    // https://social.msdn.microsoft.com/Forums/ie/en-US/60c4a813-dc66-4af5-bf43-e632c5f85593/uwpbluetoothhow-to-turn-onoff-wifi-bluetooth-programmatically?forum=wpdevelop
        //    var result222 = await Windows.Devices.Radios.Radio.RequestAccessAsync();
        //    IReadOnlyList<Windows.Devices.Radios.Radio> radios = await Windows.Devices.Radios.Radio.GetRadiosAsync();

        //    foreach (var oRadio in radios)
        //    {
        //        if (oRadio.Kind == Windows.Devices.Radios.RadioKind.WiFi)
        //        {
        //            Windows.Devices.Radios.RadioAccessStatus oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off);
        //            if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
        //                return false;
        //            await Task.Delay(3 * 1000);
        //            oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On);
        //            if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
        //                return false;
        //        }
        //    }

        //    return true;
        //}

#endregion

#region "DialogBoxy"
        // -- DialogBoxy ---------------------------------------------



        public async static System.Threading.Tasks.Task DialogBox(string sMsg)
        {
            Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
            await oMsg.ShowAsync();
        }

        public static string GetLangString(string sMsg)
        {
            if (string.IsNullOrEmpty(sMsg))
                return "";

            string sRet = sMsg;
            try
            {
                sRet = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(sMsg);
            }
            catch { }
            return sRet;
        }

        public async static System.Threading.Tasks.Task DialogBoxRes(string sMsg)
        {
            sMsg = GetLangString(sMsg);
            await DialogBox(sMsg).ConfigureAwait(true);
        }

        public async static System.Threading.Tasks.Task DialogBoxRes(string sMsg, string sErrData)
        {
            sMsg = GetLangString(sMsg) + " " + sErrData;
            await DialogBox(sMsg).ConfigureAwait(true);
        }

        public async static System.Threading.Tasks.Task DialogBoxError(int iNr, string sMsg)
        {
            string sTxt = GetLangString("errAnyError");
            sTxt = sTxt + " (" + iNr.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")" + "\n" + sMsg;
            await DialogBox(sTxt).ConfigureAwait(true);
        }

        public async static void DialogBoxResError(int iNr, string sMsg)
        {
            await DialogBoxError(iNr, GetLangString(sMsg)).ConfigureAwait(true);
        }

        public async static System.Threading.Tasks.Task<bool> DialogBoxYN(string sMsg, string sYes = "Tak", string sNo = "Nie")
        {
            Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
            Windows.UI.Popups.UICommand oYes = new Windows.UI.Popups.UICommand(sYes);
            Windows.UI.Popups.UICommand oNo = new Windows.UI.Popups.UICommand(sNo);
            oMsg.Commands.Add(oYes);
            oMsg.Commands.Add(oNo);
            oMsg.DefaultCommandIndex = 1;    // default: No
            oMsg.CancelCommandIndex = 1;
            Windows.UI.Popups.IUICommand oCmd = await oMsg.ShowAsync();
            if (oCmd == null)
                return false;
            if (oCmd.Label == sYes)
                return true;

            return false;
        }

        public async static System.Threading.Tasks.Task<bool> DialogBoxResYN(string sMsgResId, string sYesResId = "resDlgYes", string sNoResId = "resDlgNo")
        {
            string sMsg, sYes, sNo;

            {
                var withBlock = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                sMsg = withBlock.GetString(sMsgResId);
                sYes = withBlock.GetString(sYesResId);
                sNo = withBlock.GetString(sNoResId);
            }

            if (string.IsNullOrEmpty(sMsg))
                sMsg = sMsgResId;  // zabezpieczenie na brak string w resource
            if (string.IsNullOrEmpty(sYes))
                sYes = sYesResId;
            if (string.IsNullOrEmpty(sNo))
                sNo = sNoResId;

            return await DialogBoxYN(sMsg, sYes, sNo).ConfigureAwait(true);
        }


        public async static System.Threading.Tasks.Task<string> DialogBoxInput(string sMsgResId, string sDefaultResId = "", string sYesResId = "resDlgContinue", string sNoResId = "resDlgCancel")
        {
            string sMsg, sYes, sNo, sDefault;

            sMsg = GetLangString(sMsgResId);
            sYes = GetLangString(sYesResId);
            sNo = GetLangString(sNoResId);
            sDefault = "";
            if (!string.IsNullOrEmpty(sDefaultResId))
                sDefault = GetLangString(sDefaultResId);

            if (string.IsNullOrEmpty(sMsg))
                sMsg = sMsgResId;  // zabezpieczenie na brak string w resource
            if (string.IsNullOrEmpty(sYes))
                sYes = sYesResId;
            if (string.IsNullOrEmpty(sNo))
                sNo = sNoResId;
            if (string.IsNullOrEmpty(sDefault))
                sDefault = sDefaultResId;

            Windows.UI.Xaml.Controls.TextBox oInputTextBox = new Windows.UI.Xaml.Controls.TextBox();
            oInputTextBox.AcceptsReturn = false;
            oInputTextBox.Text = sDefault;
            Windows.UI.Xaml.Controls.ContentDialog oDlg = new Windows.UI.Xaml.Controls.ContentDialog();
            oDlg.Content = oInputTextBox;
            oDlg.PrimaryButtonText = sYes;
            oDlg.SecondaryButtonText = sNo;
            oDlg.Title = sMsg;

            var oCmd = await oDlg.ShowAsync();
//#if !NETFX_CORE
//            oDlg.Dispose();
//#endif
            if (oCmd != Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                return "";

            return oInputTextBox.Text;
        }

        #endregion

#region "CheckPlatform etc"

        public static string GetPlatform()
    {
#if NETFX_CORE
        return "uwp";
#elif __ANDROID__
        return "android";
#elif __IOS__
        return "ios";
#elif __WASM__
        return "wasm";
#else
        return "other";
#endif
    }

        public static bool GetPlatform(string sPlatform)
        {
            if (string.IsNullOrEmpty(sPlatform)) return false;
            if (GetPlatform().ToLower() == sPlatform.ToLower()) return true;
            return false;
        }

        public static bool GetPlatform(bool bUwp, bool bAndro, bool bIos, bool bWasm, bool bOther)
        {
#if NETFX_CORE
        return bUwp;
#elif __ANDROID__
        return bAndro;
#elif __IOS__
        return bIos;
#elif __WASM__
            return bWasm;
#else
        return bOther;
#endif
        }

        public static int GetPlatform(int bUwp, int bAndro, int bIos, int bWasm, int bOther)
        {
#if NETFX_CORE
        return bUwp;
#elif __ANDROID__
        return bAndro;
#elif __IOS__
        return bIos;
#elif __WASM__
            return bWasm;
#else
        return bOther;
#endif
        }

        public static string GetPlatform(string bUwp, string bAndro, string bIos, string bWasm, string bOther)
        {
#if NETFX_CORE
        return bUwp;
#elif __ANDROID__
        return bAndro;
#elif __IOS__
        return bIos;
#elif __WASM__
            return bWasm;
#else
        return bOther;
#endif
        }


        #endregion
        public static string GetAppVers()
        {
            return Windows.ApplicationModel.Package.Current.Id.Version.Major + "." +
                Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." + 
                Windows.ApplicationModel.Package.Current.Id.Version.Build;
        }


        // --- INNE FUNKCJE ------------------------

        //public static void SetBadgeNo(int iInt)
        //{
        //    // https://docs.microsoft.com/en-us/windows/uwp/controls-and-patterns/tiles-and-notifications-badges

        //    Windows.Data.Xml.Dom.XmlDocument oXmlBadge;
        //    oXmlBadge = Windows.UI.Notifications.BadgeUpdateManager.GetTemplateContent(Windows.UI.Notifications.BadgeTemplateType.BadgeNumber);

        //    Windows.Data.Xml.Dom.XmlElement oXmlNum;
        //    oXmlNum = (Windows.Data.Xml.Dom.XmlElement)oXmlBadge.SelectSingleNode("/badge");
        //    oXmlNum.SetAttribute("value", iInt.ToString());

        //    Windows.UI.Notifications.BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(new Windows.UI.Notifications.BadgeNotification(oXmlBadge));
        //}


        public static string XmlSafeString(string sInput)
        {
            if (sInput is null) return "";
            string sTmp;
            sTmp = sInput.Replace("&", "&amp;");
            sTmp = sTmp.Replace("<", "&lt;");
            sTmp = sTmp.Replace(">", "&gt;");
            return sTmp;
        }

        public static string XmlSafeStringQt(string sInput)
        {
            string sTmp;
            sTmp = XmlSafeString(sInput);
            sTmp = sTmp.Replace("\"", "&quote;");
            return sTmp;
        }

        public static string ToastAction(string sAType, string sAct, string sGuid, string sContent)
        {
            string sTmp = sContent;
            if (!string.IsNullOrEmpty(sTmp))
                sTmp = GetSettingsString(sTmp, sTmp);

            string sTxt = "<action " + "activationType=\"" + sAType + "\" " + "arguments=\"" + sAct + sGuid + "\" " + "content=\"" + sTmp + "\"/> ";
            return sTxt;
        }

        public static void MakeToast(string sMsg, string sMsg1 = "")
        {

#if NETFX_CORE || __ANDROID__
            string sHdr = "";
            string sAttrib = "";

            //if (WinVer() > 15062)
            //{
            //    // jako header
            //    // https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/toast-headers
            //    sHdr = "<header id=\"SmogMeter\" title=\"SmogMeter\" />";
            //}
            //else
            //{
            //    // ElseIf WinVer() > 14392 Then - ale to jest spelnione, bo kompilacja ma minimum 14393
            //    // https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/adaptive-interactive-toasts
            //    sAttrib = "<text placement=\"attribution\">SmogMeter</text>";
            //}

            var sXml = "<visual><binding template='ToastGeneric'>" + sAttrib + "<text>" + XmlSafeString(sMsg);
            if (!string.IsNullOrEmpty(sMsg1))
                sXml = sXml + "</text><text>" + XmlSafeString(sMsg1);
            sXml = sXml + "</text></binding></visual>";
            var oXml = new Windows.Data.Xml.Dom.XmlDocument();
            oXml.LoadXml("<toast>" + sHdr + sXml + "</toast>");
            var oToast = new Windows.UI.Notifications.ToastNotification(oXml);
            Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast);

#else
            // Xam.Plugins.Notifier

            string sTitle, sBody;
            if (sMsg1 == "")
            {
                sTitle = "";
                sBody = sMsg;
            }
            else
            {
                sTitle = sMsg;
                sBody = sMsg1;
            }
            Plugin.LocalNotifications.CrossLocalNotifications.Current.Show(sTitle, sBody);
#endif
        }

        public static int WinVer()
        {
#if NETFX_CORE
            // Unknown = 0,
            // Threshold1 = 1507,   // 10240
            // Threshold2 = 1511,   // 10586
            // Anniversary = 1607,  // 14393 Redstone 1
            // Creators = 1703,     // 15063 Redstone 2
            // FallCreators = 1709 // 16299 Redstone 3
            // April = 1803		// 17134
            // October = 1809		// 17763
            // ? = 190?		// 18???

            // April  1803, 17134, RS5

            ulong u = ulong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            u = (u & 0xFFFF0000L) >> 16;
            return (int)u;
#elif __ANDROID__
            return (int)Android.OS.Build.VERSION.SdkInt;
#endif
        }

        //private static Windows.Web.Http.HttpClient moHttp = new Windows.Web.Http.HttpClient();

        //public async static System.Threading.Tasks.Task<string> HttpPageAsync(string sUrl, string sErrMsg, string sData = "")
        //{
        //    try
        //    {
        //        if (!NetIsIPavailable(true))
        //            return "";
        //        if (string.IsNullOrEmpty(sUrl))
        //            return "";

        //        if ((sUrl.Substring(0, 4) ?? "") != "http")
        //            sUrl = "http://beskid.geo.uj.edu.pl/p/dysk" + sUrl;

        //        if (moHttp == null)
        //        {
        //            moHttp = new Windows.Web.Http.HttpClient();
        //            moHttp.DefaultRequestHeaders.UserAgent.TryParseAdd("GrajCyganie");
        //        }

        //        var sError = "";
        //        Windows.Web.Http.HttpResponseMessage oResp = null;

        //        try
        //        {
        //            if (!string.IsNullOrEmpty(sData))
        //            {
        //                var oHttpCont = new Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
        //                oResp = await moHttp.PostAsync(new Uri(sUrl), oHttpCont);
        //            }
        //            else
        //                oResp = await moHttp.GetAsync(new Uri(sUrl));
        //        }
        //        catch (Exception ex)
        //        {
        //            sError = ex.Message;
        //        }

        //        if (!string.IsNullOrEmpty(sError))
        //        {
        //            await DialogBox("error " + sError + " at " + sErrMsg + " page");
        //            return "";
        //        }

        //        if ((oResp.StatusCode == 303) || (oResp.StatusCode == 302) || (oResp.StatusCode == 301))
        //        {
        //            // redirect
        //            sUrl = oResp.Headers.Location.ToString;
        //            // If sUrl.ToLower.Substring(0, 4) <> "http" Then
        //            // sUrl = "https://sympatia.onet.pl/" & sUrl   ' potrzebne przy szukaniu
        //            // End If

        //            if (!string.IsNullOrEmpty(sData))
        //            {
        //                // Dim oHttpCont = New HttpStringContent(sData, Text.Encoding.UTF8, "application/x-www-form-urlencoded")
        //                var oHttpCont = new Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
        //                oResp = await moHttp.PostAsync(new Uri(sUrl), oHttpCont);
        //            }
        //            else
        //                oResp = await moHttp.GetAsync(new Uri(sUrl));
        //        }

        //        if (oResp.StatusCode > 290)
        //        {
        //            await DialogBox("ERROR " + oResp.StatusCode + " getting " + sErrMsg + " page");
        //            return "";
        //        }

        //        string sResp = "";
        //        try
        //        {
        //            sResp = await oResp.Content.ReadAsStringAsync;
        //        }
        //        catch (Exception ex)
        //        {
        //            sError = ex.Message;
        //        }

        //        if (!string.IsNullOrEmpty(sError))
        //        {
        //            await DialogBox("error " + sError + " at ReadAsStringAsync " + sErrMsg + " page");
        //            return "";
        //        }

        //        return sResp;
        //    }
        //    catch (Exception ex)
        //    {
        //        CrashMessageExit("@HttpPageAsync", ex.Message);
        //    }

        //    return "";
        //}

        public static string RemoveHtmlTags(string sHtml)
        {
            int iInd0, iInd1;
            if (sHtml is null) return "";
            iInd0 = sHtml.IndexOf("<script",StringComparison.Ordinal);
            if (iInd0 > 0)
            {
                iInd1 = sHtml.IndexOf("</script>", iInd0, StringComparison.Ordinal);
                if (iInd1 > 0)
                    sHtml = sHtml.Remove(iInd0, (iInd1 - iInd0) + 9);
            }

            iInd0 = sHtml.IndexOf("<", StringComparison.Ordinal);
            iInd1 = sHtml.IndexOf(">", StringComparison.Ordinal);
            while (iInd0 > -1)
            {
                if (iInd1 > -1)
                    sHtml = sHtml.Remove(iInd0, (iInd1 - iInd0) + 1);
                else
                    sHtml = sHtml.Substring(0, iInd0);
                sHtml = sHtml.Trim();

                iInd0 = sHtml.IndexOf("<", StringComparison.Ordinal);
                iInd1 = sHtml.IndexOf(">", StringComparison.Ordinal);
            }

            sHtml = sHtml.Replace("&nbsp;", " ");
            sHtml = sHtml.Replace('\r', '\n');
            sHtml = sHtml.Replace("\n\n", "\n");
            sHtml = sHtml.Replace("\n\n", "\n");
            sHtml = sHtml.Replace("\n\n", "\n");

            return sHtml.Trim();
        }

        public static void OpenBrowser(Uri oUri, bool bForceEdge = false)
        {
#if NETFX_CORE
            if (bForceEdge)
            {
                Windows.System.LauncherOptions options = new Windows.System.LauncherOptions();
                options.TargetApplicationPackageFamilyName = "Microsoft.MicrosoftEdge_8wekyb3d8bbwe";
                /* TODO ERROR: Skipped WarningDirectiveTrivia */
                Windows.System.Launcher.LaunchUriAsync(oUri, options);
            }
            else
#endif
            Windows.System.Launcher.LaunchUriAsync(oUri);
        }

        public static void OpenBrowser(string sUri, bool bForceEdge = false)
        {
            Uri oUri = new Uri(sUri);
            OpenBrowser(oUri, bForceEdge);
        }

            public static string FileLen2string(long iBytes)
        {
            if (iBytes == (long)1)
                return "1 byte";
            if (iBytes < (long)10000)
                return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " bytes";
            iBytes = iBytes / (long)1024;
            if (iBytes == (long)1)
                return "1 kibibyte";
            if (iBytes < (long)2000)
                return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " kibibytes";
            iBytes = iBytes / (long)1024;
            if (iBytes == (long)1)
                return "1 mebibyte";
            if (iBytes < (long)2000)
                return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " mebibytes";
            iBytes = iBytes / (long)1024;
            if (iBytes == (long)1)
                return "1 gibibyte";
            return iBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " gibibytes";
        }


        public static DateTime UnixTimeToTime(long lTime)
        {
            // 1509993360
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds((double)lTime);   // UTC
                                                                 // dtDateTime.Kind = DateTimeKind.Utc
            return dtDateTime.ToLocalTime();
        }


        public static int GPSdistanceDwa(double dLat0, double dLon0, double dLat, double dLon)
        {
            // https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

            try
            {
                int iRadix = 6371000;
                double tLat = (dLat - dLat0) * Math.PI / 180;
                double tLon = (dLon - dLon0) * Math.PI / 180;
                double a = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) + Math.Cos(Math.PI / 180 * dLat0) * Math.Cos(Math.PI / 180 * dLat) * Math.Sin(tLon / 2) * Math.Sin(tLon / 2);
                double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
                double d = iRadix * c;

                return (int)d;
            }
            catch
            {
                return 0;
            }// nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
        }

        public static int GPSdistance(Windows.Devices.Geolocation.Geoposition oPos, double dLat, double dLon)
        {
            if (oPos is null) return 0;
            return p.k.GPSdistanceDwa(oPos.Coordinate.Point.Position.Latitude, oPos.Coordinate.Point.Position.Longitude, dLat, dLon);
        }


    }


}


