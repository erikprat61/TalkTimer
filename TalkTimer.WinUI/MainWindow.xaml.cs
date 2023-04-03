using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Media.Capture;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;

namespace TalkTimer.WinUI;

public sealed partial class MainWindow : Window
{
    private MediaCapture _mediaCapture;
    private bool _isMuted = true;
    private int _elapsedSeconds;
    private DispatcherTimer _timer;
    private DispatcherTimer _muteStatusPollingTimer;

    public MainWindow()
    {
        this.InitializeComponent();
        _ = LoadInputDevices(); // Discard the Task to avoid a compiler warning

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        _muteStatusPollingTimer = new DispatcherTimer();
        _muteStatusPollingTimer.Interval = TimeSpan.FromSeconds(1);
        _muteStatusPollingTimer.Tick += MuteStatusPollingTimer_Tick;
        _muteStatusPollingTimer.Start();


        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(400, 140));

        var overlappedPresenter = appWindow.Presenter as OverlappedPresenter;
        overlappedPresenter.IsResizable = false;
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
        catch (Exception)
        {
            throw;
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
            if (_mediaCapture != null)
            {
                var audioDeviceController = _mediaCapture.AudioDeviceController;
                bool isMuted = audioDeviceController.Muted;
                audioDeviceController.Muted = !isMuted;
            }
        }
        catch (Exception)
        {
            throw;
            // Handle exceptions as appropriate
        }
    }

    private void MuteStatusPollingTimer_Tick(object sender, object e)
    {
        if (_mediaCapture != null)
        {
            var audioDeviceController = _mediaCapture.AudioDeviceController;
            bool isMuted = audioDeviceController.Muted;

            if (isMuted != _isMuted)
            {
                _isMuted = isMuted;

                if (_isMuted)
                {
                    _timer.Stop();
                    _elapsedSeconds = 0;
                    TimerText.Text = "0";
                    MainGrid.Background = null;
                }
                else
                {
                    _timer.Start();
                }
            }
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

        if (_elapsedSeconds < 10)
        {
            TimerText.Text = "0" + _elapsedSeconds.ToString();
        }
        else
        {
            TimerText.Text = _elapsedSeconds.ToString();
        }

        if (_elapsedSeconds > 15 && _elapsedSeconds <= 30)
        {
            // Set the background color to yellow
            MainGrid.Background = new SolidColorBrush(Colors.Yellow);
        }
        else if (_elapsedSeconds > 30)
        {
            // Set the background color to red
            MainGrid.Background = new SolidColorBrush(Colors.Red);
        }
    }
}
