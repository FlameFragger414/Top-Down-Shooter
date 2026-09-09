using System;
using UnityEngine;

namespace Neon
{
    /// Everything you hear is generated at runtime - there are no audio files in the project.
    /// Synth builds raw sample buffers; Sfx and MusicPlayer wrap them in AudioClips.
    public static class Synth
    {
        public const int SR = 44100;

        // ---------------------------------------------------------------- oscillators
        public static float Saw(float p) => 2f * (p - Mathf.Floor(p + 0.5f));
        public static float Square(float p, float duty = 0.5f) => (p - Mathf.Floor(p)) < duty ? 1f : -1f;
        public static float Tri(float p) => 1f - 4f * Mathf.Abs(Mathf.Repeat(p, 1f) - 0.5f);
        public static float Sine(float p) => Mathf.Sin(p * Mathf.PI * 2f);

        public static float Env(float t, float len, float a, float d, float s, float r)
        {
            if (t < a) return t / Mathf.Max(a, 1e-5f);
            if (t < a + d) return Mathf.Lerp(1f, s, (t - a) / Mathf.Max(d, 1e-5f));
            if (t < len - r) return s;
            return Mathf.Lerp(s, 0f, Mathf.Clamp01((t - (len - r)) / Mathf.Max(r, 1e-5f)));
        }

        public static float Note(int semitoneFromA4) => 440f * Mathf.Pow(2f, semitoneFromA4 / 12f);

        // ---------------------------------------------------------------- filters
        public struct LP
        {
            float z;
            public float Run(float x, float cut)
            {
                float a = Mathf.Clamp01(cut);
                z += a * (x - z);
                return z;
            }
        }

        public struct HP
        {
            float z;
            public float Run(float x, float cut)
            {
                float a = Mathf.Clamp01(cut);
                z += a * (x - z);
                return x - z;
            }
        }

        static AudioClip Make(string name, float[] buf)
        {
            var clip = AudioClip.Create(name, buf.Length, 1, SR, false);
            clip.SetData(buf, 0);
            return clip;
        }

