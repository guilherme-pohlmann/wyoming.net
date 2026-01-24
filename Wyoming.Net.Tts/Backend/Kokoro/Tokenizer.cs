using System.Buffers;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Wyoming.Net.Tts.Backend.Kokoro;

/// <summary> A static module responsible for tokenization converting plaintext to phonemes, and phonemes to tokens. </summary>
/// <remarks>
/// <para> Internally preprocesses and post-processes the input text to bring it closer to what the model expects to see. </para>
/// <para> Phonemization happens via the espeak-ng library: <b>https://github.com/espeak-ng/espeak-ng/blob/master/docs/guide.md</b> </para>
/// <para> This code was based of: https://github.dev/Lyrcaxis/KokoroSharp but optimized for less memory allocation (about 3x less based on my benchmarks) and better latency (about 25% faster)</para>
/// </remarks>
///
/// TODO: add support to other languages
/// TODO: rewrite regexes with custom parsers
public static partial class Tokenizer
{
    // private static readonly Dictionary<char, string> Currencies = new()
    // {
    //     { '$', "dollar" }, 
    //     { '€', "euro" }, 
    //     { '£', "pound" }, 
    //     { '¥', "yen" }, 
    //     { '₹', "rupee" }, 
    //     { '₽', "ruble" }, 
    //     { '₩', "won" }, 
    //     { '₺', "lira" },
    //     { '₫', "dong" }
    // };

    public static ReadOnlyDictionary<char, int> Vocab { get; }
    
    private static ReadOnlyDictionary<int, char> TokenToChar { get; }

    static Tokenizer()
    {
        Dictionary<char, int> sourceVocab = new()
        {
            ['\n'] = -1, ['$'] = 0, [';'] = 1, [':'] = 2, [','] = 3, ['.'] = 4, ['!'] = 5, ['?'] = 6, ['¡'] = 7,
            ['¿'] = 8, ['—'] = 9, ['…'] = 10, ['\"'] = 11, ['('] = 12, [')'] = 13, ['“'] = 14, ['”'] = 15, [' '] = 16,
            ['\u0303'] = 17, ['ʣ'] = 18, ['ʥ'] = 19, ['ʦ'] = 20, ['ʨ'] = 21, ['ᵝ'] = 22, ['\uAB67'] = 23, ['A'] = 24,
            ['I'] = 25, ['O'] = 31, ['Q'] = 33, ['S'] = 35, ['T'] = 36, ['W'] = 39, ['Y'] = 41, ['ᵊ'] = 42, ['a'] = 43,
            ['b'] = 44, ['c'] = 45, ['d'] = 46, ['e'] = 47, ['f'] = 48, ['h'] = 50, ['i'] = 51, ['j'] = 52, ['k'] = 53,
            ['l'] = 54, ['m'] = 55, ['n'] = 56, ['o'] = 57, ['p'] = 58, ['q'] = 59, ['r'] = 60, ['s'] = 61, ['t'] = 62,
            ['u'] = 63, ['v'] = 64, ['w'] = 65, ['x'] = 66, ['y'] = 67, ['z'] = 68, ['ɑ'] = 69, ['ɐ'] = 70, ['ɒ'] = 71,
            ['æ'] = 72, ['β'] = 75, ['ɔ'] = 76, ['ɕ'] = 77, ['ç'] = 78, ['ɖ'] = 80, ['ð'] = 81, ['ʤ'] = 82, ['ə'] = 83,
            ['ɚ'] = 85, ['ɛ'] = 86, ['ɜ'] = 87, ['ɟ'] = 90, ['ɡ'] = 92, ['ɥ'] = 99, ['ɨ'] = 101, ['ɪ'] = 102,
            ['ʝ'] = 103, ['ɯ'] = 110, ['ɰ'] = 111, ['ŋ'] = 112, ['ɳ'] = 113, ['ɲ'] = 114, ['ɴ'] = 115, ['ø'] = 116,
            ['ɸ'] = 118, ['θ'] = 119, ['œ'] = 120, ['ɹ'] = 123, ['ɾ'] = 125, ['ɻ'] = 126, ['ʁ'] = 128, ['ɽ'] = 129,
            ['ʂ'] = 130, ['ʃ'] = 131, ['ʈ'] = 132, ['ʧ'] = 133, ['ʊ'] = 135, ['ʋ'] = 136, ['ʌ'] = 138, ['ɣ'] = 139,
            ['ɤ'] = 140, ['χ'] = 142, ['ʎ'] = 143, ['ʒ'] = 147, ['ʔ'] = 148, ['ˈ'] = 156, ['ˌ'] = 157, ['ː'] = 158,
            ['ʰ'] = 162, ['ʲ'] = 164, ['↓'] = 169, ['→'] = 171, ['↗'] = 172, ['↘'] = 173, ['ᵻ'] = 177
        };

        Vocab = sourceVocab.AsReadOnly();
        TokenToChar = Vocab.Select(kv => new KeyValuePair<int, char>(kv.Value, kv.Key)).ToDictionary().AsReadOnly();
    }
    
