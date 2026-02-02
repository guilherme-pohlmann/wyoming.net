#if NET9_0_OR_GREATER

using Microsoft.ML.OnnxRuntime;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Wyoming.Net.Satellite.ML.Models.OpenWakeWord.Onnx;

internal readonly struct ModelInput : IReadOnlyDictionary<string, OrtValue>
{
    private readonly string key;
    private readonly OrtValue value;

    struct ModelInputEnumerator : IEnumerator<KeyValuePair<string, OrtValue>>
    {
        private int index = 0;
        private readonly KeyValuePair<string, OrtValue> pair;

        public ModelInputEnumerator(KeyValuePair<string, OrtValue> pair)
        {
            this.pair = pair;
        }

        public readonly KeyValuePair<string, OrtValue> Current => pair;

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            return index++ == 0;
        }

        public readonly void Reset()
        {
        }
    }

    public ModelInput(string key, OrtValue value)
    {
        this.key = key;
        this.value = value;
    }

    public OrtValue this[string key] => value;

    public IEnumerable<string> Keys => throw new NotImplementedException();

    public IEnumerable<OrtValue> Values => throw new NotImplementedException();

    public int Count => 1;

    public bool ContainsKey(string key)
    {
        return key.CompareTo(this.key) == 0;
    }

    public IEnumerator<KeyValuePair<string, OrtValue>> GetEnumerator()
    {
        return new ModelInputEnumerator(new KeyValuePair<string, OrtValue>(key, value));
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out OrtValue value)
    {
        if(ContainsKey(key))
        {
            value = this.value;
            return true;
        }

        value = default;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

#endif
