using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.UI.Xaml.Input;

namespace TalkTimer.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaCapture _mediaCapture;
        private bool _isMuted = true;
        private DispatcherTimer _timer;
        private int _elapsedSeconds;

        public MainWindow()
        {
            this.InitializeComponent();
            _ = LoadInputDevices(); // Discard the Task to avoid a compiler warning

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private async Task LoadInputDevices()
        {
            try
            {
                var audioInputSelector = MediaDevice.GetAudioCaptureSelector();
                var inputDevices = await DeviceInformation.FindAllAsync(audioInputSelector);

                InputDeviceComboBox.ItemsSource = inputDevices;
                InputDeviceComboBox.DisplayMemberPath = "Name";

                if (inputDevices.Count > 0)
                {
                    InputDeviceComboBox.SelectedIndex = 0;
                    await InitializeMediaCapture(inputDevices[0].Id);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as appropriate
            }
        }

        private async Task InitializeMediaCapture(string deviceId)
        {
            _mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                AudioDeviceId = deviceId,
                StreamingCaptureMode = StreamingCaptureMode.Audio
            };

            await _mediaCapture.InitializeAsync(settings);
        }

        private void MicrophoneIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                _isMuted = !_isMuted;
                // MicrophoneIcon.Source = new BitmapImage(new Uri(_isMuted ? "Assets/muted_microphone.png" : "Assets/unmuted_microphone.png", UriKind.Relative));

                if (_mediaCapture != null)
                {
                    var audioDeviceController = _mediaCapture.AudioDeviceController;
                    audioDeviceController.Muted = _isMuted;
                }

                if (_isMuted)
                {
                    _timer.Stop();
                    _elapsedSeconds = 0;
                    TimerText.Text = "0";
                }
                else
                {
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as appropriate
            }
        }

        private async void InputDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InputDeviceComboBox.SelectedItem is DeviceInformation selectedDevice)
            {
                await InitializeMediaCapture(selectedDevice.Id);
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            _elapsedSeconds++;
            TimerText.Text = _elapsedSeconds.ToString();
        }
    }
}
