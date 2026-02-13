# Wyoming .NET

A cross-platform voice assistant satellite built on the [Wyoming protocol](https://github.com/rhasspy/wyoming), designed to integrate with Home Assistant.

This project allows you to turn almost any device into a Wyoming satellite.

---

## Supported Platforms

- Android
- iOS
- Windows
- Linux
- macOS
- Tizen (TV)

---

## Tested Devices

### Android
- Samsung Galaxy S23+

### iOS
- iPhone 15 Pro
- iPad

### Windows
- Windows 11

### macOS
- MacBook M4 Pro
- Mac Mini M4

### Tizen
- Samsung The Frame TV

---

## Wake Word

Wake word detection is powered by **OpenWakeWord**.

Currently supported wake words:
- Alexa  

> Note: Automatic model download is not implemented yet.

---

## Environment Setup

### Android / iOS / Windows / Linux / macOS

I recommend following this setup guide:

https://www.youtube.com/watch?v=PrlsBboV-dY

Once your environment is configured, you should be able to deploy the app directly to your device using Rider (or your preferred IDE).

---

### Tizen Setup (Windows Recommended)

Tizen development is currently recommended on:

- ✅ Windows  
- ⚠️ macOS (not tested)

#### Recommended Setup

1. Install **Tizen Studio**
2. Install the **.NET Tizen workload**
3. Install the **VS Code Tizen extension**
4. Enable **Developer Mode** on your TV
5. Deploy using SDB or network connection

---

## License

MIT license
