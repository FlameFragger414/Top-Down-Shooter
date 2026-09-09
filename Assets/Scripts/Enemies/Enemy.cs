using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    public class EnemyType
    {
        public string id, kit, weapon;
        public float speed = 4.3f, patrolSpeed = 1.9f;
        public float fov = 52f, sight = 9.5f;
        public float keepDistance;       // guns hang back at this range
        public bool noKnockdown;
        public int hp = 1;
        public bool dog;
        public int score = 100;
    }

    public static class EnemyDB
    {
        public static readonly Dictionary<string, EnemyType> All = new Dictionary<string, EnemyType>();

        static void A(EnemyType t) => All[t.id] = t;

        static EnemyDB()
        {
            A(new EnemyType { id = "thug", kit = "thug", weapon = "fist", speed = 4.3f, score = 100 });
            A(new EnemyType { id = "bat", kit = "goon", weapon = "bat", speed = 4.2f, score = 120 });
            A(new EnemyType { id = "knife", kit = "goon", weapon = "knife", speed = 4.5f, score = 130 });
            A(new EnemyType { id = "pistol", kit = "suit", weapon = "pistol", speed = 3.4f,
                keepDistance = 7.0f, score = 150 });
            A(new EnemyType { id = "uzi", kit = "suit", weapon = "uzi", speed = 3.2f,
                keepDistance = 7.6f, score = 180 });
            A(new EnemyType { id = "shotgun", kit = "heavy", weapon = "shotgun", speed = 3.0f,
                keepDistance = 5.2f, noKnockdown = true, score = 220 });
            A(new EnemyType { id = "dog", kit = "dog", weapon = "fist", speed = 6.4f,
                patrolSpeed = 3.0f, fov = 60f, sight = 9.5f, dog = true, score = 140 });
            A(new EnemyType { id = "boss", kit = "boss", weapon = "rifle", speed = 3.8f,
                keepDistance = 6.0f, noKnockdown = true, hp = 3, sight = 13f, score = 750 });
        }

        public static EnemyType Get(string id) => All.TryGetValue(id, out var t) ? t : All["thug"];
    }

    public class Enemy : Actor
    {
        public static readonly List<Enemy> All = new List<Enemy>();
        public static int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < All.Count; i++) if (All[i] != null && !All[i].Dead) n++;
                return n;
            }
        }

        enum State { Idle, Patrol, Suspect, Chase, Down }

        public EnemyType type;
        public List<Vector2> patrol = new List<Vector2>();

        Rigidbody2D rb;
        SpriteRenderer sr;
        SpriteRenderer coneSr;
        Weapon weapon;
        State state = State.Idle;
        Vector2 face = Vector2.down;
        Vector2 target;
        int patrolIndex;
        float stateTimer, downTimer, idleLook, alertGlow;
        float phase;   // per-enemy offset so strafing and dodging never sync up
        float reactTimer;   // beat between spotting you and pulling the trigger
        int hp;

        public override bool CanBeExecuted => state == State.Down && !Dead;
        public bool Alerted => state == State.Chase;

        public static Enemy Spawn(string typeId, Vector2 pos, float facingDeg, List<Vector2> route)
        {
            var t = EnemyDB.Get(typeId);
            var go = new GameObject("Enemy_" + typeId);
            go.layer = Layer.EnemyL;
            go.transform.position = pos;

            var e = go.AddComponent<Enemy>();
            e.type = t;
            e.team = Team.Enemy;
            e.kit = t.kit;
            e.hp = t.hp;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            e.rb = rb;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = t.dog ? 0.26f : 0.31f;

            var vis = new GameObject("vis");
            vis.transform.SetParent(go.transform, false);
            e.aimRoot = vis.transform;
            e.sr = vis.AddComponent<SpriteRenderer>();
            e.sr.sprite = Art.Get("char_" + t.kit);
            e.sr.sortingOrder = Layer.Character;

            var cone = new GameObject("cone");
            cone.transform.SetParent(vis.transform, false);
            e.coneSr = cone.AddComponent<SpriteRenderer>();
            e.coneSr.sprite = Art.Get("fx_cone");
            e.coneSr.sortingOrder = Layer.FloorDetail;
            e.coneSr.color = new Color(1f, 0.25f, 0.45f, 0f);
            float coneScale = t.sight / 2f;
            cone.transform.localScale = new Vector3(coneScale, coneScale, 1f);

            e.face = new Vector2(Mathf.Cos(facingDeg * Mathf.Deg2Rad), Mathf.Sin(facingDeg * Mathf.Deg2Rad));
            if (route != null && route.Count > 0)
            {
                e.patrol = route;
                e.state = State.Patrol;
            }
            if (!t.dog) e.weapon = Weapon.Attach(e, t.weapon);
            e.target = pos;
            e.phase = Random.value * 10f;
            All.Add(e);
            return e;
        }

        void OnDestroy() { All.Remove(this); }

        // ------------------------------------------------------------------ senses
        bool CanSee(Vector2 p, out Vector2 dir, out float dist)
        {
            dir = p - (Vector2)transform.position;
            dist = dir.magnitude;
            if (dist > type.sight) return false;
            dir /= Mathf.Max(dist, 1e-4f);
            // chasing widens the cone but does not make them omniscient
            float cone = state == State.Chase ? type.fov + 45f : type.fov;
            if (Vector2.Angle(face, dir) > cone) return false;
            var hit = Physics2D.Raycast(transform.position, dir, dist, Layer.SightBlockMask);
            return hit.collider == null;
        }

        public void HearNoise(Vector2 pos, float strength)
        {
            if (Dead || state == State.Down) return;
            if (state == State.Chase) { target = pos; return; }
            state = State.Suspect;
            target = pos;
            stateTimer = 4.5f + strength * 3f;
            if (strength > 0.55f) Alert(pos);
        }

        public void Alert(Vector2 pos)
        {
            if (Dead || state == State.Down) return;
            bool wasCalm = state != State.Chase;
            state = State.Chase;
            target = pos;
            stateTimer = 7f;
            if (wasCalm)
            {
                alertGlow = 1f;
                reactTimer = Random.Range(0.8f, 1.3f);
                Sfx.I.PlayVaried("alert", 0.28f, 0.25f);
            }
        }

        void ShoutToFriends()
        {
            for (int i = 0; i < All.Count; i++)
            {
                var o = All[i];
                if (o == null || o == this || o.Dead) continue;
                if (Vector2.Distance(o.transform.position, transform.position) < 9f)
                    o.Alert(target);
            }
        }

        // ------------------------------------------------------------------- brain
        void Update()
        {
            if (Dead) return;
            float dt = Time.deltaTime;
            alertGlow = Mathf.Max(0f, alertGlow - dt * 1.6f);
            reactTimer = Mathf.Max(0f, reactTimer - dt);

            if (state == State.Down)
            {
                downTimer -= dt;
                rb.linearVelocity = Vector2.zero;
                if (downTimer <= 0f)
                {
                    state = State.Chase;
                    stateTimer = 6f;
                    sr.transform.localScale = Vector3.one;
                    sr.color = Color.white;
                    if (PlayerCtrl.I != null) target = PlayerCtrl.I.transform.position;
                }
                UpdateVisual();
                return;
            }

            var player = PlayerCtrl.I;
            bool sees = false;
            Vector2 toPlayer = Vector2.zero;
            float dist = 999f;
            if (player != null && !player.Dead)
                sees = CanSee(player.transform.position, out toPlayer, out dist);

            if (sees)
            {
                if (state != State.Chase) ShoutToFriends();
                state = State.Chase;
                stateTimer = 6.5f;
                target = player.transform.position;
            }

            Vector2 move = Vector2.zero;
            switch (state)
            {
                case State.Idle:
                    idleLook -= dt;
                    if (idleLook <= 0f)
                    {
                        idleLook = Random.Range(1.4f, 3.2f);
                        float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        face = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    }
                    break;

                case State.Patrol:
                    if (patrol.Count > 0)
                    {
                        Vector2 wp = patrol[patrolIndex];
                        if (Vector2.Distance(transform.position, wp) < 0.5f)
                            patrolIndex = (patrolIndex + 1) % patrol.Count;
                        move = Steer(wp) * type.patrolSpeed;
                    }
                    break;

                case State.Suspect:
                    stateTimer -= dt;
                    move = Steer(target) * (type.patrolSpeed * 1.5f);
                    if (Vector2.Distance(transform.position, target) < 0.7f || stateTimer <= 0f)
                    {
                        state = patrol.Count > 0 ? State.Patrol : State.Idle;
                        idleLook = 0f;
                    }
                    break;

                case State.Chase:
                    stateTimer -= dt;
                    if (stateTimer <= 0f && !sees)
                    {
                        state = patrol.Count > 0 ? State.Patrol : State.Idle;
                        break;
                    }
                    if (sees) target = player.transform.position;

                    if (type.dog || type.keepDistance <= 0f)
                    {
                        move = Steer(target) * type.speed;
                        if (sees && dist < (type.dog ? 0.95f : 1.05f)) TryAttack(toPlayer);
                    }
                    else
                    {
                        if (dist > type.keepDistance + 1.2f || !sees) move = Steer(target) * type.speed;
                        else if (dist < type.keepDistance - 1.4f) move = -Steer(target) * type.speed * 0.75f;
                        else move = Strafe(toPlayer) * type.speed * 0.6f;
                        if (sees) TryAttack(toPlayer);
                    }
                    break;
            }

            rb.linearVelocity = move;
            if (move.sqrMagnitude > 0.05f && state != State.Chase) face = move.normalized;
            else if (state == State.Chase)
            {
                Vector2 want = sees ? toPlayer : (target - (Vector2)transform.position).normalized;
                if (want.sqrMagnitude > 0.001f)
                    face = Vector2.Lerp(face, want, 1f - Mathf.Exp(-14f * dt)).normalized;
            }
            UpdateVisual();
        }

        void TryAttack(Vector2 dir)
        {
            if (reactTimer > 0f) return;
            // nobody opens fire in the first moment of a floor
            if (GameManager.I != null && GameManager.I.FloorAge < 1.8f) return;
            if (type.dog)
            {
                if (PlayerCtrl.I != null && !PlayerCtrl.I.Dead &&
                    Vector2.Distance(transform.position, PlayerCtrl.I.transform.position) < 0.95f)
                {
                    Sfx.I.PlayVaried("hit", 0.7f);
                    PlayerCtrl.I.Kill(dir, "dog");
                }
                return;
            }
            if (weapon == null) return;
            if (Vector2.Angle(face, dir) > 22f) return;
            weapon.Attack(transform.position, face);
        }

        Vector2 Steer(Vector2 to)
        {
            Vector2 pos = transform.position;
            Vector2 d = to - pos;
            if (d.sqrMagnitude < 1e-4f) return Vector2.zero;
            d.Normalize();

            // whisker avoidance: if the direct line is blocked, slide around the corner
            var hit = Physics2D.CircleCast(pos, 0.34f, d, 1.3f, Layer.WallMask);
            if (hit.collider != null)
            {
                Vector2 left = new Vector2(-d.y, d.x);
                bool leftFree = Physics2D.CircleCast(pos, 0.32f, left, 1.1f, Layer.WallMask).collider == null;
                bool rightFree = Physics2D.CircleCast(pos, 0.32f, -left, 1.1f, Layer.WallMask).collider == null;
                if (leftFree && !rightFree) d = (d + left * 1.8f).normalized;
                else if (rightFree && !leftFree) d = (d - left * 1.8f).normalized;
                else d = (d + left * (Mathf.PingPong(Time.time * 0.7f + phase, 2f) > 1f ? 1.6f : -1.6f)).normalized;
            }
            return d;
        }

        Vector2 Strafe(Vector2 toPlayer)
        {
            Vector2 side = new Vector2(-toPlayer.y, toPlayer.x);
            float s = Mathf.Sin(Time.time * 0.9f + phase * 3.7f);
            return (side * Mathf.Sign(s)).normalized;
        }

        void UpdateVisual()
        {
            aimRoot.rotation = Quaternion.FromToRotation(Vector3.up, face);
            if (coneSr != null)
            {
                bool show = GameManager.I != null && GameManager.I.Mask == MaskId.Owl &&
                            state != State.Down;
                float a = show ? (state == State.Chase ? 0.20f : 0.10f) : 0f;
                a = Mathf.Max(a, alertGlow * 0.28f);
                coneSr.color = new Color(1f, 0.28f, 0.42f, a);
            }
        }

        // -------------------------------------------------------------------- damage
        public override bool Knockdown(Vector2 dir, float seconds)
        {
            if (Dead || type.noKnockdown || type.dog) return false;
            if (state == State.Down) return false;
            state = State.Down;
            downTimer = seconds;
            rb.linearVelocity = dir * 4f;
            sr.transform.localScale = new Vector3(1f, 0.72f, 1f);
            sr.color = new Color(0.8f, 0.78f, 0.85f);
            Alert(transform.position);
            ShoutToFriends();
            return true;
        }

        public override void Kill(Vector2 dir, string cause)
        {
            if (Dead) return;
            hp--;
            if (hp > 0)
            {
                FX.I.BloodSpray(transform.position, dir, 0.5f);
                Sfx.I.PlayVaried("hit", 0.7f);
                Alert(transform.position);
                return;
            }

            Dead = true;
            All.Remove(this);
            if (weapon != null)
            {
                Pickup.Drop(weapon.def.id, weapon.ammo,
                            (Vector2)transform.position + Random.insideUnitCircle * 0.5f,
                            Random.value * 360f);
                Destroy(weapon.gameObject);
            }
            FX.I.BloodSpray(transform.position, dir, cause == "shotgun" ? 2.0f : 1.2f);
            Sfx.I.PlayVaried("scream" + Random.Range(0, 3), 0.55f, 0.18f);
            FX.I.Corpse(kit, transform.position,
                        Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f, dir * 6f);
            Sfx.I.PlayVaried("splat", 0.8f);
            FX.I.HitStop(cause == "execute" ? 0.08f : 0.045f, 0.05f);
            CamCtrl.I.Shake(0.22f);
            ShoutToFriends();
            GameManager.I.OnEnemyKilled(type.score, cause);
            Destroy(gameObject);
        }
    }
}
