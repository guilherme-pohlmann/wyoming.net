using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wyoming.Net.Satellite.App.Tz.ViewModels;

public sealed class SatelliteStateViewModel : INotifyPropertyChanged
{
    private bool _isRunning;
    private bool _isPaused;
    private bool _isStreaming;
    private bool _serverConnected;
    private bool _micMuted;

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused != value)
            {
                _isPaused = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        set
        {
            if (_isStreaming != value)
            {
                _isStreaming = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ServerConnected
    {
        get => _serverConnected;
        set
        {
            if (_serverConnected != value)
            {
                _serverConnected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool MicMuted
    {
        get => _micMuted;
        set
        {
            if (_micMuted != value)
            {
                _micMuted = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
