# AGENTS.md

//* Do not edit, rewrite, delete, move, refactor, or re-encode any file without explicit user permission. //*

## Startup Handshake

At the beginning of every new Codex session, read this AGENTS.md file first.
Before analyzing, editing, patching, building, or summarizing anything, reply with this exact handshake:
"AGENTS.md loaded. I will preserve UTF-8 with BOM."

## Language Policy

* Keep project documents, technical notes, code comments, commit messages, and task summaries in English unless the user explicitly asks for Korean.
* Korean may be used only for direct conversation with the user.
* For direct conversation with the user, use friendly casual Korean banmal unless the user explicitly asks for a different tone.
* When summarizing work state, use short English bullet-style lines.

## User Addressing Rule

* At the beginning of a new Codex session, address the user as "홍실장" naturally.
* Use "홍실장" only once or twice when it fits the situation.
* Do not repeat it mechanically in every reply.

For normal direct conversation, use friendly casual Korean banmal unless the user asks otherwise.

## Encoding Rules

- Preserve UTF-8 with BOM.
- Do not use Set-Content, Out-File, or script rewrites unless UTF-8 BOM is explicitly preserved.
- Verify important text files start with EF BB BF after writing.
- Do not proactively mention BOM status to the user unless the user explicitly asks about encoding or BOM.

## Work Timing and Summary Rules

* Do not create a status summary or compaction-style recap right before starting work or right before applying a patch.
* Only summarize during natural pause points.

Pause points are:

First, after file structure analysis is complete.
Second, right after the modification plan is explained.
Third, after one feature patch is complete.

When the user says "proceed", "patch it", "fix it", or "go ahead", do not summarize. Start working immediately.

## Definition-First Modification Rule

For feature work where the visual or behavioral definition is still being discussed, especially face shape, warp, masks, landmarks, preview routing, local proxy workbench, or native/C# pipeline boundaries, do not inspect implementation details or patch immediately from a partial phrase.

Required sequence:

First, listen to the complete user definition.
Second, restate the understood definition briefly.
Third, wait for explicit user confirmation.
Fourth, inspect files and modify only after confirmation.

If the definition cannot be satisfied safely, show the reason as an error or limitation instead of silently blocking, guessing, or substituting a different behavior.

## Error Backtracking Rule

When an error, crash, broken preview, failed build, or unexpected behavior appears, do not jump directly to the last touched line or make a broad patch.

Required sequence:

First, return to the beginning of the relevant workflow.
Second, trace the flow in order until the failure point is reached.
Third, identify the first incorrect state, not only the final visible symptom.
Fourth, narrow the fix to the smallest owner stage or file.
Fifth, patch only that narrowed cause.
Sixth, build or run the smallest valid verification.

For image and preview problems, trace from:

```text
Input image
-> analysis
-> candidate masks
-> confirmed masks
-> render request
-> preview/export output
```

For state and slider problems, trace from:

```text
current photo state
-> control value
-> section state
-> render plan
-> engine call
-> output assignment
```

For local proxy workbench problems, trace from:

```text
OriginalImage
-> WorkArea
-> WorkAreaCrop
-> LocalProxy
-> LocalMask
-> LocalPreview
-> Apply or Cancel
```

For crashes, include the exception, call stack, owning thread, and first unsafe object or state.

Do not treat the last visible symptom as the root cause until the ordered flow has been checked.

## Snippet And Clipboard Encoding Rule

- Pasted snippets from chat, browser, clipboard, or terminal are text fragments, not files.
- Snippets do not carry a UTF-8 BOM.
- Do not report a pasted snippet as invalid only because it has no BOM.
- The UTF-8 with BOM requirement applies only to repository files saved on disk.
- When applying a pasted snippet to a repository file, preserve or write the target file as UTF-8 with BOM.
- After writing an important text file, verify the saved file starts with `EF BB BF`.

## Repository And Build

- Main repository path: use the current checkout root that contains `KRetouchStudio.sln`.
- Solution: `KRetouchStudio.sln`
- Target platform: x64 only
- Build command:

```powershell
dotnet build .\KRetouchStudio.sln -p:Platform=x64
```

Before building, close the running app to avoid file lock errors:

```powershell
Get-Process KRetouchStudio -ErrorAction SilentlyContinue | Stop-Process -Force
```

Run after successful build:

```powershell
Start-Process -FilePath .\bin\x64\Debug\net10.0-windows\KRetouchStudio.exe
```

Use `-WindowStyle Hidden` only when the user explicitly asks for a background run.

## Korean Text And Encoding

The app has Korean UI text, so source encoding must stay predictable.

- The project uses `.editorconfig` with `charset = utf-8-bom` for source and config files.
- Keep Korean text files such as `.cs`, `.xaml`, `.json`, and `.md` as UTF-8 with BOM.
- If Korean looks broken in terminal output, first suspect the viewer or shell encoding before rewriting source files.
- In PowerShell, prefer explicit UTF-8 reads for inspection, for example `Get-Content -Encoding UTF8`.
- Avoid broad encoding conversions across the whole repository unless there is a verified file-level problem.

## Reference Documents

Read these only when the task needs them:

- `docs\LOCAL_PROXY_WORKBENCH_DESIGN.md`
- `docs\PERSON_MASK_AND_SUBJECT_FORMULA_SPEC.md`
- `docs\KRETOUCHPRO_PHOTOGRAPHIC_TERMS_DICTIONARY.md`
- `docs\PORTRAIT_BODY_HAIR_CLOTHING_LOCATION_DICTIONARY.md`
- `docs\session_logs\2026-06-15.md`

Do not load or summarize all reference documents by default.
Use them only for larger design, mask engine, local proxy, preview/save, or retouch pipeline work.
