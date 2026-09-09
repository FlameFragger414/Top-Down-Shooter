using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Neon
{
    /// Whole interface: in-game readouts, title, mask select, death and clear screens.
    /// Lives on its own camera so screen shake never moves the text.
    public class HUD : MonoBehaviour
    {
        public static HUD I;
        public const int UiLayer = 5;

        Camera hudCam;
        public Camera Cam => hudCam;
        float halfH, halfW;

        PixelLabel floorLbl, scoreLbl, comboLbl, leftLbl, weaponLbl, eventLbl, promptLbl;
        PixelLabel bigLbl, subLbl, body1, body2, body3, hintLbl;
        SpriteRenderer crosshair, scanlines, flashSr, comboBar;
        SpriteRenderer[] maskIcons;
        SpriteRenderer[] hpPips;

        // inventory bar
        SpriteRenderer[] slotBg;
        SpriteRenderer[] slotIcon;
        PixelLabel[] slotNum;
        PixelLabel[] slotAmmo;
        const float SlotBox = 1.5f, SlotGap = 1.72f;
        float flashAmount;

        public static HUD Create(Transform parent, Camera main)
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(parent, false);
            go.layer = UiLayer;
            I = go.AddComponent<HUD>();
            I.Build(main);
            return I;
        }

        void Build(Camera main)
        {
            var camGo = new GameObject("HudCam");
            camGo.transform.SetParent(transform, false);
            hudCam = camGo.AddComponent<Camera>();
            hudCam.orthographic = true;
            hudCam.orthographicSize = 9f;
            hudCam.clearFlags = CameraClearFlags.Depth;
            hudCam.cullingMask = 1 << UiLayer;
            hudCam.depth = main.depth + 10;
            hudCam.transform.position = new Vector3(0, 0, -10f);
            hudCam.allowMSAA = false;

            // In URP a Base camera always clears, so a second Base camera drawn after the
            // world would wipe it. The HUD has to be an Overlay on the main camera's stack.
            var hudData = hudCam.GetUniversalAdditionalCameraData();
            var mainData = main.GetUniversalAdditionalCameraData();
            if (hudData != null && mainData != null)
            {
                hudData.renderType = CameraRenderType.Overlay;
                hudData.renderPostProcessing = false;
                if (!mainData.cameraStack.Contains(hudCam)) mainData.cameraStack.Add(hudCam);
            }

            halfH = hudCam.orthographicSize;
            halfW = halfH * ScreenAspect;

            floorLbl = Mk("floor", 2.6f, TextAlign.Left, new Color(1f, 0.45f, 0.72f));
            scoreLbl = Mk("score", 2.2f, TextAlign.Left, new Color(1f, 0.92f, 0.7f));
            comboLbl = Mk("combo", 3.0f, TextAlign.Right, new Color(0.45f, 0.95f, 1f));
            leftLbl = Mk("left", 2.0f, TextAlign.Right, new Color(0.9f, 0.9f, 0.95f));
            weaponLbl = Mk("weapon", 2.1f, TextAlign.Center, new Color(0.95f, 0.95f, 1f));
            eventLbl = Mk("event", 3.2f, TextAlign.Center, new Color(1f, 0.85f, 0.35f));
            promptLbl = Mk("prompt", 2.0f, TextAlign.Center, new Color(0.85f, 0.85f, 0.92f));

            bigLbl = Mk("big", 7.0f, TextAlign.Center, new Color(1f, 0.28f, 0.55f));
            subLbl = Mk("sub", 3.0f, TextAlign.Center, new Color(0.5f, 0.95f, 1f));
            body1 = Mk("b1", 2.6f, TextAlign.Center, Color.white);
            body2 = Mk("b2", 2.6f, TextAlign.Center, Color.white);
            body3 = Mk("b3", 2.6f, TextAlign.Center, Color.white);
            hintLbl = Mk("hint", 2.2f, TextAlign.Center, new Color(0.75f, 0.75f, 0.85f));

            crosshair = NewUiSprite("crosshair", "ui_crosshair", Layer.Ui + 5);
            crosshair.transform.localScale = Vector3.one * 1.6f;

            scanlines = NewUiSprite("scanlines", "ui_scanline", Layer.Overlay);
            scanlines.drawMode = SpriteDrawMode.Tiled;
            scanlines.size = new Vector2(halfW * 2.4f, halfH * 2.4f);
            scanlines.color = new Color(1f, 1f, 1f, 0.55f);

            flashSr = NewUiSprite("flash", "ui_pixel", Layer.Overlay + 2);
            flashSr.color = new Color(1f, 1f, 1f, 0f);

            comboBar = NewUiSprite("combobar", "ui_pixel", Layer.Ui);
            comboBar.color = new Color(0.45f, 0.95f, 1f, 0.85f);

            hpPips = new SpriteRenderer[8];
            for (int i = 0; i < hpPips.Length; i++)
            {
                hpPips[i] = NewUiSprite("hp" + i, "ui_pixel", Layer.Ui);
                // ui_pixel is 4px square: 0.125 world units at 32 PPU
                hpPips[i].transform.localScale = new Vector3(0.42f / 0.125f, 0.42f / 0.125f, 1f);
            }

            slotBg = new SpriteRenderer[PlayerCtrl.SlotCount];
            slotIcon = new SpriteRenderer[PlayerCtrl.SlotCount];
            slotNum = new PixelLabel[PlayerCtrl.SlotCount];
            slotAmmo = new PixelLabel[PlayerCtrl.SlotCount];
            for (int i = 0; i < PlayerCtrl.SlotCount; i++)
            {
                slotBg[i] = NewUiSprite("slotbg" + i, "ui_pixel", Layer.Ui - 2);
                slotBg[i].transform.localScale = Vector3.one * (SlotBox / 0.125f);
                slotIcon[i] = NewUiSprite("sloticon" + i, null, Layer.Ui);
                slotNum[i] = Mk("slotnum" + i, 1.3f, TextAlign.Left, new Color(0.7f, 0.7f, 0.8f));
                slotAmmo[i] = Mk("slotammo" + i, 1.5f, TextAlign.Center, Color.white);
            }

            maskIcons = new SpriteRenderer[6];
            for (int i = 0; i < 6; i++)
            {
                maskIcons[i] = NewUiSprite("mask" + i, "ui_mask_" + i, Layer.Ui);
                maskIcons[i].transform.localScale = Vector3.one * 2.6f;
            }

            Layout();
        }

        PixelLabel Mk(string name, float scale, TextAlign align, Color col)
        {
            var l = PixelLabel.Create(transform, name, scale, align, col);
            l.gameObject.layer = UiLayer;
            return l;
        }

        SpriteRenderer NewUiSprite(string name, string sprite, int order)
        {
            var sr = Art.NewSprite(name, sprite, order, transform);
            sr.gameObject.layer = UiLayer;
            return sr;
        }

        /// An Overlay camera does not report a usable aspect of its own, so the screen is
        /// the only reliable source for the visible half-width.
        static float ScreenAspect =>
            Mathf.Clamp(Screen.width / Mathf.Max(1f, (float)Screen.height), 0.5f, 4f);

        const float Margin = 1.0f;

        void Layout()
        {
            halfH = hudCam.orthographicSize;
            halfW = halfH * ScreenAspect;
            float m = Margin;

            floorLbl.transform.localPosition = new Vector3(-halfW + m, halfH - m - 0.6f, 0);
            scoreLbl.transform.localPosition = new Vector3(-halfW + m, halfH - m - 1.5f, 0);
            comboLbl.transform.localPosition = new Vector3(halfW - m, halfH - m - 0.6f, 0);
            leftLbl.transform.localPosition = new Vector3(halfW - m, halfH - m - 2.2f, 0);
            // sits above the inventory bar; see the barY block below
            eventLbl.transform.localPosition = new Vector3(0f, halfH - 2.4f, 0);
            promptLbl.transform.localPosition = new Vector3(0f, -halfH + 1.6f, 0);

            bigLbl.transform.localPosition = new Vector3(0f, 3.6f, 0);
            subLbl.transform.localPosition = new Vector3(0f, 1.4f, 0);
            body1.transform.localPosition = new Vector3(0f, -0.4f, 0);
            body2.transform.localPosition = new Vector3(0f, -1.5f, 0);
            body3.transform.localPosition = new Vector3(0f, -2.6f, 0);
            hintLbl.transform.localPosition = new Vector3(0f, -halfH + 1.1f, 0);

            scanlines.size = new Vector2(halfW * 2.4f, halfH * 2.4f);
            // ui_pixel is 4px square = 0.125 world units at 32 PPU
            flashSr.transform.localScale = new Vector3(halfW * 2.4f / 0.125f,
                                                       halfH * 2.4f / 0.125f, 1f);

            for (int i = 0; i < maskIcons.Length; i++)
                maskIcons[i].transform.localPosition = new Vector3((i - 2.5f) * 2.6f, 0.6f, 0f);

            for (int i = 0; i < hpPips.Length; i++)
                hpPips[i].transform.localPosition =
                    new Vector3(-halfW + m + 0.21f + i * 0.62f, -halfH + m + 1.35f, 0f);

            float barY = -halfH + m + 0.75f;
            float barX0 = -(PlayerCtrl.SlotCount - 1) * SlotGap * 0.5f;
            for (int i = 0; i < slotBg.Length; i++)
            {
                float x = barX0 + i * SlotGap;
                slotBg[i].transform.localPosition = new Vector3(x, barY, 0f);
                // weapon sprites pivot at the grip, so nudge the icon down to centre it
                slotIcon[i].transform.localPosition = new Vector3(x, barY - 0.30f, 0f);
                slotNum[i].transform.localPosition =
                    new Vector3(x - SlotBox * 0.5f + 0.08f, barY + SlotBox * 0.5f - 0.10f, 0f);
                slotAmmo[i].transform.localPosition =
                    new Vector3(x, barY - SlotBox * 0.5f + 0.02f, 0f);
            }
            weaponLbl.transform.localPosition =
                new Vector3(0f, barY + SlotBox * 0.5f + 0.5f, 0f);
        }

        public void Flash(float amount, Color col)
        {
            flashAmount = Mathf.Max(flashAmount, amount);
            flashSr.color = new Color(col.r, col.g, col.b, flashSr.color.a);
        }

        void LateUpdate()
        {
            if (Mathf.Abs(halfW - hudCam.orthographicSize * ScreenAspect) > 0.001f) Layout();

            var gm = GameManager.I;
            if (gm == null) return;

            // crosshair tracks the real cursor; mapped by hand because ScreenToWorldPoint
            // is not dependable on an Overlay camera
            Vector2 mp = InputHub.MouseScreen;
            crosshair.transform.localPosition = new Vector3(
                (mp.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f * halfW,
                (mp.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f * halfH, 0f);
            crosshair.enabled = gm.State == GameState.Playing;

            // scoping swaps the crosshair for a reticle and closes the vignette down
            bool scoped = PlayerCtrl.I != null && PlayerCtrl.I.Scoped;
            crosshair.sprite = Art.Get(scoped ? "ui_scope" : "ui_crosshair");
            crosshair.transform.localScale = Vector3.one * (scoped ? 1f : 1.6f);
            crosshair.color = scoped ? new Color(1f, 0.85f, 0.88f) : Color.white;
            if (Boot.Vig != null)
                Boot.Vig.intensity.value = Mathf.Lerp(Boot.Vig.intensity.value,
                                                      scoped ? 0.46f : 0.28f,
                                                      1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            Cursor.visible = gm.State != GameState.Playing;

            flashAmount = Mathf.Max(0f, flashAmount - Time.unscaledDeltaTime * 4f);
            var fc = flashSr.color;
            flashSr.color = new Color(fc.r, fc.g, fc.b, flashAmount);

            bool inGame = gm.State == GameState.Playing || gm.State == GameState.Dead;
            floorLbl.gameObject.SetActive(inGame);
            scoreLbl.gameObject.SetActive(inGame);
            comboLbl.gameObject.SetActive(inGame);
            leftLbl.gameObject.SetActive(inGame);
            // the weapon readout would collide with the retry prompt on the death screen
            weaponLbl.gameObject.SetActive(gm.State == GameState.Playing);
            eventLbl.gameObject.SetActive(inGame);
            comboBar.gameObject.SetActive(inGame && gm.Combo > 1);

            DrawSlots(gm);

            var pc = PlayerCtrl.I;
            for (int i = 0; i < hpPips.Length; i++)
            {
                bool used = gm.State == GameState.Playing && pc != null && i < pc.MaxHp;
                hpPips[i].gameObject.SetActive(used);
                if (!used) continue;
                bool full = i < pc.Hp;
                hpPips[i].color = full
                    ? new Color(1f, 0.24f, 0.36f)
                    : new Color(0.32f, 0.16f, 0.24f, 0.85f);
            }
            foreach (var m in maskIcons) m.gameObject.SetActive(gm.State == GameState.MaskSelect);

            switch (gm.State)
            {
                case GameState.Title: DrawTitle(); break;
                case GameState.MaskSelect: DrawMaskSelect(); break;
                case GameState.Playing: DrawPlaying(gm); break;
                case GameState.Dead: DrawDead(gm); break;
                case GameState.Clear: DrawClear(gm); break;
            }
        }

        void DrawSlots(GameManager gm)
        {
            bool show = gm.State == GameState.Playing && PlayerCtrl.I != null;
            var pc = PlayerCtrl.I;
            for (int i = 0; i < slotBg.Length; i++)
            {
                slotBg[i].gameObject.SetActive(show);
                slotIcon[i].gameObject.SetActive(show);
                slotNum[i].gameObject.SetActive(show);
                slotAmmo[i].gameObject.SetActive(show);
                if (!show) continue;

                bool selected = i == pc.ActiveSlot;
                string id = pc.SlotId(i);

                slotBg[i].color = selected
                    ? new Color(0.95f, 0.28f, 0.55f, 0.55f)
                    : new Color(0.06f, 0.05f, 0.10f, 0.55f);
                slotNum[i].SetText((i + 1).ToString());
                slotNum[i].SetColor(selected ? Color.white : new Color(0.6f, 0.6f, 0.7f));

                if (id == null)
                {
                    slotIcon[i].sprite = null;
                    slotAmmo[i].SetText("");
                    continue;
                }

                var def = WeaponDB.Get(id);
                slotIcon[i].sprite = Art.Get(def.sprite);
                slotIcon[i].color = selected ? Color.white : new Color(0.72f, 0.72f, 0.78f);
                slotIcon[i].transform.localScale = Vector3.one;

                if (def.kind != WKind.Gun) slotAmmo[i].SetText("--");
                else
                {
                    int a = pc.SlotAmmo(i);
                    slotAmmo[i].SetText(a.ToString());
                    slotAmmo[i].SetColor(a == 0 ? new Color(1f, 0.35f, 0.35f)
                                                : new Color(1f, 0.93f, 0.72f));
                }
            }
        }

        void ShowBig(string big, string sub, string l1, string l2, string l3, string hint)
        {
            bigLbl.gameObject.SetActive(big != null);
            subLbl.gameObject.SetActive(sub != null);
            body1.gameObject.SetActive(l1 != null);
            body2.gameObject.SetActive(l2 != null);
            body3.gameObject.SetActive(l3 != null);
            hintLbl.gameObject.SetActive(hint != null);
            if (big != null) bigLbl.SetText(big);
            if (sub != null) subLbl.SetText(sub);
            if (l1 != null) body1.SetText(l1);
            if (l2 != null) body2.SetText(l2);
            if (l3 != null) body3.SetText(l3);
            if (hint != null) hintLbl.SetText(hint);
        }

        void DrawTitle()
        {
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f);
            ShowBig("NEON MIAMI", "A TOP-DOWN MASSACRE",
                    "WASD MOVE   MOUSE AIM   LMB SHOOT   RMB SCOPE",
                    "1-5 OR WHEEL SWAP   E PICK UP   G DROP   F THROW",
                    "SHIFT DASH   R RESTART FLOOR",
                    "PRESS ENTER");
            hintLbl.SetColor(new Color(1f, 1f, 1f, pulse));
            bigLbl.SetColor(Color.Lerp(new Color(1f, 0.25f, 0.55f),
                                       new Color(0.4f, 0.95f, 1f),
                                       0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.3f)));
        }

        void DrawMaskSelect()
        {
            var gm = GameManager.I;
            int sel = (int)gm.Mask;
            ShowBig("CHOOSE A MASK", null, null, null, null, "A / D TO CHANGE     ENTER TO GO");
            subLbl.gameObject.SetActive(true);
            subLbl.transform.localPosition = new Vector3(0f, -1.6f, 0f);
            subLbl.SetText(GameManager.MaskNames[sel]);
            body1.gameObject.SetActive(true);
            body1.transform.localPosition = new Vector3(0f, -3.0f, 0f);
            body1.SetText(GameManager.MaskPerks[sel]);

            for (int i = 0; i < maskIcons.Length; i++)
            {
                bool on = i == sel;
                float s = on ? 3.4f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f : 2.4f;
                maskIcons[i].transform.localScale = Vector3.one * s;
                maskIcons[i].color = on ? Color.white : new Color(0.45f, 0.42f, 0.5f);
            }
        }

        void DrawPlaying(GameManager gm)
        {
            ShowBig(null, null, null, null, null, null);
            body1.transform.localPosition = new Vector3(0f, -0.4f, 0);
            subLbl.transform.localPosition = new Vector3(0f, 1.4f, 0);

            floorLbl.SetText("FLOOR " + gm.Floor + "  " + gm.FloorName);
            scoreLbl.SetText("SCORE " + gm.Score);

            int left = Enemy.AliveCount;
            leftLbl.SetText(left > 0 ? left + " LEFT" : "GET OUT");
            leftLbl.SetColor(left > 0 ? new Color(0.9f, 0.9f, 0.95f) : new Color(0.6f, 1f, 0.5f));

            if (gm.Combo > 1)
            {
                comboLbl.SetText("X" + gm.Combo);
                float k = Mathf.Clamp01(gm.ComboTime / 4.2f);
                float barW = Mathf.Max(0.05f, 3.2f * k);
                comboBar.transform.localScale = new Vector3(barW / 0.125f, 0.16f / 0.125f, 1f);
                comboBar.transform.localPosition =
                    new Vector3(halfW - Margin - barW * 0.5f, halfH - Margin - 1.2f, 0f);
            }
            else comboLbl.SetText("");

            var p = PlayerCtrl.I;
            if (p != null && p.weapon != null)
            {
                var d = p.weapon.def;
                // the slot bar already carries the ammo count
                weaponLbl.SetText(d.label.ToUpperInvariant());
                weaponLbl.SetColor(d.kind == WKind.Gun && p.weapon.ammo == 0
                    ? new Color(1f, 0.4f, 0.4f) : Color.white);
            }

            float age = Time.unscaledTime - gm.LastEventTime;
            if (age < 1.6f && !string.IsNullOrEmpty(gm.LastEvent))
            {
                eventLbl.SetText(gm.LastEvent);
                eventLbl.SetColor(new Color(1f, 0.85f, 0.35f, Mathf.Clamp01(1.6f - age)));
            }
            else eventLbl.SetText("");

            // context prompt
            string prompt = "";
            if (p != null && !p.Dead)
            {
                var pk = Pickup.Nearest(p.transform.position, 1.15f);
                var downed = Physics2D.OverlapCircle(p.transform.position, 1.05f, Layer.EnemyMask);
                var e = downed != null ? downed.GetComponentInParent<Enemy>() : null;
                if (e != null && e.CanBeExecuted) prompt = "E - FINISH HIM";
                else if (pk != null) prompt = "E - " + WeaponDB.Get(pk.id).label.ToUpperInvariant();
                else if (gm.ExitLive) prompt = "GET TO THE EXIT";
            }
            promptLbl.gameObject.SetActive(prompt != "");
            promptLbl.transform.localPosition = new Vector3(0f, -halfH + 1.6f, 0f);
            promptLbl.SetText(prompt);
        }

        void DrawDead(GameManager gm)
        {
            promptLbl.gameObject.SetActive(false);
            eventLbl.SetText("");
            ShowBig("YOU DIED", null,
                    "FLOOR " + gm.Floor + "   " + gm.Kills + "/" + gm.TotalEnemies + " KILLED",
                    null, null, "PRESS R TO TRY AGAIN");
            bigLbl.SetColor(new Color(0.9f, 0.12f, 0.2f));
            hintLbl.SetColor(new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f)));
        }

        void DrawClear(GameManager gm)
        {
            promptLbl.gameObject.SetActive(false);
            ShowBig("FLOOR CLEAR", "RANK  " + gm.Rank,
                    "KILLS      " + gm.Kills,
                    "SCORE      " + gm.Score,
                    "TIME BONUS " + gm.TimeBonus + "   TOTAL " + gm.RunScore,
                    "PRESS ENTER FOR FLOOR " + (gm.Floor + 1));
            bigLbl.SetColor(new Color(0.55f, 1f, 0.6f));
            hintLbl.SetColor(new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f)));
        }
    }
}
