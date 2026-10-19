using System;
using System.Collections.Generic;
using Survival.Domain.Enemies;
using Survival.Domain.Heroes;
using Survival.Domain.Roster;
using Xunit;

namespace Survival.Domain.Tests;

/// <summary>
/// Derek (2026-10-19): every Locked Battle character has 3 attacks. The creatures keep attack 1 (the locked, approved attack)
/// and add attacks 2 and 3 from clips they already ship (no premade Mixamo take exists for a non-humanoid rig, and nothing
/// is hand-keyed). The hit frames come from the locked clips: the curves below are bone tail positions measured in Blender
/// 5.2 on the locked FBXs (clip frames 0-30, metres in the FBX's design units; heights for flaps and steps, forward reach
/// for Nightfang's lunge).
/// </summary>
public sealed class CreatureExtraAttackTests
{
    private static readonly float[] EmberfangFlapTipL =
    {
        0.938f, 0.933f, 0.917f, 0.889f, 0.850f, 0.799f, 0.737f, 0.667f, 0.593f, 0.517f, 0.445f, 0.381f, 0.328f, 0.288f, 0.264f, 0.256f,
        0.264f, 0.288f, 0.328f, 0.381f, 0.445f, 0.517f, 0.593f, 0.667f, 0.737f, 0.799f, 0.850f, 0.889f, 0.917f, 0.933f, 0.938f
    };

    private static readonly float[] EmberfangFlapTipR =
    {
        0.935f, 0.929f, 0.912f, 0.884f, 0.843f, 0.790f, 0.726f, 0.654f, 0.576f, 0.498f, 0.423f, 0.357f, 0.302f, 0.261f, 0.236f, 0.227f,
        0.236f, 0.261f, 0.302f, 0.357f, 0.423f, 0.498f, 0.576f, 0.654f, 0.726f, 0.790f, 0.843f, 0.884f, 0.912f, 0.929f, 0.935f
    };

    private static readonly float[] StormcrestFlapHandL =
    {
        0.913f, 0.914f, 0.911f, 0.905f, 0.897f, 0.885f, 0.871f, 0.855f, 0.837f, 0.819f, 0.800f, 0.782f, 0.765f, 0.752f, 0.741f, 0.734f,
        0.732f, 0.734f, 0.741f, 0.751f, 0.766f, 0.783f, 0.802f, 0.822f, 0.841f, 0.860f, 0.876f, 0.890f, 0.901f, 0.909f, 0.913f
    };

    private static readonly float[] StormcrestFlapHandR =
    {
        0.943f, 0.946f, 0.945f, 0.942f, 0.936f, 0.928f, 0.916f, 0.901f, 0.884f, 0.866f, 0.846f, 0.826f, 0.807f, 0.790f, 0.776f, 0.767f,
        0.762f, 0.762f, 0.767f, 0.777f, 0.791f, 0.809f, 0.828f, 0.848f, 0.868f, 0.887f, 0.903f, 0.918f, 0.929f, 0.938f, 0.943f
    };

    private static readonly float[] AshwyrmFlapTipL =
    {
        1.195f, 1.221f, 1.244f, 1.264f, 1.280f, 1.292f, 1.300f, 1.305f, 1.305f, 1.302f, 1.295f, 1.284f, 1.270f, 1.252f, 1.230f, 1.206f,
        1.181f, 1.155f, 1.130f, 1.107f, 1.089f, 1.075f, 1.067f, 1.066f, 1.070f, 1.081f, 1.097f, 1.118f, 1.143f, 1.169f, 1.195f
    };

    private static readonly float[] AshwyrmFlapTipR =
    {
        1.221f, 1.245f, 1.266f, 1.284f, 1.299f, 1.310f, 1.318f, 1.322f, 1.323f, 1.322f, 1.316f, 1.308f, 1.295f, 1.279f, 1.259f, 1.236f,
        1.212f, 1.187f, 1.162f, 1.140f, 1.121f, 1.108f, 1.100f, 1.098f, 1.102f, 1.113f, 1.128f, 1.149f, 1.172f, 1.197f, 1.221f
    };

