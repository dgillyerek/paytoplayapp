using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Grove.Unity
{
    /// <summary>
    /// Temp sprites + greybox UI. Generated at runtime so Play Mode works before any art import.
    /// Drop replacement PNGs at Resources/Grove/Art/{ui_white,cell,piece} later; code tints them.
    /// </summary>
    internal static class GroveVisuals
    {
        private static Font? _font;
        private static Shader? _unlit;
        private static Sprite? _white;
        private static Sprite? _cell;
        private static Sprite? _piece;

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

        public static Sprite WhiteSprite => _white ??= LoadOrMake("Grove/Art/ui_white", MakeWhiteTex, 4f);

        public static Sprite CellSprite => _cell ??= LoadOrMake("Grove/Art/cell", () => MakeRectTex(64, 6), 64f);

        public static Sprite PieceSprite => _piece ??= LoadOrMake("Grove/Art/piece", () => MakeCircleTex(64), 64f);

        public static Material MakeMaterial(Color color, Texture? texture = null)
        {
            _unlit ??= Shader.Find("Universal Render Pipeline/Unlit")
                       ?? Shader.Find("Sprites/Default")
                       ?? Shader.Find("Unlit/Color");
            var mat = _unlit != null ? new Material(_unlit) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            ApplyColor(mat, color);
            if (texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", texture);
                }
            }

            return mat;
        }

        public static void ApplyColor(Material mat, Color color)
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        public static GameObject SpriteObject(
            string name,
            Transform parent,
            Vector3 worldPos,
            float worldSize,
            Color color,
            Sprite sprite,
            int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = MakeMaterial(color, sprite.texture);
            var size = sprite.bounds.size;
            if (size.x > 0.0001f && size.y > 0.0001f)
            {
                go.transform.localScale = new Vector3(worldSize / size.x, worldSize / size.y, 1f);
            }

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
                meshRenderer.sortingOrder = 20;
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
            if (idx >= 0 && idx + 1 < id.Length && int.TryParse(id.Substring(idx + 1), out var tier))
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

        public static void EnsurePlayCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.orthographic = true;
            if (cam.orthographicSize < 5.2f)
            {
                cam.orthographicSize = 5.5f;
            }

            var urpType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpType != null && cam.GetComponent(urpType) == null)
            {
                cam.gameObject.AddComponent(urpType);
            }
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

        public static Image UiImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.type = Image.Type.Simple;
            image.color = color;
            return image;
        }

        public static Button UiButton(Transform parent, string name, string label, Color bg, Vector2 size)
        {
            var image = UiImage(parent, name, bg);
            var rect = image.rectTransform;
            rect.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
            button.colors = colors;
            var text = UiText(image.transform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            return button;
        }

        private static Sprite LoadOrMake(string resourcesPath, System.Func<Texture2D> factory, float pixelsPerUnit)
        {
            var tex = Resources.Load<Texture2D>(resourcesPath) ?? factory();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        private static Texture2D MakeWhiteTex()
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var fill = EnumerablePixels(8, 8, (_, _) => Color.white);
            tex.SetPixels(fill);
            tex.Apply();
            tex.name = "grove_ui_white";
            return tex;
        }

        private static Texture2D MakeRectTex(int size, int inset)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = EnumerablePixels(size, size, (x, y) =>
            {
                var edge = x < inset || y < inset || x >= size - inset || y >= size - inset;
                return edge ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            });
            tex.SetPixels(fill);
            tex.Apply();
            tex.name = "grove_cell";
            return tex;
        }

        private static Texture2D MakeCircleTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var r = (size - 2) * 0.5f;
            var cx = (size - 1) * 0.5f;
            var fill = EnumerablePixels(size, size, (x, y) =>
            {
                var dx = x - cx;
                var dy = y - cx;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r)
                {
                    return Color.clear;
                }

                var rim = d > r - 3f;
                return rim ? new Color(1f, 1f, 1f, 0.85f) : Color.white;
            });
            tex.SetPixels(fill);
            tex.Apply();
            tex.name = "grove_piece";
            return tex;
        }

        private static Color[] EnumerablePixels(int w, int h, System.Func<int, int, Color> at)
        {
            var colors = new Color[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    colors[y * w + x] = at(x, y);
                }
            }

            return colors;
        }
    }
}
