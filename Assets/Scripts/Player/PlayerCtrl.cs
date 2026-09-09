using UnityEngine;

namespace Neon
{
    public class PlayerCtrl : Actor
    {
        public static PlayerCtrl I;

        public const int SlotCount = 5;

        public float speed = 7.4f;
        public Weapon weapon;

        /// One inventory slot. A null id means the slot is empty and you punch.
        struct Slot
        {
            public string id;
            public int ammo;
        }

        readonly Slot[] slots = new Slot[SlotCount];
        int active;

        Rigidbody2D rb;
        SpriteRenderer sr;
        Vector2 aim = Vector2.up;
        float dashT, dashCd;
        Vector2 dashDir;
        int maxHp = 5;
        int hp;
        float iFrames;
        float stepTimer;
        SpriteRenderer laser;
        bool scoped;
        /// Set by the automated playtest, which cannot hold a mouse button.
        public bool ForceScopeForTest;

        public int Hp => hp;
        public int MaxHp => maxHp;
        public Vector2 Aim => aim;
        public int ActiveSlot => active;

        /// True while the right button is held on a weapon that has a scope.
        public bool Scoped => scoped;

        public string SlotId(int i) => (uint)i < SlotCount ? slots[i].id : null;

        /// The live weapon owns the active slot's ammo; the rest is parked in the slot.
        public int SlotAmmo(int i)
        {
            if ((uint)i >= SlotCount) return 0;
            if (i == active && weapon != null && slots[i].id != null) return weapon.ammo;
            return slots[i].ammo;
        }

        public static PlayerCtrl Spawn(Vector2 pos, string startWeapon)
        {
            var go = new GameObject("Player");
            go.layer = Layer.PlayerL;
            go.transform.position = pos;

            var p = go.AddComponent<PlayerCtrl>();
            p.team = Team.Player;
            p.kit = "player";

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            p.rb = rb;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.30f;

            var vis = new GameObject("vis");
            vis.transform.SetParent(go.transform, false);
            p.aimRoot = vis.transform;
            p.sr = vis.AddComponent<SpriteRenderer>();
            p.sr.sprite = Art.Get("char_player");
            p.sr.sortingOrder = Layer.Character;

            var laserGo = new GameObject("laser");
            laserGo.transform.SetParent(vis.transform, false);
            p.laser = laserGo.AddComponent<SpriteRenderer>();
            p.laser.sprite = Art.Get("ui_pixel");
            p.laser.sortingOrder = Layer.Held - 1;
            p.laser.enabled = false;

            I = p;
            p.ApplyMask();
            if (GameManager.I != null && GameManager.I.Mask == MaskId.Wolf) startWeapon = "knife";
            if (!string.IsNullOrEmpty(startWeapon) && startWeapon != "fist")
                p.slots[0] = new Slot { id = startWeapon, ammo = WeaponDB.Get(startWeapon).ammo };
            p.EquipActive();
            return p;
        }

        void ApplyMask()
        {
            var m = GameManager.I != null ? GameManager.I.Mask : MaskId.Rooster;
            if (m == MaskId.Pig) maxHp += 3;
            if (m == MaskId.Rooster) speed *= 1.22f;
            hp = maxHp;
        }

        /// Sight line from the muzzle to the first thing it would hit.
        void UpdateLaser()
        {
            if (laser == null) return;
            if (!scoped) { laser.enabled = false; return; }

            Vector2 origin = (Vector2)transform.position + aim * (weapon.def.held + 0.4f);
            var hit = Physics2D.Raycast(origin, aim, 40f, Layer.WallMask | Layer.EnemyMask);
            float len = hit.collider != null ? hit.distance : 40f;

            laser.enabled = true;
            // ui_pixel is 4px square, so 0.125 world units before scaling
            laser.transform.localPosition = new Vector3(0f, weapon.def.held + 0.4f + len * 0.5f, 0f);
            laser.transform.localRotation = Quaternion.identity;
            laser.transform.localScale = new Vector3(0.045f / 0.125f, len / 0.125f, 1f);
            bool onTarget = hit.collider != null &&
                            hit.collider.GetComponentInParent<Enemy>() != null;
            laser.color = onTarget ? new Color(1f, 0.25f, 0.3f, 0.75f)
                                   : new Color(1f, 0.25f, 0.3f, 0.35f);
        }

        void SyncActiveSlot()
        {
            if (weapon != null && slots[active].id != null) slots[active].ammo = weapon.ammo;
        }

        void EquipActive()
        {
            if (weapon != null) Destroy(weapon.gameObject);
            var s = slots[active];
            weapon = s.id != null ? Weapon.Attach(this, s.id, s.ammo) : Weapon.Attach(this, "fist");
        }

