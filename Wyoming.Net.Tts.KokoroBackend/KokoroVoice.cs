using System.Diagnostics;

namespace Wyoming.Net.Tts.KokoroBackend;

public enum KokoroGender
{
    Both, 
    Male = 'm', 
    Female = 'f'
}

public enum KokoroLanguage 
{
    AmericanEnglish     = 'a',
    BritishEnglish      = 'b',
    Japanese            = 'j',
    MandarinChinese     = 'z',
    Spanish             = 'e',
    French              = 'f',
    Hindi               = 'h',
    Italian             = 'i',
    BrazilianPortuguese = 'p'
}

public sealed class KokoroVoice
{
    private static readonly float[,,] EmptyFeatures = new float[,,] { };
    
    /// <summary> The name of the voice, consisting of 'LG_name', where 'L' is the language code, and 'G' is the gender. </summary>
    /// <remarks> For example, a Female British voice named Amy should be named "bf_Amy". </remarks>
    public required string Name { get; init; }

    /// <summary> Contains the speaker embeddings for this voice, in C# format, but otherwise representing a [510, 1, 256] Tensor. </summary>
    /// <remarks> Can initialize this via <see cref="FromPath(string)"/>. See the documentation for more information on how to prepare `.pt` voices for use in KokoroSharp. </remarks>
    public required float[,,] Features { get; init; }

    /// <summary> The language this voice's speaker is intended to be speaking. </summary>
    /// <remarks> It is based on the first character of <see cref="Name"/>. </remarks>
    public KokoroLanguage Language => this.GetLanguage();

    /// <summary> The gender of this voice's speaker. </summary>
    /// <remarks> It is based on the second character of <see cref="Name"/>. </remarks>
    public KokoroGender Gender => (KokoroGender) Name[1];

    /// <summary> Loads an exported voice from specified file path. </summary>
    public static KokoroVoice FromPath(string filePath, bool loadFeatures = true) 
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        return new KokoroVoice() 
            { 
                Name = name, 
                Features = loadFeatures ? NumSharp.np.Load<float[,,]>(filePath) : EmptyFeatures
            };
    }

    public static IEnumerable<KokoroVoice> EnumerateVoices(bool loadFeatures)
    {
        return Directory.EnumerateFiles(CrossPlatformHelper.GetVoicesPath(), "*.npy").Select(path => FromPath(path, loadFeatures));
    }

    private KokoroLanguage GetLanguage() 
    {
        if (string.IsNullOrWhiteSpace(Name)) 
        {
            Debug.WriteLine("Specified voice is not named. Mixed voices of multiple languages have to be named explicitly. Defaulting to en-us.");
            return KokoroLanguage.AmericanEnglish;
        }
        if (Name.Length > 2 && Name[2] != '_') 
        {
            Debug.WriteLine("Specified voice is not named properly. Make sure to follow naming conveniences (see KokoroLanguage.cs). Defaulting to en-us.");
            return KokoroLanguage.AmericanEnglish;
        }
        if (!Enum.IsDefined(typeof(KokoroLanguage), (int) Name[0])) 
        {
            Debug.WriteLine("Specified voice is not named properly, or language is not recognized. Make sure to follow naming conveniences (see KokoroLanguage.cs). Defaulting to en-us.");
            return KokoroLanguage.AmericanEnglish;
        }

        return (KokoroLanguage) Name[0];
    }

    public string GetLangCode()
    {
        return ToLangCode(GetLanguage());
    }
    
    private static string ToLangCode(KokoroLanguage language) =>
        language switch
        {
            KokoroLanguage.AmericanEnglish     => "en-us",
            KokoroLanguage.BritishEnglish      => "en-gb",
            KokoroLanguage.Japanese            => "ja",
            KokoroLanguage.MandarinChinese     => "cmn",
            KokoroLanguage.Spanish             => "es",
            KokoroLanguage.French              => "fr",
            KokoroLanguage.Hindi               => "hi",
            KokoroLanguage.Italian             => "it",
            KokoroLanguage.BrazilianPortuguese => "pt-br",
            _ => throw new ArgumentOutOfRangeException( nameof(language), language, "Unsupported Kokoro language")
        };
}