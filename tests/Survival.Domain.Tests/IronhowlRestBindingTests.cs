using System.Text;
using Survival.Domain.Enemies;

namespace Survival.Domain.Tests;

/// <summary>
/// Rest is played on the body instantiated from IRONHOWL_rig.fbx.
/// Its animated node paths have to match walk, which already binds.
/// A Blender round-trip inserts an Armature root and the clip plays without moving the arms.
/// </summary>
public sealed class IronhowlRestBindingTests
{
    [Fact]
    public void Rest_animated_node_paths_match_walk_and_the_arms_are_not_the_t_pose()
    {
        var root = FindRepoRoot();
        var walkPack = Path.Combine(root, "Assets", IronhowlMotion.WalkThemePackRel);
        var walkPaths = AnimatedPaths(File.ReadAllBytes(walkPack));
        Assert.DoesNotContain(walkPaths, p => p.Contains("Armature", StringComparison.Ordinal));
        Assert.Contains("mixamorig:Hips|Lcl Rotation", walkPaths);
        Assert.Contains(
            "mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm|Lcl Rotation",
            walkPaths);

        foreach (var restRel in new[]
        {
            Path.Combine("Assets", IronhowlMotion.RestThemePackRel),
            Path.Combine(IronhowlMotion.DesignMixamoDir, IronhowlMotion.RestFileName)
        })
        {
            var bytes = File.ReadAllBytes(Path.Combine(root, restRel));
            var restPaths = AnimatedPaths(bytes);
            var onlyRest = restPaths.Except(walkPaths).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var onlyWalk = walkPaths.Except(restPaths).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            Assert.True(
                onlyRest.Length == 0 && onlyWalk.Length == 0,
                restRel + " paths differ. only rest: " + string.Join(", ", onlyRest) + " only walk: " + string.Join(", ", onlyWalk));
            Assert.DoesNotContain(restPaths, p => p.Contains("Armature", StringComparison.Ordinal));
        }

        var body = File.ReadAllBytes(Path.Combine(root, "Assets", IronhowlMotion.BodyThemePackRel));
        var rest = File.ReadAllBytes(Path.Combine(root, "Assets", IronhowlMotion.RestThemePackRel));
        var bodyArm = RotationX(body, "mixamorig:LeftArm");
        var restArm = RotationX(rest, "mixamorig:LeftArm");
        var restFore = RotationX(rest, "mixamorig:LeftForeArm");
        Assert.True(bodyArm < -40f, "body LeftArm d|X " + bodyArm);
        Assert.True(restArm > 30f, "rest LeftArm d|X " + restArm);
        Assert.True(restFore < -10f, "rest LeftForeArm d|X " + restFore);
        Assert.NotEqual(bodyArm, restArm);
    }

    private static HashSet<string> AnimatedPaths(byte[] data)
    {
        var doc = Fbx.Parse(data);
        var models = new Dictionary<long, string>();
        foreach (var node in doc.Find("Model"))
        {
            models[node.Id] = node.PropString(1);
        }

        var parent = new Dictionary<long, long>();
        var animated = new Dictionary<long, SortedSet<string>>();
        foreach (var conn in doc.Find("C"))
        {
            var kind = conn.PropString(0);
            var child = conn.PropLong(1);
            var par = conn.PropLong(2);
            if (kind == "OO" && models.ContainsKey(child) && models.ContainsKey(par))
            {
                parent[child] = par;
            }

            if (kind == "OP" && models.ContainsKey(par))
            {
                var prop = conn.PropString(3);
                if (prop is "Lcl Rotation" or "Lcl Translation" or "Lcl Scaling")
                {
                    if (!animated.TryGetValue(par, out var props))
                    {
                        props = new SortedSet<string>(StringComparer.Ordinal);
                        animated[par] = props;
                    }

                    props.Add(prop);
                }
            }
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, props) in animated)
        {
            var path = PathOf(id, models, parent);
            foreach (var prop in props)
            {
                paths.Add(path + "|" + prop);
            }
        }

