using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Grove.Unity
{
    /// <summary>Greybox primitives: URP unlit quads + built-in font. No art pipeline required.</summary>
    internal static class GroveVisuals
    {
        private static Mesh? _quad;
        private static Font? _font;
        private static Shader? _unlit;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _font;
            }
        }

        public static Mesh Quad
        {
            get
            {
                if (_quad == null)
                {
                    _quad = new Mesh { name = "GroveQuad" };
                    _quad.vertices =
                    [
                        new Vector3(-0.5f, -0.5f, 0f),
                        new Vector3(0.5f, -0.5f, 0f),
                        new Vector3(0.5f, 0.5f, 0f),
                        new Vector3(-0.5f, 0.5f, 0f)
                    ];
                    _quad.uv = [Vector2.zero, Vector2.right, Vector2.one, Vector2.up];
                    _quad.triangles = [0, 2, 1, 0, 3, 2];
                    _quad.RecalculateNormals();
                    _quad.RecalculateBounds();
                }

                return _quad;
            }
        }

        public static Material MakeMaterial(Color color)
        {
            _unlit ??= Shader.Find("Universal Render Pipeline/Unlit")
                       ?? Shader.Find("Unlit/Color")
                       ?? Shader.Find("Sprites/Default");
            var mat = _unlit != null ? new Material(_unlit) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            return mat;
        }

        public static GameObject QuadObject(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = Quad;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MakeMaterial(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        public static TextMesh Label(string name, Transform parent, Vector3 localPos, string text, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = 0.04f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.richText = false;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                if (Font != null && Font.material != null)
                {
                    meshRenderer.sharedMaterial = Font.material;
                }
            }

            return tm;
        }

        public static Color PieceColor(string id)
        {
            if (id.StartsWith("wildflower"))
            {
                var tier = ParseTier(id);
                return Color.Lerp(new Color(0.95f, 0.55f, 0.75f), new Color(0.75f, 0.15f, 0.45f), (tier - 1) / 7f);
            }

            if (id.StartsWith("herb"))
            {
                return new Color(0.35f, 0.75f, 0.35f);
            }

            if (id.StartsWith("tool"))
            {
                return new Color(0.35f, 0.55f, 0.9f);
            }

            if (id.StartsWith("twig"))
            {
                return new Color(0.62f, 0.42f, 0.22f);
            }

            return id switch
            {
                "pebble" => new Color(0.55f, 0.55f, 0.58f),
                "sprout" => new Color(0.45f, 0.72f, 0.4f),
                "sapling" => new Color(0.35f, 0.6f, 0.32f),
                "tree" => new Color(0.2f, 0.45f, 0.22f),
                "grove" => new Color(0.12f, 0.35f, 0.18f),
                "coin" => new Color(0.95f, 0.8f, 0.25f),
                "puff" => new Color(0.9f, 0.92f, 1f),
                _ => new Color(0.7f, 0.7f, 0.75f)
            };
        }

        public static string ShortLabel(string id, int count)
        {
            var label = id switch
            {
                "pebble" => "Pebble",
                "sprout" => "Sprout",
                "sapling" => "Sapling",
                "tree" => "Tree",
                "grove" => "Grove",
                "coin" => "Coin",
                "puff" => "Puff",
                _ when id.StartsWith("wildflower_t") => "WF T" + ParseTier(id),
                _ when id.StartsWith("herb_t") => "Herb T" + ParseTier(id),
                _ when id.StartsWith("tool_t") => id == "tool_t1" ? "Can" : "Tool T" + ParseTier(id),
                _ when id.StartsWith("twig") => "Twig",
                _ => id
            };
            return count > 1 ? $"{label} ×{count}" : label;
        }

        private static int ParseTier(string id)
        {
            var idx = id.LastIndexOf('t');
            if (idx >= 0 && int.TryParse(id[(idx + 1)..], out var tier))
            {
                return Mathf.Max(1, tier);
            }

            return 1;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static Text UiText(Transform parent, string name, string content, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button UiButton(Transform parent, string name, string label, Color bg, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = bg;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var text = UiText(go.transform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            return button;
        }
    }
}