    public static async Task<int[]> TokenizeAsync(string inputText, string langCode = "en-us", bool preprocess = true)
    {
        var text = await PhonemizeAsync(inputText, langCode, preprocess);
        var tokens = new int[text.Length];

        for (var index = 0; index < text.Length; index++)
        {
            var c = text[index];
            tokens[index] = Vocab[c];
        }

        return tokens;
    }

    public static bool TryGetChar(int token, out char c)
    {
        return TokenToChar.TryGetValue(token, out c);
    }
    
    public static bool IsPunctuation(char c)
    {
        // Lines split on any of these occurrences, by design via espeak-ng.
        return c switch
        {
            ';' or ':' or ',' or '.' or '!' or '?' or '…' or '¿' or '\n' => true,
            _ => false
        };
    }
    
    /// <summary> Converts the input text into the corresponding phonemes, with slight preprocessing and post-processing to preserve punctuation and other TTS essentials. </summary>
    public static async Task<string> PhonemizeAsync(string inputText, string langCode = "en-us",
        bool preprocess = true)
    {
        var strings = await Task.WhenAll(PhonemeLiteral().Split(inputText).Select(async text =>
        {
            var match = PhonemeLiteral2().Match(text);

            if (match.Success)
            {
                return
                    match.Groups[1]
                        .Value; // Extract the phoneme part from the literal pronunciation, e.g. [Kokoro](/kˈOkəɹO/) => "kˈOkəɹO"
            }

            if (preprocess)
            {
                text = PreprocessText(text, langCode);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var symbols = CollectSymbols(text); // Collect symbols to prepare for phonemization.

            return await ESpeakPhonemizeAsync(symbols, langCode);
        }));

        string preprocessedText = string.Join(' ', strings);
        var phonemeList = preprocessedText.Split('\n');

        return PostProcessPhonemes(preprocessedText, phonemeList, langCode);
    }

    private static async Task<string> ESpeakPhonemizeAsync(string text, string langCode = "en-us")
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = CrossPlatformHelper.GetEspeakBinariesPath(),
            WorkingDirectory = null,
            Arguments = $"--ipa=3 -b 1 -q -v {langCode} --stdin",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8
        };

        process.StartInfo.EnvironmentVariables.Add("ESPEAK_DATA_PATH",
            @$"{CrossPlatformHelper.GetEspeakBasePath()}/espeak-ng-data");

        if (!process.Start())
        {
            throw new Exception("Could not start ESpeakNg process.");
        }

        await process.StandardInput.WriteLineAsync(text);
        process.StandardInput.Close();

        var originalSegments = await process.StandardOutput.ReadToEndAsync();
        process.StandardOutput.Close();

