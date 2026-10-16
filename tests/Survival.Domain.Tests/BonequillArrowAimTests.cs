using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Survival.Domain.Enemies;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek 10-15 23:29: side-on stance, arrow on the archer's left (outer) side of the riser, pointing straight at the target at
/// release, string drawn to the nock, no clipping. Reads work/draw_check.json (drawchk.py) and the baked Unity keys.
/// </summary>
public class BonequillArrowAimTests
{
    private static JsonElement Draw()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets")))
        {
            dir = dir.Parent;
        }

        var p = Path.Combine(dir!.FullName, BonequillMotion.DesignDir, "work", "draw_check.json");
        return JsonDocument.Parse(File.ReadAllText(p)).RootElement;
    }

    private static AttackTrack Track(string name) => BonequillAttack.Spec.Tracks.Single(t => t.ObjectName == name);

    [Fact]
    public void Arrow_points_at_the_target_within_two_degrees_at_release()
    {
        var rel = Draw().GetProperty("release");
        Assert.InRange(rel.GetProperty("yaw_deg").GetDouble(), -2.0, 2.0);
        Assert.InRange(rel.GetProperty("pitch_deg").GetDouble(), -2.0, 2.0);
        // baked Unity keys: nock (held arrow at the last nocked frame) straight to the projectile path, character forward (+Z)
        var held = Track("BONEQUILL_bonearrow_purple_fletch").Frames[16];
        var q = new System.Numerics.Quaternion(held.Qx, held.Qy, held.Qz, held.Qw);
        var fwd = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, q);
        Assert.True(fwd.Z > Math.Cos(2.0 * Math.PI / 180.0), $"held arrow forward {fwd}");
        var proj = Track("BONEQUILL_bonearrow_purple_fletch_inflight").Frames;
        var a = proj[BonequillAttack.ReleaseFrame];
        var b = proj[30];
        var d = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z));
        Assert.True(d.Z > Math.Cos(2.0 * Math.PI / 180.0), $"projectile path {d}");
    }

    [Fact]
    public void Arrow_rests_on_the_outer_left_side_of_the_riser_not_through_it()
    {
        var g = BonequillAttack.BowGrip;
        Assert.NotNull(g.ArrowRest);
        Assert.True(g.ArrowRest![0] >= 0.03f, "arrow rest must sit beside the riser (wrap half-width 0.024 + shaft)");
        var rows = Draw().GetProperty("rows").EnumerateArray().ToArray();
        foreach (var r in rows)
        {
            Assert.True(r[5].GetDouble() > 0.024, $"f{r[0]}: arrow within the riser wrap");
            Assert.True(r[6].GetDouble() > 0.0, $"f{r[0]}: arrow on the wrong side of the riser");
        }
    }

    [Fact]
    public void String_is_drawn_to_the_nock_while_nocked_and_straight_otherwise()
    {
        var g = BonequillAttack.BowGrip;
        Assert.True(g.HasDrawnString);
        Assert.Equal("BONEQUILL_bonearrow_purple_fletch", g.ArrowTrack);
        Assert.Equal("BONEQUILL_bow_string", g.StringMaterial);
        Assert.Equal(BonequillAttack.ReleaseFrame, (int)g.ReleaseFrame);
        Assert.False(g.Nocked(true, 9.5f));
        Assert.True(g.Nocked(true, 10f));
        Assert.True(g.Nocked(true, 16.9f));
        Assert.False(g.Nocked(true, 17f));
        Assert.False(g.Nocked(false, 14f));
        Assert.True(Track("BONEQUILL_bonearrow_purple_fletch").Frames[10].Visible);
    }

    [Fact]
    public void Drawn_string_and_arrow_clear_the_body_and_cloak()
    {
        var m = Draw().GetProperty("min");
        Assert.True(m.GetProperty("string_body_m").GetDouble() >= 0.01);
        Assert.True(m.GetProperty("string_cloak_m").GetDouble() >= 0.01);
        Assert.True(m.GetProperty("arrow_body_m").GetDouble() >= 0.01);
        Assert.True(m.GetProperty("arrow_cloak_m").GetDouble() >= 0.01);
    }
}
