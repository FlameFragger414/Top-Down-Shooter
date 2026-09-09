using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    /// One-shot sound bank plus a tiny voice pool. Clips are synthesised on first use.
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;

        readonly Dictionary<string, AudioClip> bank = new Dictionary<string, AudioClip>();
        AudioSource[] voices;
        int next;

        AudioSource musicA, musicB;
        readonly Dictionary<int, AudioClip> tracks = new Dictionary<int, AudioClip>();
        int currentTrack = -1;

        public static void Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<Sfx>();
            I.Init();
        }

        void Init()
        {
            voices = new AudioSource[16];
            for (int i = 0; i < voices.Length; i++)
            {
                var a = gameObject.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 0f;
                voices[i] = a;
            }
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB })
            {
                m.playOnAwake = false;
                m.loop = true;
                m.volume = 0f;
            }

            bank["pistol"] = Synth.Gunshot("pistol", 0.22f, 320f, 0.9f, 1f);
            bank["silenced"] = Synth.Gunshot("silenced", 0.12f, 200f, 0.35f, 2f);
            bank["shotgun"] = Synth.Gunshot("shotgun", 0.42f, 190f, 1.15f, 3f);
            bank["uzi"] = Synth.Gunshot("uzi", 0.13f, 380f, 0.8f, 4f);
            bank["rifle"] = Synth.Gunshot("rifle", 0.18f, 300f, 0.95f, 5f);
            bank["swing"] = Synth.Whoosh("swing", 0.20f, 0.55f);
            bank["dash"] = Synth.Whoosh("dash", 0.28f, 0.30f);
            bank["hit"] = Synth.Thud("hit", 0.20f, 210f, 60f);
            bank["kick"] = Synth.Thud("kick", 0.30f, 150f, 40f);
            bank["splat"] = Synth.Splat("splat", 0.42f);
            bank["pickup"] = Synth.Blip("pickup", 0.12f, 620f, 1180f, 0.35f);
            bank["deny"] = Synth.Blip("deny", 0.14f, 300f, 150f, 0.5f);
            bank["ui"] = Synth.Blip("ui", 0.07f, 900f, 900f, 0.25f);
            bank["alert"] = Synth.Blip("alert", 0.20f, 500f, 900f, 0.5f);
            bank["shell"] = Synth.Shell("shell");
            bank["glass"] = Synth.Tink("glass", 0.30f, 2100f);
            bank["ric"] = Synth.Tink("ric", 0.16f, 3000f);
            bank["clear"] = Synth.Blip("clear", 0.5f, 440f, 880f, 0.4f);
            bank["boom"] = Synth.Gunshot("boom", 0.95f, 95f, 1.5f, 9f);
            bank["spinup"] = Synth.Blip("spinup", 0.55f, 110f, 820f, 0.42f);

            // per-weapon voices, so the roster is audibly distinct
            bank["revolver"] = Synth.Gunshot("revolver", 0.34f, 240f, 1.1f, 11f);
            bank["sniper"] = Synth.Crack("sniper", 0.55f, 520f, 0.55f);
            bank["lmg"] = Synth.Gunshot("lmg", 0.20f, 260f, 1.0f, 13f);
            bank["minigun"] = Synth.Gunshot("minigun", 0.10f, 430f, 0.75f, 17f);
            bank["double"] = Synth.Gunshot("double", 0.52f, 160f, 1.3f, 19f);
            bank["smg"] = Synth.Gunshot("smg", 0.11f, 410f, 0.72f, 23f);
            bank["gl"] = Synth.Gunshot("gl", 0.30f, 140f, 0.9f, 29f);
            bank["flamer"] = Synth.Flame("flamer");

            // reactions and feedback
            bank["scream0"] = Synth.Scream("scream0", 0.55f, 300f, 5);
            bank["scream1"] = Synth.Scream("scream1", 0.44f, 380f, 11);
            bank["scream2"] = Synth.Scream("scream2", 0.62f, 250f, 17);
            bank["crunch"] = Synth.Crunch("crunch");
            bank["whiz"] = Synth.Whiz("whiz");
            bank["step"] = Synth.Footstep("step");
            bank["ammo"] = Synth.Blip("ammo", 0.18f, 500f, 1000f, 0.3f);
            bank["heal"] = Synth.Arp("heal", new[] { 4, 11, 16 }, 0.09f, 0.35f);
            bank["combo"] = Synth.Blip("combo", 0.10f, 900f, 1500f, 0.3f);
            bank["fanfare"] = Synth.Arp("fanfare", new[] { 4, 9, 16, 21 }, 0.13f, 0.4f);
            bank["sting"] = Synth.Arp("sting", new[] { 4, 1, -3, -8 }, 0.18f, 0.5f);
        }

        public void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (!bank.TryGetValue(name, out var clip) || clip == null) return;
            var v = voices[next];
            next = (next + 1) % voices.Length;
            v.pitch = Mathf.Clamp(pitch, 0.2f, 3f);
            v.volume = Mathf.Clamp01(vol) * 0.8f;
            v.clip = clip;
            v.Play();
        }

        public void PlayVaried(string name, float vol = 1f, float spread = 0.12f)
        {
            Play(name, vol * Random.Range(0.9f, 1.05f), 1f + Random.Range(-spread, spread));
        }

        // -------------------------------------------------------------------- music
        public void PlayTrack(int index, float bpm = 124f)
        {
            if (currentTrack == index && musicA.isPlaying) return;
            currentTrack = index;
            if (!tracks.TryGetValue(index, out var clip))
            {
                clip = Synth.Track(index * 91 + 17, bpm, out _);
                tracks[index] = clip;
            }
            var from = musicA.isPlaying ? musicA : musicB;
            var to = from == musicA ? musicB : musicA;
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            StopAllCoroutines();
            StartCoroutine(Crossfade(from, to, 0.8f));
        }

        public void StopMusic(float time = 0.4f)
        {
            currentTrack = -1;
            StopAllCoroutines();
            StartCoroutine(FadeOut(musicA, time));
            StartCoroutine(FadeOut(musicB, time));
        }

        public void SetMusicPitch(float p)
        {
            musicA.pitch = p;
            musicB.pitch = p;
        }

        System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to, float time)
        {
            float t = 0f;
            float fromStart = from.volume;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                float k = t / time;
                to.volume = Mathf.Lerp(0f, 0.42f, k);
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                yield return null;
            }
            from.Stop();
        }

        System.Collections.IEnumerator FadeOut(AudioSource s, float time)
        {
            float v0 = s.volume, t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                s.volume = Mathf.Lerp(v0, 0f, t / time);
                yield return null;
            }
            s.Stop();
        }
    }
}
