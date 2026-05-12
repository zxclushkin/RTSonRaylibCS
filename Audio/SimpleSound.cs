using Raylib_cs;

namespace TinyRts.Audio;

public sealed class SimpleSound : IDisposable
{
    readonly Sound clickSound;

    public SimpleSound()
    {
        Raylib.InitAudioDevice();
        clickSound = CreateToneSound(880, 0.08f);
    }

    public void PlayClick()
    {
        Raylib.PlaySound(clickSound);
    }

    public void Dispose()
    {
        Raylib.UnloadSound(clickSound);
        Raylib.CloseAudioDevice();
    }

    static Sound CreateToneSound(int frequency, float durationSeconds)
    {
        const int sampleRate = 44100;
        const short bitsPerSample = 16;
        const short channels = 1;
        var sampleCount = (int)(sampleRate * durationSeconds);
        var pcm = new short[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = 1f - i / (float)sampleCount;
            var sample = MathF.Sin(2 * MathF.PI * frequency * t) * envelope;
            pcm[i] = (short)(sample * short.MaxValue * 0.25f);
        }

        var wavData = BuildWav(pcm, sampleRate, bitsPerSample, channels);
        var wave = Raylib.LoadWaveFromMemory(".wav", wavData);
        var sound = Raylib.LoadSoundFromWave(wave);
        Raylib.UnloadWave(wave);
        return sound;
    }

    static byte[] BuildWav(short[] pcm, int sampleRate, short bitsPerSample, short channels)
    {
        var dataSize = pcm.Length * sizeof(short);
        var buffer = new byte[44 + dataSize];
        using var ms = new MemoryStream(buffer);
        using var bw = new BinaryWriter(ms);

        bw.Write("RIFF"u8.ToArray());
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8.ToArray());
        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write(channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * bitsPerSample / 8);
        bw.Write((short)(channels * bitsPerSample / 8));
        bw.Write(bitsPerSample);
        bw.Write("data"u8.ToArray());
        bw.Write(dataSize);

        foreach (var sample in pcm)
        {
            bw.Write(sample);
        }

        return buffer;
    }
}
