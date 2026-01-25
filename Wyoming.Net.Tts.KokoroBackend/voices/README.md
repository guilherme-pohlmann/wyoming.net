---
license: apache-2.0
language:
- en
base_model:
- yl4579/StyleTTS2-LJSpeech
pipeline_tag: text-to-speech
---
**Kokoro** is an open-weight TTS model with 82 million parameters. Despite its lightweight architecture, it delivers comparable quality to larger models while being significantly faster and more cost-efficient. With Apache-licensed weights, Kokoro can be deployed anywhere from production environments to personal projects.

<audio controls><source src="https://huggingface.co/hexgrad/Kokoro-82M/resolve/main/samples/HEARME.wav" type="audio/wav"></audio>

⬆️ **Kokoro has been upgraded to v1.0!** See [Releases](https://huggingface.co/hexgrad/Kokoro-82M#releases).

✨ You can now [`pip install kokoro`](https://github.com/hexgrad/kokoro)! See [Usage](https://huggingface.co/hexgrad/Kokoro-82M#usage).

- [Releases](#releases)
- [Usage](#usage)
- [SAMPLES.md](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/SAMPLES.md) ↗️
- [VOICES.md](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md) ↗️
- [Model Facts](#model-facts)
- [Training Details](#training-details)
- [Creative Commons Attribution](#creative-commons-attribution)
- [Acknowledgements](#acknowledgements)

### Releases

| Model | Published | Training Data | Langs & Voices | SHA256 |
| ----- | --------- | ------------- | -------------- | ------ |
| [v0.19](https://huggingface.co/hexgrad/kLegacy/tree/main/v0.19) | 2024 Dec 25 | <100 hrs | 1 & 10 | `3b0c392f` |
| **v1.0** | **2025 Jan 27** | **Few hundred hrs** | [**8 & 54**](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md) | `496dba11` |

| Training Costs | v0.19 | v1.0 | **Total** |
| -------------- | ----- | ---- | ----- |
| in A100 80GB GPU hours | 500 | 500 | **1000** |
| average hourly rate | $0.80/h | $1.20/h | **$1/h** |
| in USD | $400 | $600 | **$1000** |

### Usage

[`pip install kokoro`](https://pypi.org/project/kokoro/) installs the inference library at https://github.com/hexgrad/kokoro

You can run this cell on [Google Colab](https://colab.research.google.com/). [Listen to samples](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/SAMPLES.md).
```py
# 1️⃣ Install kokoro
!pip install -q kokoro>=0.3.4 soundfile
# 2️⃣ Install espeak, used for English OOD fallback and some non-English languages
!apt-get -qq -y install espeak-ng > /dev/null 2>&1
# 🇪🇸 'e' => Spanish es
# 🇫🇷 'f' => French fr-fr
# 🇮🇳 'h' => Hindi hi
# 🇮🇹 'i' => Italian it
# 🇧🇷 'p' => Brazilian Portuguese pt-br

# 3️⃣ Initalize a pipeline
from kokoro import KPipeline
from IPython.display import display, Audio
import soundfile as sf
# 🇺🇸 'a' => American English, 🇬🇧 'b' => British English
# 🇯🇵 'j' => Japanese: pip install misaki[ja]
# 🇨🇳 'z' => Mandarin Chinese: pip install misaki[zh]
pipeline = KPipeline(lang_code='a') # <= make sure lang_code matches voice

# This text is for demonstration purposes only, unseen during training
text = '''
The sky above the port was the color of television, tuned to a dead channel.
"It's not like I'm using," Case heard someone say, as he shouldered his way through the crowd around the door of the Chat. "It's like my body's developed this massive drug deficiency."
It was a Sprawl voice and a Sprawl joke. The Chatsubo was a bar for professional expatriates; you could drink there for a week and never hear two words in Japanese.

These were to have an enormous impact, not only because they were associated with Constantine, but also because, as in so many other areas, the decisions taken by Constantine (or in his name) were to have great significance for centuries to come. One of the main issues was the shape that Christian churches were to take, since there was not, apparently, a tradition of monumental church buildings when Constantine decided to help the Christian church build a series of truly spectacular structures. The main form that these churches took was that of the basilica, a multipurpose rectangular structure, based ultimately on the earlier Greek stoa, which could be found in most of the great cities of the empire. Christianity, unlike classical polytheism, needed a large interior space for the celebration of its religious services, and the basilica aptly filled that need. We naturally do not know the degree to which the emperor was involved in the design of new churches, but it is tempting to connect this with the secular basilica that Constantine completed in the Roman forum (the so-called Basilica of Maxentius) and the one he probably built in Trier, in connection with his residence in the city at a time when he was still caesar.

[Kokoro](/kˈOkəɹO/) is an open-weight TTS model with 82 million parameters. Despite its lightweight architecture, it delivers comparable quality to larger models while being significantly faster and more cost-efficient. With Apache-licensed weights, [Kokoro](/kˈOkəɹO/) can be deployed anywhere from production environments to personal projects.
'''
# text = '「もしおれがただ偶然、そしてこうしようというつもりでなくここに立っているのなら、ちょっとばかり絶望するところだな」と、そんなことが彼の頭に思い浮かんだ。'
# text = '中國人民不信邪也不怕邪，不惹事也不怕事，任何外國不要指望我們會拿自己的核心利益做交易，不要指望我們會吞下損害我國主權、安全、發展利益的苦果！'
# text = 'Los partidos políticos tradicionales compiten con los populismos y los movimientos asamblearios.'
# text = 'Le dromadaire resplendissant déambulait tranquillement dans les méandres en mastiquant de petites feuilles vernissées.'
# text = 'ट्रांसपोर्टरों की हड़ताल लगातार पांचवें दिन जारी, दिसंबर से इलेक्ट्रॉनिक टोल कलेक्शनल सिस्टम'
# text = "Allora cominciava l'insonnia, o un dormiveglia peggiore dell'insonnia, che talvolta assumeva i caratteri dell'incubo."
# text = 'Elabora relatórios de acompanhamento cronológico para as diferentes unidades do Departamento que propõem contratos.'

# 4️⃣ Generate, display, and save audio files in a loop.
generator = pipeline(
    text, voice='af_heart', # <= change voice here
    speed=1, split_pattern=r'\n+'
)
for i, (gs, ps, audio) in enumerate(generator):
    print(i)  # i => index
    print(gs) # gs => graphemes/text
    print(ps) # ps => phonemes
    display(Audio(data=audio, rate=24000, autoplay=i==0))
    sf.write(f'{i}.wav', audio, 24000) # save each audio file
```

Under the hood, `kokoro` uses [`misaki`](https://pypi.org/project/misaki/), a G2P library at https://github.com/hexgrad/misaki

### Model Facts

**Architecture:**
- StyleTTS 2: https://arxiv.org/abs/2306.07691
- ISTFTNet: https://arxiv.org/abs/2203.02395
- Decoder only: no diffusion, no encoder release

**Architected by:** Li et al @ https://github.com/yl4579/StyleTTS2

**Trained by**: `@rzvzn` on Discord

**Languages:** American English, British English, French, Hindi

**Model SHA256 Hash:** `496dba118d1a58f5f3db2efc88dbdc216e0483fc89fe6e47ee1f2c53f18ad1e4`

### Training Details

**Data:** Kokoro was trained exclusively on **permissive/non-copyrighted audio data** and IPA phoneme labels. Examples of permissive/non-copyrighted audio include:
- Public domain audio
- Audio licensed under Apache, MIT, etc
- Synthetic audio<sup>[1]</sup> generated by closed<sup>[2]</sup> TTS models from large providers<br/>
[1] https://copyright.gov/ai/ai_policy_guidance.pdf<br/>
[2] No synthetic audio from open TTS models or "custom voice clones"

**Total Dataset Size:** A few hundred hours of audio

**Total Training Cost:** About $1000 for 1000 hours of A100 80GB vRAM

### Creative Commons Attribution

The following CC BY audio was part of the dataset used to train Kokoro v1.0.

| Audio Data | Duration Used | License | Added to Training Set After |
| ---------- | ------------- | ------- | --------------------------- |
| [Koniwa](https://github.com/koniwa/koniwa) `tnc` | <1h | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/deed.ja) | v0.19 / 22 Nov 2024 |
| [SIWIS](https://datashare.ed.ac.uk/handle/10283/2353) | <11h | [CC BY 4.0](https://datashare.ed.ac.uk/bitstream/handle/10283/2353/license_text) | v0.19 / 22 Nov 2024 |

### Acknowledgements

- 🛠️ [@yl4579](https://huggingface.co/yl4579) for architecting StyleTTS 2.
- 🏆 [@Pendrokar](https://huggingface.co/Pendrokar) for adding Kokoro as a contender in the TTS Spaces Arena.
- 📊 Thank you to everyone who contributed synthetic training data.
- ❤️ Special thanks to all compute sponsors.
- 👾 Discord server: https://discord.gg/QuGxSWBfQy
- 🪽 Kokoro is a Japanese word that translates to "heart" or "spirit". Kokoro is also the name of an [AI in the Terminator franchise](https://terminator.fandom.com/wiki/Kokoro).

<img src="https://static0.gamerantimages.com/wordpress/wp-content/uploads/2024/08/terminator-zero-41-1.jpg" width="400" alt="kokoro" />
