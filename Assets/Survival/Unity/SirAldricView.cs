using System;
using System.Collections.Generic;
using System.IO;
using Survival.Domain.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.Unity
{
    /// <summary>
    /// One locked rear master as a single skinned UV mesh. Vertices bend; the silhouette stays connected.
    /// </summary>
    public sealed class SirAldricView : MaskableGraphic
    {
        public const float CharacterHeight = 760f;

        private Texture? _tex;
        private SirAldricMotion.Pose _pose;
        private int _bx;
        private int _by;
        private int _bw;
        private int _bh;
        private int _tw = 1;
        private int _th = 1;
        private float _height = CharacterHeight;

        public bool Built => _tex != null;

        public override Texture mainTexture => _tex != null ? _tex : s_WhiteTexture;

        public void Build(RectTransform parent, Sprite? master)
        {
            raycastTarget = false;
            color = Color.white;
            transform.SetParent(parent, false);

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
                _tex = null;
                return;
            }

            var tex = master.texture;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            _tex = tex;
            _tw = tex.width;
            _th = tex.height;

            var tr = master.textureRect;
            if (!TryOpaqueBounds(tex, (int)tr.x, (int)tr.y, (int)tr.width, (int)tr.height, out _bx, out _by, out _bw, out _bh))
            {
                _bx = (int)tr.x;
                _by = (int)tr.y;
                _bw = Mathf.Max(1, (int)tr.width);
                _bh = Mathf.Max(1, (int)tr.height);
            }

            var rt = rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.22f);
            rt.anchorMax = new Vector2(0.5f, 0.22f);
            rt.pivot = new Vector2(0.5f, 0.08f);
            rt.sizeDelta = new Vector2(_height * (_bw / (float)_bh), _height);
            SetMaterialDirty();
            SetVerticesDirty();
        }

        public void Apply(SirAldricMotion.Pose pose)
        {
            if (_tex == null)
            {
                return;
            }

            _pose = pose;
            var rt = rectTransform;
            rt.anchorMin = new Vector2(0.5f, pose.MarchY);
            rt.anchorMax = new Vector2(0.5f, pose.MarchY);
            rt.anchoredPosition = new Vector2(pose.Root.X * _height, pose.Root.Y * _height);
            rt.localEulerAngles = new Vector3(0f, 0f, pose.Root.RotZ);
            rt.localScale = new Vector3(pose.Root.ScaleX, pose.Root.ScaleY, 1f);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_tex == null || _bw < 1 || _bh < 1)
            {
                return;
            }

            var r = rectTransform.rect;
            var cols = SirAldricWarp.GridCols;
            var rows = SirAldricWarp.GridRows;
            var color32 = (Color32)color;
            for (var j = 0; j <= rows; j++)
            {
                var v = j / (float)rows;
                for (var i = 0; i <= cols; i++)
                {
                    var u = i / (float)cols;
                    SirAldricWarp.Displace(u, v, _pose, out var nu, out var nv);
                    var px = r.xMin + nu * r.width;
                    var py = r.yMin + nv * r.height;
                    var tu = (_bx + u * _bw) / _tw;
                    var tv = (_by + v * _bh) / _th;
                    vh.AddVert(new Vector3(px, py, 0f), color32, new Vector2(tu, tv));
                }
            }

            var stride = cols + 1;
            var tris = new List<(int a, int b, int c, float score)>();
            for (var j = 0; j < rows; j++)
            {
                for (var i = 0; i < cols; i++)
                {
                    var i0 = j * stride + i;
                    var i1 = i0 + 1;
                    var i2 = i0 + stride;
                    var i3 = i2 + 1;
                    var s0 = Score(i0);
                    var s1 = Score(i1);
                    var s2 = Score(i2);
                    var s3 = Score(i3);
                    tris.Add((i0, i2, i3, Math.Max(s0, Math.Max(s2, s3))));
                    tris.Add((i0, i3, i1, Math.Max(s0, Math.Max(s3, s1))));
                }
            }

            tris.Sort((p, q) => p.score.CompareTo(q.score));
            for (var t = 0; t < tris.Count; t++)
            {
                vh.AddTriangle(tris[t].a, tris[t].b, tris[t].c);
            }

            float Score(int idx)
            {
                var u = (idx % stride) / (float)cols;
                var v = (idx / stride) / (float)rows;
                SirAldricWarp.Displace(u, v, _pose, out var nu, out var nv);
                var du = nu - u;
                var dv = nv - v;
                return du * du + dv * dv;
            }
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

        private static bool TryOpaqueBounds(
            Texture2D tex,
            int x0,
            int y0,
            int rw,
            int rh,
            out int x,
            out int y,
            out int w,
            out int h)
        {
            var pixels = tex.GetPixels();
            var tw = tex.width;
            var th = tex.height;
            var minX = tw;
            var minY = th;
            var maxX = 0;
            var maxY = 0;
            var x1 = Mathf.Min(tw, x0 + rw);
            var y1 = Mathf.Min(th, y0 + rh);
            for (var py = Mathf.Max(0, y0); py < y1; py++)
            {
                var row = py * tw;
                for (var px = Mathf.Max(0, x0); px < x1; px++)
                {
                    if (pixels[row + px].a < 0.08f)
                    {
                        continue;
                    }

                    if (px < minX) minX = px;
                    if (py < minY) minY = py;
                    if (px > maxX) maxX = px;
                    if (py > maxY) maxY = py;
                }
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
