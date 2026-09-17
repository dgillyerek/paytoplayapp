using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;

namespace Survival.Unity
{
    /// <summary>
    /// Paper-doll from ONE locked rear master: torso + legs + sword crops.
    /// Rest pose composites back to the painted sprite; walk/attack are bone deltas.
    /// </summary>
    public sealed class SirAldricView : MonoBehaviour
    {
        public const float CharacterHeight = 760f;

        private RectTransform? _root;
        private RectTransform? _torso;
        private RectTransform? _legL;
        private RectTransform? _legR;
        private RectTransform? _sword;
        private Vector2 _torsoRest;
        private Vector2 _legLRest;
        private Vector2 _legRRest;
        private Vector2 _swordRest;
        private float _height = CharacterHeight;

        public bool Built => _root != null;

        public void Build(RectTransform parent, Sprite? master)
        {
            if (master == null || master.texture == null)
            {
                var missing = SurvivalVisuals.Text(
                    parent,
                    "AldricMissing",
                    "SIR ALDRIC rear master missing",
                    28,
                    TextAnchor.MiddleCenter,
                    SurvivalVisuals.Cream);
                SurvivalVisuals.Stretch(missing.rectTransform);
                return;
            }

            var tex = master.texture;
            if (!TryOpaqueBounds(tex, out var bx, out var by, out var bw, out var bh))
            {
                bx = 0;
                by = 0;
                bw = tex.width;
                bh = tex.height;
            }

            var body = CopyTexture(tex);
            Punch(body, bx, by, bw, bh, SirAldricMotion.Layout.Sword);
            body.Apply();

            _root = MakeNode(parent, "SirAldric");
            _root.anchorMin = new Vector2(0.5f, 0.22f);
            _root.anchorMax = new Vector2(0.5f, 0.22f);
            _root.pivot = new Vector2(0.5f, 0.08f);
            _root.sizeDelta = new Vector2(_height * (bw / (float)bh), _height);

            var rootW = _root.sizeDelta.x;
            var rootH = _root.sizeDelta.y;
            _torso = MakePart(_root, "Torso", body, bx, by, bw, bh, SirAldricMotion.Layout.Torso, new Vector2(0.5f, 0.18f), rootW, rootH, out _torsoRest);
            _legL = MakePart(_root, "LegL", body, bx, by, bw, bh, SirAldricMotion.Layout.LegL, new Vector2(0.5f, 0.90f), rootW, rootH, out _legLRest);
            _legR = MakePart(_root, "LegR", body, bx, by, bw, bh, SirAldricMotion.Layout.LegR, new Vector2(0.5f, 0.90f), rootW, rootH, out _legRRest);
            _sword = MakePart(_root, "Sword", tex, bx, by, bw, bh, SirAldricMotion.Layout.Sword, new Vector2(0.30f, 0.86f), rootW, rootH, out _swordRest);
            _legL.SetSiblingIndex(0);
            _legR.SetSiblingIndex(1);
            _torso.SetAsLastSibling();
            _sword.SetAsLastSibling();
        }

        public void Apply(SirAldricMotion.Pose pose)
        {
            if (_root == null)
            {
                return;
            }

            _root.anchorMin = new Vector2(0.5f, pose.MarchY);
            _root.anchorMax = new Vector2(0.5f, pose.MarchY);
            _root.anchoredPosition = new Vector2(pose.Root.X * _height, pose.Root.Y * _height);
            _root.localEulerAngles = new Vector3(0f, 0f, pose.Root.RotZ);
            _root.localScale = new Vector3(pose.Root.ScaleX, pose.Root.ScaleY, 1f);
            ApplyBone(_torso, _torsoRest, pose.Torso);
            ApplyBone(_legL, _legLRest, pose.LegL);
            ApplyBone(_legR, _legRRest, pose.LegR);
            ApplyBone(_sword, _swordRest, pose.Sword);
        }

        private void ApplyBone(RectTransform? part, Vector2 rest, SirAldricMotion.Bone bone)
        {
            if (part == null)
            {
                return;
            }

            part.anchoredPosition = rest + new Vector2(bone.X, bone.Y) * _height;
            part.localEulerAngles = new Vector3(0f, 0f, bone.RotZ);
            part.localScale = new Vector3(bone.ScaleX, bone.ScaleY, 1f);
        }

