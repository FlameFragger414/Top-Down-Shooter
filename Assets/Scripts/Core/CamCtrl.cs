using UnityEngine;

namespace Neon
{
    /// Follows the player, leans toward the cursor so you can see what you are aiming at,
    /// and owns the trauma-based screen shake.
    public class CamCtrl : MonoBehaviour
    {
        public static CamCtrl I;

        public Transform target;
        public float baseSize = 8.6f;

        Camera cam;
        float trauma;
        float sizePunch;
        float scopeAdd;
        Vector2 shakeOffset;
        float noiseSeed;

        void Awake()
        {
            I = this;
            cam = GetComponent<Camera>();
            noiseSeed = Random.value * 100f;
        }

        public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        public void Punch(float amount) => sizePunch = Mathf.Max(sizePunch, amount);

        public void SnapTo(Vector3 p)
        {
            transform.position = new Vector3(p.x, p.y, -10f);
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);

            // scoping runs the camera down the sightline and pulls back a little
            var pc = PlayerCtrl.I;
            bool scoped = pc != null && pc.Scoped && pc.weapon != null;
            float leadCap = scoped ? pc.weapon.def.scopeLead : 3.6f;
            float leadMul = scoped ? 0.9f : 0.34f;
            float wantScope = scoped ? pc.weapon.def.scopeZoom : 0f;
            scopeAdd = Mathf.Lerp(scopeAdd, wantScope, 1f - Mathf.Exp(-8f * dt));

            Vector3 want = transform.position;
            if (target != null)
            {
                Vector3 mouse = ScreenToWorld(InputHub.MouseScreen);
                Vector3 lead = (mouse - target.position) * leadMul;
                if (lead.magnitude > leadCap) lead = lead.normalized * leadCap;
                want = target.position + lead;
            }
            want.z = -10f;

            Vector3 pos = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-11f * dt));

            trauma = Mathf.Max(0f, trauma - dt * 2.4f);
            float s = trauma * trauma;
            float t = Time.unscaledTime * 27f;
            shakeOffset = new Vector2(
                (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f,
                (Mathf.PerlinNoise(noiseSeed + 11f, t) - 0.5f) * 2f) * (s * 0.6f);

            transform.position = pos + (Vector3)shakeOffset;
            transform.rotation = Quaternion.Euler(0, 0, (Mathf.PerlinNoise(noiseSeed + 31f, t) - 0.5f) * s * 2.2f);

            sizePunch = Mathf.Lerp(sizePunch, 0f, 1f - Mathf.Exp(-7f * dt));
            cam.orthographicSize = baseSize + scopeAdd - sizePunch;
        }

        public Vector3 ScreenToWorld(Vector2 screen)
        {
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -transform.position.z));
            w.z = 0f;
            return w;
        }
    }
}
