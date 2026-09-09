using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    /// Sprite cache. Every texture lives in Assets/Resources/Art and is turned into a
    /// Sprite here so pivots stay under code control (weapons pivot at the grip, muzzle
    /// flashes at the barrel, everything else centred).
    public static class Art
    {
        public const float PPU = 32f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        // name prefix -> pivot in normalised sprite space
        static readonly (string prefix, Vector2 pivot)[] pivots =
        {
            ("wpn_",       new Vector2(0.5f, 0.17f)),
            ("fx_muzzle",  new Vector2(0.5f, 0.12f)),
            ("fx_tracer",  new Vector2(0.5f, 0.5f)),
            ("prop_door",  new Vector2(0.03f, 0.5f)),
            ("fx_cone",    new Vector2(0.5f, 0.0f)),
        };

        /// Drops every cached Sprite. Sprites made with Sprite.Create are destroyed when
        /// play mode ends, so with "Reload Domain" disabled the cache would otherwise hand
        /// out destroyed objects on the next run.
        public static void ResetCache()
        {
            cache.Clear();
            texCache.Clear();
        }

        public static Texture2D Tex(string name)
        {
            if (texCache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Art/" + name);
            if (t == null)
            {
                // Do not cache the miss: the asset may still be importing.
                Debug.LogWarning("[Art] missing texture Art/" + name);
                return null;
            }
            t.filterMode = FilterMode.Point;
            texCache[name] = t;
            return t;
        }

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var tex = Tex(name);
            if (tex == null) return null;

            Vector2 pivot = new Vector2(0.5f, 0.5f);
            foreach (var p in pivots)
                if (name.StartsWith(p.prefix)) { pivot = p.pivot; break; }

            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PPU,
                              0, SpriteMeshType.FullRect);
            s.name = name;
            cache[name] = s;
            return s;
        }

        /// Sub-rect sprite, used by the bitmap font atlas.
        public static Sprite Sub(string name, Rect rect, Vector2 pivot)
        {
            string key = name + rect;
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = Tex(name);
            if (tex == null) return null;
            s = Sprite.Create(tex, rect, pivot, PPU, 0, SpriteMeshType.FullRect);
            cache[key] = s;
            return s;
        }

        public static SpriteRenderer NewSprite(string parentName, string spriteName, int order,
                                               Transform parent = null)
        {
            var go = new GameObject(parentName);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (spriteName != null) sr.sprite = Get(spriteName);
            sr.sortingOrder = order;
            return sr;
        }
    }

    /// Render order bands. Everything in the game uses these instead of raw ints.
    public static class Layer
    {
        public const int Floor = 0;
        public const int FloorDetail = 5;
        public const int Decal = 10;
        public const int Corpse = 20;
        public const int Debris = 25;
        public const int Pickup = 30;
        public const int Prop = 40;
        public const int Wall = 45;
        public const int Character = 50;
        public const int Held = 55;
        public const int Shot = 60;
        public const int Muzzle = 65;
        public const int Overlay = 90;
        public const int Ui = 100;

        // physics layers, mirrored from ProjectSettings/TagManager.asset
        public const int PlayerL = 6;
        public const int EnemyL = 7;
        public const int WallL = 8;
        public const int PlayerShotL = 9;
        public const int EnemyShotL = 10;
        public const int PickupL = 11;
        public const int CorpseL = 12;
        public const int PropL = 13;

        public static readonly int WallMask = 1 << WallL;
        public static readonly int EnemyMask = 1 << EnemyL;
        public static readonly int PlayerMask = 1 << PlayerL;
        public static readonly int SightBlockMask = 1 << WallL;
    }
}
