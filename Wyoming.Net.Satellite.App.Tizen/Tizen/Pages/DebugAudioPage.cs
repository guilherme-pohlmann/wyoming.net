using System;
using System.IO;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Components;
using Wyoming.Net.Satellite;
using Wyoming.Net.Satellite.App.Tz.Components;
using Wyoming.Net.Satellite.App.Tz.Platform;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class DebugAudioPage : ContentPage, ISelectableView
{
    private readonly View _parent;
    private readonly ScrollableBase _scrollable;
    private readonly TextLabel _statusLabel;

    private TizenAudioFocusManager? _audioFocusManager;
    private TizenSpeakerProvider? _speakerProvider;
    private bool _isPlaying;
    private Button? _firstButton;

    public DebugAudioPage(View parent)
    {
        _parent = parent;

        _scrollable = new ScrollableBase
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            ScrollingDirection = ScrollableBase.Direction.Vertical,
            Padding = new Extents(200, 200, 20, 20),
            Layout = new LinearLayout
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
            }
        };

        _statusLabel = new TextLabel("Debug audio files")
        {
            PointSize = 22,
            TextColor = new Color("#9CA3AF"),
            Margin = new Extents(0, 0, 0, 30),
            Focusable = false,
            MultiLine = true,
            WidthResizePolicy = ResizePolicyType.FillToParent,
        };
        _scrollable.Add(_statusLabel);

        Content = _scrollable;
        Focusable = true;

        FocusGained += (s, args) =>
        {
            if (_firstButton != null)
                FocusManager.Instance.SetCurrentFocusView(_firstButton);
        };

        parent.ChildAdded += (s, args) =>
        {
            if (args.Added == this)
                RefreshFileList();
        };

        parent.ChildRemoved += (s, args) =>
        {
            if (args.Removed == this)
                Cleanup();
        };
    }

    public bool Selected { get; set; }

    public View View => this;

    private void RefreshFileList()
    {
        // Remove all children except the status label
        var children = _scrollable.Children.ToList();
        foreach (var child in children)
        {
            if (child != _statusLabel)
                _scrollable.Remove(child);
        }

        _firstButton = null;

        string dataDir = TizenAssetReader.DataDir;
        string[] files;
        try
        {
            files = Directory.GetFiles(dataDir, "ww_debug_*.wav")
                .OrderByDescending(File.GetCreationTime)
                .ToArray();
        }
        catch
        {
            files = Array.Empty<string>();
        }

        if (files.Length == 0)
        {
            _statusLabel.Text = "No debug audio files found";
            return;
        }

        _statusLabel.Text = $"{files.Length} debug audio file(s)";

        Button? previousButton = null;

        foreach (var filePath in files)
        {
            var row = new View
            {
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightSpecification = 80,
                Margin = new Extents(0, 0, 0, 10),
                Layout = new LinearLayout
                {
                    LinearOrientation = LinearLayout.Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Begin,
                    CellPadding = new Size2D(20, 0),
                }
            };

            var label = new TextLabel(System.IO.Path.GetFileName(filePath))
            {
                PointSize = 24,
                TextColor = new Color("#E5E7EB"),
                Focusable = false,
                WidthSpecification = 800,
                VerticalAlignment = VerticalAlignment.Center,
            };

            string capturedPath = filePath;
            var playBtn = new Button
            {
                Text = "Play",
                Focusable = true,
                WidthSpecification = 200,
                HeightSpecification = 70,
                BorderlineWidth = 2,
                BorderlineColor = TvStyle.ButtonBorderlineColor,
                BackgroundColor = new Color("#1F2937"),
                TextColor = Color.White,
            };

            playBtn.Clicked += (s, e) => PlayFileAsync(capturedPath, playBtn);

            playBtn.FocusGained += (s, e) =>
            {
                playBtn.BorderlineColor = TvStyle.ButtonFocusedBorderlineColor;
                playBtn.Scale = new Vector3(1.05f, 1.05f, 1);
            };

            playBtn.FocusLost += (s, e) =>
            {
                playBtn.BorderlineColor = TvStyle.ButtonBorderlineColor;
                playBtn.Scale = Vector3.One;
            };

            if (previousButton != null)
            {
                playBtn.UpFocusableView = previousButton;
                previousButton.DownFocusableView = playBtn;
            }
            else
            {
                playBtn.UpFocusableView = _parent;
                _firstButton = playBtn;
            }

            row.Add(label);
            row.Add(playBtn);
            _scrollable.Add(row);
            previousButton = playBtn;
        }
    }

    private async void PlayFileAsync(string filePath, Button button)
    {
        if (_isPlaying) return;
        _isPlaying = true;

        var originalText = button.Text;
        button.Text = "Playing...";

        try
        {
            _audioFocusManager ??= new TizenAudioFocusManager(TizenLogger.Singleton);
            _speakerProvider ??= new TizenSpeakerProvider(_audioFocusManager);

            var wav = await File.ReadAllBytesAsync(filePath);
            var wavInfo = WavHelper.ReadWavInfo(wav);
            var pcmData = WavHelper.ReadWavData(wav);

            await _speakerProvider.StartAsync(wavInfo.SampleRate, wavInfo.BytesPerSample, wavInfo.Channels);
            await _speakerProvider.PlayAsync(pcmData, null);
            await _speakerProvider.StopAsync();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Playback error: {ex.Message}";
        }
        finally
        {
            button.Text = originalText;
            _isPlaying = false;
        }
    }

    private void Cleanup()
    {
        _isPlaying = false;
        _speakerProvider?.Dispose();
        _speakerProvider = null;
        _audioFocusManager?.Dispose();
        _audioFocusManager = null;
    }
}