    private static readonly float[] EmberfangWalkToeL =
    {
        0.014f, 0.008f, 0.003f, -0.001f, -0.004f, -0.005f, -0.006f, -0.007f, -0.007f, -0.006f, -0.005f, -0.004f, -0.001f, 0.003f, 0.008f, 0.014f,
        0.025f, 0.038f, 0.052f, 0.066f, 0.079f, 0.087f, 0.092f, 0.092f, 0.087f, 0.079f, 0.066f, 0.052f, 0.038f, 0.025f, 0.014f
    };

    private static readonly float[] EmberfangWalkToeR =
    {
        0.014f, 0.025f, 0.038f, 0.052f, 0.066f, 0.079f, 0.087f, 0.092f, 0.092f, 0.087f, 0.079f, 0.066f, 0.052f, 0.038f, 0.025f, 0.014f,
        0.008f, 0.003f, -0.001f, -0.004f, -0.005f, -0.006f, -0.007f, -0.007f, -0.006f, -0.005f, -0.004f, -0.001f, 0.003f, 0.008f, 0.014f
    };

    private static readonly float[] StormcrestWalkToeL =
    {
        -0.032f, -0.032f, -0.032f, -0.032f, -0.032f, -0.031f, -0.031f, -0.031f, -0.030f, -0.028f, -0.028f, -0.028f, -0.028f, -0.028f, -0.028f, -0.028f,
        -0.018f, -0.006f, 0.004f, 0.014f, 0.023f, 0.028f, 0.032f, 0.031f, 0.029f, 0.022f, 0.012f, 0.002f, -0.009f, -0.021f, -0.032f
    };

    private static readonly float[] StormcrestWalkToeR =
    {
        0.042f, 0.051f, 0.063f, 0.074f, 0.084f, 0.092f, 0.098f, 0.101f, 0.101f, 0.098f, 0.092f, 0.085f, 0.074f, 0.063f, 0.052f, 0.042f,
        0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f, 0.042f
    };

    private static readonly float[] AshwyrmWalkShinL =
    {
        0.273f, 0.271f, 0.271f, 0.272f, 0.274f, 0.277f, 0.279f, 0.281f, 0.281f, 0.281f, 0.279f, 0.277f, 0.276f, 0.275f, 0.276f, 0.278f,
        0.270f, 0.265f, 0.263f, 0.263f, 0.263f, 0.264f, 0.265f, 0.264f, 0.263f, 0.261f, 0.259f, 0.259f, 0.261f, 0.266f, 0.273f
    };

    private static readonly float[] AshwyrmWalkShinR =
    {
        0.192f, 0.182f, 0.174f, 0.170f, 0.167f, 0.166f, 0.166f, 0.166f, 0.166f, 0.166f, 0.166f, 0.168f, 0.170f, 0.175f, 0.183f, 0.193f,
        0.191f, 0.190f, 0.190f, 0.191f, 0.192f, 0.193f, 0.193f, 0.193f, 0.192f, 0.191f, 0.190f, 0.189f, 0.189f, 0.190f, 0.192f
    };

    private static readonly float[] NightfangTrotToeL =
    {
        0.016f, 0.036f, 0.055f, 0.072f, 0.085f, 0.094f, 0.098f, 0.100f, 0.096f, 0.087f, 0.076f, 0.065f, 0.053f, 0.041f, 0.028f, 0.016f,
        0.007f, 0.001f, -0.004f, -0.008f, -0.013f, -0.019f, -0.026f, -0.032f, -0.037f, -0.038f, -0.034f, -0.027f, -0.016f, -0.002f, 0.016f
    };

    private static readonly float[] NightfangTrotToeR =
    {
        -0.066f, -0.061f, -0.053f, -0.047f, -0.043f, -0.042f, -0.044f, -0.048f, -0.054f, -0.062f, -0.070f, -0.075f, -0.076f, -0.075f, -0.071f, -0.065f,
        -0.056f, -0.047f, -0.038f, -0.033f, -0.031f, -0.031f, -0.035f, -0.042f, -0.048f, -0.054f, -0.060f, -0.065f, -0.068f, -0.068f, -0.066f
    };

