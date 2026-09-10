# Migrate / open notes

## This checkout

Open the **repository root** in Unity 6.3 LTS. Do not nest another project folder.

## From the side mint

The finished scaffold was first built in a token-scoped mint (`derek-gilbert/tmp-b2828a51439189c8` @ `d4fdeec`). That mint cannot push here, and this repo's Origin token cannot clone the mint (403 / scope). **This tree is the canonical Grove + DEV-001 scaffold on `paytoplayapp`.** If a byte-identical copy of the mint is required, re-run the import from a token that can read both remotes and replace these files.

## Unity first import

- Expect URP/shader compile on first open.
- `Library/`, `Logs/`, `UserSettings/` are gitignored.
- If Graphics Settings lose the URP asset, assign `Assets/Settings/URP/GroveURP.asset` to Scriptable Render Pipeline Settings **and** the Default quality level.

## Tests vs Unity

`*.csproj` under `tests/` is **not** the Unity-generated project. Unity's own `.csproj` files stay gitignored. Use `dotnet test tests/Grove.Domain.Tests/Grove.Domain.Tests.csproj`.
