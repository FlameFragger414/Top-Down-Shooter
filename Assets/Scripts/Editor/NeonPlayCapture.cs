using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Neon.EditorTools
{
    /// Headless-ish playtest driver. Launch Unity with:
    ///   -executeMethod Neon.EditorTools.NeonPlayCapture.Arm -neonCapture &lt;outputDir&gt;
    /// It enters play mode, exercises shooting and dying, writes screenshots and quits.
    /// Never runs unless that command-line flag is present.
    [InitializeOnLoad]
    public static class NeonPlayCapture
    {
        const string StartedKey = "neon.capture.started";
        static double t0;
        static int stage;
        static string outDir;

        static NeonPlayCapture()
        {
            outDir = Arg("-neonCapture");
            if (outDir == null) return;
            Directory.CreateDirectory(outDir);
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("Neon Miami/Arm Playtest Capture")]
        public static void Arm() { /* entry point for -executeMethod; the static ctor does the work */ }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        const string RunKey = "neon.capture.run";

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            int run = SessionState.GetInt(RunKey, 0);

            if (!Application.isPlaying)
            {
                if (run >= 2) return;                 // second run finished, exit happens below
                SessionState.SetInt(RunKey, run + 1);
                SessionState.SetBool(StartedKey, true);
                t0 = 0d;
                stage = 0;
                shotFloor = false;
                EditorApplication.EnterPlaymode();
                return;
            }

            if (t0 == 0d) { t0 = EditorApplication.timeSinceStartup; stage = 0; }
            double t = EditorApplication.timeSinceStartup - t0;
            var gm = GameManager.I;
            if (gm == null) return;

            // Second play session: with Reload Domain disabled this is where stale statics
            // bite. StartFloor here is the exact call that threw a NullReferenceException.
            if (run >= 2)
            {
                if (stage == 0 && t > 1.0) { gm.Mask = MaskId.Tiger; gm.StartFloor(); stage = 1; }
                else if (stage == 1 && t > 2.2) { Shot("05_replay"); stage = 2; }
                else if (stage == 2 && t > 3.0)
                {
                    Debug.Log("NEON CAPTURE DONE errors=" + errors);
                    EditorApplication.Exit(errors > 0 ? 2 : 0);
                }
                return;
            }

            switch (stage)
            {
                case 0:
                    if (t > 1.2) { Shot("01_title"); stage++; }
                    break;
                case 1:
                    if (t > 2.0)
                    {
                        gm.Mask = MaskId.Owl;
                        gm.StartFloor();
                        stage++;
                    }
                    break;
                case 2:
                    if (t > 3.0)
                    {
                        var p = PlayerCtrl.I;
                        if (p != null)
                        {
                            // fill every inventory slot so the bar can be inspected
                            p.GiveForTest("minigun");
                            p.GiveForTest("rpg");
                            p.GiveForTest("sniper");
                            p.GiveForTest("katana");
                            p.GiveForTest("autoshotgun");
                        }
                        stage++;   // let the HUD refresh before the shot
                    }
                    break;
                case 3:
                    if (t > 3.15 && !shotFloor) { Shot("02_floor1"); shotFloor = true; }
                    // spray a few shots so bullets, casings, decals and audio all run
                    if (t > 3.2 && t < 4.4)
                    {
                        var p = PlayerCtrl.I;
                        if (p != null && p.weapon != null)
                        {
                            if (t > 3.9 && p.weapon.def.id != "flamer")
                            {
                                UnityEngine.Object.Destroy(p.weapon.gameObject);
                                p.weapon = Weapon.Attach(p, "flamer");
                            }
                            float a = (float)t * 5f;
                            p.weapon.Attack(p.transform.position,
                                            new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                        }
                    }
                    if (t > 4.4)
                    {
                        // kill a few guards outright to exercise gore, corpses and combo
                        for (int i = 0; i < 3 && Enemy.All.Count > 0; i++)
                        {
                            var e = Enemy.All[0];
                            if (e != null) e.Kill(UnityEngine.Random.insideUnitCircle.normalized, "shotgun");
                        }
                        stage++;
                    }
                    break;
                case 4:
                    if (t > 5.0 && PlayerCtrl.I != null && !PlayerCtrl.I.ForceScopeForTest)
                    {
                        // swap to the sniper and scope in for the next frame's shot
                        var p = PlayerCtrl.I;
                        UnityEngine.Object.Destroy(p.weapon.gameObject);
                        p.weapon = Weapon.Attach(p, "sniper");
                        p.ForceScopeForTest = true;
                    }
                    if (t > 5.2) { Shot("03_carnage"); stage++; }
                    break;
                case 5:
                    if (t > 5.3 && PlayerCtrl.I != null) PlayerCtrl.I.ForceScopeForTest = false;
                    // fire a rocket at a wall to exercise the explosion path
                    if (t > 5.4 && t < 5.5)
                    {
                        var p = PlayerCtrl.I;
                        if (p != null)
                        {
                            UnityEngine.Object.Destroy(p.weapon.gameObject);
                            p.weapon = Weapon.Attach(p, "rpg");
                            p.weapon.Attack(p.transform.position, Vector2.right);
                        }
                    }
                    if (t > 6.0)
                    {
                        // three hits now, so kill the player outright
                        for (int i = 0; i < 6 && PlayerCtrl.I != null && !PlayerCtrl.I.Dead; i++)
                        {
                            PlayerCtrl.I.ForceHurtForTest();
                        }
                        stage++;
                    }
                    break;
                case 6:
                    if (t > 7.0) { Shot("04_death"); stage++; }
                    break;
                case 7:
                    // leave play mode so the next tick starts a second session
                    if (t > 8.0) { stage++; EditorApplication.ExitPlaymode(); }
                    break;
            }
        }

        static int errors;
        static bool shotFloor;

        static void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                Debug.Log("NEON RUNTIME PROBLEM: " + condition + "\n" + stack);
            }
        }

        /// Batch mode has no game view, so render the cameras into a texture by hand.
        static void Shot(string name)
        {
            const int W = 1600, H = 900;
            var main = Camera.main;
            if (main == null) { Debug.Log("NEON SHOT skipped, no camera"); return; }

            // there is no real cursor in batch mode, so the camera's mouse lead is garbage
            if (PlayerCtrl.I != null && CamCtrl.I != null)
                CamCtrl.I.SnapTo(PlayerCtrl.I.transform.position);

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Point
            };
            rt.Create();

            // The HUD is an Overlay on the main camera's stack, so one render gets both.
            Render(main, rt);
            Save(rt, W, H, name);

            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("NEON SHOT " + name);
        }

        static void Save(RenderTexture rt, int w, int h, string name)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(outDir, name + ".png").Replace('\\', '/'),
                               tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void Render(Camera cam, RenderTexture rt)
        {
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
                return;
            }
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
        }
    }
}
