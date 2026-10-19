namespace Survival.Domain.Tests;

/// <summary>
/// Derek's Game-view notes 2026-10-18 on #70: "the screen is jumping around a bit". The camera eases to its target orbit, reads one input backend, caps scroll, drag and frame step,
/// and keeps hit shake to a small, short tremor.
/// </summary>
public sealed class LockedBattleCameraSteadyTests
{
    private static string Unity(string file) =>
        File.ReadAllText(Path.Combine(ArcherExtraAttackTests.FindRepoRoot(), "Assets", "Survival", "Unity", file));

    [Fact]
    public void Camera_eases_to_its_target_and_holds_still_without_input()
    {
        var camera = Unity("LockedBattleCamera.cs");
        Assert.Contains("var k = 1f - Mathf.Exp(-SmoothPerSecond * dt);", camera, StringComparison.Ordinal);
        Assert.Contains("Mathf.LerpAngle(_viewYaw, _yaw, k)", camera, StringComparison.Ordinal);
        Assert.Contains("var dt = Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);", camera, StringComparison.Ordinal);
        Assert.Contains("notches = Mathf.Clamp(notches, -MaxNotchesPerFrame, MaxNotchesPerFrame);", camera, StringComparison.Ordinal);
        Assert.Contains("var delta = pressed ? Vector2.zero : ClampDrag(mouse.delta.ReadValue());", camera, StringComparison.Ordinal);
        Assert.Contains("notches = ScrollNotches(wheel);", camera, StringComparison.Ordinal);
        Assert.Contains("notches = ScrollNotches(Input.mouseScrollDelta.y);", camera, StringComparison.Ordinal);
        // the legacy backend is read only when the Input System has no device (no double read)
        Assert.Contains("heard = true;", camera, StringComparison.Ordinal);
        Assert.Contains("if (keyboard == null || !keyboard.enabled)", camera, StringComparison.Ordinal);
        // the camera never follows a fighter
        Assert.DoesNotContain("Target", camera, StringComparison.Ordinal);
        Assert.Contains("beginCameraRendering += OnBeginCamera", camera, StringComparison.Ordinal);
    }

    [Fact]
    public void Hit_shake_is_small_and_short()
    {
        var camera = Unity("LockedBattleCamera.cs");
        Assert.Contains("public const float ShakeSeconds = 0.12f;", camera, StringComparison.Ordinal);
        Assert.Contains("public const float ShakeScale = 0.25f;", camera, StringComparison.Ordinal);
        Assert.Contains("public const float MaxShakeMetres = 0.012f;", camera, StringComparison.Ordinal);
        Assert.Contains("_shakeMetres = Mathf.Min(Mathf.Max(metres * ShakeScale, left), MaxShakeMetres);", camera, StringComparison.Ordinal);
        Assert.DoesNotContain("* 55f", camera, StringComparison.Ordinal);
    }
}
