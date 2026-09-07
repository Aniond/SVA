using System.Collections.Concurrent;
using System.Speech.Recognition;

namespace SolaceWeather.Relationships;

/// <summary>Opt-in, local Windows dictation. Callbacks enqueue text; only the menu touches game input.</summary>
internal sealed class PhoneDictation : IDisposable
{
    private SpeechRecognitionEngine? engine;
    private readonly ConcurrentQueue<string> results = new();
    internal bool Listening { get; private set; }
    internal string Status { get; private set; } = "";
    private DateTime started;
    private int generation;
    private readonly ConcurrentQueue<string> completions = new();

    internal void Start()
    {
        Dispose();
        if (!OperatingSystem.IsWindows()) { Status = "Dictation needs Windows speech recognition. You can still type."; return; }
        try
        {
            int session = generation;
            engine = new SpeechRecognitionEngine();
            engine.LoadGrammar(new DictationGrammar());
            engine.InitialSilenceTimeout = TimeSpan.FromSeconds(8);
            engine.BabbleTimeout = TimeSpan.FromSeconds(15);
            engine.SpeechRecognized += (_, e) =>
            {
                if (OperatingSystem.IsWindows() && session == Volatile.Read(ref generation) && e.Result.Confidence >= .35f)
                    results.Enqueue(e.Result.Text);
            };
            engine.RecognizeCompleted += (_, e) =>
            {
                if (session == Volatile.Read(ref generation))
                    completions.Enqueue(e.Error == null ? "Dictation finished. Review the draft, then Send." : "Dictation stopped unexpectedly. Check your microphone, or type instead.");
            };
            engine.SetInputToDefaultAudioDevice();
            engine.RecognizeAsync(RecognizeMode.Multiple);
            Listening = true; started = DateTime.UtcNow;
            Status = "Listening locally... Mic to stop. Review the draft, then Send.";
        }
        catch
        {
            Dispose();
            Status = "Mic unavailable. Check Windows microphone access and speech language.";
        }
    }

    internal string? Poll()
    {
        if (results.TryDequeue(out var text)) return text;
        if (completions.TryDequeue(out var status)) { Dispose(); Status = status; }
        else if (Listening && DateTime.UtcNow - started > TimeSpan.FromSeconds(30)) Stop();
        return null;
    }

    internal void Stop()
    {
        bool wasListening = Listening;
        Dispose();
        if (wasListening) Status = "Dictation stopped. Review the draft before sending.";
    }
    public void Dispose()
    {
        Listening = false;
        Interlocked.Increment(ref generation);
        var previous = engine; engine = null;
        if (previous != null && OperatingSystem.IsWindows())
        {
            try { previous.RecognizeAsyncCancel(); } catch { }
            previous.Dispose();
        }
        while (results.TryDequeue(out _)) { }
        while (completions.TryDequeue(out _)) { }
    }
}
