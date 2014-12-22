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
    /// Page for reading custom data over nfc.
    /// </summary>
    public sealed partial class ReadCustomDataPage : Page
    {
        /// <summary>
        /// The nfc communication device.
        /// </summary>
        private ProximityDevice device;

        /// <summary>
        /// Id for the subscription, this is used to unsubscribe, when the subscription is no longer needed.
        /// </summary>
        private long subscribedMessageId;

        public ReadCustomDataPage()
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
                // Subscribe to the windows protocol and the custom subtype
                this.subscribedMessageId = device.SubscribeForMessage("Windows.Person", this.MessageReceivedHandler);
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

            byte[] array = new byte[buffer.Length];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                // Deserialize object
                var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(Person));
                var item = (Person)serializer.ReadObject(stream);

                // Display result in text area - use the dispatcher, as the reading occurs in a separate thread
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.TextBlockFirstName.Text = item.FirstName;
                    this.TextBlockLastName.Text = item.LastName;
                });

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
