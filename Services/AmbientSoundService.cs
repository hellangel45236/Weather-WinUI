using System;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ phát âm thanh thời tiết tự nhiên (Ambient Soundscapes / White Noise)
/// Tự động sinh sóng âm thanh thiên nhiên chân thực (Procedural Audio)
/// chạy lặp vô tận (Seamless Loop), không tốn RAM và không cần kết nối mạng.
/// </summary>
public class AmbientSoundService : IDisposable
{
    private MediaPlayer? _mediaPlayer;
    private string? _tempAudioFilePath;
    private bool _isPlaying;
    private double _volume = 0.5;

    public bool IsPlaying => _isPlaying;

    public AmbientSoundType CurrentType { get; private set; } = AmbientSoundType.Rain;

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

        AmbientSoundType type = effect switch
        {
            WeatherEffectType.Thunderstorm => AmbientSoundType.Thunderstorm,
            WeatherEffectType.HeavyRain or WeatherEffectType.ModerateRain => AmbientSoundType.Rain,
            WeatherEffectType.LightRain => AmbientSoundType.CafeRain,
            WeatherEffectType.Fog => AmbientSoundType.PineWind,
            _ => AmbientSoundType.PineWind
        };

        Play(type);
    }

    public enum AmbientSoundType
    {
        Rain,           // 🌧️ Mưa rào mùa hạ
        Thunderstorm,   // ⛈️ Sấm chớp đêm mưa
        PineWind,       // 🌲 Gió rừng thông Đà Lạt
        OceanWaves,     // 🌊 Sóng biển Nha Trang
        CafeRain        // ☕ Mưa quán cà phê
    }

    public static AmbientSoundType ParseType(string? name) => name switch
    {
        "Thunderstorm" => AmbientSoundType.Thunderstorm,
        "PineWind" => AmbientSoundType.PineWind,
        "OceanWaves" => AmbientSoundType.OceanWaves,
        "CafeRain" => AmbientSoundType.CafeRain,
        _ => AmbientSoundType.Rain
    };

    public void Play(AmbientSoundType type)
    {
        try
        {
            CurrentType = type;

            if (_mediaPlayer == null)
            {
                _mediaPlayer = new MediaPlayer
                {
                    IsLoopingEnabled = true,
                    Volume = _volume
                };
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "WeatherAppAudio");
            Directory.CreateDirectory(tempDir);
            string fileName = type switch
            {
                AmbientSoundType.Thunderstorm => "ambient_thunderstorm.wav",
                AmbientSoundType.PineWind => "ambient_pinewind.wav",
                AmbientSoundType.OceanWaves => "ambient_oceanwaves.wav",
                AmbientSoundType.CafeRain => "ambient_caferain.wav",
                _ => "ambient_rain.wav"
            };
            _tempAudioFilePath = Path.Combine(tempDir, fileName);

            if (!File.Exists(_tempAudioFilePath) || new FileInfo(_tempAudioFilePath).Length < 1000)
            {
                switch (type)
                {
                    case AmbientSoundType.Thunderstorm:
                        GenerateThunderstormWav(_tempAudioFilePath, durationSeconds: 8);
                        break;
                    case AmbientSoundType.PineWind:
                        GeneratePineWindWav(_tempAudioFilePath, durationSeconds: 7);
                        break;
                    case AmbientSoundType.OceanWaves:
                        GenerateOceanWavesWav(_tempAudioFilePath, durationSeconds: 8);
                        break;
                    case AmbientSoundType.CafeRain:
                        GenerateCafeRainWav(_tempAudioFilePath, durationSeconds: 6);
                        break;
                    default:
                        GenerateRainSoundWav(_tempAudioFilePath, durationSeconds: 6);
                        break;
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
            System.Diagnostics.Debug.WriteLine($"[AmbientSound] Error playing {type}: {ex.Message}");
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
    /// 1. Mưa rào mùa hạ: Pink noise + giọt nước tí tách
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
            double white = (random.NextDouble() * 2.0) - 1.0;
            b0 = 0.99886 * b0 + white * 0.0555179;
            b1 = 0.99332 * b1 + white * 0.0750759;
            b2 = 0.96900 * b2 + white * 0.1538520;
            b3 = 0.86650 * b3 + white * 0.3104856;
            b4 = 0.55000 * b4 + white * 0.5329522;
            b5 = -0.7616 * b5 - white * 0.0168980;
            double pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
            b6 = white * 0.115926;

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
    /// 2. Sấm chớp đêm mưa: Mưa to dồn dập kèm tiếng sấm rền trầm ấm từ xa
    /// </summary>
    private static void GenerateThunderstormWav(string filePath, int durationSeconds)
    {
        int sampleRate = 22050;
        short channels = 1;
        short bitsPerSample = 16;
        int totalSamples = sampleRate * durationSeconds;

        var random = new Random(99);
        short[] samples = new short[totalSamples];

        double b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;

        for (int i = 0; i < totalSamples; i++)
        {
            double white = (random.NextDouble() * 2.0) - 1.0;
            b0 = 0.99886 * b0 + white * 0.0555179;
            b1 = 0.99332 * b1 + white * 0.0750759;
            b2 = 0.96900 * b2 + white * 0.1538520;
            b3 = 0.86650 * b3 + white * 0.3104856;
            b4 = 0.55000 * b4 + white * 0.5329522;
            b5 = -0.7616 * b5 - white * 0.0168980;
            double pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
            b6 = white * 0.115926;

            double timeSec = (double)i / sampleRate;
            double thunder = 0;
            if (timeSec >= 1.5 && timeSec <= 3.8)
            {
                double envelope = Math.Sin((timeSec - 1.5) / 2.3 * Math.PI);
                double freq = 45.0 + 8.0 * Math.Sin(timeSec * 7.0);
                thunder = Math.Sin(2.0 * Math.PI * freq * timeSec) * envelope * 0.45;
            }
            else if (timeSec >= 5.2 && timeSec <= 7.2)
            {
                double envelope = Math.Sin((timeSec - 5.2) / 2.0 * Math.PI);
                double freq = 38.0 + 6.0 * Math.Sin(timeSec * 5.0);
                thunder = Math.Sin(2.0 * Math.PI * freq * timeSec) * envelope * 0.35;
            }

            double sample = (pink * 0.16) + thunder;
            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * short.MaxValue * 0.48);
        }

        WriteWavFile(filePath, samples, sampleRate, channels, bitsPerSample);
    }

    /// <summary>
    /// 3. Gió rừng thông Đà Lạt: Brown noise lọc sâu với tiếng gió rít êm qua rặng thông
    /// </summary>
    private static void GeneratePineWindWav(string filePath, int durationSeconds)
    {
        int sampleRate = 22050;
        short channels = 1;
        short bitsPerSample = 16;
        int totalSamples = sampleRate * durationSeconds;

        var random = new Random(108);
        short[] samples = new short[totalSamples];

        double lastBrown = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double white = (random.NextDouble() * 2.0) - 1.0;
            lastBrown = (lastBrown + (0.02 * white)) / 1.02;

            double timeSec = (double)i / sampleRate;
            double lfo = 0.5 + 0.5 * Math.Sin((2.0 * Math.PI * timeSec) / 3.5);
            double pineHarmonic = Math.Sin(2.0 * Math.PI * 440.0 * timeSec) * 0.04 * lfo;

            double sample = (lastBrown * 3.5 * lfo) + pineHarmonic;
            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * short.MaxValue * 0.42);
        }

        WriteWavFile(filePath, samples, sampleRate, channels, bitsPerSample);
    }

    /// <summary>
    /// 4. Sóng biển Nha Trang: Từng đợt sóng dạt dào xô bờ cát và bọt nước tan
    /// </summary>
    private static void GenerateOceanWavesWav(string filePath, int durationSeconds)
    {
        int sampleRate = 22050;
        short channels = 1;
        short bitsPerSample = 16;
        int totalSamples = sampleRate * durationSeconds;

        var random = new Random(77);
        short[] samples = new short[totalSamples];

        double lastBrown = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double white = (random.NextDouble() * 2.0) - 1.0;
            lastBrown = (lastBrown + (0.03 * white)) / 1.03;

            double wavePeriod = 4.0;
            double timeInWave = ((double)i / sampleRate) % wavePeriod;
            double wavePhase = timeInWave / wavePeriod; // 0.0 -> 1.0

            double surge;
            if (wavePhase < 0.6)
            {
                surge = Math.Sin(wavePhase / 0.6 * Math.PI * 0.5);
            }
            else
            {
                surge = Math.Cos((wavePhase - 0.6) / 0.4 * Math.PI * 0.5);
            }

            double foam = (white * 0.08) * (wavePhase > 0.45 && wavePhase < 0.75 ? 1.0 : 0.2);

            double sample = (lastBrown * 2.8 * surge) + foam;
            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * short.MaxValue * 0.45);
        }

        WriteWavFile(filePath, samples, sampleRate, channels, bitsPerSample);
    }

    /// <summary>
    /// 5. Mưa quán cà phê: Tiếng mưa tí tách trầm ấm nghe từ bên trong ô cửa kính
    /// </summary>
    private static void GenerateCafeRainWav(string filePath, int durationSeconds)
    {
        int sampleRate = 22050;
        short channels = 1;
        short bitsPerSample = 16;
        int totalSamples = sampleRate * durationSeconds;

        var random = new Random(222);
        short[] samples = new short[totalSamples];

        double lowPass1 = 0, lowPass2 = 0;

        for (int i = 0; i < totalSamples; i++)
        {
            double white = (random.NextDouble() * 2.0) - 1.0;

            lowPass1 = lowPass1 + 0.08 * (white - lowPass1);
            lowPass2 = lowPass2 + 0.08 * (lowPass1 - lowPass2);

            double softDroplet = 0;
            if (random.Next(350) == 0)
            {
                softDroplet = (random.NextDouble() * 0.8) - 0.4;
            }

            double sample = (lowPass2 * 0.9) + softDroplet;
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

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataChunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write(channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write(bitsPerSample);

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