    private static readonly float[] NightfangAttackSnoutForward =
    {
        0.906f, 0.907f, 0.909f, 0.909f, 0.905f, 0.898f, 0.887f, 0.874f, 0.864f, 0.860f, 0.905f, 0.975f, 0.997f, 0.970f, 0.949f, 0.946f,
        0.938f, 0.926f, 0.910f, 0.895f, 0.889f, 0.890f, 0.892f, 0.896f, 0.899f, 0.903f, 0.905f, 0.906f, 0.906f, 0.906f, 0.906f
    };

    private static readonly float[] NightfangAttackToeRForward =
    {
        0.320f, 0.303f, 0.287f, 0.242f, 0.189f, 0.137f, 0.085f, 0.044f, 0.015f, 0.007f, 0.093f, 0.318f, 0.583f, 0.779f, 0.876f, 0.856f,
        0.775f, 0.672f, 0.576f, 0.497f, 0.465f, 0.481f, 0.463f, 0.454f, 0.431f, 0.403f, 0.379f, 0.350f, 0.332f, 0.320f, 0.320f
    };

    private static readonly string[] Creatures = { "Emberfang", "Stormcrest", "Nightfang", "Ashwyrm" };

    private static LockedBattleFighter Fighter(string name)
    {
        foreach (var f in LockedBattleRoster.All)
        {
            if (f.Name == name)
            {
                return f;
            }
        }

        throw new InvalidOperationException(name);
    }

    private static string[] PoseNames(string name)
    {
        switch (name)
        {
            case "Emberfang":
                return EmberfangMotion.PoseNames;
            case "Stormcrest":
                return StormcrestMotion.PoseNames;
            case "Nightfang":
                return NightfangMotion.PoseNames;
            default:
                return AshwyrmMotion.PoseNames;
        }
    }

    private static string[] BoneNames(string name)
    {
        switch (name)
        {
            case "Emberfang":
                return EmberfangMotion.BoneNames;
            case "Stormcrest":
                return StormcrestMotion.BoneNames;
            case "Nightfang":
                return NightfangMotion.BoneNames;
            default:
                return AshwyrmMotion.BoneNames;
        }
    }

    private static int ArgMin(float[] curve)
    {
        var best = 0;
        for (var i = 1; i < curve.Length; i++)
        {
            if (curve[i] < curve[best])
            {
                best = i;
            }
        }

        return best;
    }

    private static int ArgMax(float[] curve)
    {
        var best = 0;
        for (var i = 1; i < curve.Length; i++)
        {
            if (curve[i] > curve[best])
            {
                best = i;
            }
        }

        return best;
    }

    /// <summary>Height of a looping 30-frame clip curve at a frame of a multi-loop attack, 0 = lowest, 1 = highest.</summary>
    private static float Lift(float[] curve, int frame)
    {
        var min = curve[ArgMin(curve)];
        var max = curve[ArgMax(curve)];
        return (curve[((frame % 30) + 30) % 30] - min) / (max - min);
    }

    /// <summary>Highest lift over the six frames before <paramref name="frame"/>: the foot was up in its step.</summary>
    private static float PeakBefore(float[] curve, int frame)
    {
        var best = 0f;
        for (var k = 1; k <= 6; k++)
        {
            best = Math.Max(best, Lift(curve, frame - k));
        }

        return best;
    }

    [Fact]
    public void Each_creature_cycles_three_attacks_with_attack_one_still_the_locked_attack()
    {
        foreach (var name in Creatures)
        {
            var f = Fighter(name);
            Assert.Equal(3, LockedBattleRotation.Count(f));
            var first = LockedBattleRotation.AttackAt(f, 0);
            Assert.Equal(BlenderRigAttackSpec.PoseName, first.PoseName);
            Assert.Equal(f.AttackPoseName, first.PoseName);
            Assert.Equal(f.AttackLabel, first.Label);
            Assert.Equal(BlenderRigAttackSpec.ClipSeconds, first.Seconds, 4);
            Assert.Equal(LockedBattleAttack.ProfileContactFrame, first.MeleeContactFrame);
            Assert.Null(first.Impact);
            Assert.Empty(first.ExtraHits);
            Assert.Equal(0, first.FlightFrames);
            Assert.True(first.ReachShift.IsNone);
            Assert.True(f.AttackCyclesItself);

            // 1 -> 2 -> 3 -> 1, each followed by the rest gap.
            var at = 0f;
            for (var i = 0; i < 4; i++)
            {
                Assert.True(LockedBattleRotation.Locate(f, at + 0.01f, out var index, out _));
                Assert.Equal(i % 3, index);
                at += LockedBattleRotation.CycleSeconds(f, i % 3);
            }
        }
    }

