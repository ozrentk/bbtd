﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using BBTDApp.Models;
using BBTDApp.NLogEx;
using BBTDApp.Services;
using Java.Util.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        private PermissionStatus _permissionStatusReadExternalStorage;
        private PermissionStatus _permissionStatusWriteExternalStorage;

        private EditText TxtWebAppEndpoint =>
            FindViewById<EditText>(Resource.Id.txtWebAppEndpoint);

        private EditText TxtNumberOfItems =>
            FindViewById<EditText>(Resource.Id.txtNumberOfItems);

        private int NumberOfItemsValue
        {
            get
            {
                int.TryParse(TxtNumberOfItems.Text, out int numOfItems);
                return numOfItems;
            }
        }

        private Spinner SpinBarcodeType =>
            FindViewById<Spinner>(Resource.Id.spinBarcodeType);

        private Android.Widget.Button BtnScanWebAppConfiguration =>
            FindViewById<Android.Widget.Button>(Resource.Id.btnScanWebAppConfiguration);

        private EditText TxtScanTimeout =>
            FindViewById<EditText>(Resource.Id.txtScanTimeout);

        private int ScanTimeoutValue
        {
            get
            {
                int.TryParse(TxtScanTimeout.Text, out int delay);
                return delay;
            }
        }

        private EditText TxtDelay =>
            FindViewById<EditText>(Resource.Id.txtDelay);

        private int DelayValue
        {
            get
            {
                int.TryParse(TxtDelay.Text, out int delay);
                return delay;
            }
        }

        private Android.Widget.Switch SwitchAdaptiveDelay =>
            FindViewById<Android.Widget.Switch>(Resource.Id.switchAdaptiveDelay);

        private Android.Widget.Switch SwitchNativeScanning =>
            FindViewById<Android.Widget.Switch>(Resource.Id.switchNativeScanning);

        private Android.Widget.Switch SwitchAutoFocus =>
            FindViewById<Android.Widget.Switch>(Resource.Id.switchAutofocus);

        private Android.Widget.Switch SwitchTryHarder =>
            FindViewById<Android.Widget.Switch>(Resource.Id.switchTryHarder);

        private Android.Widget.Button BtnScanSingle =>
            FindViewById<Android.Widget.Button>(Resource.Id.btnScanSingle);

        private Android.Widget.Button BtnScanOneByOne =>
            FindViewById<Android.Widget.Button>(Resource.Id.btnScanOneByOne);

        private Android.Widget.Button BtnScanContinuous =>
            FindViewById<Android.Widget.Button>(Resource.Id.btnScanContinuous);

        private LogDelivery _logger;

        private LogDelivery Logger
        {
            get 
            {
                if (_logger == null || !_logger.IsActive)
                    _logger = new LogDelivery(TxtWebAppEndpoint.Text);

                return _logger;
            }
        }

        private KeyValuePair<string, int>[] BarcodeTypes = new KeyValuePair<string, int>[]
        {
            KeyValuePair.Create("Aztec", 1),
            KeyValuePair.Create("Data Matrix", 32),
            KeyValuePair.Create("PDF417", 1024),
            KeyValuePair.Create("QR Code", 2048)
        };

        private MobileBarcodeScanningOptions BaseOptionsFactory()
        {
            var delay = DelayValue == default ? 10 : DelayValue;

            return new MobileBarcodeScanningOptions
            {
                InitialDelayBeforeAnalyzingFrames = 0, // without this delay, the scanner might attempt to decode the first few frames before the camera feed has fully stabilized, potentially resulting in false positives or inaccurate readings
                DelayBetweenAnalyzingFrames = 5, // analyze data every "delay" ms, reduce the processing load on the system and provide a more efficient scanning experience
                DelayBetweenContinuousScans = delay, // delay between scans, when the scanner is in continuous mode - helps prevent the scanner from immediately initiating a new scan after detecting a barcode
                UseNativeScanning = SwitchNativeScanning.Checked, // use the ZXing.Net.Mobile library to scan barcodes, instead of the native Android API
                DisableAutofocus = !SwitchAutoFocus.Checked,
                AutoRotate = false, // don't rotate the camera feed to match the device orientation
                TryInverted = false, // don't try inverted barcodes
                TryHarder = SwitchTryHarder.Checked, // several ways to improve readability of barcodes, but also increase processing time
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

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            var barcodeTypeKeys = BarcodeTypes.Select(item => item.Key).ToArray();
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, barcodeTypeKeys);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            SpinBarcodeType.Adapter = adapter;

            SpinBarcodeType.ItemSelected += SpinBarcodeType_ItemSelected;
            BtnScanWebAppConfiguration.Click += OnBtnScanConfiguration_Click;
            BtnScanSingle.Click += OnBtnScanSingle_Click;
            BtnScanOneByOne.Click += OnBtnScanOneByOne_Click;
            BtnScanContinuous.Click += OnBtnScanContinuous_Click;

            _permissionStatusCamera = await Permissions.CheckStatusAsync<Permissions.Camera>();
            _permissionStatusFlashlight = await Permissions.CheckStatusAsync<Permissions.Flashlight>();
            _permissionStatusReadExternalStorage = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            _permissionStatusWriteExternalStorage = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();

            TxtWebAppEndpoint.Text = Preferences.Get("TxtWebAppEndpoint", "");
            TxtNumberOfItems.Text = Preferences.Get("TxtNumberOfItems", "");
            var selectedBarcodeTypeValue = Preferences.Get("SpinBarcodeType", 2048);
            var selectedBarcodeType = BarcodeTypes.FirstOrDefault(x => x.Value == selectedBarcodeTypeValue);
            var selectedIdx = Array.IndexOf(BarcodeTypes, selectedBarcodeType);
            SpinBarcodeType.SetSelection(selectedIdx);

            await Logger.LogAsync("Mobile app started", LogLevel.Info);
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

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void SpinBarcodeType_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var selectedBarcodeType = BarcodeTypes[e.Position];
            Toast.MakeText(this, "Selected: " + selectedBarcodeType.Key, ToastLength.Short).Show();
        }

        private async void OnBtnScanConfiguration_Click(object sender, System.EventArgs e)
        {
            if (!(await RequestPermissions()))
                return;

            var configJson = await ReadFromScanner(ZXing.BarcodeFormat.QR_CODE);
            if (string.IsNullOrEmpty(configJson))
                return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<WebAppConfiguration>(configJson, options);

            TxtWebAppEndpoint.Text = config.ServerUrl.ToString();
            Preferences.Set("TxtWebAppEndpoint", TxtWebAppEndpoint.Text);

            TxtNumberOfItems.Text = config.NumberOfItems.ToString();
            Preferences.Set("TxtNumberOfItems", TxtNumberOfItems.Text);

            var selectedBarcodeType = BarcodeTypes.FirstOrDefault(item => item.Value == config.BarcodeType);
            if (selectedBarcodeType.Key != null)
            {
                var selectedIdx = Array.IndexOf(BarcodeTypes, selectedBarcodeType);
                SpinBarcodeType.SetSelection(selectedIdx);
                Preferences.Set("SpinBarcodeType", selectedBarcodeType.Value);
            }

            _logger = null;

            Logger.CalculateOffset();

            Toast.MakeText(this, "Configuration scanned", ToastLength.Short).Show();
        }

        private async void OnBtnScanSingle_Click(object sender, System.EventArgs e)
        {
            try
            {
                if (!(await RequestPermissions()))
                    return;

                //await Logger.LogAsync("Single item scan started", SyslogSeverity.Debug);
                Logger.AddLog("Single item scan started", LogLevel.Debug);

                var scanned = await ReadFromScanner();
                Console.WriteLine($"Raw data scanned: {scanned}");

                if (string.IsNullOrEmpty(scanned))
                {
                    Logger.AddLog("Did not scan", LogLevel.Error);
                }
                else
                {
                    Logger.AddLog("Single item scanned", LogLevel.Debug);

                    var personData = JsonSerializer.Deserialize<Models.Person>(scanned);

                    Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data deserialized in app", LogLevel.Debug, personData.Id, LogOperation.BC_READ);

                    await SendBarcodeData(scanned);

                    Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data sent to server", LogLevel.Debug, personData.Id, LogOperation.BC_SENT);
                }

                await Logger.DeliverLogsAsync();
                Toast.MakeText(this, "Logs delivered", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Problem with scanning: {ex.Message}");
            }
        }

        private async void OnBtnScanOneByOne_Click(object sender, System.EventArgs e)
        {
            try
            {
                if (!(await RequestPermissions()))
                    return;

                Logger.AddLog("One-by-one item scan started", LogLevel.Debug);

                for (int i = 0; i < NumberOfItemsValue; i++)
                {
                    var scanned = await ReadFromScanner(addDelay: 500, timeout: ScanTimeoutValue);
                    Console.WriteLine($"Raw data scanned: {scanned}");

                    if (string.IsNullOrEmpty(scanned))
                    {
                        Logger.AddLog("Did not scan", LogLevel.Error);

                        await SendBarcodeData(scanned);
                    }
                    else
                    {
                        Logger.AddLog("One-by-one item scanned", LogLevel.Debug);

                        Models.Person personData = null;
                        try {
                            personData = JsonSerializer.Deserialize<Models.Person>(scanned);
                            Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data deserialized in app", LogLevel.Debug, personData.Id, LogOperation.BC_READ);
                            await SendBarcodeData(scanned);
                            Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data sent to server", LogLevel.Debug, personData.Id, LogOperation.BC_SENT);
                        }
                        catch (JsonException ex)
                        {
                            Logger.AddLog($"Read barcode data deserialization error", LogLevel.Debug);
                            await SendBarcodeData(scanned);
                        }
                    }
                }

                await Logger.DeliverLogsAsync();
                Toast.MakeText(this, "Logs delivered", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Problem with scanning: {ex.Message}");
            }
        }


        //private async void OnBtnScanOneByOne_Click(object sender, System.EventArgs e)
        //{
        //    try
        //    {
        //        if (!(await RequestPermissions()))
        //            return;

        //        Logger.AddLog("One-by-one item scan started", LogLevel.Debug);

        //        var scanned = await ReadMultipleFromScanner();
        //        Console.WriteLine($"Raw data scanned: {scanned}");

        //        if (string.IsNullOrEmpty(scanned))
        //        {
        //            Logger.AddLog("Did not scan", LogLevel.Error);
        //        }
        //        else
        //        {
        //            Logger.AddLog("One-by-one item scanned", LogLevel.Debug);

        //            var personData = JsonSerializer.Deserialize<Models.Person>(scanned);

        //            Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data deserialized in app", LogLevel.Debug, personData.Id, LogOperation.BC_READ);

        //            await SendBarcodeData(scanned);

        //            Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data sent to server", LogLevel.Debug, personData.Id, LogOperation.BC_SENT);
        //        }

        //        await Logger.DeliverLogsAsync();
        //        Toast.MakeText(this, "Logs delivered", ToastLength.Short).Show();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: Problem with scanning: {ex.Message}");
        //    }
        //}

        private long? LastChecksum = null;

        private async void OnBtnScanContinuous_Click(object sender, System.EventArgs e)
        {
            if (!(await RequestPermissions()))
                return;

            Logger.AddLog("Continuous scan started", LogLevel.Debug);

            LastChecksum = null;

            await StartScanContinuous(OnScannerRead, OnScannerClosed);
        }

        private async Task StartScanContinuous(Func<ScanResult, MobileBarcodeScanningOptions, Task> scanResultAction, Func<Task> scannerClosedAction)
        {
            try
            {
                var scanner = new MyMobileBarcodeScanner();

                var options = BaseOptionsFactory();
                var selectedBarcodeType = BarcodeTypes[SpinBarcodeType.SelectedItemPosition];
                options.PossibleFormats = new List<ZXing.BarcodeFormat>() { (ZXing.BarcodeFormat)selectedBarcodeType.Value };

                await scanner.ScanContinuouslyAsync(this, options, async (scanResult, options) =>
                {
                    if (scanResult == null)
                    {
                        await scannerClosedAction();
                        return;
                    }

                    await scanResultAction(scanResult, options);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Barcode scanning failed: {ex.Message}");
            }
        }

        private async Task OnScannerRead(ScanResult scanResult, MobileBarcodeScanningOptions options)
        {
            try
            {
                if (string.IsNullOrEmpty(scanResult.Text))
                {
                    Logger.AddLog("Did not scan", LogLevel.Error);
                    return;
                }

                var crc = new CRC32();
                crc.Update(Encoding.ASCII.GetBytes(scanResult.Text));
                if (LastChecksum == crc.Value)
                {
                    if (SwitchAdaptiveDelay.Checked)
                    {
                        options.DelayBetweenContinuousScans *= 2;
                        Logger.AddLog($"DelayBetweenContinuousScans = {options.DelayBetweenContinuousScans}", LogLevel.Debug);
                    }
                    return;
                }
                else
                {
                    Logger.AddLog("New barcode scanned in continuous mode", LogLevel.Debug);
                }
                LastChecksum = crc.Value;

                var personData = JsonSerializer.Deserialize<Models.Person>(scanResult.Text);

                Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data deserialized in app", LogLevel.Debug, personData.Id, LogOperation.BC_READ);

                await SendBarcodeData(scanResult.Text);

                Logger.AddLog($"[Barcode id={personData.Id}] Read barcode data sent to server", LogLevel.Debug, personData.Id, LogOperation.BC_SENT);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Problem with scanning: {ex.Message}");
            }
        }

        private async Task OnScannerClosed()
        {
            try
            {
                await Logger.DeliverLogsAsync();
                Toast.MakeText(this, "Logs delivered", ToastLength.Short).Show();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Problem with log delivery: {ex.Message}");
            }
        }

        private async Task<string> ReadFromScanner(ZXing.BarcodeFormat? barcodeFormat = null, int? addDelay = null, int? timeout = null)
        {
            try
            {
                var scanner = new MyMobileBarcodeScanner();

                var options = BaseOptionsFactory();

                if (!barcodeFormat.HasValue)
                {
                    var selectedBarcodeType = BarcodeTypes[SpinBarcodeType.SelectedItemPosition];
                    options.PossibleFormats = new List<ZXing.BarcodeFormat>() { (ZXing.BarcodeFormat)selectedBarcodeType.Value };
                }
                else
                {
                    options.PossibleFormats = new List<ZXing.BarcodeFormat>() { barcodeFormat.Value };
                }

                var scanResult =
                    timeout.HasValue ?
                        await scanner.ScanWithTimeout(this, options, timeout.Value) :
                        await scanner.Scan(this, options);

                if (scanResult == null)
                {
                    Console.WriteLine("Warning: Barcode scanning returned null (timeout?)");
                }

                if (addDelay.HasValue)
                {
                    await Task.Delay(addDelay.Value); // Wait before cancelling, otherwise the scanner can crash in the following scan
                    scanner.Cancel();
                    await Task.Delay(addDelay.Value); // Wait after cancelling, for the UI to update properly
                }

                return scanResult.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Barcode scanning failed");
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        //private async Task<List<string>> ReadMultipleFromScanner(ZXing.BarcodeFormat? barcodeFormat = null)
        //{
        //    try
        //    {
        //        var scanner = new MyMobileBarcodeScanner();

        //        var options = BaseOptionsFactory();

        //        if (!barcodeFormat.HasValue)
        //        {
        //            var selectedBarcodeType = BarcodeTypes[SpinBarcodeType.SelectedItemPosition];
        //            options.PossibleFormats = new List<ZXing.BarcodeFormat>() { (ZXing.BarcodeFormat)selectedBarcodeType.Value };
        //        }
        //        else
        //        {
        //            options.PossibleFormats = new List<ZXing.BarcodeFormat>() { barcodeFormat.Value };
        //        }

        //        List<string> results = new List<string>();
        //        for (int i = 0; i < 10; i++)
        //        {
        //            var res = await scanner.ScanOneByOneAsync(this, options);
        //            results.Add(res.Text);
        //            await Task.Delay(500); // Wait before cancelling, otherwise the scanner can crash in the following scan
        //            scanner.Cancel();
        //            await Task.Delay(500); // Wait after cancelling, for the UI to update properly
        //        }

        //        return results;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error: Barcode scanning failed");
        //        Console.WriteLine(ex.Message);
        //        return "";
        //    }
        //}

        private async Task SendBarcodeData(string scanned)
        {
            try
            {
                var client = new HttpClient();
                var content = new StringContent(scanned, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync($"{TxtWebAppEndpoint.Text}/TestingArea/DataFromReader", content);
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: HttpClient failed: {resp.StatusCode}: {resp.ReasonPhrase}");
                    Toast.MakeText(this, "Error: HttpClient failed", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: SendBarcodeData failed: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            BtnScanWebAppConfiguration.Click -= OnBtnScanConfiguration_Click;
            BtnScanSingle.Click -= OnBtnScanSingle_Click;
            BtnScanContinuous.Click -= OnBtnScanContinuous_Click;
        }
    }
}