        private static RectTransform MakePart(
            RectTransform parent,
            string name,
            Texture2D src,
            int bx,
            int by,
            int bw,
            int bh,
            SirAldricMotion.NRect uv,
            Vector2 pivot,
            float parentW,
            float parentH,
            out Vector2 rest)
        {
            var sprite = Crop(src, bx, by, bw, bh, uv);
            var img = SurvivalVisuals.Image(parent, name, Color.white, sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.pivot = pivot;
            var w = uv.Width * parentW;
            var h = uv.Height * parentH;
            rt.sizeDelta = new Vector2(w, h);
            var cx = (uv.XMin + uv.Width * pivot.x - 0.5f) * parentW;
            var cy = (uv.YMin + uv.Height * pivot.y - 0.5f) * parentH;
            rest = new Vector2(cx, cy);
            rt.anchoredPosition = rest;
            return rt;
        }

        private static RectTransform MakeNode(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Sprite? LoadMasterPng(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    continue;
                }

                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes, markNonReadable: false))
                {
                    Object.Destroy(tex);
                    continue;
                }

                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.alphaIsTransparency = true;
                tex.name = Path.GetFileNameWithoutExtension(path);
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }

            return null;
        }

        private static Texture2D CopyTexture(Texture2D src)
        {
            var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            dst.filterMode = FilterMode.Bilinear;
            dst.wrapMode = TextureWrapMode.Clamp;
            dst.alphaIsTransparency = true;
            dst.SetPixels(src.GetPixels());
            return dst;
        }

        private static void Punch(Texture2D tex, int bx, int by, int bw, int bh, SirAldricMotion.NRect uv)
        {
            var x0 = Mathf.Clamp(bx + Mathf.FloorToInt(uv.XMin * bw) - 2, 0, tex.width - 1);
            var y0 = Mathf.Clamp(by + Mathf.FloorToInt(uv.YMin * bh) - 2, 0, tex.height - 1);
            var x1 = Mathf.Clamp(bx + Mathf.CeilToInt(uv.XMax * bw) + 2, 0, tex.width);
            var y1 = Mathf.Clamp(by + Mathf.CeilToInt(uv.YMax * bh) + 2, 0, tex.height);
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    var c = tex.GetPixel(x, y);
                    if (!IsScabbardPixel(c))
                    {
                        continue;
                    }

                    c.a = 0f;
                    tex.SetPixel(x, y, c);
                }
            }
        }

        private static bool IsScabbardPixel(Color c)
        {
            if (c.a < 0.08f)
            {
                return false;
            }

            Color.RGBToHSV(c, out var h, out var s, out var v);
            var leather = s > 0.22f && v > 0.10f && v < 0.62f && h > 0.02f && h < 0.13f;
            var gold = s > 0.32f && v > 0.42f && h > 0.07f && h < 0.17f;
            return leather || gold;
        }

        private static Sprite Crop(Texture2D src, int bx, int by, int bw, int bh, SirAldricMotion.NRect uv)
        {
            var x = Mathf.Clamp(bx + Mathf.FloorToInt(uv.XMin * bw), 0, src.width - 1);
            var y = Mathf.Clamp(by + Mathf.FloorToInt(uv.YMin * bh), 0, src.height - 1);
            var w = Mathf.Clamp(Mathf.CeilToInt(uv.Width * bw), 1, src.width - x);
            var h = Mathf.Clamp(Mathf.CeilToInt(uv.Height * bh), 1, src.height - y);
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.filterMode = FilterMode.Bilinear;
            dst.wrapMode = TextureWrapMode.Clamp;
            dst.alphaIsTransparency = true;
            dst.SetPixels(src.GetPixels(x, y, w, h));
            dst.Apply();
            return Sprite.Create(dst, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static bool TryOpaqueBounds(Texture2D tex, out int x, out int y, out int w, out int h)
        {
            var pixels = tex.GetPixels();
            var tw = tex.width;
            var th = tex.height;
            var minX = tw;
            var minY = th;
            var maxX = 0;
            var maxY = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 0.08f)
                {
                    continue;
                }

                var px = i % tw;
                var py = i / tw;
                if (px < minX) minX = px;
                if (py < minY) minY = py;
                if (px > maxX) maxX = px;
                if (py > maxY) maxY = py;
            }

            if (maxX < minX)
            {
                x = y = w = h = 0;
                return false;
            }

            x = minX;
            y = minY;
            w = maxX - minX + 1;
            h = maxY - minY + 1;
            return true;
        }
    }
}