    [Fact]
    public void Extra_attacks_play_poses_the_creature_ships_and_bones_it_has()
    {
        foreach (var name in Creatures)
        {
            var f = Fighter(name);
            var profile = LockedBattleHits.ProfileOf(f.Name);
            var labels = new HashSet<string>();
            for (var i = 0; i < 3; i++)
            {
                var a = LockedBattleRotation.AttackAt(f, i);
                Assert.Contains(a.PoseName, PoseNames(name));
                Assert.True(labels.Add(a.Label), name + " " + a.Label);
                if (i == 0)
                {
                    continue;
                }

                Assert.NotNull(a.Impact);
                Assert.True(LockedBattleRotation.HitCount(f, profile, i) >= 2, name + " " + a.Label);
                for (var h = 0; h < LockedBattleRotation.HitCount(f, profile, i); h++)
                {
                    Assert.Contains(LockedBattleRotation.HitStrikeBone(f, profile, i, h), BoneNames(name));
                    var frame = LockedBattleRotation.HitFrame(f, profile, i, h);
                    // Inside the clip, before its last frame, so the hit never falls into the rest gap.
                    Assert.InRange(frame, 1, (int)Math.Round(a.Seconds * BlenderRigAttackSpec.FrameRate) - 1);
                    Assert.True(LockedBattleRoster.InAttackClip(f, LockedBattleRotation.OffsetSeconds(f, i) + (frame / 30f)), name + " " + a.Label);
                    Assert.True(frame - a.FlightFrames >= 0, name + " " + a.Label);
                    if (h > 0)
                    {
                        // Hits land on separate frames, so the battle registers every one.
                        Assert.True(frame > LockedBattleRotation.HitFrame(f, profile, i, h - 1), name + " " + a.Label);
                    }
                }
            }
        }
    }

    [Fact]
    public void Dragons_never_replay_the_fireball_pose_outside_attack_one()
    {
        foreach (var name in new[] { "Emberfang", "Stormcrest", "Ashwyrm" })
        {
            var f = Fighter(name);
            Assert.NotEqual(BlenderRigAttackSpec.PoseName, LockedBattleRotation.AttackAt(f, 1).PoseName);
            Assert.NotEqual(BlenderRigAttackSpec.PoseName, LockedBattleRotation.AttackAt(f, 2).PoseName);
        }
    }

    [Fact]
    public void Each_attack_has_its_own_impact()
    {
        foreach (var name in Creatures)
        {
            var f = Fighter(name);
            var profile = LockedBattleHits.ProfileOf(f.Name);
            var kinds = new HashSet<BattleImpactKind>();
            for (var i = 0; i < 3; i++)
            {
                Assert.True(kinds.Add(LockedBattleRotation.Impact(f, profile, i)), name + " attack " + (i + 1));
            }
        }

        Assert.Equal(BattleImpactKind.WingGust, LockedBattleRotation.Impact(LockedBattleRoster.Emberfang, LockedBattleHits.Emberfang, 1));
        Assert.Equal(BattleImpactKind.StompQuake, LockedBattleRotation.Impact(LockedBattleRoster.Ashwyrm, LockedBattleHits.Ashwyrm, 2));
        Assert.Equal(BattleImpactKind.VillainShadowLash, LockedBattleRotation.Impact(LockedBattleRoster.Nightfang, LockedBattleHits.Nightfang, 1));
        Assert.Equal(BattleImpactKind.ClawWave, LockedBattleRotation.Impact(LockedBattleRoster.Nightfang, LockedBattleHits.Nightfang, 2));
    }

