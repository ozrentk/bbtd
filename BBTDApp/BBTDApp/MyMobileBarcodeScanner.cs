using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ZXing.Mobile;

namespace BBTDApp
{
    public class MyMobileBarcodeScanner : MobileBarcodeScanner
    {
        public static ActivityLifecycleContextListener _lifecycleListener { get; set; }

        protected static ActivityLifecycleContextListener GetLifecycleListener()
        {
            if (_lifecycleListener == null)
            {
                var lifecycleListenerField =
                    typeof(MobileBarcodeScanner)
                        .GetField("lifecycleListener", BindingFlags.Static | BindingFlags.NonPublic);
                var lifecycleListener = lifecycleListenerField.GetValue(null) as ActivityLifecycleContextListener;
            }

            return _lifecycleListener;
        }

        protected Context GetContext(Context context)
        {
            if (context != null)
            {
                return context;
            }

            BuildVersionCodes sdkInt = Build.VERSION.SdkInt;
            if (sdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                return _lifecycleListener.Context;
            }

            return Application.Context;
        }

        public async Task ScanContinuouslyAsync(Context context, MobileBarcodeScanningOptions options, Action<ZXing.Result, MobileBarcodeScanningOptions> scanHandler)
        {
            Context context2 = GetContext(context);
            Intent intent = new Intent(context2, typeof(ZxingActivity));
            intent.AddFlags(ActivityFlags.NewTask);
            ZxingActivity.UseCustomOverlayView = base.UseCustomOverlay;
            ZxingActivity.CustomOverlayView = CustomOverlay;
            ZxingActivity.ScanningOptions = options;
            ZxingActivity.ScanContinuously = true;
            ZxingActivity.TopText = base.TopText;
            ZxingActivity.BottomText = base.BottomText;
            ZxingActivity.ScanCompletedHandler = delegate (ZXing.Result result)
            {
                if (scanHandler != null)
                {
                    scanHandler(result, ZxingActivity.ScanningOptions);
                }
            };
            context2.StartActivity(intent);
        }
    }
}