        // ---------------------------------------------------------------- sfx builders
        public static AudioClip Gunshot(string name, float len, float body, float bright, float seed)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            var rng = new System.Random((int)(seed * 1000));
            var lp = new LP();
            var hp = new HP();
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float e = Mathf.Exp(-t * (14f / len));
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float f = body * Mathf.Exp(-t * 26f);
                phase += f / SR;
                float tone = Square(phase, 0.35f) * 0.6f + Sine(phase) * 0.5f;
                float x = noise * bright + tone;
                x = lp.Run(x, Mathf.Clamp01(0.06f + e * 0.85f));
                x = hp.Run(x, 0.02f);
                buf[i] = Mathf.Clamp(x * e * 1.4f, -1f, 1f);
            }
            return Make(name, buf);
        }

        public static AudioClip Whoosh(string name, float len, float cut)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            var rng = new System.Random(7);
            var lp = new LP();
            var hp = new HP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float sweep = Mathf.Sin(t * Mathf.PI);
                float x = lp.Run(noise, cut * (0.25f + sweep));
                x = hp.Run(x, 0.08f);
                buf[i] = Mathf.Clamp(x * sweep * 0.9f, -1f, 1f);
            }
            return Make(name, buf);
        }

        public static AudioClip Thud(string name, float len, float f0, float f1)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            var rng = new System.Random(13);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t * t);
                phase += f / SR;
                float e = Mathf.Exp(-t * 7f);
                float x = Sine(phase) * 0.9f + (float)(rng.NextDouble() * 2 - 1) * 0.25f * e;
                buf[i] = Mathf.Clamp(x * e, -1f, 1f);
            }
            return Make(name, buf);
        }

        public static AudioClip Splat(string name, float len)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            var rng = new System.Random(29);
            var lp = new LP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float e = Mathf.Exp(-t * 9f);
                float x = lp.Run(noise, Mathf.Lerp(0.6f, 0.04f, t));
                x += Sine(t * 90f) * 0.3f * e;
                buf[i] = Mathf.Clamp(x * e * 1.6f, -1f, 1f);
            }
            return Make(name, buf);
        }

        public static AudioClip Blip(string name, float len, float f0, float f1, float duty = 0.5f)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / SR;
                float e = Env(t * len, len, 0.005f, 0.02f, 0.7f, len * 0.5f);
                buf[i] = Square(phase, duty) * e * 0.5f;
            }
            return Make(name, buf);
        }

        public static AudioClip Tink(string name, float len, float f)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            float p = 0f, p2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                p += f / SR; p2 += f * 2.71f / SR;
                float e = Mathf.Exp(-t * 18f);
                buf[i] = (Sine(p) * 0.6f + Sine(p2) * 0.4f) * e * 0.5f;
            }
            return Make(name, buf);
        }

        public static AudioClip Shell(string name)
        {
            int n = (int)(SR * 0.22f);
            var buf = new float[n];
            var rng = new System.Random(5);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                // three little bounces
                float e = Mathf.Exp(-t * 10f) *
                          (1f + 0.6f * Mathf.Exp(-Mathf.Abs(t - 0.35f) * 60f)
                             + 0.4f * Mathf.Exp(-Mathf.Abs(t - 0.62f) * 80f));
                float x = Sine(t * 2600f) * 0.5f + (float)(rng.NextDouble() * 2 - 1) * 0.4f;
                buf[i] = Mathf.Clamp(x * e * 0.35f, -1f, 1f);
            }
            return Make(name, buf);
        }

        /// Rifle-style crack: hard transient, long slapback tail.
        public static AudioClip Crack(string name, float len, float f0, float tail)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            var rng = new System.Random(41);
            var hp = new HP();
            var lp = new LP();
            float phase = 0f;
            int delay = (int)(SR * 0.055f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float e = Mathf.Exp(-t * 40f);
                float body = Mathf.Exp(-t * 9f) * tail;
                phase += Mathf.Lerp(f0, f0 * 0.25f, Mathf.Clamp01(t * 30f)) / SR;
                float x = hp.Run((float)(rng.NextDouble() * 2 - 1), 0.5f) * (e * 1.4f + body * 0.5f);
                x += Sine(phase) * e * 0.8f;
                x = lp.Run(x, 0.65f);
                if (i >= delay) x += buf[i - delay] * 0.33f;
                buf[i] = Mathf.Clamp(x, -1f, 1f);
            }
            return Make(name, buf);
        }

        /// Short vocal yelp: two detuned formants sliding down under a noise breath.
        public static AudioClip Scream(string name, float len, float f0, int seed)
        {
            int n = (int)(SR * len);
            var buf = new float[n];
            var rng = new System.Random(seed);
            var lp = new LP();
            float p1 = 0f, p2 = 0f, p3 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = f0 * Mathf.Lerp(1.15f, 0.45f, t * t);
                float vib = 1f + Mathf.Sin(t * len * 34f) * 0.035f;
                p1 += f * vib / SR;
                p2 += f * 2.02f * vib / SR;
                p3 += f * 3.05f * vib / SR;
                float e = Env(t * len, len, 0.02f, 0.08f, 0.55f, len * 0.5f);
                float x = Saw(p1) * 0.5f + Sine(p2) * 0.3f + Sine(p3) * 0.16f;
                x += (float)(rng.NextDouble() * 2 - 1) * 0.14f;
                buf[i] = Mathf.Clamp(lp.Run(x, 0.34f) * e * 0.62f, -1f, 1f);
            }
            return Make(name, buf);
        }

        /// Wet bone crunch for melee kills.
        public static AudioClip Crunch(string name)
        {
            int n = (int)(SR * 0.26f);
            var buf = new float[n];
            var rng = new System.Random(77);
            var lp = new LP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float grain = (i / 220) % 2 == 0 ? 1f : 0.35f;   // stuttered texture
                float x = (float)(rng.NextDouble() * 2 - 1) * grain;
                x = lp.Run(x, Mathf.Lerp(0.55f, 0.06f, t));
                x += Sine(t * 130f) * 0.4f * Mathf.Exp(-t * 12f);
                buf[i] = Mathf.Clamp(x * Mathf.Exp(-t * 8f) * 1.5f, -1f, 1f);
            }
            return Make(name, buf);
        }

        /// Round snapping past your ear.
        public static AudioClip Whiz(string name)
        {
            int n = (int)(SR * 0.18f);
            var buf = new float[n];
            var rng = new System.Random(101);
            var lp = new LP();
            var hp = new HP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float x = (float)(rng.NextDouble() * 2 - 1);
                x = hp.Run(x, 0.55f);
                x = lp.Run(x, Mathf.Lerp(0.9f, 0.25f, t));      // doppler drop
                buf[i] = Mathf.Clamp(x * Mathf.Sin(t * Mathf.PI) * 0.5f, -1f, 1f);
            }
            return Make(name, buf);
        }

        /// Continuous fuel roar for the flamethrower.
        public static AudioClip Flame(string name)
        {
            int n = (int)(SR * 0.3f);
            var buf = new float[n];
            var rng = new System.Random(202);
            var lp = new LP();
            var hp = new HP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float x = (float)(rng.NextDouble() * 2 - 1);
                x = lp.Run(x, 0.28f) * 1.6f;
                x += hp.Run((float)(rng.NextDouble() * 2 - 1), 0.7f) * 0.3f;
                buf[i] = Mathf.Clamp(x * Mathf.Sin(t * Mathf.PI) * 0.65f, -1f, 1f);
            }
            return Make(name, buf);
        }

        public static AudioClip Footstep(string name)
        {
            int n = (int)(SR * 0.09f);
            var buf = new float[n];
            var rng = new System.Random(303);
            var lp = new LP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float x = lp.Run((float)(rng.NextDouble() * 2 - 1), 0.20f) * 2.2f;
                buf[i] = Mathf.Clamp(x * Mathf.Exp(-t * 22f) * 0.4f, -1f, 1f);
            }
            return Make(name, buf);
        }

        /// Arpeggio used for the clear fanfare and the death sting.
        public static AudioClip Arp(string name, int[] semitones, float noteLen, float duty)
        {
            int n = (int)(SR * noteLen * semitones.Length);
            var buf = new float[n];
            for (int s = 0; s < semitones.Length; s++)
            {
                float f = Note(semitones[s]);
                int start = (int)(s * noteLen * SR);
                float ph = 0f;
                for (int i = 0; i < noteLen * SR && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    ph += f / SR;
                    float e = Env(t, noteLen, 0.006f, 0.05f, 0.55f, noteLen * 0.45f);
                    buf[start + i] += (Square(ph, duty) * 0.45f + Tri(ph * 2f) * 0.2f) * e * 0.5f;
                }
            }
            for (int i = 0; i < n; i++) buf[i] = Mathf.Clamp(buf[i], -1f, 1f);
            return Make(name, buf);
        }

        // ---------------------------------------------------------------- music
        /// Driving synth loop: kick, snare, hats, saw bass, arpeggiated lead with echo.
        public static AudioClip Track(int seed, float bpm, out float loopSeconds)
        {
            var rng = new System.Random(seed);
            const int steps = 128;                 // 8 bars of 16ths
            float stepDur = 60f / bpm / 4f;
            loopSeconds = steps * stepDur;
            int n = (int)(SR * loopSeconds);
            var buf = new float[n];

            // minor scale degrees, rooted low
            int[] scale = { 0, 2, 3, 5, 7, 8, 10 };
            int root = -17 + rng.Next(0, 5);       // around D2..F#2
            int[] prog = { 0, 0, 5, 5, 3, 3, 4, 4 };

            // --- bass: root/fifth 16th pulse with occasional octave jump
            var bLp = new LP();
            for (int s = 0; s < steps; s++)
            {
                int bar = s / 16;
                int deg = prog[bar % prog.Length];
                int semi = root + scale[deg % 7] + (deg >= 7 ? 12 : 0);
                bool on = (s % 2 == 0) || rng.NextDouble() < 0.22;
                if (!on) continue;
                if (rng.NextDouble() < 0.12) semi += 12;
                float f = Note(semi);
                float len = stepDur * 0.9f;
                int start = (int)(s * stepDur * SR);
                float ph = 0f;
                for (int i = 0; i < len * SR && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    ph += f / SR;
                    float e = Env(t, len, 0.002f, 0.05f, 0.55f, 0.05f);
                    float x = Saw(ph) * 0.6f + Square(ph * 0.5f, 0.5f) * 0.25f;
                    x = bLp.Run(x, 0.10f + 0.5f * Mathf.Exp(-t * 30f));
                    buf[start + i] += x * e * 0.34f;
                }
            }

            // --- lead arp with feedback delay
            int delaySamples = (int)(stepDur * 3f * SR);
            var lead = new float[n];
            int[] arp = { 0, 4, 7, 11, 7, 4 };
            for (int s = 0; s < steps; s++)
            {
                if (s % 2 == 1) continue;
                int bar = s / 16;
                int deg = prog[bar % prog.Length];
                int semi = root + 24 + scale[deg % 7] + arp[(s / 2) % arp.Length];
                float f = Note(semi);
                float len = stepDur * 1.4f;
                int start = (int)(s * stepDur * SR);
                float ph = 0f;
                for (int i = 0; i < len * SR && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    ph += f / SR;
                    float e = Env(t, len, 0.004f, 0.08f, 0.35f, 0.12f);
                    lead[start + i] += (Square(ph, 0.28f) * 0.5f + Saw(ph * 1.005f) * 0.3f) * e * 0.16f;
                }
            }
            for (int i = delaySamples; i < n; i++)
                lead[i] += lead[i - delaySamples] * 0.42f;
            var lLp = new LP();
            for (int i = 0; i < n; i++)
                buf[i] += lLp.Run(lead[i], 0.42f);

            // --- drums
            var drng = new System.Random(seed * 7 + 3);
            for (int s = 0; s < steps; s++)
            {
                int start = (int)(s * stepDur * SR);
                int m = s % 16;
                bool kick = m == 0 || m == 6 || m == 8 || (m == 14 && drng.NextDouble() < 0.5);
                bool snare = m == 4 || m == 12;
                bool hat = s % 2 == 0 || drng.NextDouble() < 0.35;

                if (kick)
                {
                    float len = 0.16f, ph = 0f;
                    for (int i = 0; i < len * SR && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        float f = Mathf.Lerp(140f, 44f, Mathf.Clamp01(t / 0.055f));
                        ph += f / SR;
                        buf[start + i] += Sine(ph) * Mathf.Exp(-t * 22f) * 0.72f;
                    }
                }
                if (snare)
                {
                    float len = 0.17f;
                    var hp = new HP();
                    for (int i = 0; i < len * SR && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        float x = (float)(drng.NextDouble() * 2 - 1);
                        x = hp.Run(x, 0.42f);
                        buf[start + i] += (x * 0.7f + Sine(t * 190f) * 0.35f) * Mathf.Exp(-t * 26f) * 0.42f;
                    }
                }
                if (hat)
                {
                    float len = 0.045f;
                    var hp = new HP();
                    for (int i = 0; i < len * SR && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        float x = hp.Run((float)(drng.NextDouble() * 2 - 1), 0.75f);
                        buf[start + i] += x * Mathf.Exp(-t * 90f) * 0.20f;
                    }
                }
            }

            // soft clip so the mix stays hot without tearing
            for (int i = 0; i < n; i++)
                buf[i] = (float)Math.Tanh(buf[i] * 1.25f) * 0.92f;

            return Make("track" + seed, buf);
        }
    }
}