        return paths;
    }

    private static float RotationX(byte[] data, string bone)
    {
        var doc = Fbx.Parse(data);
        long? boneId = null;
        foreach (var node in doc.Find("Model"))
        {
            if (node.PropString(1) == bone)
            {
                boneId = node.Id;
            }
        }

        Assert.True(boneId.HasValue, bone);
        long? curveNode = null;
        foreach (var conn in doc.Find("C"))
        {
            if (conn.PropString(0) == "OP" && conn.PropLong(2) == boneId && conn.PropString(3) == "Lcl Rotation")
            {
                curveNode = conn.PropLong(1);
            }
        }

        Assert.True(curveNode.HasValue, bone + " Lcl Rotation");
        long? curve = null;
        foreach (var conn in doc.Find("C"))
        {
            if (conn.PropString(0) == "OP" && conn.PropLong(2) == curveNode && conn.PropString(3) == "d|X")
            {
                curve = conn.PropLong(1);
            }
        }

        Assert.True(curve.HasValue, bone + " d|X");
        foreach (var node in doc.Find("AnimationCurve"))
        {
            if (node.Id != curve)
            {
                continue;
            }

            var keys = node.Children.Single(c => c.Name == "KeyValueFloat");
            return keys.FirstFloat();
        }

        throw new InvalidOperationException(bone + " curve missing");
    }

    private static string PathOf(long id, Dictionary<long, string> models, Dictionary<long, long> parent)
    {
        var name = models[id];
        return parent.TryGetValue(id, out var par) ? PathOf(par, models, parent) + "/" + name : name;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Assets", "ThemePack", "fantasy_kingdom_a", "pack.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("repo root");
    }

    private sealed class Fbx
    {
        private readonly List<Node> _top = new();

        public static Fbx Parse(byte[] data)
        {
            if (data.Length < 27 || Encoding.ASCII.GetString(data, 0, 20) != "Kaydara FBX Binary  ")
            {
                throw new InvalidOperationException("not an FBX binary");
            }

            var version = BitConverter.ToUInt32(data, 23);
            var doc = new Fbx();
            var pos = 27;
            var wide = version >= 7500;
            while (pos < data.Length - 200)
            {
                if (wide)
                {
                    if (BitConverter.ToUInt64(data, pos) == 0)
                    {
                        break;
                    }
                }
                else if (BitConverter.ToUInt32(data, pos) == 0)
                {
                    break;
                }

                doc._top.Add(Node.Read(data, ref pos, wide));
            }

            return doc;
        }

        public IEnumerable<Node> Find(string name)
        {
            foreach (var node in _top)
            {
                foreach (var hit in node.Find(name))
                {
                    yield return hit;
                }
            }
        }

        public sealed class Node
        {
            public string Name { get; private set; } = "";
            public List<object> Props { get; } = new();
            public List<Node> Children { get; } = new();
            public long Id => PropLong(0);

            public IEnumerable<Node> Find(string name)
            {
                if (Name == name)
                {
                    yield return this;
                }

                foreach (var child in Children)
                {
                    foreach (var hit in child.Find(name))
                    {
                        yield return hit;
                    }
                }
            }

            public string PropString(int index)
            {
                var raw = (byte[])Props[index];
                var end = Array.IndexOf(raw, (byte)0);
                return Encoding.UTF8.GetString(raw, 0, end < 0 ? raw.Length : end);
            }

            public long PropLong(int index) => (long)Props[index];

            public float FirstFloat()
            {
                var values = (float[])Props[0];
                return values[0];
            }

            public static Node Read(byte[] data, ref int pos, bool wide)
            {
                var node = new Node();
                ulong end;
                int count;
                if (wide)
                {
                    end = BitConverter.ToUInt64(data, pos);
                    count = (int)BitConverter.ToUInt64(data, pos + 8);
                    pos += 24;
                }
                else
                {
                    end = BitConverter.ToUInt32(data, pos);
                    count = (int)BitConverter.ToUInt32(data, pos + 4);
                    pos += 12;
                }

                var nameLen = data[pos];
                pos += 1;
                node.Name = Encoding.ASCII.GetString(data, pos, nameLen);
                pos += nameLen;
                for (var i = 0; i < count; i++)
                {
                    node.Props.Add(ReadProp(data, ref pos));
                }

                var sentinel = wide ? 25 : 13;
                if (end != 0)
                {
                    while (pos < (int)end - sentinel)
                    {
                        var isNull = wide
                            ? BitConverter.ToUInt64(data, pos) == 0
                            : BitConverter.ToUInt32(data, pos) == 0;
                        if (isNull)
                        {
                            break;
                        }

                        node.Children.Add(Read(data, ref pos, wide));
                    }

                    pos = (int)end;
                }

                return node;
            }

            private static object ReadProp(byte[] data, ref int pos)
            {
                var type = (char)data[pos];
                pos += 1;
                switch (type)
                {
                    case 'Y':
                        pos += 2;
                        return (short)0;
                    case 'C':
                        pos += 1;
                        return data[pos - 1];
                    case 'I':
                        var i = BitConverter.ToInt32(data, pos);
                        pos += 4;
                        return i;
                    case 'F':
                        pos += 4;
                        return BitConverter.ToSingle(data, pos - 4);
                    case 'D':
                        pos += 8;
                        return BitConverter.ToDouble(data, pos - 8);
                    case 'L':
                        var l = BitConverter.ToInt64(data, pos);
                        pos += 8;
                        return l;
                    case 'S':
                    case 'R':
                        var n = BitConverter.ToInt32(data, pos);
                        pos += 4;
                        var raw = new byte[n];
                        Buffer.BlockCopy(data, pos, raw, 0, n);
                        pos += n;
                        return raw;
                    case 'f':
                    case 'd':
                    case 'i':
                    case 'l':
                    case 'b':
                        var count = BitConverter.ToInt32(data, pos);
                        var enc = BitConverter.ToInt32(data, pos + 4);
                        var clen = BitConverter.ToInt32(data, pos + 8);
                        pos += 12;
                        if (type == 'f' && enc == 0)
                        {
                            var values = new float[count];
                            for (var k = 0; k < count; k++)
                            {
                                values[k] = BitConverter.ToSingle(data, pos + (k * 4));
                            }

                            pos += clen;
                            return values;
                        }

                        pos += clen;
                        return Array.Empty<float>();
                    default:
                        throw new InvalidOperationException("fbx prop " + type);
                }
            }
        }
    }
}
