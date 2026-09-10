# P0 build order

1. **Unity 6.3 LTS + URP project** — this repo. Open `Board.unity`, confirm URP pink-free camera.
2. **DEV-001 domain** — 7×5 board, catalog recipes, `TryDrag`, snap-back. Keep `dotnet test` green.
3. **Board view** — spawn 2D placeholders on the grid; drag with snap-back tween.
4. **Seed content** — one merge chain (pebble→grove) and a boot seed layout.
5. **IAP seam** — keep `IPurchaseService` / `FakeStore`; no store SDK until a later DEV.
6. **Android + iOS player settings** — API 26 / iOS 15 already set; make a development APK and an iOS Xcode dump.
7. **QA pass** — [QA_DEVICE_MATRIX.md](QA_DEVICE_MATRIX.md) on two Android + two iPhone class devices.
8. **Progression / live ops / real IAP** — after DEV-001 is boringly stable.

Do not start 3D Maya pipelines, addressables content farms, or paid CI before the merge loop is done.