    [Theory]
    [InlineData("Emberfang")]
    [InlineData("Stormcrest")]
    [InlineData("Ashwyrm")]
    public void Wing_gusts_leave_each_wing_at_the_bottom_of_its_measured_downstroke(string name)
    {
        var f = Fighter(name);
        var profile = LockedBattleHits.ProfileOf(f.Name);
        var gust = LockedBattleRotation.AttackAt(f, 1);
        float[] left;
        float[] right;
        switch (name)
        {
            case "Emberfang":
                left = EmberfangFlapTipL;
                right = EmberfangFlapTipR;
                break;
            case "Stormcrest":
                left = StormcrestFlapHandL;
                right = StormcrestFlapHandR;
                break;
            default:
                left = AshwyrmFlapTipL;
                right = AshwyrmFlapTipR;
                break;
        }

        Assert.Equal(1f, gust.Seconds, 4);
        Assert.Equal(2, LockedBattleRotation.HitCount(f, profile, 1));
        Assert.EndsWith(".L", LockedBattleRotation.HitStrikeBone(f, profile, 1, 0), StringComparison.Ordinal);
        Assert.EndsWith(".R", LockedBattleRotation.HitStrikeBone(f, profile, 1, 1), StringComparison.Ordinal);
        Assert.True(gust.FlightFrames > 0);
        Assert.True(gust.SpreadMetres > 0f);
        var looseL = LockedBattleRotation.HitFrame(f, profile, 1, 0) - gust.FlightFrames;
        var looseR = LockedBattleRotation.HitFrame(f, profile, 1, 1) - gust.FlightFrames;
        Assert.Equal(ArgMin(left), looseL);
        Assert.InRange(looseR - ArgMin(right), 0, 1);

        // The wing really swept down into it: at least 40 % of its stroke higher eight frames earlier.
        Assert.True(Lift(left, looseL - 8) > 0.4f, name);
        Assert.True(Lift(right, looseR - 8) > 0.4f, name);
    }

    [Theory]
    [InlineData("Emberfang")]
    [InlineData("Stormcrest")]
    [InlineData("Ashwyrm")]
    public void Stomp_cracks_leave_the_foot_as_it_comes_down_in_two_walk_loops(string name)
    {
        var f = Fighter(name);
        var profile = LockedBattleHits.ProfileOf(f.Name);
        var stomp = LockedBattleRotation.AttackAt(f, 2);
        Assert.Equal(f.WalkPoseName, stomp.PoseName);
        Assert.Equal(2f, stomp.Seconds, 4);
        Assert.Equal(0f, stomp.FlightLoftMetres);
        Assert.True(stomp.FlightFrames > 0);
        Assert.Equal(3, LockedBattleRotation.HitCount(f, profile, 2));
        for (var h = 0; h < 3; h++)
        {
            var bone = LockedBattleRotation.HitStrikeBone(f, profile, 2, h);
            var left = bone.EndsWith(".L", StringComparison.Ordinal);
            float[] curve;
            switch (name)
            {
                case "Emberfang":
                    curve = left ? EmberfangWalkToeL : EmberfangWalkToeR;
                    break;
                case "Stormcrest":
                    curve = left ? StormcrestWalkToeL : StormcrestWalkToeR;
                    break;
                default:
                    curve = left ? AshwyrmWalkShinL : AshwyrmWalkShinR;
                    break;
            }

            var footfall = LockedBattleRotation.HitFrame(f, profile, 2, h) - stomp.FlightFrames;
            Assert.True(Lift(curve, footfall) <= 0.35f, name + " " + bone + " down at " + footfall);
            Assert.True(PeakBefore(curve, footfall) >= 0.5f, name + " " + bone + " lifted before " + footfall);
            if (h > 0)
            {
                // Alternating feet.
                Assert.NotEqual(LockedBattleRotation.HitStrikeBone(f, profile, 2, h - 1), bone);
            }
        }
    }

