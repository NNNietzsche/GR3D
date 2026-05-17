# GR3D / Echo Room Unity

Unity 3D party-board game prototype powered by local fallback prompts and an optional Gemini LLM proxy.

## Features

- 3D board-game table scene with dice, card stack, lighting, and atmospheric UI
- 2-6 player turn system
- Truth / Dare / Fortune / Jail / Double / All-player board cells
- Target-score win condition
- Chinese / English / Japanese UI switching
- Optional local Gemini proxy for dynamic scene setup, challenges, and fortune cards
- Local fallback content when the proxy is closed or API quota is unavailable
- Unity editor tools for scene generation and Windows packaging

## Unity Version

Created with Unity `6000.4.7f1`.

## Open Project

Open this folder in Unity Hub:

```text
echo-room-unity
```

Then run:

```text
Tools > Echo Room > Build Starter Scene
```

Press Play to test.

## Optional LLM Proxy

Unity does not store API keys. To use Gemini:

1. Run `start-gemini-proxy.cmd`.
2. Paste your Gemini API key into the terminal.
3. Keep the terminal open.
4. Start the game in Unity or the packaged build.

Unity calls:

```text
http://127.0.0.1:8787/echo-room
```

If the proxy fails, the game automatically uses local fallback prompts.

## Build Windows Package

In Unity:

```text
Tools > Echo Room > Build Windows Package
```

The packaged build is generated under:

```text
Builds/EchoRoom-Windows
```

Generated builds are ignored by Git.
