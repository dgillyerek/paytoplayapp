using Survival.Domain.Heroes;
using Survival.Domain.Roster;

namespace Survival.Domain.Tests;

public sealed class OakenshieldSideStepTests
{
    private static AttackTrack Held => OakenshieldAttack.Spec.Tracks[0];
    private static AttackTrack Flight => OakenshieldAttack.Spec.Tracks[1];

    [Fact]
    public void Take_starts_and_ends_on_the_old_rest_frame()
    {
        var f0 = Held.Frames[0];
        Assert.Equal(0.36195f, f0.X, 4);
        Assert.Equal(0.79451f, f0.Y, 4);
        Assert.Equal(0.04801f, f0.Z, 4);
        var f30 = Held.Frames[30];
        Assert.Equal(f0.X, f30.X, 4);
        Assert.Equal(f0.Y, f30.Y, 4);
        Assert.Equal(f0.Z, f30.Z, 4);
    }

    [Fact]
    public void Spear_points_at_the_target_from_the_cock_to_the_release_and_leaves_from_the_hand()
    {
        var hold = Held.Frames[10];
        for (var f = 8; f <= 14; f++)
        {
            var fr = Held.Frames[f];
            Assert.Equal(hold.Qx, fr.Qx, 3);
            Assert.Equal(hold.Qy, fr.Qy, 3);
            Assert.Equal(hold.Qz, fr.Qz, 3);
            Assert.Equal(hold.Qw, fr.Qw, 3);
        }

        var r = OakenshieldAttack.ReleaseFrame;
        Assert.Equal(14, r);
        Assert.Equal(Held.Frames[r].X, Flight.Frames[r].X, 4);
        Assert.Equal(Held.Frames[r].Y, Flight.Frames[r].Y, 4);
        Assert.Equal(Held.Frames[r].Z, Flight.Frames[r].Z, 4);
        for (var f = r + 1; f <= 30; f++)
        {
            Assert.Equal(11f / 30f, Flight.Frames[f].Z - Flight.Frames[f - 1].Z, 3);
            Assert.Equal(Flight.Frames[r].X, Flight.Frames[f].X, 4);
            Assert.Equal(Flight.Frames[r].Y, Flight.Frames[f].Y, 4);
        }
    }

    [Fact]
    public void Released_spear_still_stops_on_Ashwyrm_upper_body()
    {
        var rel = Flight.Frames[OakenshieldAttack.ReleaseFrame];
        var box = LockedBattleHits.Ashwyrm.Box;
        Assert.InRange(rel.Y, 1.0f, box.Height);
        Assert.InRange(Math.Abs(rel.X), 0f, box.HalfWidth);

        // Same lane, facing each other at attack range: the spear flies down Ashwyrm's -Z (his front faces +Z toward her).
        var range = LockedBattleRoster.Oakenshield.AttackRangeMetres;
        var startZ = range - rel.Z;          // distance in front of Ashwyrm's origin
        var x = -rel.X;                      // facing flips x
        Assert.True(LockedBattleHits.ProjectileStopsOnContact(box, x, rel.Y, startZ, x, rel.Y, -(box.Back + 1f)));
        Assert.True(LockedBattleHits.ProjectileSegmentHits(box, x, rel.Y, startZ, x, rel.Y, -(box.Back + 1f), out var t));
        var hitZ = startZ + (t * (-(box.Back + 1f) - startZ));
        Assert.Equal(box.Front, hitZ, 3);
    }

    [Fact]
    public void Side_step_note_and_script_are_with_the_design_files()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
        {
            root = root.Parent;
        }

        var design = Path.Combine(root!.FullName, OakenshieldMotion.DesignDir);
        Assert.True(File.Exists(Path.Combine(design, "work", "sidestep.py")));
        Assert.Contains("LEFT foot", File.ReadAllText(Path.Combine(design, "NOTE.md")), StringComparison.Ordinal);
        Assert.Contains(OakenshieldAttack.FileMd5 + "  " + OakenshieldAttack.FileName, File.ReadAllText(Path.Combine(design, "CHECKSUMS.md5")), StringComparison.Ordinal);
    }
}
