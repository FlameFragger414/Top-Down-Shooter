using UnityEngine;

namespace Neon
{
    public enum Team { Player, Enemy }

    /// Anything that can be killed. Both sides die in one hit, exactly like the game
    /// this is modelled on - the only difference is what happens afterwards.
    public abstract class Actor : MonoBehaviour
    {
        public Team team;
        public string kit = "thug";
        public bool Dead { get; protected set; }

        /// Transform that faces the aim direction; weapons and sprites hang off it.
        public Transform aimRoot;
        public Transform AimRoot => aimRoot != null ? aimRoot : transform;

        public abstract void Kill(Vector2 dir, string cause);

        /// Non-lethal stagger. Returns true if the actor was actually knocked down.
        public virtual bool Knockdown(Vector2 dir, float seconds) => false;

        public virtual bool CanBeExecuted => false;
    }

    /// Global "someone made a noise here" bus. Enemies subscribe by polling in their
    /// own update; broadcasting through a static keeps the call sites trivial.
    public static class Noise
    {
        public static void Emit(Vector2 pos, float radius, Actor source = null)
        {
            if (radius <= 0f) return;
            var all = Enemy.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || e.Dead) continue;
                float d = Vector2.Distance(e.transform.position, pos);
                if (d <= radius) e.HearNoise(pos, 1f - d / radius);
            }
        }
    }
}
