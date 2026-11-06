using Naval.Core;
using UnityEngine;

namespace Naval.Slice
{
    /// <summary>
    /// The placeholder map: a port marker and name at every port, and a line for every route.
    /// Owned routes are bright and thick, the rest faint and thin. Map units are world units times <see cref="scale"/>.
    /// </summary>
    public sealed class SliceMap : MonoBehaviour
    {
        // ports.csv is validated to the same range (map_x 0 to 100, map_y 0 to 60).
        const float MapWidth = 100f;
        const float MapHeight = 60f;
        const float Margin = 6f;

        static readonly Color SeaColor = new Color(0.08f, 0.2f, 0.32f);
        static readonly Color SeaPanelColor = new Color(0.11f, 0.27f, 0.42f);
        static readonly Color PortColor = new Color(0.93f, 0.9f, 0.82f);
        static readonly Color ShipyardColor = new Color(1f, 0.74f, 0.2f);
        static readonly Color CoastOwned = new Color(1f, 0.93f, 0.62f, 1f);
        static readonly Color OceanOwned = new Color(0.55f, 0.9f, 1f, 1f);
        static readonly Color CoastIdle = new Color(1f, 0.93f, 0.62f, 0.22f);
        static readonly Color OceanIdle = new Color(0.55f, 0.9f, 1f, 0.22f);

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] Camera sceneCamera;
        [SerializeField] float scale = 1f;
        [SerializeField] float ownedWidth = 0.4f;
        [SerializeField] float idleWidth = 0.12f;
        /// <summary>Height of a port name in world units.</summary>
        [SerializeField] float labelSize = 1.7f;

        LineRenderer[] lines;
        Material lineMaterial;
        int ownedCount = -1;
        float lastAspect;

        public float Scale { get { return scale; } }

        /// <summary>Where a port sits in the world.</summary>
        public Vector3 PortPosition(Port port)
        {
            return new Vector3((float)port.MapX * scale, (float)port.MapY * scale, 0f);
        }

        void Start()
        {
            if (bootstrap == null || sceneCamera == null)
            {
                Debug.LogError("SliceMap: the bootstrap and sceneCamera fields must be assigned.");
                enabled = false;
                return;
            }
            FitCamera();
            GameState state = bootstrap.State;
            if (state == null) return;
            BuildSea();
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            BuildRoutes(state.Data);
            foreach (Port port in state.Data.Ports) BuildPort(port);
            RefreshRoutes(state);
        }

        void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
        }

        void LateUpdate()
        {
            if (sceneCamera != null && !Mathf.Approximately(lastAspect, sceneCamera.aspect)) FitCamera();
            GameState state = bootstrap.State;
            if (state == null || lines == null) return;
            if (state.Routes.Owned.Count != ownedCount) RefreshRoutes(state);
        }

        void FitCamera()
        {
            lastAspect = sceneCamera.aspect;
            sceneCamera.orthographic = true;
            sceneCamera.backgroundColor = SeaColor;
            float halfHeight = (MapHeight / 2f + Margin) * scale;
            float halfWidth = (MapWidth / 2f + Margin) * scale;
            sceneCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, lastAspect));
            sceneCamera.transform.position = new Vector3(MapWidth / 2f * scale, MapHeight / 2f * scale, -10f);
        }

        void BuildSea()
        {
            var sea = new GameObject("Sea");
            sea.transform.SetParent(transform, false);
            sea.transform.position = new Vector3(MapWidth / 2f * scale, MapHeight / 2f * scale, 0f);
            sea.transform.localScale = new Vector3(MapWidth * scale, MapHeight * scale, 1f);
            SpriteRenderer sr = sea.AddComponent<SpriteRenderer>();
            sr.sprite = SliceSprites.Square();
            sr.color = SeaPanelColor;
            sr.sortingOrder = -10;
        }

        void BuildRoutes(GameData data)
        {
            lines = new LineRenderer[data.Routes.Count];
            foreach (Route route in data.Routes)
            {
                var go = new GameObject("Route " + route.A.Name + " - " + route.B.Name);
                go.transform.SetParent(transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = lineMaterial;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.SetPosition(0, PortPosition(route.A));
                line.SetPosition(1, PortPosition(route.B));
                line.numCapVertices = 4;
                line.sortingOrder = 0;
                lines[route.Index] = line;
            }
        }

        void RefreshRoutes(GameState state)
        {
            ownedCount = state.Routes.Owned.Count;
            foreach (Route route in state.Data.Routes)
            {
                bool owned = state.Routes.Owns(route);
                Color color = route.Ocean ? (owned ? OceanOwned : OceanIdle) : (owned ? CoastOwned : CoastIdle);
                LineRenderer line = lines[route.Index];
                float width = (owned ? ownedWidth : idleWidth) * scale;
                line.startWidth = width;
                line.endWidth = width;
                line.startColor = color;
                line.endColor = color;
                line.sortingOrder = owned ? 1 : 0;
            }
        }

        void BuildPort(Port port)
        {
            var go = new GameObject("Port " + port.Name);
            go.transform.SetParent(transform, false);
            go.transform.position = PortPosition(port);
            float size = (port.HasShipyard ? 2.4f : 1.5f) * scale;
            go.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SliceSprites.Circle();
            sr.color = port.HasShipyard ? ShipyardColor : PortColor;
            sr.sortingOrder = 10;

            var label = new GameObject("Label");
            label.transform.SetParent(transform, false);
            label.transform.position = PortPosition(port) + new Vector3(0f, (size / 2f + 0.3f * scale), 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            const int fontSize = 64;
            text.font = font;
            text.text = port.Name;
            text.fontSize = fontSize;
            text.characterSize = labelSize * scale / (fontSize * 0.1f);   // about labelSize units tall
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            MeshRenderer mr = label.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 30;
        }
    }

    /// <summary>Generates the placeholder sprites. Each is one world unit across at scale 1.</summary>
    public static class SliceSprites
    {
        const int Size = 64;
        static Sprite circle;
        static Sprite triangle;
        static Sprite square;

        public static Sprite Circle()
        {
            if (circle == null) circle = Make((x, y) =>
            {
                float dx = x - 0.5f, dy = y - 0.5f;
                return Mathf.Clamp01((0.5f - Mathf.Sqrt(dx * dx + dy * dy)) * Size / 1.5f);
            });
            return circle;
        }

        /// <summary>A pointed ship shape facing up (+Y).</summary>
        public static Sprite Triangle()
        {
            if (triangle == null) triangle = Make((x, y) =>
            {
                // Narrows from a 0.8 wide base at y = 0.05 to a point at y = 0.95.
                float t = (y - 0.05f) / 0.9f;
                if (t < 0f || t > 1f) return 0f;
                float half = 0.4f * (1f - t);
                return Mathf.Clamp01((half - Mathf.Abs(x - 0.5f)) * Size / 1.5f + 0.5f);
            });
            return triangle;
        }

        public static Sprite Square()
        {
            if (square == null) square = Make((x, y) => 1f);
            return square;
        }

        static Sprite Make(System.Func<float, float, float> alphaAt)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float a = alphaAt((x + 0.5f) / Size, (y + 0.5f) / Size);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
