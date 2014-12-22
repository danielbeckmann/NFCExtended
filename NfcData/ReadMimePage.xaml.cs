using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Proximity;
using Windows.Phone.Devices.Notification;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace NfcData
{
    /// <summary>
    /// Page for reading mime messages.
    /// </summary>
    public sealed partial class ReadMimePage : Page
    {
        /// <summary>
        /// The nfc communication device.
        /// </summary>
        private ProximityDevice device;

        /// <summary>
        /// Id for the subscription, this is used to unsubscribe, when the subscription is no longer needed.
        /// </summary>
        private long subscribedMessageId;

        public ReadMimePage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Handle hardware back button
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            // Get the nfc device
            this.device = ProximityDevice.GetDefault();

            // Test if nfc is available
            if (device != null)
            {
                // Subscribe to mime messages
                this.subscribedMessageId = device.SubscribeForMessage("WindowsMime", this.MessageReceivedHandler);
            }
            else
            {
                await new MessageDialog("Your phone has no NFC or it is disabled").ShowAsync();
            }
        }

        /// <summary>
        /// Handles the incoming messages.
        /// </summary>
        /// <param name="device">The nfc device, the message is read from</param>
        /// <param name="message">The incoming message</param>
        private async void MessageReceivedHandler(ProximityDevice device, ProximityMessage message)
        {
            // Get the data
            var buffer = message.Data.ToArray();
            int mimesize = 0;

            // Search first '\0' character
            for (mimesize = 0; mimesize < 256; ++mimesize)
            {
                if (buffer[mimesize] == 0)
                {
                    break;
                }
            };

            // Extract mimetype
            var mimeType = Encoding.UTF8.GetString(buffer, 0, mimesize);

            // Handle mime message according to the mime type
            if (mimeType == "text/plain")
            {
                // Convert data to string, cut off the mime header
                var data = Encoding.UTF8.GetString(buffer, 256, buffer.Length - 256);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.TextArea.Text = data;
                });
            }
            else if (mimeType == "image/png")
            {
                // Get the image data, cut off the mime header
                byte[] array = new byte[buffer.Length - 256];
                System.Buffer.BlockCopy(buffer, 256, array, 0, buffer.Length - 256);

                using (var stream = new InMemoryRandomAccessStream())
                {
                    using (var writer = new DataWriter(stream))
                    {
                        writer.WriteBytes(array);
                        await writer.StoreAsync();
                        writer.DetachStream();
                    }

                    stream.Seek(0);

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var image = new BitmapImage();
                        image.SetSource(stream);

                        this.ImageRead.Source = image;
                    });
                }
            }

            // Let the device vibrate to indicate that a new message was read
            var vibrationDevice = VibrationDevice.GetDefault();
            if (vibrationDevice != null)
            {
                vibrationDevice.Vibrate(TimeSpan.FromSeconds(0.2));
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;

            if (device != null)
            {
                // Unsubscribe the handler
                this.device.StopSubscribingForMessage(this.subscribedMessageId);
            }
        }

        void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                e.Handled = true;
            }
        }
    }
}
