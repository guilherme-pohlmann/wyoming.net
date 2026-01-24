using System.Runtime.CompilerServices;

namespace Wyoming.Net.Tts.Backend.Kokoro;
using static Tokenizer;

public static class PauseAfterSegmentStrategy
{
    private const float CommaPause = 0.1f;

    private const float PeriodPause = 0.5f;

    private const float QuestionMarkPause = 0.5f;

    private const float ExclamationMarkPause = 0.5f;

    private const float NewLinePause = 0.5f;

    private const float OthersPause = 0.5f;

    public static float GetPauseForSegment(char c)
    {
        return c switch
        {
            ',' => CommaPause,
            '.' => PeriodPause,
            '?' => QuestionMarkPause,
            '!' => ExclamationMarkPause,
            '\n' => NewLinePause,
            '¿' => OthersPause,
            _ => 0
        };
    }
}

public static class SegmentationStrategy
{
    private const int MinFirstSegmentLength = 10;
    private const int MaxFirstSegmentLength = 100;
    private const int MaxSecondSegmentLength = 100;
    private const int MinFollowupSegmentsLength = 200;

    private static readonly int NewLineToken = Vocab['\n'];
    private static readonly int SpaceToken = Vocab[' '];

    public static IEnumerable<Memory<int>> SplitToSegments(int[] tokens)
    {
        // Short-circuit if the whole text fits in the first segment
        if (tokens.Length <= MaxFirstSegmentLength)
        {
            yield return new Memory<int>(tokens);
            yield break;
        }

        int totalSegments = 0;
        int totalTokensProcessed = 0;

        while (totalTokensProcessed < tokens.Length)
        {
            var (min, max, _) = GetSegmentRange(totalSegments);
            
            // Ensure we never exceed the model's hard limit
            max = Math.Min(max, KokoroBackend.MaxTokens);
            
            // For the first segments, we prefer the FIRST punctuation found to reduce latency
            bool preferFirst = totalSegments < 2;

            int split = FindSplitIndex(tokens, totalTokensProcessed, min, max, preferFirst);

            if (split < 0)
            {
                // If no punctuation found in the window, search the rest of the string
                split = FindSplitUnbounded(tokens, totalTokensProcessed, min);
            }

            if (split < 0)
            {
                // Hard fallback: cut at the max limit
                int len = Math.Min(max, tokens.Length - totalTokensProcessed);
                yield return new Memory<int>(tokens, totalTokensProcessed, len);
                totalSegments++;
                totalTokensProcessed += len;
                continue;
            }

            // --- Post-Processing the Split ---

            // 1. Extend through trailing punctuation (e.g., "!!", "...") 
            // but don't cross MaxTokens or a NewLine
            int i = split;
            while (i < tokens.Length && 
                   (i - totalTokensProcessed) < KokoroBackend.MaxTokens && 
                   tokens[i] != NewLineToken && 
                   (IsProperEnd(tokens[i]) || IsFallbackEnd(tokens[i])))
            {
                i++;
            }

            // 2. Trim trailing spaces so they don't start the next segment
            int newEnd = i;
            while (newEnd > totalTokensProcessed && tokens[newEnd - 1] == SpaceToken)
            {
                newEnd--;
            }

            // 3. Near-end pull-in: If very little text is left, just take it all
            if (newEnd < tokens.Length && tokens[newEnd - 1] != NewLineToken && (tokens.Length - newEnd) < 20)
            {
                // Ensure the final chunk isn't too big for the model
                if (tokens.Length - totalTokensProcessed <= KokoroBackend.MaxTokens)
                {
                    newEnd = tokens.Length;
                }
            }

            if (newEnd > totalTokensProcessed)
            {
                yield return new Memory<int>(tokens, totalTokensProcessed, newEnd - totalTokensProcessed);
                totalTokensProcessed = newEnd;
                totalSegments++;
            }
            else
            {
                // Safety valve to prevent infinite loops
                totalTokensProcessed++; 
            }
        }
    }

    private static (int min, int max, int _) GetSegmentRange(int segmentIndex)
    {
        return segmentIndex switch
        {
            0 => (MinFirstSegmentLength, MaxFirstSegmentLength, 0),
            1 => (0, MaxSecondSegmentLength, 0),
            _ => (MinFollowupSegmentsLength, Math.Min(MinFollowupSegmentsLength * 2, KokoroBackend.MaxTokens), 0)
        };
    }

    private static int FindSplitIndex(ReadOnlySpan<int> tokens, int start, int min, int max, bool preferFirst)
    {
        int searchEnd = Math.Min(start + max, tokens.Length);
        int searchStart = start + min;

        // If the text left is shorter than our 'min' threshold, we have to look earlier 
        // or we'll return -1 and trigger the hard fallback.
        if (searchStart >= searchEnd) searchStart = start;

        int firstNewLine = -1;
        int firstProper = -1;
        int lastProper = -1;
        int lastFallback = -1;

        for (int i = searchStart; i < searchEnd; i++)
        {
            int t = tokens[i];

            if (t == NewLineToken)
            {
                firstNewLine = i;
                break; // New lines are highest priority
            }

            if (IsProperEnd(t))
            {
                if (firstProper < 0) firstProper = i;
                lastProper = i;
            }
            else if (IsFallbackEnd(t))
            {
                lastFallback = i;
            }
        }

        if (firstNewLine >= 0) return firstNewLine + 1;
        
        if (firstProper >= 0)
        {
            return preferFirst ? firstProper + 1 : lastProper + 1;
        }

        if (lastFallback >= 0) return lastFallback + 1;

        return -1;
    }

    private static int FindSplitUnbounded(ReadOnlySpan<int> tokens, int start, int min)
    {
        int searchStart = Math.Min(start + min, tokens.Length - 1);
        for (int i = searchStart; i < tokens.Length; i++)
        {
            int t = tokens[i];
            if (t == NewLineToken || IsProperEnd(t) || IsFallbackEnd(t))
            {
                return i + 1;
            }
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsProperEnd(int t) => t == Vocab['.'] || t == Vocab['!'] || t == Vocab['?'] || t == Vocab[':'];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFallbackEnd(int t) => t == Vocab[','] || t == Vocab[' '];
}
