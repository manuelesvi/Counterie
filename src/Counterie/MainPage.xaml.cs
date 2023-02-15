using Humanizer;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Synthesis;
using Timer = System.Timers.Timer;
namespace MauiApp3;
#pragma warning disable CA1416 // Validate platform compatibility

public partial class MainPage : ContentPage
{
    static int steps = 0;
    static readonly CultureInfo _frCI = CultureInfo.GetCultureInfo("fr-FR");

    const int SayNumberTimeout_ms = 4250; // 4.25s

    int count = 0;
    bool isStarted;
    readonly SpeechSynthesizer synthesizer = new();
    readonly Timer countTimer = new(SayNumberTimeout_ms); 
    Prompt promptToWatch;
    Stopwatch stopWatch = new();
    VoiceInfo selectedVI;

    public MainPage()
    {
        InitializeComponent();
        LoadVoices();
        synthesizer.SpeakStarted += Synthesizer_SpeakStarted;
        synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
        synthesizer.SpeakProgress += (s, e) =>
        {
            SpeakingLbl.Text = e.Text;
        };

        Loaded += (s, e) => Welcome();
    }

    private void Welcome()
    {
        string message = selectedVI.Culture.TwoLetterISOLanguageName switch
        {
            "en" => @"Welcome, I am talking with Microsoft's text to speech technology.
Press the lower buttons to hear numbers in english",

            "es" => @"Bienvenido, hablo con tecnología de texto a voz de Microsoft.
Presiona los botones inferiores para oir los números en español",

            "fr" or _ => @"
Bonjour tout le monde, 
parlant via la technologie de voix au texte de Microsoft.
Appuyez sur les boutons pour écouter les chiffres en français",
        };

        synthesizer.SpeakAsync(message);
    }

    private void LoadVoices()
    {
        synthesizer.SelectVoiceByHints(VoiceGender.NotSet,
                    VoiceAge.NotSet, 0, _frCI);
        selectedVI = synthesizer.Voice;

        var voices = synthesizer.GetInstalledVoices();
        var items = voices
            .Select(v => (v.VoiceInfo.Name, v.VoiceInfo.Culture.DisplayName))
            .ToArray();

        int index = 0, selectedIndex = -1;
        foreach (var item in items)
        {
            string shortened = item.Name;
            shortened = shortened.Replace("Microsoft", string.Empty);
            shortened = shortened.Replace("Desktop", string.Empty);
            VoicesPck.Items.Add(string.Format("{0} [{1}]", shortened, item.DisplayName));
            index++;

            if (item.Name == selectedVI.Name)
            {
                selectedIndex = index;
            }
        }

        VoicesPck.SelectedIndex = index;
    }

    private PromptBuilder SayNumber(
        PromptBreak promptBreak = PromptBreak.Small,
        bool sayIt = true, bool endVoice = true)
    {
        var builder = new PromptBuilder();
        builder.StartVoice(selectedVI);
        builder = SayNumber(builder, promptBreak, endVoice, sayIt);

        return builder;
    }

    private PromptBuilder SayNumber(
        PromptBuilder builder,
        PromptBreak promptBreak = PromptBreak.Small,
        bool endVoice = true,
        bool sayIt = true)
    {
        //string veces = count > 1 ? "fois" : "fois";

        //synthesizer.SpeakAsync(
        //    $"bouton cliqué {count} {veces}.");
        int presentCount = count; // pass a copy

        string inWords = count.ToWords(selectedVI.Culture);
        Dispatcher.Dispatch(() =>
        {
            NumberLbl.Text = count.ToString("n0"); // humanized :D
            CounterBtn.Text = $"{count.ToString("n0")}";
        });

        //builder.AppendTextWithHint(count.ToString(), SayAs.NumberOrdinal);
        //builder.AppendBreak();
        builder.AppendText(inWords);
        builder.AppendBreak(promptBreak);
        if (endVoice)
        {
            builder.EndVoice();
        }
        if (sayIt)
        {
            stopWatch.Stop();
            countTimer.Interval = stopWatch.ElapsedMilliseconds > SayNumberTimeout_ms
                ? stopWatch.ElapsedMilliseconds + 500
                : SayNumberTimeout_ms;
            //stopWatch.Reset();
            promptToWatch = synthesizer.SpeakAsync(builder);
        }

        return builder;
    }
    
