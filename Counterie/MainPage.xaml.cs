using System.Globalization;
using System.Speech.Synthesis;
using Timer = System.Timers.Timer;
namespace MauiApp3;

public partial class MainPage : ContentPage
{
	int count = 0;
    readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();
    readonly VoiceInfo selectedVI;
    readonly Timer countTimer = new Timer(2000);

    public MainPage()
	{
		InitializeComponent();
		synthesizer.SelectVoiceByHints(VoiceGender.NotSet, 
			VoiceAge.NotSet, 0, CultureInfo.GetCultureInfo("fr-FR"));
        selectedVI = synthesizer.Voice;

        synthesizer.SpeakProgress += (s, e) =>
            SpeakingLbl.Text = e.Text;

        this.Loaded += (s, e) =>
        synthesizer.SpeakAsync(@"Bonjour tout le monde, 
parlant via la technologie de voix au texte de Microsoft.
Appuyez sur les boutons pour écouter les chiffres en français");

        countTimer.Elapsed += (s, e) => {
            SayNumber(PromptBreak.Large);
            
            MoreBtn_Clicked(s, e);
        };
	}

    private void SayText(string text)
    {
        synthesizer.SpeakAsync(CustomEntry.Text);
    }

    private void SayBtn_Clicked(object sender, EventArgs e)
    {
        SayText(CustomEntry.Text);
    }

    private void CustomEntry_Completed(object sender, EventArgs e)
    {
        SayText(CustomEntry.Text);
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        SayNumber();
        SemanticScreenReader.Announce(CounterBtn.Text);
    }

    private void SayNumber(PromptBreak promptBreak = PromptBreak.Small)
    {
        if (count == 1)
            CounterBtn.Text = $"{count}";
        else
            CounterBtn.Text = $"{count}"; ;

        //string veces = count > 1 ? "fois" : "fois";

        //synthesizer.SpeakAsync(
        //    $"bouton cliqué {count} {veces}.");

        var builder = new PromptBuilder();
        builder.StartVoice(selectedVI);
        builder.AppendTextWithHint(count.ToString(), SayAs.NumberOrdinal);
        builder.AppendBreak(promptBreak);
        builder.EndVoice();

        synthesizer.SpeakAsync(builder);
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

    private void Reset()
    {
        if (int.TryParse(ResetEntry.Text,
                        NumberStyles.Integer, null, out int newCount))
        {
            count = newCount;
            SayNumber();
        }
    }

    private void PlayBtn_Clicked(object sender, EventArgs e)
    {        
        countTimer.Start();
        // TODO: Change UI to pause
    }

    private void StopBtn_Clicked(object sender, EventArgs e)
    {
        countTimer.Stop();
        // TODO: Change UI to play
    }
}

