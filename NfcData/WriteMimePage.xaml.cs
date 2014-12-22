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
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace NfcData
{
    /// <summary>
    /// Page for writing mime messages to nfc tags.
    /// </summary>
    public sealed partial class WriteMimePage : Page
    {
        /// <summary>
        /// The nfc communication device.
        /// </summary>
        private ProximityDevice device;

        /// <summary>
        /// Id for the subscription, this is used to unsubscribe, when the subscription is no longer needed.
        /// </summary>
        private long publishedMessageId;

        public WriteMimePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Handle hardware back button
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            // Get the nfc device
            this.device = ProximityDevice.GetDefault();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;

            this.StopPublish();
        }

        private async void WriteHelloWorld_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.StopPublish();

            // Test if nfc is available
            if (device != null)
            {
                var buffer = Encoding.UTF8.GetBytes("Hello world!");

                // Write the text to a nfc tag
                this.publishedMessageId = this.device.PublishBinaryMessage("WindowsMime:WriteTag.text/plain", buffer.AsBuffer(), this.PublishedHandler);

                // Publish the text to nfc devices
                // this.publishedMessageId = this.device.PublishBinaryMessage("WindowsMime.text/plain", buffer.AsBuffer(), this.PublishedHandler);
            }
            else
            {
                await new MessageDialog("Your phone has no NFC or it is disabled").ShowAsync();
            }

            StatusText.Text = "Waiting for tag...";
        }

        private async void WritePng_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.StopPublish();

            // Test if nfc is available
            if (device != null)
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/tuna-icon.png", UriKind.RelativeOrAbsolute));

                using (Stream stream = await file.OpenStreamForReadAsync())
                {
                    byte[] array = new byte[stream.Length];
                    stream.Read(array, 0, (int)stream.Length);

                    // Write the image to a nfc tag
                    this.publishedMessageId = this.device.PublishBinaryMessage("WindowsMime:WriteTag.image/png", array.AsBuffer(), this.PublishedHandler);

                    // Publish the image to nfc devices
                    // this.publishedMessageId = this.device.PublishBinaryMessage("WindowsMime.image/png", array.AsBuffer(), this.PublishedHandler);
                }
            }
            else
            {
                await new MessageDialog("Your phone has no NFC or it is disabled").ShowAsync();
            }

            StatusText.Text = "Waiting for tag...";
        }

        /// <summary>
        /// Invoked when nfc message was published.
        /// </summary>
        /// <param name="sender">The nfc device the message was subscribed with</param>
        /// <param name="messageId">The id of the written message</param>
        private async void PublishedHandler(ProximityDevice sender, long messageId)
        {
            // Let the device vibrate to indicate that a new message was written
            var vibrationDevice = VibrationDevice.GetDefault();
            if (vibrationDevice != null)
            {
                vibrationDevice.Vibrate(TimeSpan.FromSeconds(0.2));
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusText.Text = "Data was written!";
            });

            // Stops the publishing of the current message
            sender.StopPublishingMessage(messageId);
        }

        /// <summary>
        /// Stops the publishing.
        /// </summary>
        private void StopPublish()
        {
            if (device != null)
            {
                // Stops the publishing of the current message
                this.device.StopPublishingMessage(this.publishedMessageId);
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
