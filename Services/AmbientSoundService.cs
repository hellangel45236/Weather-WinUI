using System;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ phát âm thanh thời tiết tự nhiên (Ambient Soundscapes / White Noise)
/// Tự động sinh sóng âm thanh mưa rơi hoặc gió thoảng chân thực (Procedural Audio)
/// chạy lặp vô tận (Seamless Loop), không tốn RAM và không cần kết nối mạng.
/// </summary>
public class AmbientSoundService : IDisposable
{
    private MediaPlayer? _mediaPlayer;
    private string? _tempAudioFilePath;
    private bool _isPlaying;
    private double _volume = 0.5;

    public bool IsPlaying => _isPlaying;

    public AmbientSoundService()
    {
        try
        {
            _mediaPlayer = new MediaPlayer
            {
                IsLoopingEnabled = true,
                Volume = _volume
            };
        }
        catch { }
    }

    public void SetVolume(double volume)
    {
        _volume = Math.Clamp(volume, 0.0, 1.0);
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = _volume;
        }
    }

    public void PlayForWeather(WeatherEffectType effect, double volume = 0.5)
    {
        _volume = Math.Clamp(volume, 0.0, 1.0);

        bool isRain = effect is WeatherEffectType.LightRain 
            or WeatherEffectType.ModerateRain 
            or WeatherEffectType.HeavyRain 
            or WeatherEffectType.Thunderstorm;

        Play(isRain ? AmbientSoundType.Rain : AmbientSoundType.GentleBreeze);
    }

    public enum AmbientSoundType
    {
        Rain,
        GentleBreeze
    }

    public void Play(AmbientSoundType type)
    {
        try
        {
            if (_mediaPlayer == null)
            {
                _mediaPlayer = new MediaPlayer
                {
                    IsLoopingEnabled = true,
                    Volume = _volume
                };
            }

            // Tạo file sóng âm WAV thủ tục trong thư mục Temp nếu chưa có
            string tempDir = Path.Combine(Path.GetTempPath(), "WeatherAppAudio");
            Directory.CreateDirectory(tempDir);
            string fileName = type == AmbientSoundType.Rain ? "ambient_rain.wav" : "ambient_breeze.wav";
            _tempAudioFilePath = Path.Combine(tempDir, fileName);

            if (!File.Exists(_tempAudioFilePath) || new FileInfo(_tempAudioFilePath).Length < 1000)
            {
                if (type == AmbientSoundType.Rain)
                {
                    GenerateRainSoundWav(_tempAudioFilePath, durationSeconds: 6);
                }
                else
                {
                    GenerateBreezeSoundWav(_tempAudioFilePath, durationSeconds: 6);
                }
            }

            var mediaSource = MediaSource.CreateFromUri(new Uri(_tempAudioFilePath));
            _mediaPlayer.Source = mediaSource;
            _mediaPlayer.Volume = _volume;
            _mediaPlayer.Play();
            _isPlaying = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AmbientSound] Error playing: {ex.Message}");
            _isPlaying = false;
        }
    }

    public void Stop()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
            }
            _isPlaying = false;
        }
        catch { }
    }

    /// <summary>
    /// Thuật toán tạo sóng âm mưa rơi tí tách (Filtered Pink Noise + Droplet Bursts)
    /// </summary>
    private static void GenerateRainSoundWav(string filePath, int durationSeconds)
    {
        int sampleRate = 22050;
        short channels = 1;
        short bitsPerSample = 16;
        int totalSamples = sampleRate * durationSeconds;

        var random = new Random(42);
        short[] samples = new short[totalSamples];

        double b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;

        for (int i = 0; i < totalSamples; i++)
        {
            // Pink noise Paul Kellet filter
            double white = (random.NextDouble() * 2.0) - 1.0;
            b0 = 0.99886 * b0 + white * 0.0555179;
            b1 = 0.99332 * b1 + white * 0.0750759;
            b2 = 0.96900 * b2 + white * 0.1538520;
            b3 = 0.86650 * b3 + white * 0.3104856;
            b4 = 0.55000 * b4 + white * 0.5329522;
            b5 = -0.7616 * b5 - white * 0.0168980;
            double pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
            b6 = white * 0.115926;

            // Mưa rơi tí tách: thi thoảng có giọt nước nhỏ va chạm
            double droplet = 0;
            if (random.Next(250) == 0)
            {
                droplet = (random.NextDouble() * 1.5) - 0.75;
            }

            double sample = (pink * 0.14) + droplet;
            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * short.MaxValue * 0.45);
        }

        WriteWavFile(filePath, samples, sampleRate, channels, bitsPerSample);
    }

    /// <summary>
    /// Thuật toán tạo sóng âm gió thoảng dịu êm (Modulated Low-Pass Brown Noise)
    /// </summary>
    private static void GenerateBreezeSoundWav(string filePath, int durationSeconds)
    {
        int sampleRate = 22050;
        short channels = 1;
        short bitsPerSample = 16;
        int totalSamples = sampleRate * durationSeconds;

        var random = new Random(108);
        short[] samples = new short[totalSamples];

        double lastOutput = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double white = (random.NextDouble() * 2.0) - 1.0;
            // Brown noise (tích phân của white noise lọc tần số thấp)
            lastOutput = (lastOutput + (0.025 * white)) / 1.025;

            // Điều chế biên độ hình sin mô phỏng từng đợt gió thoảng
            double lfo = 0.6 + 0.4 * Math.Sin((2.0 * Math.PI * i) / (sampleRate * 2.5));
            double sample = lastOutput * 3.2 * lfo;

            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * short.MaxValue * 0.40);
        }

        WriteWavFile(filePath, samples, sampleRate, channels, bitsPerSample);
    }

    private static void WriteWavFile(string filePath, short[] samples, int sampleRate, short channels, short bitsPerSample)
    {
        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));
        int dataChunkSize = samples.Length * (bitsPerSample / 8);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataChunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // subchunk1 size
        bw.Write((short)1); // PCM format
        bw.Write(channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write(bitsPerSample);

        // data subchunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataChunkSize);

        for (int i = 0; i < samples.Length; i++)
        {
            bw.Write(samples[i]);
        }
    }

    public void Dispose()
    {
        try
        {
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
        }
        catch { }
    }
}
