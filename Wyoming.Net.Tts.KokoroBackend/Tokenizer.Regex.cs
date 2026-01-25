using System.Text.RegularExpressions;

namespace Wyoming.Net.Tts.KokoroBackend;

public static partial class Tokenizer
{
    [GeneratedRegex(
        @"\b(https?://)?(www\.)?[a-zA-Z0-9]+\b|\b[a-zA-Z0-9]+\.(com|net|org|io|edu|gov|mil|info|biz|co|us|uk|ca|de|fr|jp|au|cn|ru|gr)\b")]
    private static partial Regex WebUrl();

    [GeneratedRegex(@"^```[A-Za-z]{0,10}\n([\s\S]*?)\n```(?:\n|$)", RegexOptions.Multiline)]
    private static partial Regex CodeBlock(); // Markdown code blocks: ```csharp\ncode\n```

    [GeneratedRegex(@"\[(.*?)\]\(.*?\)")]
    private static partial Regex HeaderLink(); // Markdown links: [Header](link)

    [GeneratedRegex(@"\[.*?\[(.*?)\].*?\]\(.*?\)|\[(.*?)\]\(.*?\)")]
    private static partial Regex HeaderImgLink(); // Markdown image links: [Header[(img](link)]

    [GeneratedRegex(@"(\d)(\.)(\d+)")]
    private static partial Regex DecimalPoint(); // Decimal point: 3.1415

    [GeneratedRegex(@"(?<!`)`([^`]+)`(?!`)")]
    private static partial Regex TickQuote(); // Inline code: `code`

    [GeneratedRegex(@"\b(\d+(?:\.\d+)?)(KB|MB|GB|TB)(\s)")]
    private static partial Regex ByteNumber(); // Byte numbers: 1KB, 2.5MB, etc.

    [GeneratedRegex(@"([$€£¥₹₽₩₺₫]) ?(\d+)(?:[\.,](\d+))?")]
    private static partial Regex Money(); // Money amounts: $1, €2.50, etc.

    [GeneratedRegex(@"(\d+)(?:[\.,](\d+))? ?([$€£¥₹₽₩₺₫])")]
    private static partial Regex Money2(); // Money amounts: 1€, 2,50€, etc.

    [GeneratedRegex(@"\bD[Rr]\.(?= [A-Z])")]
    private static partial Regex Doctor(); // Doctor: Dr. Smith

    [GeneratedRegex(@"\b(Mr|MR)\.(?= [A-Z])")]
    private static partial Regex Mister(); // Mister: Mr. Smith

    [GeneratedRegex(@"\b(Ms|MS)\.(?= [A-Z])")]
    private static partial Regex Miss(); // Miss: Ms. Smith

    [GeneratedRegex(@"\x20{2,}")]
    private static partial Regex WhiteSpace(); // Multiple spaces: "  "

    [GeneratedRegex(@"(?<!\:)\b([1-9]|1[0-2]):([0-5]\d)\b(?!\:)")]
    private static partial Regex Time(); // Time: 12:30, 9:45, etc.

    [GeneratedRegex(@"(\[[^\]]+\]\(/[^/]+/\))")]
    private static partial Regex
        PhonemeLiteral(); // Literal Pronunciation: [Kokoro](/kˈOkəɹO/). Captures the entire string

    [GeneratedRegex(@"\[[^\]]+\]\(/([^/]+)/\)")]
    private static partial Regex
        PhonemeLiteral2(); // Literal Pronunciation: [Kokoro](/kˈOkəɹO/). Captures only the phoneme part e.g. kˈOkəɹO
}