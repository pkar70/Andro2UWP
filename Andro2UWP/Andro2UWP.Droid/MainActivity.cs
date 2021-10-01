using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;

namespace Andro2UWP.Droid
{
    [Activity(
            MainLauncher = true,
            // ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, // linia dla starszego
            ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges, // linia PO 3.0.12
            WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
        )]
    public class MainActivity : Windows.UI.Xaml.ApplicationActivity
    {
        // https://github.com/LeversonCarlos/Xamarin.OneDrive.Connector
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            Uno.OneDrive.Connector.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }
    }
}