        return originalSegments.Replace("\r\n", "\n").Trim();
    }

    private static bool IsReplaceable(char c)
    {
        return c switch
        {
            '\n' or ';' or ':' or ',' or '.' or '!' or '?' or '¡' or '¿' or '—' or '…' or '\\' or '"' or '«' or '»'
                or '“' or '”' or '(' or ')' => true,
            _ => false
        };
    }

    private static bool IsDeletable(char c)
    {
        return c switch
        {
            '-' or '`' or '(' or ')' or '[' or ']' or '{' or '}' => true,
            _ => false
        };
    }

    private static bool IsSpaceNeeded(char c)
    {
        return c switch
        {
            '\\' or '"' or '…' or '<' or '«' or '“' => true,
            _ => false
        };
    }

    private static string  PreprocessText(string text, string langCode = "en-us")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Replace("\r\n", "\n");

        text = HeaderImgLink().Replace(text, "$1$2");
        text = HeaderLink().Replace(text, "$1");

        text = CodeBlock().Replace(text, ProcessCodeBlock);
        text = TickQuote().Replace(text, m => ProcessInlineCode(m.Groups[1].Value));

        var rx = Money();

        if (rx.IsMatch(text))
        {
            text = rx.Replace(text, "$2 $1 $3");
        }

        rx = Money2();

        if (rx.IsMatch(text))
        {
            text = rx.Replace(text, "$1 $3 $2");
        }

        rx = ByteNumber();

        if (rx.IsMatch(text))
        {
            text = rx.Replace(text, ExpandBytes);
        }

        rx = Time();

        if (rx.IsMatch(text))
        {
            text = rx.Replace(text, "$1 $2");
        }

        rx = DecimalPoint();

        if (rx.IsMatch(text))
        {
            text = rx.Replace(text, ExpandDecimal);
        }

        //TODO: language aware currency

        text = NormalizePunctuation(text);

        return StreamNormalize(text);
    }

    // static string ReplaceCurrencies(ReadOnlySpan<char> input)
    // {
    //     var sb = new StringBuilder(input.Length + 16);
    //
    //     foreach (char c in input)
    //     {
    //         if (Currencies.TryGetValue(c, out string word))
    //         {
    //             sb.Append(' ');
    //             sb.Append(word);
    //             sb.Append(' ');
    //         }
    //         else
    //         {
    //             sb.Append(c);
    //         }
    //     }
    //
    //     return sb.ToString();
    // }

    private static string NormalizePunctuation(ReadOnlySpan<char> input)
    {
        var sb = new StringBuilder(input.Length + 8);

        bool lastWasSpace = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (IsPunctuation(c))
            {
                // Remove space before punctuation
                if (sb.Length > 0 && sb[^1] == ' ')
                {
                    sb.Length--;
                }

                sb.Append(c);

                // Force exactly one space after
                if (i + 1 < input.Length && sb[^1] != ' ')
                {
                    sb.Append(' ');
                }

                lastWasSpace = true;
            }
            else
            {
                if (c == ' ')
                {
                    if (!lastWasSpace)
                    {
                        sb.Append(' ');
                    }

                    lastWasSpace = true;
                }
                else
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
            }
        }

        return sb.ToString();
    }

    private static string ProcessCodeBlock(Match m)
    {
        //TODO: make language aware
        var lines = m.Groups[1].Value.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            int comment = Math.Max(line.IndexOf("//", StringComparison.Ordinal), line.IndexOf('#'));

            if (comment >= 0)
            {
                lines[i] =
                    line[..comment].Replace(".", " dot ") +
                    line[comment..];
            }
            else
            {
                lines[i] = line.Replace(".", " dot ");
            }
        }

        return string.Join('\n', lines);
    }

    private static string ProcessInlineCode(string code)
    {
        return code.Replace(".", " dot ");
    }

    private static string ExpandDecimal(Match m)
    {
        var digits = m.Groups[3].Value;
        var sb = new StringBuilder(digits.Length * 2 + 12);

        sb.Append(m.Groups[1].Value);
        sb.Append(" point ");

        for (int i = 0; i < digits.Length; i++)
        {
            sb.Append(digits[i]);
            if (i + 1 < digits.Length)
                sb.Append(' ');
        }

        return sb.ToString();
    }

    private static string ExpandBytes(Match m)
    {
        string unit = m.Groups[2].Value switch
        {
            "KB" => " kilobyte",
            "MB" => " megabyte",
            "GB" => " gigabyte",
            "TB" => " terabyte",
            _ => m.Groups[2].Value
        };

        return m.Groups[1].Value + unit + m.Groups[3].Value;
    }

    private static string StreamNormalize(string text)
    {
        var input = text.AsSpan();
        var maxLen = input.Length * 2;
        char[]? rented = null;
        var dest = maxLen * sizeof(char) <= 256 ? stackalloc char[maxLen] : Rent<char>(maxLen, out rented);
        int pos = 0;

        bool lastWasSpace = true;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            // Drop deletable characters
            if (IsDeletable(c))
            {
                continue;
            }

            // Replace punctuation phonemes
            if (IsReplaceable(c))
            {
                c = ',';
            }

            // Token rewrites
            if (MatchLiteral(input, i, "C#"))
            {
                WriteLiteral("C SHARP", dest, ref pos, ref lastWasSpace);
                i += 1;
                continue;
            }

            if (MatchLiteral(input, i, ".NET"))
            {
                WriteLiteral("dot net", dest, ref pos, ref lastWasSpace);
                i += 3;
                continue;
            }

            if (MatchLiteral(input, i, "->"))
            {
                WriteLiteral(" to ", dest, ref pos, ref lastWasSpace);
                i += 1;
                continue;
            }

            if (c == '/')
            {
                WriteLiteral(" slash ", dest, ref pos, ref lastWasSpace);
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                WriteSpace(dest, ref pos, ref lastWasSpace);
                continue;
            }

            dest[pos++] = c;
            lastWasSpace = false;
        }

        var output = dest.Slice(0, pos).Trim().ToString();

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return output;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteLiteral(
        ReadOnlySpan<char> s,
        Span<char> buffer,
        ref int pos,
        ref bool lastWasSpace)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            if (c == ' ')
            {
                if (lastWasSpace)
                {
                    continue;
                }

                buffer[pos++] = ' ';
                lastWasSpace = true;
            }
            else
            {
                buffer[pos++] = c;
                lastWasSpace = false;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSpace(Span<char> buffer, ref int pos, ref bool lastWasSpace)
    {
        if (!lastWasSpace)
        {
            buffer[pos++] = ' ';
            lastWasSpace = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchLiteral(ReadOnlySpan<char> s, int i, string literal)
    {
        return i + literal.Length <= s.Length &&
               s.Slice(i, literal.Length).SequenceEqual(literal);
    }

    private static Span<T> Rent<T>(int size, out T[] rented)
    {
        rented = ArrayPool<T>.Shared.Rent(size);
        return new Span<T>(rented, 0, size);
    }

    private static string CollectSymbols(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var input = text.AsSpan();
        var maxLen = input.Length * 2; // Worst case: every '\n' expands to "\n "
        char[]? rented = null;
        var dest = maxLen * sizeof(char) <= 256 ? stackalloc char[maxLen] : Rent<char>(maxLen, out rented);
        int pos = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            // \n → "\n "
            if (c == '\n')
            {
                dest[pos++] = '\n';
                dest[pos++] = ' ';
                continue;
            }

            // punctuation → ','
            if (IsReplaceable(c))
            {
                c = ',';
            }

            // normalize " ," → ", "
            if (c == ',' && pos > 0 && dest[pos - 1] == ' ')
            {
                dest[pos - 1] = ',';
                dest[pos++] = ' ';
                continue;
            }

            dest[pos++] = c;
        }

        var output = dest.Slice(0, pos).ToString();

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return output;
    }

    /// <summary> Post-processes the phonemes to Kokoro's specs, preparing them for tokenization. </summary>
    /// <remarks> We also use the initial text to restore the punctuation that was discarded by Espeak. </remarks>
    private static string PostProcessPhonemes(
        string initialText,
        string[] phonemesArray,
        string lang = "en-us")
    {
        if (phonemesArray.Length == 0)
        {
            return string.Empty;
        }

        Span<PunctuationSpan> punctuations = stackalloc PunctuationSpan[phonemesArray.Length];
        var list = new PunctuationSpanList();

        for (int i = 0; i < initialText.Length; i++)
        {
            if (!IsReplaceable(initialText[i]))
            {
                continue;
            }

            int start = i;
            i++;

            while (i < initialText.Length && (IsReplaceable(initialText[i]) || initialText[i] == ' '))
            {
                i++;
            }

            list.Add(punctuations, new PunctuationSpan(start, i - start));
            i--;
        }

        var sb = new StringBuilder(phonemesArray.Length * 6);

        for (int i = 0; i < phonemesArray.Length; i++)
        {
            string vf = phonemesArray[i];

            if (vf is ['ˈ', _, ..] && vf[1] == 'ɛ')
            {
                sb.Append("ˌɛ");
                sb.Append(vf.AsSpan(2));
            }
            else
            {
                sb.Append(vf);
            }

            if (i < list.Count)
            {
                var span = punctuations[i];
                sb.Append(initialText, span.Start, span.Length);
            }
        }

        ReadOnlySpan<char> input = NormalizePunctuation(sb.ToString().AsSpan());
        char[]? rented = null;
        var maxLen = input.Length * 2;
        var dest = maxLen * sizeof(char) <= 256 ? stackalloc char[maxLen] : Rent(maxLen, out rented);
        int pos = 0;

        bool lastWasSpace = true;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            // Skip unsupported vocab early
            if (!Vocab.ContainsKey(c))
            {
                continue;
            }

            switch (c)
            {
                // Collapse whitespace
                case ' ':
                {
                    if (!lastWasSpace)
                    {
                        dest[pos++] = ' ';
                        lastWasSpace = true;
                    }

                    continue;
                }
                // Condense punctuation
                case '!' or '?' when pos > 0 && dest[pos - 1] == c:
                {
                    continue;
                }
            }

            // Phoneme spacing rule
            if (IsSpaceNeeded(c) && pos > 0 && dest[pos - 1] != ' ')
            {
                dest[pos++] = ' ';
            }

            switch (c)
            {
                // Phoneme refinements
                case 'ː' when i + 1 < input.Length && input[i + 1] == ' ':
                    dest[pos++] = ' ';
                    i++;
                    lastWasSpace = true;
                    continue;
                case 'ɔ' when i + 1 < input.Length && input[i + 1] == 'ː':
                    dest[pos++] = 'ˌ';
                    dest[pos++] = 'ɔ';
                    i++;
                    lastWasSpace = false;
                    continue;
                case '\n' when  i + 1 < input.Length && input[i + 1] == ' ':
                    dest[pos++] = '\n';
                    i++;
                    continue;
                default:
                    dest[pos++] = c;
                    lastWasSpace = false;
                    break;
            }
        }

        var output = dest.Slice(0, pos).Trim().ToString();

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return output;
    }
}

file readonly struct PunctuationSpan
{
    public readonly int Start;
    public readonly int Length;

    public PunctuationSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }
}

file ref struct PunctuationSpanList
{
    private int pos;

    public void Add(scoped Span<PunctuationSpan> span, in PunctuationSpan item)
    {
        span[pos] = item;
        pos++;
    }

    public int Count => pos;
}