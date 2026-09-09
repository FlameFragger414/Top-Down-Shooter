using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Neon.EditorTools
{
    /// Batch-mode sanity check: every sprite the game asks for exists and imported as a
    /// point-filtered, uncompressed Sprite, and every generated floor is actually walkable
    /// from the player spawn to the exit and to every enemy.
    /// Run with: Unity -batchmode -quit -executeMethod Neon.EditorTools.NeonSmokeTest.Run
    public static class NeonSmokeTest
    {
        static int failures;

        [MenuItem("Neon Miami/Run Smoke Test")]
        public static void Run()
        {
            failures = 0;
            var log = new StringBuilder();
            log.AppendLine("=== NEON SMOKE TEST ===");

            CheckSprites(log);
            CheckIntroMap(log);
            CheckGeneratedFloors(log);

            log.AppendLine(failures == 0 ? "RESULT: PASS" : "RESULT: FAIL (" + failures + ")");
            Debug.Log(log.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static void Fail(StringBuilder log, string msg)
        {
            failures++;
            log.AppendLine("  FAIL: " + msg);
        }

        static void CheckSprites(StringBuilder log)
        {
            var names = new List<string>
            {
                "char_player", "char_thug", "char_suit", "char_heavy", "char_boss",
                "char_goon", "char_dog",
                "corpse_player", "corpse_thug", "corpse_suit", "corpse_heavy",
                "corpse_boss", "corpse_goon",
                "fx_pool", "fx_hole", "fx_shell", "fx_tracer", "fx_spark", "fx_smoke",
                "fx_glow", "fx_cone", "fx_rocket",
                "tile_check", "tile_wood", "tile_carpet", "tile_concrete", "tile_neon",
                "wall_brick", "wall_dark", "wall_stripe",
                "prop_table", "prop_sofa", "prop_tv", "prop_plant", "prop_crate",
                "prop_barrel", "prop_bed", "prop_counter", "prop_exit", "prop_door",
                "prop_ammo", "prop_medkit",
                "ui_font", "ui_crosshair", "ui_pixel", "ui_scanline", "ui_scope",
            };
            for (int i = 0; i < 6; i++) names.Add("fx_blood_" + i);
            for (int i = 0; i < 3; i++) names.Add("fx_muzzle_" + i);
            for (int i = 0; i < 4; i++) names.Add("fx_gore_" + i);
            for (int i = 0; i < 4; i++) names.Add("fx_explosion_" + i);
            for (int i = 0; i < 3; i++) names.Add("fx_flame_" + i);
            for (int i = 0; i < 6; i++) names.Add("ui_mask_" + i);
            foreach (var w in WeaponDB.All.Values) names.Add(w.sprite);

            log.AppendLine("sprites: checking " + names.Count);
            foreach (var n in names)
            {
                string path = "Assets/Resources/Art/" + n + ".png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) { Fail(log, "missing " + path); continue; }
                if (Resources.Load<Texture2D>("Art/" + n) == null)
                    Fail(log, "not loadable via Resources: " + n);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) { Fail(log, "no TextureImporter for " + n); continue; }
                if (imp.textureType != TextureImporterType.Sprite)
                    Fail(log, n + " textureType=" + imp.textureType);
                if (imp.filterMode != FilterMode.Point)
                    Fail(log, n + " filterMode=" + imp.filterMode);
                if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                    Fail(log, n + " is compressed");
                if (!Mathf.Approximately(imp.spritePixelsPerUnit, 32f))
                    Fail(log, n + " ppu=" + imp.spritePixelsPerUnit);
            }
        }

        static void CheckIntroMap(StringBuilder log)
        {
            int w = 0;
            foreach (var r in Levels.Intro) w = Mathf.Max(w, r.Length);
            foreach (var r in Levels.Intro)
                if (r.Length != w) Fail(log, "intro map row width " + r.Length + " != " + w);

            var grid = LevelGen.FromAscii(Levels.Intro);
            CheckGrid(log, grid, "intro");
        }

        static void CheckGeneratedFloors(StringBuilder log)
        {
            for (int floor = 2; floor <= 25; floor++)
            {
                var g = LevelGen.Generate(floor, out string name);
                if (string.IsNullOrEmpty(name)) Fail(log, "floor " + floor + " has no name");
                CheckGrid(log, g, "floor " + floor);
            }
        }

        static void CheckGrid(StringBuilder log, char[,] g, string label)
        {
            int w = g.GetLength(0), h = g.GetLength(1);
            Vector2Int start = new Vector2Int(-1, -1), exit = new Vector2Int(-1, -1);
            var enemies = new List<Vector2Int>();
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    char c = g[x, y];
                    if (c == 'P') start = new Vector2Int(x, y);
                    else if (c == 'X') exit = new Vector2Int(x, y);
                    else if (c >= '1' && c <= '8') enemies.Add(new Vector2Int(x, y));
                }

            if (start.x < 0) { Fail(log, label + ": no player start"); return; }
            if (exit.x < 0) { Fail(log, label + ": no exit"); return; }
            if (enemies.Count == 0) Fail(log, label + ": no enemies");

            var seen = new bool[w, h];
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);
            seen[start.x, start.y] = true;
            int reached = 0;
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                reached++;
                var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var d in dirs)
                {
                    var n = p + d;
                    if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h) continue;
                    if (seen[n.x, n.y]) continue;
                    char c = g[n.x, n.y];
                    if (c == '#' || c == ' ') continue;
                    seen[n.x, n.y] = true;
                    q.Enqueue(n);
                }
            }

            if (!seen[exit.x, exit.y]) Fail(log, label + ": exit unreachable from spawn");
            int unreachable = 0;
            foreach (var e in enemies) if (!seen[e.x, e.y]) unreachable++;
            if (unreachable > 0)
                Fail(log, label + ": " + unreachable + "/" + enemies.Count + " enemies walled off");
            if (reached < 40) Fail(log, label + ": only " + reached + " walkable cells reachable");
        }
    }
}
