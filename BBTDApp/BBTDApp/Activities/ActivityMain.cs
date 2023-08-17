using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Util.Zip;
//using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using ZXing.Mobile;
using ScanResult = ZXing.Result;

namespace BBTDApp.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class ActivityMain : AppCompatActivity
    {
        private PermissionStatus _permissionStatusCamera;
        private PermissionStatus _permissionStatusFlashlight;

        private EditText TxtAuthority =>
            FindViewById<EditText>(Resource.Id.txtAuthority);

        private Button BtnScanAuthority =>
            FindViewById<Button>(Resource.Id.btnScanAuthority);

        private EditText TxtDelay =>
            FindViewById<EditText>(Resource.Id.txtDelay);

        private Switch SwitchAdaptiveDelay =>
            FindViewById<Switch>(Resource.Id.switchAdaptiveDelay);

        private Switch SwitchNativeScanning =>
            FindViewById<Switch>(Resource.Id.switchNativeScanning);

        private Switch SwitchAutoFocus =>
            FindViewById<Switch>(Resource.Id.switchAutofocus);

        private Button BtnScanSingle =>
            FindViewById<Button>(Resource.Id.btnScanSingle);

        private Button BtnScanContinuous =>
            FindViewById<Button>(Resource.Id.btnScanContinuous);

        private MobileBarcodeScanningOptions BaseOptionsFactory()
        {
            int.TryParse(TxtDelay.Text, out int delay);
            delay = delay == default ? 10 : delay;
            
            return new MobileBarcodeScanningOptions
            {
                PossibleFormats = new List<ZXing.BarcodeFormat>()
                {
                    ZXing.BarcodeFormat.QR_CODE,
                },
                InitialDelayBeforeAnalyzingFrames = 0, // without this delay, the scanner might attempt to decode the first few frames before the camera feed has fully stabilized, potentially resulting in false positives or inaccurate readings
                DelayBetweenAnalyzingFrames = 5, // analyze data every "delay" ms, reduce the processing load on the system and provide a more efficient scanning experience
                DelayBetweenContinuousScans = delay, // delay between scans, when the scanner is in continuous mode - helps prevent the scanner from immediately initiating a new scan after detecting a barcode
                UseNativeScanning = SwitchNativeScanning.Checked, // use the ZXing.Net.Mobile library to scan barcodes, instead of the native Android API
                DisableAutofocus = !SwitchAutoFocus.Checked,
                AutoRotate = false, // don't rotate the camera feed to match the device orientation
                TryInverted = false, // don't try inverted barcodes
                TryHarder = false, // don't spend extra time to decode "difficult" barcodes (e.g. poor quality)
                //AssumeGS1 = false,
                //CharacterSet = null,
                //PureBarcode = false,
                //UseCode39ExtendedMode = false,
                //UseFrontCameraIfAvailable = false,
                //CameraResolutionSelector = null,
            };

        }

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Log.Logger = new LoggerConfiguration()
            //    .WriteTo.AndroidLog()
            //    .CreateLogger();

            //Log.Information("Started serilog logger");
            //Log.CloseAndFlush();

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            BtnScanAuthority.Click += OnBtnScanAuthority_Click;
            BtnScanSingle.Click += OnBtnScanSingle_Click;
            BtnScanContinuous.Click += OnBtnScanContinuous_Click;

            _permissionStatusCamera = await Permissions.CheckStatusAsync<Permissions.Camera>();
            _permissionStatusFlashlight = await Permissions.CheckStatusAsync<Permissions.Flashlight>();

            TxtAuthority.Text = Preferences.Get("TxtAuthority", "");
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async void OnBtnScanAuthority_Click(object sender, System.EventArgs e)
        {
            if (!(await RequestPermissions()))
                return;

            TxtAuthority.Text = await ReadFromScanner();
            Preferences.Set("TxtAuthority", TxtAuthority.Text);
        }

        private async void OnBtnScanSingle_Click(object sender, System.EventArgs e)
        {
            if (!(await RequestPermissions()))
                return;

            var scanned = await ReadFromScanner();
            await SendBarcodeData(scanned);
        }

        private long? LastChecksum = null;

        private async void OnBtnScanContinuous_Click(object sender, System.EventArgs e)
        {
            if (!(await RequestPermissions()))
                return;

            await ReadFromScannerContinuously(async (ScanResult scanResult, MobileBarcodeScanningOptions options) =>
            {
                try
                {
                    var crc = new CRC32();
                    crc.Update(Encoding.ASCII.GetBytes(scanResult.Text));
                    if (LastChecksum == crc.Value)
                    {
                        if (SwitchAdaptiveDelay.Checked)
                        {
                            options.DelayBetweenContinuousScans *= 2;
                            Console.WriteLine($"DEBUG: DelayBetweenContinuousScans = {options.DelayBetweenContinuousScans}");
                        }
                        return;
                    }
                    LastChecksum = crc.Value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: Problem with adaptive scanning");
                }

                await SendBarcodeData(scanResult.Text);
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            BtnScanAuthority.Click -= OnBtnScanAuthority_Click;
            BtnScanSingle.Click -= OnBtnScanSingle_Click;
            BtnScanContinuous.Click -= OnBtnScanContinuous_Click;
        }

        private async Task<bool> RequestPermissions()
        {
            if (_permissionStatusCamera != PermissionStatus.Granted)
            {
                _permissionStatusCamera = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (_permissionStatusFlashlight != PermissionStatus.Granted)
            {
                _permissionStatusFlashlight = await Permissions.RequestAsync<Permissions.Flashlight>();
            }

            var requiredPermissionsGranted =
                _permissionStatusCamera == PermissionStatus.Granted &&
                _permissionStatusFlashlight == PermissionStatus.Granted;

            return requiredPermissionsGranted;
        }

        private async Task<string> ReadFromScanner()
        {
            try
            {
                var scanner = new MobileBarcodeScanner();

                var scanResult = await scanner.Scan(this, BaseOptionsFactory());
                if (scanResult == null)
                {
                    Console.WriteLine("WARNING: Barcode scanning returned null");
                }

                return scanResult.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Barcode scanning failed");
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        private async Task ReadFromScannerContinuously(Func<ScanResult, MobileBarcodeScanningOptions, Task> scanResultAction)
        {
            try
            {
                var scanner = new MyMobileBarcodeScanner();

                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                await scanner.ScanContinuouslyAsync(this, BaseOptionsFactory(), async (scanResult, options) => {
                    Console.WriteLine($"Elapsed: {stopWatch.Elapsed.Milliseconds}");
                    await scanResultAction(scanResult, options);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Barcode scanning failed");
                Console.WriteLine(ex.Message);
            }
        }

        private async Task SendBarcodeData(string scanned)
        {
            try
            {
                var client = new HttpClient();
                var content = new StringContent(scanned, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync($"{TxtAuthority.Text}/Home/Reader", content);
                if (resp.IsSuccessStatusCode)
                {
                    Console.WriteLine("MESSAGE: HttpClient returned success");
                }
                else
                {
                    Console.WriteLine("ERROR: HttpClient failed");
                    Console.WriteLine($"{resp.StatusCode}: {resp.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: SendBarcodeData failed");
                Console.WriteLine(ex.Message);
            }
        }
    }
}