        void SelectSlot(int i)
        {
            i = Mathf.Clamp(i, 0, SlotCount - 1);
            if (i == active) return;
            SyncActiveSlot();
            active = i;
            EquipActive();
            Sfx.I.Play("ui", 0.4f);
        }

        void CycleSlot(int dir)
        {
            SelectSlot((active + dir + SlotCount) % SlotCount);
        }

        int FirstEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++) if (slots[i].id == null) return i;
            return -1;
        }

        void DropSlot(int i, Vector2 at)
        {
            if (slots[i].id == null) return;
            Pickup.Drop(slots[i].id, slots[i].ammo, at + Random.insideUnitCircle * 0.4f,
                        Random.value * 360f);
            slots[i] = default;
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (Dead || GameManager.I == null || !GameManager.I.AcceptsGameplayInput)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            var m = GameManager.I.Mask;

            // ---- invulnerability window after a hit, with a blink so it reads
            if (iFrames > 0f)
            {
                iFrames -= Time.deltaTime;
                sr.enabled = Mathf.FloorToInt(iFrames * 22f) % 2 == 0;
                if (iFrames <= 0f) sr.enabled = true;
            }

            // ---- aim
            Vector3 mouse = CamCtrl.I.ScreenToWorld(InputHub.MouseScreen);
            Vector2 toMouse = (Vector2)(mouse - transform.position);
            if (toMouse.sqrMagnitude > 0.0004f) aim = toMouse.normalized;
            aimRoot.rotation = Quaternion.FromToRotation(Vector3.up, aim);

            // ---- dash
            dashCd -= Time.deltaTime;
            float dashCdMax = m == MaskId.Rabbit ? 0.45f : 0.95f;
            if (dashT > 0f) dashT -= Time.deltaTime;
            if (InputHub.DashDown && dashCd <= 0f)
            {
                Vector2 mv = InputHub.Move;
                dashDir = mv.sqrMagnitude > 0.01f ? mv.normalized : aim;
                dashT = m == MaskId.Rabbit ? 0.20f : 0.15f;
                dashCd = dashCdMax;
                Sfx.I.PlayVaried("dash", 0.4f);
                Noise.Emit(transform.position, 3f, this);
            }

            // ---- scope
            scoped = (InputHub.Aiming || ForceScopeForTest) &&
                     weapon != null && weapon.def.HasScope && dashT <= 0f;
            UpdateLaser();

            // ---- move
            Vector2 vel;
            if (dashT > 0f) vel = dashDir * speed * 2.5f;
            else vel = InputHub.Move * speed * (scoped ? 0.45f : 1f);
            rb.linearVelocity = vel;

            if (vel.sqrMagnitude > 1f)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    stepTimer = 0.28f;
                    Noise.Emit(transform.position, 2.6f, this);
                    Sfx.I.PlayVaried("step", 0.28f, 0.18f);
                }
            }

            // ---- attack
            bool wantAttack = weapon.def.auto ? InputHub.Attack : InputHub.AttackDown;
            if (wantAttack) weapon.Attack(transform.position, aim);

            // ---- throw
            if (InputHub.ThrowDown && weapon.def.id != "fist")
            {
                Thrown.Launch(this, weapon.def, weapon.ammo,
                              (Vector2)transform.position + aim * 0.5f, aim);
                slots[active] = default;
                EquipActive();
            }

            // ---- inventory
            int want = InputHub.SlotDown;
            if (want >= 0) SelectSlot(want);
            int scroll = InputHub.ScrollStep;
            if (scroll != 0) CycleSlot(-scroll);
            if (InputHub.DropDown && slots[active].id != null)
            {
                SyncActiveSlot();
                DropSlot(active, transform.position);
                EquipActive();
                Sfx.I.Play("pickup", 0.4f);
            }

            // ---- pick up / execute
            if (InputHub.PickupDown) DoPickupOrExecute();
        }

        void DoPickupOrExecute()
        {
            // execution beats pickup: a downed enemy under your feet is the better prize
            var hits = Physics2D.OverlapCircleAll(transform.position, 1.05f, Layer.EnemyMask);
            foreach (var h in hits)
            {
                var e = h.GetComponentInParent<Enemy>();
                if (e != null && !e.Dead && e.CanBeExecuted)
                {
                    Vector2 d = ((Vector2)e.transform.position - (Vector2)transform.position).normalized;
                    e.Kill(d == Vector2.zero ? aim : d, "execute");
                    GameManager.I.AddScore(250, "EXECUTION");
                    CamCtrl.I.Shake(0.45f);
                    CamCtrl.I.Punch(0.7f);
                    FX.I.HitStop(0.09f, 0.03f);
                    return;
                }
            }

            var p = Pickup.Nearest(transform.position, 1.25f);
            if (p == null) { Sfx.I.Play("deny", 0.35f); return; }

            if (p.kind == PickupKind.Medkit)
            {
                if (hp >= maxHp) { Sfx.I.Play("deny", 0.35f); return; }
                hp = Mathf.Min(maxHp, hp + 2);
                Destroy(p.gameObject);
                Sfx.I.Play("heal", 0.7f);
                GameManager.I.Flash("PATCHED UP");
                return;
            }

            if (p.kind == PickupKind.Ammo)
            {
                if (weapon.def.kind != WKind.Gun)
                {
                    GameManager.I.Flash("NO GUN TO LOAD");
                    Sfx.I.Play("deny", 0.35f);
                    return;
                }
                weapon.ammo = weapon.def.ammo;
                Destroy(p.gameObject);
                Sfx.I.Play("ammo", 0.7f);
                GameManager.I.Flash("AMMO FULL");
                return;
            }

            SyncActiveSlot();

            // already carrying one: top it up rather than filling a second slot
            var pdef = WeaponDB.Get(p.id);
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].id != p.id) continue;
                if (pdef.kind == WKind.Gun)
                {
                    slots[i].ammo = Mathf.Min(pdef.ammo * 2, slots[i].ammo + Mathf.Max(1, p.ammo));
                    if (i == active) EquipActive();
                    Destroy(p.gameObject);
                    Sfx.I.Play("ammo", 0.6f);
                    GameManager.I.Flash("+" + Mathf.Max(1, p.ammo) + " ROUNDS");
                    return;
                }
                Sfx.I.Play("deny", 0.35f);
                GameManager.I.Flash("ALREADY CARRYING ONE");
                return;
            }

            string newId = p.id;
            int newAmmo = p.ammo;
            Destroy(p.gameObject);

            int free = FirstEmptySlot();
            if (free < 0)
            {
                // every slot full: the one in your hands gets swapped out
                DropSlot(active, transform.position);
                free = active;
                GameManager.I.Flash("SWAPPED SLOT " + (active + 1));
            }
            slots[free] = new Slot { id = newId, ammo = newAmmo };
            active = free;
            EquipActive();
            Sfx.I.Play("pickup", 0.6f);
        }

        /// Used by the automated playtest to fill the inventory without simulating input.
        public void GiveForTest(string id)
        {
            SyncActiveSlot();
            int free = FirstEmptySlot();
            if (free < 0) return;
            slots[free] = new Slot { id = id, ammo = WeaponDB.Get(id).ammo };
            active = free;
            EquipActive();
        }

        /// Used by the automated playtest to burn through the health bar deterministically.
        public void ForceHurtForTest()
        {
            iFrames = 0f;
            Kill(Vector2.up, "test");
        }

        public override void Kill(Vector2 dir, string cause)
        {
            if (Dead || iFrames > 0f) return;

            hp--;
            if (hp > 0)
            {
                iFrames = 1.4f;
                rb.linearVelocity = dir.normalized * 4f;
                Sfx.I.Play("glass", 0.55f);
                Sfx.I.PlayVaried("hit", 0.7f);
                // deliberately gentle: a hit should read, not throw the whole screen around
                CamCtrl.I.Shake(0.18f);
                CamCtrl.I.Punch(0.12f);
                FX.I.HitStop(0.05f, 0.12f);
                FX.I.BloodSpray(transform.position, dir, 0.5f);
                if (HUD.I != null) HUD.I.Flash(0.55f, new Color(0.8f, 0.05f, 0.12f));
                GameManager.I.Flash(hp == 1 ? "ONE HIT LEFT" : hp + " HITS LEFT");
                return;
            }

            Dead = true;
            rb.linearVelocity = Vector2.zero;
            sr.enabled = false;
            SyncActiveSlot();
            for (int i = 0; i < SlotCount; i++) DropSlot(i, transform.position);
            if (weapon != null) Destroy(weapon.gameObject);
            FX.I.BloodSpray(transform.position, dir, 1.6f);
            FX.I.Corpse(kit, transform.position,
                        Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f, dir * 5f);
            Sfx.I.Play("splat", 1f);
            Sfx.I.Play("scream" + Random.Range(0, 3), 0.7f);
            CamCtrl.I.Shake(0.45f);
            GameManager.I.OnPlayerDied();
        }
    }
}
