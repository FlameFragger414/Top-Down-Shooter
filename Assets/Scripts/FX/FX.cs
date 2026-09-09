using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    /// Gore, debris, decals, hit-stop and screen flashes. Decals are pooled and recycled
    /// oldest-first so a long firefight never leaks renderers.
    public class FX : MonoBehaviour
    {
        public static FX I;

        const int MaxDecals = 700;
        readonly Queue<SpriteRenderer> decalPool = new Queue<SpriteRenderer>();
        readonly List<SpriteRenderer> liveDecals = new List<SpriteRenderer>();
        Transform decalRoot, debrisRoot, corpseRoot;

        struct Debris
        {
            public Transform tr;
            public Vector2 vel;
            public float spin, life, maxLife, drag;
            public SpriteRenderer sr;
            public bool bleeds;
        }
        readonly List<Debris> debris = new List<Debris>();

        public static void Create(Transform parent)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<FX>();
            I.decalRoot = new GameObject("Decals").transform;
            I.debrisRoot = new GameObject("Debris").transform;
            I.corpseRoot = new GameObject("Corpses").transform;
            I.decalRoot.SetParent(go.transform, false);
            I.debrisRoot.SetParent(go.transform, false);
            I.corpseRoot.SetParent(go.transform, false);
        }

        public void ClearAll()
        {
            foreach (var d in liveDecals) { d.gameObject.SetActive(false); decalPool.Enqueue(d); }
            liveDecals.Clear();
            foreach (var d in debris) if (d.tr) Destroy(d.tr.gameObject);
            debris.Clear();
            for (int i = corpseRoot.childCount - 1; i >= 0; i--)
                Destroy(corpseRoot.GetChild(i).gameObject);
        }

        // ------------------------------------------------------------------ decals
        SpriteRenderer TakeDecal()
        {
            SpriteRenderer sr;
            if (liveDecals.Count >= MaxDecals)
            {
                sr = liveDecals[0];
                liveDecals.RemoveAt(0);
            }
            else if (decalPool.Count > 0) sr = decalPool.Dequeue();
            else
            {
                var go = new GameObject("decal");
                go.transform.SetParent(decalRoot, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.gameObject.SetActive(true);
            liveDecals.Add(sr);
            return sr;
        }

        public void Decal(string sprite, Vector2 pos, float scale, Color col, int order = Layer.Decal)
        {
            var sr = TakeDecal();
            sr.sprite = Art.Get(sprite);
            sr.color = col;
            sr.sortingOrder = order;
            var t = sr.transform;
            t.position = pos;
            t.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
            t.localScale = Vector3.one * scale;
        }

        public void BloodSpray(Vector2 pos, Vector2 dir, float power = 1f)
        {
            int n = Mathf.RoundToInt(Random.Range(2f, 4f) * power);
            for (int i = 0; i < n; i++)
            {
                Vector2 off = dir.normalized * Random.Range(0.1f, 1.5f * power) +
                              Random.insideUnitCircle * 0.7f;
                Decal("fx_blood_" + Random.Range(0, 6), pos + off,
                      Random.Range(0.6f, 1.25f) * power,
                      new Color(1f, 1f, 1f, Random.Range(0.75f, 1f)));
            }
            for (int i = 0; i < Mathf.RoundToInt(4 * power); i++)
                SpawnDebris("fx_gore_" + Random.Range(0, 4), pos,
                            dir.normalized * Random.Range(2f, 8f) + Random.insideUnitCircle * 4f,
                            Random.Range(0.5f, 1.1f), true);
        }

        public void BulletHole(Vector2 pos)
        {
            Decal("fx_hole", pos, 1f, new Color(1f, 1f, 1f, 0.85f));
        }

        public void Corpse(string kit, Vector2 pos, float angle, Vector2 shove)
        {
            var sr = Art.NewSprite("corpse", "corpse_" + kit, Layer.Corpse, corpseRoot);
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            Decal("fx_pool", pos, Random.Range(0.8f, 1.15f), new Color(1f, 1f, 1f, 0.9f));
            StartCoroutine(SlideCorpse(sr.transform, shove));
        }

        IEnumerator SlideCorpse(Transform t, Vector2 vel)
        {
            float life = 0.45f;
            while (life > 0f && t != null)
            {
                life -= Time.deltaTime;
                vel *= 1f - 8f * Time.deltaTime;
                var step = (Vector3)(vel * Time.deltaTime);
                if (!Physics2D.OverlapCircle(t.position + step, 0.35f, Layer.WallMask))
                    t.position += step;
                if (Random.value < 0.25f)
                    Decal("fx_blood_" + Random.Range(0, 6), t.position, 0.5f,
                          new Color(1f, 1f, 1f, 0.5f));
                yield return null;
            }
        }

        // ------------------------------------------------------------------ debris
        public void SpawnDebris(string sprite, Vector2 pos, Vector2 vel, float scale, bool bleeds)
        {
            var sr = Art.NewSprite("bit", sprite, Layer.Debris, debrisRoot);
            sr.transform.position = pos;
            sr.transform.localScale = Vector3.one * scale;
            debris.Add(new Debris
            {
                tr = sr.transform,
                sr = sr,
                vel = vel,
                spin = Random.Range(-900f, 900f),
                life = 0f,
                maxLife = Random.Range(0.5f, 1.1f),
                drag = Random.Range(5f, 9f),
                bleeds = bleeds
            });
        }

        public void Casing(Vector2 pos, Vector2 dir)
        {
            SpawnDebris("fx_shell", pos, dir * Random.Range(3f, 6f), 1f, false);
            if (Random.value < 0.4f) Sfx.I.PlayVaried("shell", 0.25f, 0.3f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = debris.Count - 1; i >= 0; i--)
            {
                var d = debris[i];
                if (d.tr == null) { debris.RemoveAt(i); continue; }
                d.life += dt;
                d.vel *= 1f - d.drag * dt;
                d.tr.position += (Vector3)(d.vel * dt);
                d.tr.Rotate(0, 0, d.spin * dt);
                if (d.bleeds && Random.value < 0.35f)
                    Decal("fx_blood_" + Random.Range(0, 6), d.tr.position, 0.35f,
                          new Color(1f, 1f, 1f, 0.55f));
                if (d.life >= d.maxLife)
                {
                    // settle: leave a permanent mark where it landed
                    if (d.bleeds)
                        Decal("fx_blood_" + Random.Range(0, 6), d.tr.position, 0.5f, Color.white);
                    else
                        Decal(d.sr.sprite.name, d.tr.position, 1f, Color.white, Layer.Debris);
                    Destroy(d.tr.gameObject);
                    debris.RemoveAt(i);
                    continue;
                }
                debris[i] = d;
            }
        }

        // ------------------------------------------------------------------ feel
        public void HitStop(float duration, float scale = 0.02f)
        {
            StartCoroutine(HitStopCo(duration, scale));
        }

        IEnumerator HitStopCo(float duration, float scale)
        {
            if (GameManager.I != null && GameManager.I.Paused) yield break;
            float prev = Time.timeScale;
            Time.timeScale = scale;
            float t = 0f;
            while (t < duration) { t += Time.unscaledDeltaTime; yield return null; }
            if (Mathf.Approximately(Time.timeScale, scale)) Time.timeScale = prev;
        }

        public void MuzzleFlash(Transform parent, Vector3 localPos, float scale)
        {
            var sr = Art.NewSprite("flash", "fx_muzzle_" + Random.Range(0, 3), Layer.Muzzle, parent);
            sr.transform.localPosition = localPos;
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one * scale * Random.Range(0.85f, 1.2f);
            sr.color = new Color(1f, 0.95f, 0.75f, 1f);
            Destroy(sr.gameObject, 0.055f);
        }

        public void Explosion(Vector2 pos, float radius)
        {
            Decal("fx_pool", pos, radius * 0.55f, new Color(0.16f, 0.13f, 0.18f, 0.9f));
            for (int i = 0; i < 14; i++)
            {
                Vector2 v = Random.insideUnitCircle.normalized * Random.Range(6f, 16f);
                bool chunk = Random.value < 0.75f;
                SpawnDebris(chunk ? "fx_gore_" + Random.Range(0, 4) : "fx_shell",
                            pos, v, Random.Range(0.6f, 1.3f), chunk);
            }
            for (int i = 0; i < 6; i++)
                SpawnDebris("fx_smoke", pos + Random.insideUnitCircle * radius * 0.5f,
                            Random.insideUnitCircle * 2f, Random.Range(0.8f, 1.6f), false);
            StartCoroutine(ExplosionCo(pos, radius));
        }

        IEnumerator ExplosionCo(Vector2 pos, float radius)
        {
            var sr = Art.NewSprite("boom", "fx_explosion_0", Layer.Muzzle, debrisRoot);
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
            // the frames are 96px at 32 PPU, so three world units across
            for (int i = 0; i < 4; i++)
            {
                sr.sprite = Art.Get("fx_explosion_" + i);
                sr.transform.localScale = Vector3.one * (radius * 2f / 3f) * (0.7f + i * 0.16f);
                sr.color = new Color(1f, 1f, 1f, 1f - i * 0.16f);
                yield return new WaitForSecondsRealtime(0.055f);
            }
            Destroy(sr.gameObject);
        }

        public void Spark(Vector2 pos)
        {
            var sr = Art.NewSprite("spark", "fx_spark", Layer.Muzzle, debrisRoot);
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0, 0, Random.value * 360f);
            Destroy(sr.gameObject, 0.07f);
        }
    }
}