    private void SayText(string text) => synthesizer.SpeakAsync(text);

    private void StartCounter()
    {
        countTimer.Elapsed += CountTimer_Elapsed;
        countTimer.Start();
        isStarted = true;
        PlayBtn.Text = "\u23F8"; // pause
        PlayBtn.BackgroundColor = Colors.Yellow;
    }

    private void Reset()
    {
        if (int.TryParse(ResetEntry.Text,
                        NumberStyles.Integer,
                        null, out int newCount))
        {
            count = newCount;
            SayNumber();
        }
    }

    private void StopCounter()
    {
        countTimer.Elapsed -= CountTimer_Elapsed;
        countTimer.Stop();
        isStarted = false;
        Interlocked.Exchange(ref steps, 0);
        PlayBtn.Text = "\u23F5"; // play
        PlayBtn.BackgroundColor = Colors.Green;
    }

    private void SayWelcome_Clicked(object sender, EventArgs e)
    {
        Welcome();
    }

    private void Synthesizer_SpeakStarted(object sender, SpeakStartedEventArgs e)
    {
        stopWatch.Restart();        
    }

    private void Synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
    {
        stopWatch.Stop();
    }

    private void CountTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        Debug.WriteLine($"{nameof(CountTimer_Elapsed)} started. Timer: {countTimer.Interval}");
        Interlocked.Increment(ref count);
        Debug.WriteLine(stopWatch.Elapsed);
        countTimer.Interval = (!stopWatch.IsRunning 
            && stopWatch.ElapsedMilliseconds + 500 > SayNumberTimeout_ms) 
                ? stopWatch.ElapsedMilliseconds + 500 
                : SayNumberTimeout_ms;

        var builder = SayNumber(PromptBreak.Medium, false, false);
        SayNumber(builder, PromptBreak.None, true, true);
#if DEBUG
        Interlocked.Increment(ref steps);
        Debug.WriteLine($"Step {steps}, counter = {count}");
#endif
    }

    private void CustomEntry_Completed(object sender, EventArgs e)
    {
        SayText(CustomEntry.Text);
    }

    private void SayBtn_Clicked(object sender, EventArgs e)
    {
        SayText(CustomEntry.Text);
    }


    private void OnCounterClicked(object sender, EventArgs e)
    {
        SayNumber();
    }

    private void LessBtn_Clicked(object sender, EventArgs e)
    {
        --count;
        SayNumber();
    }

    private void MoreBtn_Clicked(object sender, EventArgs e)
    {
        ++count;
        SayNumber();
    }

    private void ResetBtn_Clicked(object sender, EventArgs e)
    {
        Reset();
    }

    private void ResetEntry_Completed(object sender, EventArgs e)
    {
        Reset();
    }

    private void PlayBtn_Clicked(object sender, EventArgs e)
    {
        if (isStarted)
        {
            // pause (count remains same)
            StopCounter();
        }
        else
        {
            count++;
            var builder = SayNumber(PromptBreak.Medium, false, false);
            SayNumber(builder, PromptBreak.None, true, true);
            StartCounter();
        }
    }

    private void StopBtn_Clicked(object sender, EventArgs e)
    {
        StopCounter();

        count = 0;
        SayNumber();
    }

    private void VoicesPck_SelectedIndexChanged(object sender, EventArgs e)
    {
        var voices = synthesizer.GetInstalledVoices();
        string selectedV = VoicesPck.GetItemsAsArray()[VoicesPck.SelectedIndex];
        selectedV = selectedV.Substring(0, selectedV.IndexOf("[")).TrimEnd();

        selectedVI = voices
            .Where(v => v.VoiceInfo.Name.Contains(selectedV))
            .Select(v => v.VoiceInfo)
            .First();

        synthesizer.SelectVoice(selectedVI.Name);
    }
}

#pragma warning restore CA1416