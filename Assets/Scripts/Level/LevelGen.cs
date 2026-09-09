using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    /// Builds a floor from a character grid, and can produce that grid procedurally.
    ///   '#' wall   '.' floor   ' ' void   'P' player   'X' exit   'd' door
    ///   '1'..'8'   enemy slots   'w' melee  'W' gun  'R' explosive  'a' ammo  'h' medkit
    ///   'T' table  'S' sofa  'V' tv  'L' plant  'C' crate  'B' barrel  'E' bed  'K' counter
    public class LevelGen : MonoBehaviour
    {
        public static LevelGen I;

        public Vector2 PlayerStart { get; private set; }
        public Vector2 ExitPos { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        Transform root;
        SpriteRenderer exitSr;
        readonly List<Vector2> openCells = new List<Vector2>();

        static readonly string[] floorTiles = { "tile_check", "tile_wood", "tile_carpet", "tile_concrete", "tile_neon" };
        static readonly string[] wallTiles = { "wall_brick", "wall_dark", "wall_stripe" };

        public static void Create(Transform parent)
        {
            var go = new GameObject("Level");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<LevelGen>();
        }

        public void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            exitSr = null;
            openCells.Clear();
            foreach (var e in new List<Enemy>(Enemy.All)) if (e != null) Destroy(e.gameObject);
            Enemy.All.Clear();
            foreach (var p in new List<Pickup>(Pickup.All)) if (p != null) Destroy(p.gameObject);
        }

        // ==================================================================== building
        public void Build(char[,] grid, int floorNumber)
        {
            Clear();
            root = new GameObject("Floor" + floorNumber).transform;
            root.SetParent(transform, false);

            Width = grid.GetLength(0);
            Height = grid.GetLength(1);

            var rng = new System.Random(floorNumber * 7919 + 13);
            string floorTile = floorTiles[rng.Next(floorTiles.Length)];
            string altTile = floorTiles[rng.Next(floorTiles.Length)];
            string wallTile = wallTiles[rng.Next(wallTiles.Length)];
            Color floorTint = Palette(floorNumber) * 1.14f;
            floorTint.a = 1f;
            Color wallTint = Palette(floorNumber) * 0.92f;
            wallTint.a = 1f;

            var floorParent = new GameObject("Tiles").transform;
            floorParent.SetParent(root, false);
            var wallParent = new GameObject("Walls").transform;
            wallParent.SetParent(root, false);
            wallParent.gameObject.layer = Layer.WallL;

            var enemySlots = new List<(char c, Vector2 pos)>();
            var pickupSlots = new List<(char c, Vector2 pos)>();
            var propSlots = new List<(char c, Vector2 pos)>();
            var doorSlots = new List<Vector2>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char ch = grid[x, y];
                    if (ch == ' ') continue;
                    Vector2 p = new Vector2(x, y);

                    if (ch == '#')
                    {
                        var w = Art.NewSprite("w", wallTile, Layer.Wall, wallParent);
                        w.transform.position = p;
                        w.color = wallTint;
                        continue;
                    }

                    // every non-wall, non-void cell gets a floor tile
                    bool alt = ((x / 6) + (y / 5)) % 3 == 0;
                    var f = Art.NewSprite("f", alt ? altTile : floorTile, Layer.Floor, floorParent);
                    f.transform.position = p;
                    f.color = floorTint * (0.92f + 0.16f * ((x * 7 + y * 13) % 5) / 4f);
                    f.color = new Color(f.color.r, f.color.g, f.color.b, 1f);
                    openCells.Add(p);

                    switch (ch)
                    {
                        case 'P': PlayerStart = p; break;
                        case 'X': ExitPos = p; break;
                        case 'd': doorSlots.Add(p); break;
                        case 'w':
                        case 'W':
                        case 'R':
                        case 'a':
                        case 'h': pickupSlots.Add((ch, p)); break;
                        default:
                            if (ch >= '1' && ch <= '8') enemySlots.Add((ch, p));
                            else if (char.IsLetter(ch) && char.IsUpper(ch)) propSlots.Add((ch, p));
                            break;
                    }
                }
            }

            // Skirt of wall tiles beyond the playable grid, so the camera never shows void
            // at the edges of a small floor. No colliders - the grid border already has them.
            const int skirt = 8;
            Color skirtTint = wallTint * 0.8f;
            skirtTint.a = 1f;
            for (int y = -skirt; y < Height + skirt; y++)
                for (int x = -skirt; x < Width + skirt; x++)
                {
                    if (x >= 0 && y >= 0 && x < Width && y < Height) continue;
                    var w = Art.NewSprite("w", wallTile, Layer.Wall, wallParent);
                    w.transform.position = new Vector2(x, y);
                    w.color = skirtTint;
                }

            BuildWallColliders(grid, wallParent);

            foreach (var d in doorSlots) Door.Spawn(d, IsOpen(grid, (int)d.x - 1, (int)d.y), root);
            foreach (var s in propSlots) SpawnProp(s.c, s.pos, root);
            string[] explosives = { "rpg", "gl" };
            foreach (var s in pickupSlots)
            {
                if (s.c == 'a') { Pickup.Supply(PickupKind.Ammo, s.pos); continue; }
                if (s.c == 'h') { Pickup.Supply(PickupKind.Medkit, s.pos); continue; }
                string id = s.c switch
                {
                    'R' => explosives[rng.Next(explosives.Length)],
                    'W' => WeaponDB.RandomGun(rng),
                    _ => WeaponDB.Melees[rng.Next(WeaponDB.Melees.Count)],
                };
                Pickup.Drop(id, WeaponDB.Get(id).ammo, s.pos, (float)rng.NextDouble() * 360f);
            }

            // exit marker
            exitSr = Art.NewSprite("exit", "prop_exit", Layer.FloorDetail, root);
            exitSr.transform.position = ExitPos;
            exitSr.color = new Color(1f, 1f, 1f, 0.25f);

            Physics2D.SyncTransforms();
            SpawnEnemies(enemySlots, grid, rng);
        }

        static bool IsOpen(char[,] g, int x, int y)
        {
            if (x < 0 || y < 0 || x >= g.GetLength(0) || y >= g.GetLength(1)) return false;
            return g[x, y] != '#' && g[x, y] != ' ';
        }

        void BuildWallColliders(char[,] grid, Transform parent)
        {
            // merge horizontal runs of wall into as few box colliders as possible
            for (int y = 0; y < Height; y++)
            {
                int x = 0;
                while (x < Width)
                {
                    if (grid[x, y] != '#') { x++; continue; }
                    int start = x;
                    while (x < Width && grid[x, y] == '#') x++;
                    int len = x - start;
                    var box = parent.gameObject.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(len, 1f);
                    box.offset = new Vector2(start + len * 0.5f - 0.5f, y);
                }
            }
        }

        void SpawnProp(char c, Vector2 pos, Transform parent)
        {
            string sprite = c switch
            {
                'T' => "prop_table",
                'S' => "prop_sofa",
                'V' => "prop_tv",
                'L' => "prop_plant",
                'C' => "prop_crate",
                'B' => "prop_barrel",
                'E' => "prop_bed",
                'K' => "prop_counter",
                _ => null
            };
            if (sprite == null) return;
            var sr = Art.NewSprite("prop", sprite, Layer.Prop, parent);
            sr.transform.position = pos;
            sr.gameObject.layer = Layer.PropL;
            if (sr.sprite == null) return;
            var b = sr.gameObject.AddComponent<BoxCollider2D>();
            b.size = sr.sprite.bounds.size * 0.8f;
        }

        void SpawnEnemies(List<(char c, Vector2 pos)> slots, char[,] grid, System.Random rng)
        {
            string[] byDigit = { "thug", "bat", "knife", "pistol", "uzi", "shotgun", "dog", "boss" };
            foreach (var s in slots)
            {
                int idx = Mathf.Clamp(s.c - '1', 0, byDigit.Length - 1);
                var route = BuildRoute(s.pos, grid, rng);
                Enemy.Spawn(byDigit[idx], s.pos, (float)rng.NextDouble() * 360f, route);
            }
        }

        List<Vector2> BuildRoute(Vector2 from, char[,] grid, System.Random rng)
        {
            if (rng.NextDouble() < 0.35) return null;   // some guards just stand and look around
            var route = new List<Vector2> { from };
            Vector2 cur = from;
            for (int i = 0; i < 2; i++)
            {
                for (int tries = 0; tries < 24; tries++)
                {
                    var cand = openCells[rng.Next(openCells.Count)];
                    float dd = Vector2.Distance(cand, cur);
                    if (dd > 3f && dd < 11f && !Physics2D.Linecast(cur, cand, Layer.WallMask))
                    {
                        route.Add(cand);
                        cur = cand;
                        break;
                    }
                }
            }
            return route.Count > 1 ? route : null;
        }

        static Color Palette(int floor)
        {
            // each floor gets its own wash of colour, cycling through a neon set
            Color[] cols =
            {
                new Color(1.00f, 0.62f, 0.72f),
                new Color(0.60f, 0.78f, 1.00f),
                new Color(1.00f, 0.82f, 0.48f),
                new Color(0.72f, 1.00f, 0.74f),
                new Color(0.88f, 0.66f, 1.00f),
                new Color(1.00f, 0.55f, 0.45f),
            };
            return cols[Mathf.Abs(floor) % cols.Length];
        }

        public void SetExitLive(bool live)
        {
            if (exitSr == null) return;
            exitSr.color = live
                ? new Color(1f, 1f, 1f, 0.55f + Mathf.PingPong(Time.time * 0.8f, 0.45f))
                : new Color(1f, 1f, 1f, 0.18f);
        }

        void Update()
        {
            if (exitSr != null && GameManager.I != null && GameManager.I.ExitLive)
                SetExitLive(true);
        }

        // ================================================================== procedural
        public static char[,] Generate(int floor, out string name)
        {
            var rng = new System.Random(floor * 104729 + 7);
            int w = Mathf.Clamp(34 + floor * 2, 34, 54);
            int h = Mathf.Clamp(24 + floor, 24, 38);
            var g = new char[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++) g[x, y] = '#';

            var rooms = new List<RectInt>();
            Split(new RectInt(1, 1, w - 2, h - 2), rooms, rng, 0);

            foreach (var r in rooms)
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int y = r.yMin; y < r.yMax; y++) g[x, y] = '.';

            // connect rooms in order, L-shaped corridors, door at the room mouth
            for (int i = 1; i < rooms.Count; i++)
            {
                Vector2Int a = Center(rooms[i - 1]);
                Vector2Int b = Center(rooms[i]);
                if (rng.Next(2) == 0)
                {
                    Corridor(g, a.x, b.x, a.y, true);
                    Corridor(g, a.y, b.y, b.x, false);
                }
                else
                {
                    Corridor(g, a.y, b.y, a.x, false);
                    Corridor(g, a.x, b.x, b.y, true);
                }
            }

            // doors: floor cells pinched between two walls
            for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                {
                    if (g[x, y] != '.') continue;
                    bool hor = g[x, y - 1] == '#' && g[x, y + 1] == '#';
                    bool ver = g[x - 1, y] == '#' && g[x + 1, y] == '#';
                    if ((hor ^ ver) && rng.NextDouble() < 0.45) g[x, y] = 'd';
                }

            // player in the first room, exit in the last
            var start = Center(rooms[0]);
            g[start.x, start.y] = 'P';
            var end = Center(rooms[rooms.Count - 1]);
            g[end.x, end.y] = 'X';

            // populate the rest
            int budget = 3 + Mathf.RoundToInt(floor * 1.2f);
            var pool = new List<char>();
            for (int i = 0; i < budget; i++)
            {
                double r = rng.NextDouble();
                if (floor >= 3 && r < 0.10) pool.Add('7');
                else if (floor >= 2 && r < 0.22) pool.Add('6');
                else if (r < 0.38) pool.Add('4');
                else if (r < 0.52) pool.Add('5');
                else if (r < 0.70) pool.Add('2');
                else if (r < 0.82) pool.Add('3');
                else pool.Add('1');
            }
            if (floor % 5 == 0 && floor > 0) pool.Add('8');

            foreach (char e in pool)
                PlaceInRandomRoom(g, rooms, rng, e, 1, true);

            int guns = 5 + floor / 2;
            for (int i = 0; i < guns; i++) PlaceInRandomRoom(g, rooms, rng, 'W', 0, false);
            for (int i = 0; i < 4; i++) PlaceInRandomRoom(g, rooms, rng, 'w', 0, false);
            // every floor is guaranteed something that explodes, plus resupply
            for (int i = 0; i < 2; i++) PlaceInRandomRoom(g, rooms, rng, 'R', 0, false);
            for (int i = 0; i < 4; i++) PlaceInRandomRoom(g, rooms, rng, 'a', 0, false);
            for (int i = 0; i < 3; i++) PlaceInRandomRoom(g, rooms, rng, 'h', 0, false);

            char[] props = { 'T', 'S', 'V', 'L', 'C', 'B', 'E', 'K' };
            for (int i = 0; i < rooms.Count * 2; i++)
                PlaceInRandomRoom(g, rooms, rng, props[rng.Next(props.Length)], 0, false);

            string[] names =
            {
                "OVERDOSE", "PANTHER", "NEON BLOOD", "DECADENCE", "FULL HOUSE",
                "TENSION", "CRACKDOWN", "DEAD AHEAD", "HOTEL", "SUBWAY", "PENTHOUSE"
            };
            name = names[floor % names.Length];
            return g;
        }

        static void PlaceInRandomRoom(char[,] g, List<RectInt> rooms, System.Random rng,
                                      char ch, int skipFirstRooms, bool awayFromStart)
        {
            for (int tries = 0; tries < 60; tries++)
            {
                int ri = rng.Next(skipFirstRooms, rooms.Count);
                var r = rooms[ri];
                int x = rng.Next(r.xMin + 1, Mathf.Max(r.xMin + 2, r.xMax - 1));
                int y = rng.Next(r.yMin + 1, Mathf.Max(r.yMin + 2, r.yMax - 1));
                if (g[x, y] != '.') continue;
                if (awayFromStart && ri == 0) continue;
                g[x, y] = ch;
                return;
            }
        }

        static Vector2Int Center(RectInt r) => new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);

        static void Corridor(char[,] g, int a, int b, int fixedCoord, bool horizontal)
        {
            int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            for (int i = lo; i <= hi; i++)
            {
                int x = horizontal ? i : fixedCoord;
                int y = horizontal ? fixedCoord : i;
                if (x <= 0 || y <= 0 || x >= g.GetLength(0) - 1 || y >= g.GetLength(1) - 1) continue;
                if (g[x, y] == '#') g[x, y] = '.';
                // widen to two tiles so fights have room to breathe
                int x2 = horizontal ? x : x + 1;
                int y2 = horizontal ? y + 1 : y;
                if (x2 > 0 && y2 > 0 && x2 < g.GetLength(0) - 1 && y2 < g.GetLength(1) - 1 &&
                    g[x2, y2] == '#') g[x2, y2] = '.';
            }
        }

        static void Split(RectInt area, List<RectInt> rooms, System.Random rng, int depth)
        {
            const int minSide = 8;
            bool canSplit = depth < 4 && (area.width > minSide * 2 || area.height > minSide * 2);
            if (!canSplit)
            {
                int pad = 1;
                var r = new RectInt(area.xMin + pad, area.yMin + pad,
                                    Mathf.Max(4, area.width - pad * 2),
                                    Mathf.Max(4, area.height - pad * 2));
                rooms.Add(r);
                return;
            }
            bool horizontal = area.width < area.height;
            if (area.width > minSide * 2 && area.height > minSide * 2) horizontal = rng.Next(2) == 0;

            if (horizontal)
            {
                int cut = rng.Next(minSide, area.height - minSide);
                Split(new RectInt(area.xMin, area.yMin, area.width, cut), rooms, rng, depth + 1);
                Split(new RectInt(area.xMin, area.yMin + cut, area.width, area.height - cut), rooms, rng, depth + 1);
            }
            else
            {
                int cut = rng.Next(minSide, area.width - minSide);
                Split(new RectInt(area.xMin, area.yMin, cut, area.height), rooms, rng, depth + 1);
                Split(new RectInt(area.xMin + cut, area.yMin, area.width - cut, area.height), rooms, rng, depth + 1);
            }
        }

        public static char[,] FromAscii(string[] rows)
        {
            int w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            int h = rows.Length;
            var g = new char[w, h];
            for (int y = 0; y < h; y++)
            {
                // ASCII art reads top-down; the world is bottom-up
                string row = rows[h - 1 - y];
                for (int x = 0; x < w; x++)
                    g[x, y] = x < row.Length ? row[x] : '#';
            }
            return g;
        }
    }
}
