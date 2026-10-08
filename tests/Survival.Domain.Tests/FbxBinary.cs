using System.IO.Compression;
using System.Text;

namespace Survival.Domain.Tests;

/// <summary>
/// Minimal binary FBX 7.x reader for tests: bone (LimbNode) names in file order, parents by name,
/// and animation curve key ranges per bone. Only KeyValueFloat arrays are decompressed.
/// </summary>
internal sealed class FbxRig
{
    public List<string> Limbs { get; } = new();
    public Dictionary<string, string> Parent { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<(string Channel, string Axis, int Keys, double Range)>> Curves { get; } = new(StringComparer.Ordinal);
    public List<string> Stacks { get; } = new();

    public double MaxRange(string bone, string? channel = null) =>
        Curves.TryGetValue(bone, out var list)
            ? list.Where(c => channel == null || c.Channel == channel).Select(c => c.Range).DefaultIfEmpty(0).Max()
            : 0;

    public static FbxRig Read(string path)
    {
        var data = File.ReadAllBytes(path);
        if (Encoding.ASCII.GetString(data, 0, 20) != "Kaydara FBX Binary  ")
        {
            throw new InvalidDataException("Not a binary FBX: " + path);
        }

        var version = BitConverter.ToUInt32(data, 23);
        var reader = new Reader(data, version >= 7500);
        var top = new List<Node>();
        var offset = 27;
        while (offset < data.Length - 160)
        {
            var node = reader.ReadNode(ref offset);
            if (node == null)
            {
                break;
            }

            top.Add(node);
        }

        var objects = top.First(n => n.Name == "Objects").Children;
        var connections = top.First(n => n.Name == "Connections").Children;
        var models = new Dictionary<long, (string Name, string Kind)>();
        var curveNodes = new Dictionary<long, string>();
        var curves = new Dictionary<long, float[]>();
        var rig = new FbxRig();
        foreach (var n in objects)
        {
            switch (n.Name)
            {
                case "Model":
                    var name = ((string)n.Props[1]).Split('\0')[0];
                    var kind = (string)n.Props[2];
                    models[(long)n.Props[0]] = (name, kind);
                    if (kind == "LimbNode")
                    {
                        rig.Limbs.Add(name);
                    }

                    break;
                case "AnimationCurveNode":
                    curveNodes[(long)n.Props[0]] = ((string)n.Props[1]).Split('\0')[0];
                    break;
                case "AnimationCurve":
                    var keys = n.Children.FirstOrDefault(c => c.Name == "KeyValueFloat");
                    curves[(long)n.Props[0]] = keys?.Props.Count > 0 ? (float[])keys.Props[0] : Array.Empty<float>();
                    break;
                case "AnimationStack":
                    rig.Stacks.Add(((string)n.Props[1]).Split('\0')[0]);
                    break;
            }
        }

        var nodeToModel = new Dictionary<long, (string Model, string Channel)>();
        var curveToNode = new Dictionary<long, (long Node, string Axis)>();
        foreach (var c in connections)
        {
            var type = (string)c.Props[0];
            var child = (long)c.Props[1];
            var parent = (long)c.Props[2];
            if (type == "OO" && models.TryGetValue(child, out var cm) && cm.Kind == "LimbNode" && models.TryGetValue(parent, out var pm))
            {
                rig.Parent[cm.Name] = pm.Name;
            }

            if (type == "OP")
            {
                var prop = (string)c.Props[3];
                if (curveNodes.ContainsKey(child) && models.TryGetValue(parent, out var am))
                {
                    nodeToModel[child] = (am.Name, prop);
                }

                if (curves.ContainsKey(child) && curveNodes.ContainsKey(parent))
                {
                    curveToNode[child] = (parent, prop);
                }
            }
        }

        foreach (var (curveId, (nodeId, axis)) in curveToNode)
        {
            if (!nodeToModel.TryGetValue(nodeId, out var target))
            {
                continue;
            }

            var keys = curves[curveId];
            var range = keys.Length == 0 ? 0 : keys.Max() - (double)keys.Min();
            if (!rig.Curves.TryGetValue(target.Model, out var list))
            {
                rig.Curves[target.Model] = list = new();
            }

            list.Add((target.Channel, axis, keys.Length, range));
        }

        return rig;
    }

    private sealed class Node
    {
        public string Name = "";
        public List<object> Props = new();
        public List<Node> Children = new();
    }

    private sealed class Reader
    {
        private readonly byte[] _d;
        private readonly bool _big;

        public Reader(byte[] data, bool big)
        {
            _d = data;
            _big = big;
        }

        public Node? ReadNode(ref int o)
        {
            long end;
            long count;
            if (_big)
            {
                end = BitConverter.ToInt64(_d, o);
                count = BitConverter.ToInt64(_d, o + 8);
                o += 24;
            }
            else
            {
                end = BitConverter.ToUInt32(_d, o);
                count = BitConverter.ToUInt32(_d, o + 4);
                o += 12;
            }

            int nameLen = _d[o];
            o += 1;
            if (end == 0)
            {
                return null;
            }

            var node = new Node { Name = Encoding.ASCII.GetString(_d, o, nameLen) };
            o += nameLen;
            var decode = node.Name == "KeyValueFloat";
            for (var i = 0; i < count; i++)
            {
                node.Props.Add(ReadProp(ref o, decode));
            }

            while (o < end)
            {
                var child = ReadNode(ref o);
                if (child == null)
                {
                    break;
                }

                node.Children.Add(child);
            }

            o = (int)end;
            return node;
        }

        private object ReadProp(ref int o, bool decode)
        {
            var t = (char)_d[o];
            o += 1;
            switch (t)
            {
                case 'Y': o += 2; return BitConverter.ToInt16(_d, o - 2);
                case 'C': o += 1; return _d[o - 1] != 0;
                case 'I': o += 4; return BitConverter.ToInt32(_d, o - 4);
                case 'F': o += 4; return BitConverter.ToSingle(_d, o - 4);
                case 'D': o += 8; return BitConverter.ToDouble(_d, o - 8);
                case 'L': o += 8; return BitConverter.ToInt64(_d, o - 8);
                case 'S':
                case 'R':
                {
                    var len = BitConverter.ToInt32(_d, o);
                    o += 4;
                    var s = t == 'S' ? Encoding.UTF8.GetString(_d, o, len) : (object)len;
                    o += len;
                    return s;
                }

                case 'f':
                case 'd':
                case 'l':
                case 'i':
                case 'b':
                {
                    var n = BitConverter.ToInt32(_d, o);
                    var enc = BitConverter.ToInt32(_d, o + 4);
                    var clen = BitConverter.ToInt32(_d, o + 8);
                    o += 12;
                    var start = o;
                    o += clen;
                    if (!decode || t != 'f')
                    {
                        return n;
                    }

                    byte[] raw;
                    if (enc == 1)
                    {
                        using var z = new ZLibStream(new MemoryStream(_d, start, clen), CompressionMode.Decompress);
                        using var ms = new MemoryStream();
                        z.CopyTo(ms);
                        raw = ms.ToArray();
                    }
                    else
                    {
                        raw = new byte[clen];
                        Array.Copy(_d, start, raw, 0, clen);
                    }

                    var values = new float[n];
                    Buffer.BlockCopy(raw, 0, values, 0, n * 4);
                    return values;
                }

                default:
                    throw new InvalidDataException("FBX property type " + t);
            }
        }
    }
}
