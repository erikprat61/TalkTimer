namespace TalkTimer.Wpf;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NAudio.CoreAudioApi;

public sealed partial class MainWindow : Window
{
    private DispatcherTimer _timer = default!;
    private MMDevice _selectedDevice = default!;
    private string _selectedDeviceId = default!;
    private int _elapsedSeconds = 0;

    public MainWindow()
    {
        InitializeComponent();
        LoadInputDevices();
        InitializeTimer();
        StartMonitoringMuteState();
    }

    private void DevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DevicesComboBox.SelectedIndex >= 0)
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            _selectedDeviceId = devices[DevicesComboBox.SelectedIndex].ID;
        }
    }

    private void InitializeTimer()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        _elapsedSeconds++;
        TimerTextBlock.Text = TimeSpan.FromSeconds(_elapsedSeconds).ToString(@"mm\:ss");
    }

    private void LoadInputDevices()
    {
        var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

        foreach (var device in devices)
        {
            DevicesComboBox.Items.Add(device.FriendlyName);
        }

        if (DevicesComboBox.Items.Count > 0)
        {
            DevicesComboBox.SelectedIndex = 0;
            _selectedDeviceId = devices[0].ID;
        }
    }

    private void StartMonitoringMuteState()
    {
        Task.Run(async () =>
        {
            var enumerator = new MMDeviceEnumerator();

            while (true)
            {
                await Task.Delay(500); // Check every 500 milliseconds

                if (_selectedDeviceId != null)
                {
                    // Get the selected device using its ID
                    var selectedDevice = enumerator.GetDevice(_selectedDeviceId);
                    bool isMuted = selectedDevice.AudioEndpointVolume.Mute;

                    if (isMuted)
                    {
                        if (_timer.IsEnabled)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _timer.Stop();
                                _elapsedSeconds = 0;
                                TimerTextBlock.Text = "00:00";
                            });
                        }
                    }
                    else
                    {
                        if (!_timer.IsEnabled)
                        {
                            Dispatcher.Invoke(() => _timer.Start());
                        }
                    }
                }
            }
        });
    }
}
