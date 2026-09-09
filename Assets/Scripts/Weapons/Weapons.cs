using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    public enum WKind { Melee, Gun }

    public class WeaponDef
    {
        public string id, sprite, label, sfx;
        public WKind kind;
        public float rate = 3f;          // attacks per second
        public bool auto;
        public int ammo = 0;             // guns only
        public int pellets = 1;
        public float spread;             // degrees, half-angle
        public float bulletSpeed = 34f;
        public float noise = 4f;         // alert radius in world units
        public float reach = 1.0f;       // melee
        public float arc = 70f;          // melee half-arc, degrees
        public float knockdown;          // 0..1 chance to floor instead of kill
        public float kick = 0.25f;       // camera trauma
        public float held = 0.42f;       // how far in front of the owner it is drawn
        public bool lethalThrow;

        public float rarity = 1f;        // relative weight when scattering pickups
        public bool pierce;              // shot carries on through whoever it kills
        public bool rocket;              // fires a Rocket instead of a Bullet
        public float blast;              // explosion radius, rockets only
        public float spinUp;             // seconds of wind-up before the first shot
        public bool flame;               // short-lived fire projectiles instead of rounds
        public float scopeZoom;          // extra orthographic size while scoped, 0 = no scope
        public float scopeLead = 3.6f;   // how far the camera runs down the sightline

        public bool HasScope => scopeZoom > 0f;

        public string label2 => label.ToUpperInvariant();
    }

    public static class WeaponDB
    {
        public static readonly Dictionary<string, WeaponDef> All = new Dictionary<string, WeaponDef>();
        public static readonly List<string> Guns = new List<string>();
        public static readonly List<string> Melees = new List<string>();

        static void Add(WeaponDef d)
        {
            All[d.id] = d;
            if (d.kind == WKind.Gun) Guns.Add(d.id); else if (d.id != "fist") Melees.Add(d.id);
        }

        static WeaponDB()
        {
            Add(new WeaponDef { id = "fist", sprite = "wpn_fist", label = "Fists", kind = WKind.Melee,
                rate = 3.4f, reach = 0.95f, arc = 75f, noise = 2.5f, knockdown = 0.55f,
                sfx = "swing", kick = 0.10f, held = 0.30f });
            Add(new WeaponDef { id = "bat", sprite = "wpn_bat", label = "Bat", kind = WKind.Melee,
                rate = 2.1f, reach = 1.45f, arc = 65f, noise = 4f, knockdown = 0.35f,
                sfx = "swing", kick = 0.16f, held = 0.55f });
            Add(new WeaponDef { id = "pipe", sprite = "wpn_pipe", label = "Pipe", kind = WKind.Melee,
                rate = 2.4f, reach = 1.35f, arc = 62f, noise = 4f, knockdown = 0.3f,
                sfx = "swing", kick = 0.15f, held = 0.52f });
            Add(new WeaponDef { id = "knife", sprite = "wpn_knife", label = "Knife", kind = WKind.Melee,
                rate = 3.6f, reach = 1.05f, arc = 55f, noise = 2f, knockdown = 0f,
                sfx = "swing", kick = 0.10f, held = 0.38f, lethalThrow = true });
            Add(new WeaponDef { id = "katana", sprite = "wpn_katana", label = "Katana", kind = WKind.Melee,
                rate = 2.7f, reach = 1.7f, arc = 80f, noise = 3f, knockdown = 0f,
                sfx = "swing", kick = 0.18f, held = 0.62f, lethalThrow = true });

            Add(new WeaponDef { id = "pistol", sprite = "wpn_pistol", label = "Pistol", kind = WKind.Gun,
                rate = 5.5f, ammo = 17, spread = 1.6f, noise = 20f, sfx = "pistol",
                kick = 0.28f, held = 0.46f, rarity = 1.6f });
            Add(new WeaponDef { id = "silenced", sprite = "wpn_silenced", label = "Silencer", kind = WKind.Gun,
                rate = 4.5f, ammo = 17, spread = 1.4f, noise = 4.5f, sfx = "silenced",
                kick = 0.16f, held = 0.50f, rarity = 1.0f,
                scopeZoom = 0.9f, scopeLead = 5.5f });
            Add(new WeaponDef { id = "shotgun", sprite = "wpn_shotgun", label = "Shotgun", kind = WKind.Gun,
                rate = 1.35f, ammo = 10, pellets = 7, spread = 11f, noise = 27f, sfx = "shotgun",
                kick = 0.55f, held = 0.66f, bulletSpeed = 30f, rarity = 1.2f });
            Add(new WeaponDef { id = "uzi", sprite = "wpn_uzi", label = "Uzi", kind = WKind.Gun,
                rate = 12f, auto = true, ammo = 45, spread = 6.5f, noise = 18f, sfx = "uzi",
                kick = 0.16f, held = 0.48f, rarity = 1.4f });
            Add(new WeaponDef { id = "rifle", sprite = "wpn_rifle", label = "Assault Rifle", kind = WKind.Gun,
                rate = 8.5f, auto = true, ammo = 45, spread = 3.2f, noise = 24f, sfx = "rifle",
                kick = 0.22f, held = 0.62f, rarity = 1.3f,
                scopeZoom = 1.3f, scopeLead = 7f });
            Add(new WeaponDef { id = "revolver", sprite = "wpn_revolver", label = "Revolver", kind = WKind.Gun,
                rate = 2.3f, ammo = 9, spread = 1.0f, noise = 25f, sfx = "revolver",
                kick = 0.42f, held = 0.48f, rarity = 1.3f, bulletSpeed = 40f,
                scopeZoom = 0.8f, scopeLead = 5.5f });
            Add(new WeaponDef { id = "double", sprite = "wpn_double", label = "Double Barrel", kind = WKind.Gun,
                rate = 1.1f, ammo = 6, pellets = 10, spread = 15f, noise = 29f, sfx = "double",
                kick = 0.7f, held = 0.62f, rarity = 1.0f, bulletSpeed = 29f });
            Add(new WeaponDef { id = "sniper", sprite = "wpn_sniper", label = "Sniper", kind = WKind.Gun,
                rate = 1.2f, ammo = 12, spread = 0.35f, noise = 30f, sfx = "sniper", pierce = true,
                kick = 0.5f, held = 0.7f, rarity = 1.0f, bulletSpeed = 62f,
                scopeZoom = 3.4f, scopeLead = 13f });
            Add(new WeaponDef { id = "lmg", sprite = "wpn_lmg", label = "Machine Gun", kind = WKind.Gun,
                rate = 10f, auto = true, ammo = 120, spread = 5.5f, noise = 26f, sfx = "lmg",
                kick = 0.2f, held = 0.64f, rarity = 1.0f,
                scopeZoom = 1.1f, scopeLead = 6.5f });
            Add(new WeaponDef { id = "minigun", sprite = "wpn_minigun", label = "Minigun", kind = WKind.Gun,
                rate = 20f, auto = true, ammo = 300, spread = 8f, noise = 30f, sfx = "minigun",
                kick = 0.14f, held = 0.66f, rarity = 0.9f, spinUp = 0.55f });
            Add(new WeaponDef { id = "rpg", sprite = "wpn_rpg", label = "Rocket Launcher", kind = WKind.Gun,
                rate = 0.85f, ammo = 8, spread = 0.6f, noise = 34f, sfx = "gl",
                kick = 0.9f, held = 0.68f, rarity = 1.1f, rocket = true, blast = 3.4f,
                bulletSpeed = 14f, scopeZoom = 1.2f, scopeLead = 7f });
            Add(new WeaponDef { id = "smg", sprite = "wpn_smg", label = "SMG", kind = WKind.Gun,
                rate = 14f, auto = true, ammo = 50, spread = 7f, noise = 17f, sfx = "smg",
                kick = 0.13f, held = 0.46f, rarity = 1.4f });
            Add(new WeaponDef { id = "gl", sprite = "wpn_gl", label = "Grenade Launcher", kind = WKind.Gun,
                rate = 1.6f, ammo = 10, spread = 1.4f, noise = 30f, sfx = "gl",
                kick = 0.6f, held = 0.6f, rarity = 1.0f, rocket = true, blast = 2.6f,
                bulletSpeed = 19f });
            Add(new WeaponDef { id = "autoshotgun", sprite = "wpn_autoshotgun", label = "Auto Shotgun",
                kind = WKind.Gun, rate = 3.2f, auto = true, ammo = 24, pellets = 6, spread = 9f,
                noise = 27f, sfx = "shotgun", kick = 0.42f, held = 0.64f, rarity = 1.0f,
                bulletSpeed = 30f });
            Add(new WeaponDef { id = "akimbo", sprite = "wpn_akimbo", label = "Twin Pistols",
                kind = WKind.Gun, rate = 7f, auto = true, ammo = 40, pellets = 2, spread = 5f,
                noise = 21f, sfx = "pistol", kick = 0.2f, held = 0.46f, rarity = 1.2f });
            Add(new WeaponDef { id = "flamer", sprite = "wpn_flamer", label = "Flamethrower",
                kind = WKind.Gun, rate = 18f, auto = true, ammo = 250, pellets = 3, spread = 13f,
                noise = 12f, sfx = "flamer", kick = 0.09f, held = 0.6f, rarity = 0.8f,
                bulletSpeed = 12f, flame = true, pierce = true });
        }

        public static WeaponDef Get(string id) =>
            id != null && All.TryGetValue(id, out var d) ? d : All["fist"];

        /// Weighted pick so miniguns and rockets stay a treat, not the default find.
        public static string RandomGun(System.Random rng)
        {
            float total = 0f;
            foreach (var id in Guns) total += Get(id).rarity;
            float roll = (float)rng.NextDouble() * total;
            foreach (var id in Guns)
            {
                roll -= Get(id).rarity;
                if (roll <= 0f) return id;
            }
            return Guns[Guns.Count - 1];
        }
    }

    // ========================================================================= weapon
    /// Draws the weapon in the owner's hands and performs the actual attack.
    public class Weapon : MonoBehaviour
    {
        public WeaponDef def;
        public int ammo;
        public Actor owner;

        SpriteRenderer sr;
        float nextShot;
        float swingT;
        bool swingRight;

        public bool IsGun => def.kind == WKind.Gun;
        public bool Empty => IsGun && ammo <= 0;

        public static Weapon Attach(Actor owner, string id, int ammoOverride = -1)
        {
            var go = new GameObject("held");
            go.transform.SetParent(owner.AimRoot, false);
            var w = go.AddComponent<Weapon>();
            w.owner = owner;
            w.def = WeaponDB.Get(id);
            w.ammo = ammoOverride >= 0 ? ammoOverride : w.def.ammo;
            w.sr = go.AddComponent<SpriteRenderer>();
            w.sr.sprite = Art.Get(w.def.sprite);
            w.sr.sortingOrder = Layer.Held;
            go.transform.localPosition = new Vector3(0f, w.def.held, 0f);
            return w;
        }

        void Update()
        {
            if (swingT > 0f)
            {
                swingT -= Time.deltaTime * 7f;
                float k = Mathf.Clamp01(swingT);
                float ang = Mathf.Sin(k * Mathf.PI) * (swingRight ? -62f : 62f);
                float push = Mathf.Sin(k * Mathf.PI) * 0.22f;
                transform.localRotation = Quaternion.Euler(0, 0, ang);
                transform.localPosition = new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad) * 0.2f,
                                                      def.held + push, 0f);
            }
            else
            {
                transform.localRotation = Quaternion.identity;
                transform.localPosition = new Vector3(0f, def.held, 0f);
            }
        }

        public bool Ready => Time.time >= nextShot;

        float spin, lastTrigger;
        public float SpinCharge => def.spinUp > 0f ? Mathf.Clamp01(spin) : 1f;

        /// dir must be normalised. Returns true if an attack actually happened.
        public bool Attack(Vector2 origin, Vector2 dir)
        {
            // barrels have to come up to speed before anything comes out of them
            if (def.spinUp > 0f && IsGun && ammo > 0)
            {
                if (Time.time - lastTrigger > 0.2f) spin = 0f;
                if (spin <= 0f) Sfx.I.Play("spinup", 0.45f);
                lastTrigger = Time.time;
                spin += Time.deltaTime / def.spinUp;
                if (spin < 1f) return false;
            }

            if (!Ready) return false;
            nextShot = Time.time + 1f / def.rate;

            if (IsGun)
            {
                if (ammo <= 0)
                {
                    Sfx.I.Play("deny", 0.5f);
                    return false;
                }
                ammo--;
                FireGun(origin, dir);
            }
            else
            {
                Swing(origin, dir);
            }
            return true;
        }

        void FireGun(Vector2 origin, Vector2 dir)
        {
            Vector2 muzzle = origin + dir * (def.held + 0.45f);

            // hired muscle cannot shoot straight; the player gets the weapon's real accuracy
            float spread = owner.team == Team.Enemy ? def.spread * 2.4f + 4f : def.spread;
            if (owner.team == Team.Player && PlayerCtrl.I != null && PlayerCtrl.I.Scoped)
                spread *= 0.22f;

            if (def.rocket)
            {
                Rocket.Spawn(muzzle, dir, def.bulletSpeed, owner, def);
            }
            else
            {
                for (int i = 0; i < def.pellets; i++)
                {
                    float a = Random.Range(-spread, spread) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(dir.x * Mathf.Cos(a) - dir.y * Mathf.Sin(a),
                                            dir.x * Mathf.Sin(a) + dir.y * Mathf.Cos(a));
                    Bullet.Spawn(muzzle, d, def.bulletSpeed * Random.Range(0.94f, 1.06f), owner, def);
                }
            }
            FX.I.MuzzleFlash(transform, new Vector3(0f, 0.55f, 0f), def.pellets > 1 ? 1.4f : 1f);
            FX.I.Casing(muzzle, new Vector2(-dir.y, dir.x) * Mathf.Sign(Random.value - 0.5f));
            Sfx.I.PlayVaried(def.sfx, owner.team == Team.Player ? 1f : 0.7f);
            Noise.Emit(origin, def.noise, owner);
            if (owner.team == Team.Player)
            {
                CamCtrl.I.Shake(def.kick);
                CamCtrl.I.Punch(def.kick * 0.55f);
            }
        }

        void Swing(Vector2 origin, Vector2 dir)
        {
            swingT = 1f;
            swingRight = !swingRight;
            Sfx.I.PlayVaried(def.sfx, 0.55f);
            Noise.Emit(origin, def.noise, owner);
            if (owner.team == Team.Player) CamCtrl.I.Shake(def.kick * 0.5f);

            bool tiger = owner.team == Team.Player && GameManager.I != null &&
                         GameManager.I.Mask == MaskId.Tiger;
            float reach = tiger ? def.reach * 1.45f : def.reach;

            int mask = owner.team == Team.Player ? Layer.EnemyMask : Layer.PlayerMask;
            var hits = Physics2D.OverlapCircleAll(origin + dir * reach * 0.55f,
                                                  reach * 0.75f, mask);
            Actor best = null;
            float bestD = float.MaxValue;
            foreach (var h in hits)
            {
                var a = h.GetComponentInParent<Actor>();
                if (a == null || a == owner || a.Dead) continue;
                Vector2 to = (Vector2)a.transform.position - origin;
                if (Vector2.Angle(dir, to) > def.arc) continue;
                if (to.magnitude < bestD) { bestD = to.magnitude; best = a; }
            }
            if (best == null) return;

            Vector2 hitDir = ((Vector2)best.transform.position - origin).normalized;
            float knock = tiger ? 0f : def.knockdown;
            if (knock > 0f && Random.value < knock && best.Knockdown(hitDir, 3.5f))
            {
                Sfx.I.PlayVaried("hit", 0.8f);
                FX.I.HitStop(0.035f, 0.08f);
                if (owner.team == Team.Player) CamCtrl.I.Shake(0.25f);
                return;
            }
            Sfx.I.PlayVaried("crunch", 0.7f);
            best.Kill(hitDir, def.id);
        }
    }

    // ========================================================================= bullet
    public class Bullet : MonoBehaviour
    {
        Vector2 dir;
        float speed, life;
        Actor owner;
        WeaponDef def;
        SpriteRenderer sr;

        bool whizzed;

        public static void Spawn(Vector2 pos, Vector2 dir, float speed, Actor owner, WeaponDef def)
        {
            bool flame = def != null && def.flame;
            var srr = Art.NewSprite("bullet",
                                    flame ? "fx_flame_" + Random.Range(0, 3) : "fx_tracer",
                                    Layer.Shot);
            var b = srr.gameObject.AddComponent<Bullet>();
            b.sr = srr;
            b.dir = dir.normalized;
            b.speed = speed;
            b.owner = owner;
            b.def = def;
            b.life = flame ? Random.Range(0.28f, 0.40f) : 2.2f;
            b.transform.position = pos;
            b.transform.rotation = Quaternion.FromToRotation(Vector3.up, b.dir);
            if (flame)
            {
                srr.color = new Color(1f, 1f, 1f, 0.95f);
                srr.transform.localScale = Vector3.one * 1.6f;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }

            float step = speed * dt;
            Vector2 p = transform.position;

            if (def != null && def.flame)
            {
                // fire fans out and thins as it travels
                transform.localScale = Vector3.one * (1.6f + (0.4f - life) * 5.5f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(life * 3.2f));
                speed *= 1f - 2.6f * dt;
            }

            // a round snapping past your head should be audible
            if (!whizzed && owner != null && owner.team == Team.Enemy && PlayerCtrl.I != null &&
                !PlayerCtrl.I.Dead)
            {
                float near = Vector2.Distance(p, PlayerCtrl.I.transform.position);
                if (near < 1.4f)
                {
                    whizzed = true;
                    Sfx.I.PlayVaried("whiz", 0.5f, 0.25f);
                }
            }
            int mask = Layer.WallMask |
                       (owner != null && owner.team == Team.Player ? Layer.EnemyMask : Layer.PlayerMask);
            var hit = Physics2D.CircleCast(p, 0.06f, dir, step, mask);
            if (hit.collider != null)
            {
                var a = hit.collider.GetComponentInParent<Actor>();
                if (a != null && a != owner && !a.Dead)
                {
                    a.Kill(dir, def != null ? def.id : "shot");
                    // a piercing round carries straight on through the body
                    if (def == null || !def.pierce) { Destroy(gameObject); return; }
                    transform.position = p + dir * step;
                    return;
                }
                if (a == null)
                {
                    if (def != null && def.flame)
                    {
                        FX.I.Decal("fx_pool", hit.point, 0.35f, new Color(0.2f, 0.12f, 0.1f, 0.5f));
                    }
                    else
                    {
                        FX.I.BulletHole(hit.point - dir * 0.05f);
                        FX.I.Spark(hit.point);
                        if (Random.value < 0.5f) Sfx.I.PlayVaried("ric", 0.35f, 0.3f);
                    }
                    Destroy(gameObject);
                    return;
                }
            }
            transform.position = p + dir * step;
        }
    }

    // ======================================================================== rockets
    /// Slow, unmissable-looking projectile that levels whatever is standing near it -
    /// including whoever fired it, so do not shoot walls at point blank.
    public class Rocket : MonoBehaviour
    {
        Vector2 dir;
        float speed, life, armTime;
        Actor owner;
        WeaponDef def;

        public static void Spawn(Vector2 pos, Vector2 dir, float speed, Actor owner, WeaponDef def)
        {
            var sr = Art.NewSprite("rocket", "fx_rocket", Layer.Shot);
            var r = sr.gameObject.AddComponent<Rocket>();
            r.dir = dir.normalized;
            r.speed = speed;
            r.owner = owner;
            r.def = def;
            r.life = 4f;
            r.armTime = 0.06f;
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.FromToRotation(Vector3.up, r.dir);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            armTime -= dt;
            if (life <= 0f) { Explode(transform.position); return; }

            Vector2 p = transform.position;
            float step = speed * dt;
            int mask = Layer.WallMask | Layer.EnemyMask | Layer.PlayerMask;
            var hit = Physics2D.CircleCast(p, 0.14f, dir, step, mask);
            if (hit.collider != null)
            {
                var a = hit.collider.GetComponentInParent<Actor>();
                bool ownHit = a != null && a == owner;
                if (!ownHit || armTime <= 0f)
                {
                    Explode(hit.point);
                    return;
                }
            }
            transform.position = p + dir * step;
            if (Random.value < 0.6f)
                FX.I.SpawnDebris("fx_smoke", p, -dir * Random.Range(0.5f, 1.5f), 0.5f, false);
        }

        void Explode(Vector2 p)
        {
            float r = Mathf.Max(def.blast, 1f);
            FX.I.Explosion(p, r);
            Sfx.I.PlayVaried("boom", 1f, 0.08f);
            CamCtrl.I.Shake(1f);
            CamCtrl.I.Punch(1.1f);
            FX.I.HitStop(0.07f, 0.05f);
            Noise.Emit(p, 34f);

            var hits = Physics2D.OverlapCircleAll(p, r, Layer.EnemyMask | Layer.PlayerMask);
            foreach (var h in hits)
            {
                var a = h.GetComponentInParent<Actor>();
                if (a == null || a.Dead) continue;
                Vector2 away = ((Vector2)a.transform.position - p);
                a.Kill(away.sqrMagnitude > 1e-4f ? away.normalized : dir, "rpg");
            }
            Destroy(gameObject);
        }
    }

    // ======================================================================== pickups
    public enum PickupKind { Weapon, Ammo, Medkit }

    /// Something lying on the floor. Walk over it and press E.
    public class Pickup : MonoBehaviour
    {
        public PickupKind kind = PickupKind.Weapon;
        public string id;
        public int ammo;
        static readonly List<Pickup> all = new List<Pickup>();
        public static IReadOnlyList<Pickup> All => all;

        public static Pickup Drop(string id, int ammo, Vector2 pos, float angle)
        {
            var sr = Art.NewSprite("pickup_" + id, WeaponDB.Get(id).sprite, Layer.Pickup);
            var p = sr.gameObject.AddComponent<Pickup>();
            p.id = id;
            p.ammo = ammo;
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            sr.gameObject.layer = Layer.PickupL;
            return p;
        }

        /// Ammo crates and medkits use the same pickup plumbing as weapons.
        public static Pickup Supply(PickupKind kind, Vector2 pos)
        {
            string sprite = kind == PickupKind.Medkit ? "prop_medkit" : "prop_ammo";
            var sr = Art.NewSprite("supply", sprite, Layer.Pickup);
            var p = sr.gameObject.AddComponent<Pickup>();
            p.kind = kind;
            sr.transform.position = pos;
            var col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            sr.gameObject.layer = Layer.PickupL;
            return p;
        }

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        public static void ResetAll() => all.Clear();

        public static Pickup Nearest(Vector2 pos, float maxDist)
        {
            Pickup best = null;
            float bd = maxDist * maxDist;
            for (int i = 0; i < all.Count; i++)
            {
                float d = ((Vector2)all[i].transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = all[i]; }
            }
            return best;
        }
    }

    /// A weapon in flight after being thrown.
    public class Thrown : MonoBehaviour
    {
        Vector2 vel;
        float spin, life;
        Actor owner;
        WeaponDef def;
        int ammo;

        public static void Launch(Actor owner, WeaponDef def, int ammo, Vector2 pos, Vector2 dir)
        {
            var sr = Art.NewSprite("thrown", def.sprite, Layer.Shot);
            var t = sr.gameObject.AddComponent<Thrown>();
            t.owner = owner;
            t.def = def;
            t.ammo = ammo;
            t.vel = dir.normalized * 17f;
            t.spin = Random.value < 0.5f ? -900f : 900f;
            t.life = 1.1f;
            sr.transform.position = pos;
            Sfx.I.PlayVaried("swing", 0.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            transform.Rotate(0, 0, spin * dt);

            Vector2 p = transform.position;
            float step = vel.magnitude * dt;
            int mask = Layer.WallMask |
                       (owner != null && owner.team == Team.Player ? Layer.EnemyMask : Layer.PlayerMask);
            var hit = Physics2D.CircleCast(p, 0.18f, vel.normalized, step, mask);
            if (hit.collider != null)
            {
                var a = hit.collider.GetComponentInParent<Actor>();
                if (a != null && a != owner && !a.Dead)
                {
                    Vector2 d = vel.normalized;
                    if (def.lethalThrow) a.Kill(d, def.id);
                    else if (!a.Knockdown(d, 3.2f)) a.Kill(d, def.id);
                    Sfx.I.PlayVaried("hit", 0.8f);
                    FX.I.HitStop(0.04f, 0.08f);
                    Land(hit.point);
                    return;
                }
                Land(p);
                Sfx.I.PlayVaried("ric", 0.4f, 0.3f);
                return;
            }
            transform.position = p + vel * dt;
            if (life <= 0f) Land(transform.position);
        }

        void Land(Vector2 p)
        {
            Pickup.Drop(def.id, ammo, p, Random.value * 360f);
            Destroy(gameObject);
        }
    }
}
