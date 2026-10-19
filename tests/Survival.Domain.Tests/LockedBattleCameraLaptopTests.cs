namespace Survival.Domain.Tests;

/// <summary>
/// Laptop camera controls (Derek 2026-10-18, no middle mouse button): Shift + left-drag pans like middle-drag, WASD / arrow keys
/// pan relative to the camera's facing, Q/E and -/+ zoom. Orbit, scroll zoom and middle-drag pan are unchanged.
/// </summary>
public sealed class LockedBattleCameraLaptopTests
{
    [Fact]
    public void Shift_left_drag_and_keys_pan_inside_the_same_field_margin()
    {
        var camera = File.ReadAllText(Path.Combine(ArcherExtraAttackTests.FindRepoRoot(), "Assets", "Survival", "Unity", "LockedBattleCamera.cs"));
        // unchanged mouse controls
        Assert.Contains("middleButton", camera, StringComparison.Ordinal);
        Assert.Contains("mouse.scroll", camera, StringComparison.Ordinal);
        Assert.Contains("_orbiting = !_shiftPanning && (leftHeld || rightHeld);", camera, StringComparison.Ordinal);
        // Shift + left-drag pan, both input backends
        Assert.Contains("keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed", camera, StringComparison.Ordinal);
        Assert.Contains("Input.GetKey(KeyCode.LeftShift)", camera, StringComparison.Ordinal);
        Assert.Contains("_shiftPanning = shift && leftHeld && !rightHeld;", camera, StringComparison.Ordinal);
        // keys: WASD + arrows relative to the camera facing, Q/E and -/+ zoom
        foreach (var key in new[] { "wKey", "aKey", "sKey", "dKey", "upArrowKey", "downArrowKey", "leftArrowKey", "rightArrowKey", "qKey", "eKey", "equalsKey", "minusKey" })
        {
            Assert.Contains("keyboard." + key + ".isPressed", camera, StringComparison.Ordinal);
        }

        Assert.Contains("Slide(keyMove.normalized * (speed * dt));", camera, StringComparison.Ordinal);
        Assert.Contains("Slide(-pixels * metresPerPixel);", camera, StringComparison.Ordinal);
        // every pan goes through Solve's clamp: the orbit centre stays within PanMarginMetres (3 m) of the field
        Assert.Contains("public const float PanMarginMetres = 3f;", camera, StringComparison.Ordinal);
        Assert.Contains("Mathf.Clamp(_pivot.x, lo.x, hi.x)", camera, StringComparison.Ordinal);
    }
}
