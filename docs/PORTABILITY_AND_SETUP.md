# Portability And Setup

This document keeps KRetouch Studio portable across developer computers.

## Path Policy

- Use the current checkout root as the repository root.
- Do not depend on a fixed path such as `C:\Users\<name>\source\repos\...`.
- Use repository-relative paths for source files, model files, helper scripts, and documentation links.
- Store user settings outside the repository:
  - app config: `%AppData%\KRetouchStudio\config.json`
  - MediaPipe test output: `%LocalAppData%\KRetouchStudio\MediaPipeOutput`
- Store user-selected photo folders only in user config, not in source files.

## Build

- Required platform: Windows x64.
- Required .NET SDK: `10.0.301` or compatible `10.0.x` SDK.
- Build from the checkout root:

```powershell
dotnet build .\KRetouchStudio.sln -p:Platform=x64
```

## MediaPipe Helper

- Tested Python runtime: Python `3.12.10`.
- Install helper dependencies from the checkout root:

```powershell
python -m pip install -r requirements.txt
```

- The C# app resolves the helper and model folder from the runtime base directory:
  - `Tools\MediaPipe\mediapipe_helper.py`
  - `Assets\AiModels\MediaPipe`
- Keep model files available in the runtime output when MediaPipe preview tests are enabled.

## Development Package Guard

- The development package command is a developer-only command.
- It is exposed under `Prefs > Development Package...` in Debug builds.
- It requires the local development password before changing project version files or starting package creation.
- Generated release zip files stay out of Git tracking.

## Commercial Release Note

- MediaPipe model files are included here for reproducible prototype setup.
- Before commercial redistribution, verify the current model license and distribution policy from the official model source.