    [Fact]
    public void Nightfang_pounce_bites_and_claws_on_the_measured_lunge_frames()
    {
        var f = LockedBattleRoster.Nightfang;
        var profile = LockedBattleHits.Nightfang;
        var pounce = LockedBattleRotation.AttackAt(f, 1);
        Assert.Equal(NightfangMotion.AttackPoseName, pounce.PoseName);
        Assert.Equal(2, LockedBattleRotation.HitCount(f, profile, 1));
        Assert.Equal("snout", LockedBattleRotation.HitStrikeBone(f, profile, 1, 0));
        Assert.Equal("toe.R", LockedBattleRotation.HitStrikeBone(f, profile, 1, 1));
        Assert.Equal(ArgMax(NightfangAttackSnoutForward), LockedBattleRotation.HitFrame(f, profile, 1, 0));
        Assert.Equal(ArgMax(NightfangAttackToeRForward), LockedBattleRotation.HitFrame(f, profile, 1, 1));

        // Attack 1's claw rake is the left paw; the pounce adds a short host slide through the bite and claw only.
        Assert.Equal("toe.L", LockedBattleRotation.StrikeBone(f, profile, 0));
        Assert.False(pounce.ReachShift.IsNone);
        Assert.InRange(pounce.ReachShift.Metres, 0.1f, 0.4f);
        Assert.Equal(pounce.ReachShift.Metres, pounce.ReachShift.At(12f), 4);
        Assert.Equal(pounce.ReachShift.Metres, pounce.ReachShift.At(14f), 4);
        Assert.Equal(0f, pounce.ReachShift.At(30f));
    }

    [Fact]
    public void Nightfang_claw_waves_leave_each_fore_paw_as_it_lands_in_the_trot()
    {
        var f = LockedBattleRoster.Nightfang;
        var profile = LockedBattleHits.Nightfang;
        var wave = LockedBattleRotation.AttackAt(f, 2);
        Assert.Equal(NightfangMotion.TrotPoseName, wave.PoseName);
        Assert.Equal(2f, wave.Seconds, 4);
        Assert.Equal(3, LockedBattleRotation.HitCount(f, profile, 2));
        for (var h = 0; h < 3; h++)
        {
            var bone = LockedBattleRotation.HitStrikeBone(f, profile, 2, h);
            var curve = bone == "toe.L" ? NightfangTrotToeL : NightfangTrotToeR;
            Assert.True(bone == "toe.L" || bone == "toe.R", bone);
            var pawfall = LockedBattleRotation.HitFrame(f, profile, 2, h) - wave.FlightFrames;
            Assert.True(Lift(curve, pawfall) <= 0.35f, bone + " down at " + pawfall);
            Assert.True(PeakBefore(curve, pawfall) >= 0.5f, bone + " lifted before " + pawfall);
        }
    }

    [Fact]
    public void Dragons_stay_twice_as_big_and_keep_duelling_each_other()
    {
        Assert.Equal(2f, LockedBattleRoster.BattleScaleOf(LockedBattleRoster.Emberfang));
        Assert.Equal(2f, LockedBattleRoster.BattleScaleOf(LockedBattleRoster.Ashwyrm));
        Assert.Equal(LockedBattleRoster.Emberfang.Lane, LockedBattleRoster.Ashwyrm.Lane);
        Assert.Equal("Ashwyrm", LockedBattleRoster.TargetOf(LockedBattleRoster.Emberfang).Name);
        Assert.Equal("Emberfang", LockedBattleRoster.TargetOf(LockedBattleRoster.Ashwyrm).Name);

        // Their extra attacks reach across the duel gap by streak (gust, crack), never by walking up to each other.
        foreach (var f in new[] { LockedBattleRoster.Emberfang, LockedBattleRoster.Ashwyrm })
        {
            for (var i = 1; i < 3; i++)
            {
                var a = LockedBattleRotation.AttackAt(f, i);
                Assert.True(a.FlightFrames > 0, f.Name);
                Assert.True(a.ReachShift.IsNone, f.Name);
                Assert.False(a.InPlace, f.Name);
            }
        }
    }
}
