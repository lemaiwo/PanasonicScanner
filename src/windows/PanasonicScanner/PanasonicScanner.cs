using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Core;
using Windows.UI.Popups;
using System.Runtime.InteropServices.WindowsRuntime;

namespace PanasonicScanner
{
    public interface IBarcode
    {
        String Code { get; }
    }
    public sealed class Barcode : IBarcode
    {
        private string _code;

        public string Code
        {
            get
            {
                return _code;
            }
        }
        internal Barcode(String code)
        {
            _code = code;
        }

    }
    public sealed class PanasonicScanner
    {
        BarcodeScanner scanner = null;
        ClaimedBarcodeScanner claimedScanner = null;
        public event EventHandler<Object> BarcodeEvent;

        private void OnBarcode(string args)
        {
            try
            {
                Object barcode = new Barcode(args);
                var completedEvent = BarcodeEvent;
                if (completedEvent != null)
                {
                    completedEvent(this, barcode);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error sending event");
            }
        }
        public bool BarcodeDeviceDetected
        {
            get
            {
                return claimedScanner != null;
            }
        }

        public string BarcodeDeviceId
        {
            get
            {
                return BarcodeDeviceDetected ? claimedScanner.DeviceId : string.Empty;
            }
        }

        public IAsyncOperation<bool> activate()
        {
            Debug.WriteLine("Plugin activate function start");
            return this.activateHelper().AsAsyncOperation();
        }

        private async Task<bool> activateHelper()
        {
            Debug.WriteLine("Creating barcode scanner object.");

            // create the barcode scanner. 
            if (await CreateDefaultScannerObject())
            {
                // after successful creation, claim the scanner for exclusive use and enable it so that data reveived events are received.
                if (await ClaimScanner())
                {

                    // It is always a good idea to have a release device requested event handler. If this event is not handled, there are chances of another app can 
                    // claim ownsership of the barcode scanner.
                    claimedScanner.ReleaseDeviceRequested += claimedScanner_ReleaseDeviceRequested;

                    // after successfully claiming, attach the datareceived event handler.
                    claimedScanner.DataReceived += claimedScanner_DataReceived;

                    // Ask the API to decode the data by default. By setting this, API will decode the raw data from the barcode scanner and 
                    // send the ScanDataLabel and ScanDataType in the DataReceived event
                    claimedScanner.IsDecodeDataEnabled = true;

                    // enable the scanner.
                    // Note: If the scanner is not enabled (i.e. EnableAsync not called), attaching the event handler will not be any useful because the API will not fire the event 
                    // if the claimedScanner has not beed Enabled
                    await claimedScanner.EnableAsync();

                    Debug.WriteLine("Barcodescanner is ready. Device ID: " + claimedScanner.DeviceId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool Deactivate()
        {
            if (claimedScanner != null)
            {
                try
                {
                    // Detach the event handlers
                    claimedScanner.DataReceived -= claimedScanner_DataReceived;
                    claimedScanner.ReleaseDeviceRequested -= claimedScanner_ReleaseDeviceRequested;

                    // Release the Barcode Scanner and set to null
                    claimedScanner.Dispose();
                    claimedScanner = null;
                }
                catch (Exception)
                {
                }

            }

            scanner = null;

            return true;
        }
        public void Dispose()
        {
            this.Deactivate();
        }
        async void claimedScanner_ReleaseDeviceRequested(object sender, ClaimedBarcodeScanner e)
        {
            await CoreApplication.GetCurrentView().CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // always retain the device
                e.RetainDevice();

                Debug.WriteLine("Event ReleaseDeviceRequested received. Retaining the barcode scanner.");
            });
        }

        string GetDataString(IBuffer data)
        {
            StringBuilder result = new StringBuilder();

            if (data == null)
            {
                result.Append("No data");
            }
            else
            {
                // Just to show that we have the raw data, we'll print the value of the bytes.
                // Arbitrarily limit the number of bytes printed to 20 so the UI isn't overloaded.
                const uint MAX_BYTES_TO_PRINT = 20;
                uint bytesToPrint = Math.Min(data.Length, MAX_BYTES_TO_PRINT);

                DataReader reader = DataReader.FromBuffer(data);
                byte[] dataBytes = new byte[bytesToPrint];
                reader.ReadBytes(dataBytes);

                for (uint byteIndex = 0; byteIndex < bytesToPrint; ++byteIndex)
                {
                    result.AppendFormat("{0:X2} ", dataBytes[byteIndex]);
                }

                if (bytesToPrint < data.Length)
                {
                    result.Append("...");
                }
            }

            return result.ToString();
        }

        string GetDataLabelString(IBuffer data, uint scanDataType)
        {
            string result = null;

            if (data == null)
            {
                result = string.Empty;
            }
            else
            {
                switch (BarcodeSymbologies.GetName(scanDataType))
                {
                    case "Upca":
                    case "UpcaAdd2":
                    case "UpcaAdd5":
                    case "Upce":
                    case "UpceAdd2":
                    case "UpceAdd5":
                    case "Ean8":
                    case "Ean13":
                    case "TfStd":
                    case "Code39":
                    case "Code128":
                    case "Qr":
                        // The UPC, EAN8, and 2 of 5 families encode the digits 0..9
                        // which are then sent to the app in a UTF8 string (like "01234")
                        DataReader reader = DataReader.FromBuffer(data);
                        result = reader.ReadString(data.Length);
                        break;

                    default:
                        // Some other symbologies (typically 2-D symbologies) contain binary data that
                        //  should not be converted to text.
                        result = string.Format("Decoded data unavailable. Raw label data: {0}", GetDataString(data));
                        break;
                }
            }

            return result;
        }


        /// <summary>
        /// Event handler for the DataReceived event fired when a barcode is scanned by the barcode scanner 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"> Contains the BarcodeScannerReport which contains the data obtained in the scan</param>
        public async void claimedScanner_DataReceived(ClaimedBarcodeScanner sender, BarcodeScannerDataReceivedEventArgs args)
        {
            try
            {
                var barcodeLabel = GetDataLabelString(args.Report.ScanDataLabel, args.Report.ScanDataType);
                Debug.WriteLine("BARCODE");
                Debug.WriteLine(barcodeLabel);

                Task.Run(() =>
                {
                    if (BarcodeEvent != null)
                    {
                        OnBarcode(barcodeLabel);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not send barcode to ViewModel");
            }
        }

        private async Task<bool> CreateDefaultScannerObject()
        {
            if (scanner == null)
            {
                Debug.WriteLine("Creating barcode scanner object: Retrieving default device");

                try
                {
                    scanner = await BarcodeScanner.GetDefaultAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not retrieve default barcode device: " + ex.Message);
                    return false;
                }

                if (scanner == null)
                {
                    Debug.WriteLine("Barcode scanner not found. Please connect a barcode scanner.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This method claims the barcode scanner 
        /// </summary>
        /// <returns>true if claim is successful. Otherwise returns false</returns>
        private async Task<bool> ClaimScanner()
        {
            if (claimedScanner == null)
            {
                try
                {
                    // claim the barcode scanner
                    claimedScanner = await scanner.ClaimScannerAsync();
                    return true;
                }
                catch (Exception)
                {
                }

                // enable the claimed barcode scanner
                if (claimedScanner == null)
                {
                    Debug.WriteLine("Claim barcode scanner failed.");
                    return false;
                }
            }
            return true;
        }

        public async void SoftTrigger()
        {
            if (claimedScanner != null)
            {
                await claimedScanner.StartSoftwareTriggerAsync();
            }
        }
    }
}
