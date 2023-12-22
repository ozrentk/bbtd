using Android.App;
using Android.Content;
using Android.OS;
using System;
using System.Reflection;
using System.Threading;
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

        public async Task<ZXing.Result> ScanWithTimeout(Context context, MobileBarcodeScanningOptions options, int timeout)
        {
            Context ctx = GetContext(context);
            var scanTask = Task.Run(() =>
            {
                ManualResetEvent waitScanResetEvent = new ManualResetEvent(initialState: false);
                Intent intent = new Intent(ctx, typeof(ZxingActivity));
                intent.AddFlags(ActivityFlags.NewTask);
                ZxingActivity.UseCustomOverlayView = base.UseCustomOverlay;
                ZxingActivity.CustomOverlayView = CustomOverlay;
                ZxingActivity.ScanningOptions = options;
                ZxingActivity.ScanContinuously = false;
                ZxingActivity.TopText = base.TopText;
                ZxingActivity.BottomText = base.BottomText;
                ZXing.Result scanResult = null;
                ZxingActivity.CanceledHandler = () =>
                {
                    waitScanResetEvent.Set();
                };
                ZxingActivity.ScanCompletedHandler = (ZXing.Result result) =>
                {
                    scanResult = result;
                    waitScanResetEvent.Set();
                };
                ctx.StartActivity(intent);
                waitScanResetEvent.WaitOne();
                return scanResult;
            });

            var delayTask = Task.Delay(timeout);
            var finishedTask = await Task.WhenAny(scanTask, delayTask);
            if (finishedTask == scanTask)
            {
                return scanTask.Result;
            }
            else
            {
                return default;
            }
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
            ZxingActivity.CanceledHandler = delegate
            {
                if (scanHandler != null)
                {
                    scanHandler(null, ZxingActivity.ScanningOptions);
                }
            };
            context2.StartActivity(intent);
        }
    }
}