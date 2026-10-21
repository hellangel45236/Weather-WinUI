using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WeatherApp.Helpers;

/// <summary>
/// Trình tạo và phát âm thanh cơ khí xúc giác (Mechanical Tactile SFX) phong cách Nothing Tech.
/// Tự tổng hợp sóng âm PCM WAV trực tiếp trong bộ nhớ (Zero External Assets),
/// tương thích 100% mọi phiên bản Windows 10 & 11 không cần codec phụ hay assembly ngoại vi.
/// </summary>
public static class MechanicalSoundHelper
{
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(byte[]? ptrToSound, IntPtr hmod, uint fdwSound);

    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_MEMORY = 0x0004;

    private static byte[]? _clickWav;
    private static byte[]? _tabSwitchWav;
    private static byte[]? _shutterWav;
    private static bool _isInitialized = false;

    private static readonly object _lock = new();

    private static void EnsureInitialized()
    {
        if (_isInitialized) return;
        lock (_lock)
        {
            if (_isInitialized) return;
            try
            {
                _clickWav = GeneratePcmWav(sampleRate: 44100, durationMs: 14, baseFreq: 2400, decayRate: 35.0, noiseFactor: 0.35);
                _tabSwitchWav = GeneratePcmWav(sampleRate: 44100, durationMs: 18, baseFreq: 1850, decayRate: 28.0, noiseFactor: 0.25);
                _shutterWav = GenerateDoublePcmWav(sampleRate: 44100, durationMs: 28);
                _isInitialized = true;
            }
            catch
            {
                // Fallback nếu hệ thống không có thiết bị âm thanh
            }
        }
    }

    /// <summary>
    /// Phát âm thanh click cơ khí khi bấm nút
    /// </summary>
    public static void PlayClick()
    {
        try
        {
            var settings = new Services.SettingsService().LoadSettings();
            if (!settings.EnableMechanicalSound) return;

            EnsureInitialized();
            if (_clickWav != null)
            {
                PlaySound(_clickWav, IntPtr.Zero, SND_ASYNC | SND_MEMORY | SND_NODEFAULT);
            }
        }
        catch { }
    }

    /// <summary>
    /// Phát âm thanh chuyển tab điều hướng
    /// </summary>
    public static void PlayTabSwitch()
    {
        try
        {
            var settings = new Services.SettingsService().LoadSettings();
            if (!settings.EnableMechanicalSound) return;

            EnsureInitialized();
            if (_tabSwitchWav != null)
            {
                PlaySound(_tabSwitchWav, IntPtr.Zero, SND_ASYNC | SND_MEMORY | SND_NODEFAULT);
            }
        }
        catch { }
    }

    /// <summary>
    /// Phát âm thanh màn trập đôi cơ khí khi làm mới hoặc áp dụng cài đặt
    /// </summary>
    public static void PlayShutter()
    {
        try
        {
            var settings = new Services.SettingsService().LoadSettings();
            if (!settings.EnableMechanicalSound) return;

            EnsureInitialized();
            if (_shutterWav != null)
            {
                PlaySound(_shutterWav, IntPtr.Zero, SND_ASYNC | SND_MEMORY | SND_NODEFAULT);
            }
        }
        catch { }
    }

    /// <summary>
    /// Sinh file WAV PCM 16-bit Mono trực tiếp trong bộ nhớ
    /// </summary>
    private static byte[] GeneratePcmWav(int sampleRate, int durationMs, double baseFreq, double decayRate, double noiseFactor)
    {
        int numSamples = (int)(sampleRate * (durationMs / 1000.0));
        short[] samples = new short[numSamples];
        var rnd = new Random(42);

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = Math.Exp(-decayRate * t * 100.0);
            double sine = Math.Sin(2.0 * Math.PI * baseFreq * t);
            double noise = (rnd.NextDouble() * 2.0 - 1.0) * noiseFactor;
            double sample = (sine * (1.0 - noiseFactor) + noise) * envelope;

            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * 18000.0); // Âm lượng vừa phải, thanh lịch
        }

        return CreateWavBytes(samples, sampleRate);
    }

    /// <summary>
    /// Sinh âm thanh cú nhắp kép (double click shutter)
    /// </summary>
    private static byte[] GenerateDoublePcmWav(int sampleRate, int durationMs)
    {
        int numSamples = (int)(sampleRate * (durationMs / 1000.0));
        short[] samples = new short[numSamples];
        var rnd = new Random(99);

        int pulse1 = 0;
        int pulse2 = (int)(sampleRate * 0.012); // cách nhau 12ms

        for (int i = 0; i < numSamples; i++)
        {
            double sample = 0;
            if (i >= pulse1 && i < pulse1 + (sampleRate * 0.008))
            {
                double t = (double)(i - pulse1) / sampleRate;
                double env = Math.Exp(-40.0 * t * 100.0);
                sample += Math.Sin(2.0 * Math.PI * 2800.0 * t) * env;
            }
            if (i >= pulse2 && i < pulse2 + (sampleRate * 0.010))
            {
                double t = (double)(i - pulse2) / sampleRate;
                double env = Math.Exp(-32.0 * t * 100.0);
                sample += Math.Sin(2.0 * Math.PI * 2100.0 * t) * env;
            }

            double noise = (rnd.NextDouble() * 2.0 - 1.0) * 0.2;
            sample += noise * Math.Exp(-30.0 * ((double)i / sampleRate) * 100.0);

            sample = Math.Clamp(sample, -1.0, 1.0);
            samples[i] = (short)(sample * 16000.0);
        }

        return CreateWavBytes(samples, sampleRate);
    }

    private static byte[] CreateWavBytes(short[] samples, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int subChunk2Size = samples.Length * 2;
        int chunkSize = 36 + subChunk2Size;

        // RIFF Header
        bw.Write(new char[] { 'R', 'I', 'F', 'F' });
        bw.Write(chunkSize);
        bw.Write(new char[] { 'W', 'A', 'V', 'E' });

        // Format Header
        bw.Write(new char[] { 'f', 'm', 't', ' ' });
        bw.Write(16); // SubChunk1Size (PCM = 16)
        bw.Write((short)1); // AudioFormat (1 = PCM)
        bw.Write((short)1); // NumChannels (1 = Mono)
        bw.Write(sampleRate); // SampleRate
        bw.Write(sampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        bw.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
        bw.Write((short)16); // BitsPerSample (16-bit)

        // Data Chunk
        bw.Write(new char[] { 'd', 'a', 't', 'a' });
        bw.Write(subChunk2Size);

        for (int i = 0; i < samples.Length; i++)
        {
            bw.Write(samples[i]);
        }

        bw.Flush();
        return ms.ToArray();
    }
}
