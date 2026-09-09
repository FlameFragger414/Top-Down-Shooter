using UnityEngine;

namespace Neon
{
    /// A door you kick open. Anyone standing on the far side eats the slab and goes down.
    public class Door : MonoBehaviour
    {
        SpriteRenderer sr;
        BoxCollider2D block;
        bool open;
        float closeTimer;
        float openAngle;
        float angle;
        bool horizontal;

        public static Door Spawn(Vector2 pos, bool horizontalOpening, Transform parent)
        {
            var sr = Art.NewSprite("door", "prop_door", Layer.Prop, parent);
            var d = sr.gameObject.AddComponent<Door>();
            d.sr = sr;
            d.horizontal = horizontalOpening;
            sr.transform.position = pos + (horizontalOpening ? new Vector2(-0.5f, 0f) : new Vector2(0f, -0.5f));
            sr.transform.rotation = Quaternion.Euler(0, 0, horizontalOpening ? 0f : 90f);

            var go = sr.gameObject;
            go.layer = Layer.WallL;
            d.block = go.AddComponent<BoxCollider2D>();
            d.block.size = new Vector2(1.05f, 0.32f);
            d.block.offset = new Vector2(0.5f, 0f);

            var trig = go.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            trig.size = new Vector2(1.6f, 1.5f);
            trig.offset = new Vector2(0.5f, 0f);
            return d;
        }

        void OnTriggerEnter2D(Collider2D other) => TryKick(other);
        void OnTriggerStay2D(Collider2D other) => TryKick(other);

        void TryKick(Collider2D other)
        {
            if (open) { closeTimer = 1.4f; return; }
            var actor = other.GetComponentInParent<Actor>();
            if (actor == null || actor.Dead) return;
            if (actor.team == Team.Player)
            {
                var rb = other.attachedRigidbody;
                float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
                Kick(speed > 3f);
            }
            else
            {
                Kick(false);
            }
        }

        public void Kick(bool violent)
        {
            open = true;
            closeTimer = 1.6f;
            block.enabled = false;
            openAngle = Random.value < 0.5f ? -96f : 96f;
            Sfx.I.PlayVaried(violent ? "kick" : "hit", violent ? 0.85f : 0.35f);
            if (!violent) return;

            Noise.Emit(transform.position, 12f);
            CamCtrl.I.Shake(0.3f);
            Vector2 swing = transform.position + transform.right * 0.9f;
            var hits = Physics2D.OverlapCircleAll(swing, 0.95f, Layer.EnemyMask);
            foreach (var h in hits)
            {
                var e = h.GetComponentInParent<Enemy>();
                if (e == null || e.Dead) continue;
                Vector2 dir = ((Vector2)e.transform.position - (Vector2)transform.position).normalized;
                if (!e.Knockdown(dir, 3.5f)) e.Kill(dir, "door");
                FX.I.HitStop(0.05f, 0.06f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (open)
            {
                closeTimer -= dt;
                angle = Mathf.Lerp(angle, openAngle, 1f - Mathf.Exp(-18f * dt));
                if (closeTimer <= 0f)
                {
                    open = false;
                    block.enabled = true;
                }
            }
            else
            {
                angle = Mathf.Lerp(angle, 0f, 1f - Mathf.Exp(-9f * dt));
            }
            float baseAngle = horizontal ? 0f : 90f;
            sr.transform.rotation = Quaternion.Euler(0, 0, baseAngle + angle);
        }
    }
}
