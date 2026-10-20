using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Audio
{
    public enum SFXType
    {
        Footstep,
        TypingKey,
        SwordSwing,
        Parry,
        Damage,
        Miss,
        DoorCreak,
        KeyJingle
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const int SampleRate = 44100;

        private AudioSource sfxSource;
        private AudioSource typingSource;
        private readonly Dictionary<SFXType, AudioClip> proceduralClips = new Dictionary<SFXType, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            typingSource = gameObject.AddComponent<AudioSource>();
            typingSource.playOnAwake = false;

            GenerateAllProceduralClips();
        }

        private void GenerateAllProceduralClips()
        {
            proceduralClips[SFXType.Footstep] = CreateFootstepClip();
            proceduralClips[SFXType.TypingKey] = CreateTypingBlipClip();
            proceduralClips[SFXType.SwordSwing] = CreateSwordSwingClip();
            proceduralClips[SFXType.Parry] = CreateParryClip();
            proceduralClips[SFXType.Damage] = CreateDamageClip();
            proceduralClips[SFXType.Miss] = CreateMissClip();
            proceduralClips[SFXType.DoorCreak] = CreateDoorCreakClip();
            proceduralClips[SFXType.KeyJingle] = CreateKeyJingleClip();

            Debug.Log("[AudioManager] Generated 8 procedural retro audio clips via AudioClip.Create.");
        }

        public void PlaySFX(SFXType type, float volume = 1f)
        {
            if (proceduralClips.TryGetValue(type, out AudioClip clip) && clip != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        public void PlayTypingBlip(int characterIndex = 0, float volume = 0.7f)
        {
            if (proceduralClips.TryGetValue(SFXType.TypingKey, out AudioClip clip) && clip != null)
            {
                float pitch = 1.0f + Mathf.Clamp(characterIndex * 0.09f, 0f, 0.7f);
                typingSource.pitch = pitch;
                typingSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        // ================= PROCEDURAL SYNTHESIS ================= //

        private AudioClip CreateFootstepClip()
        {
            float duration = 0.09f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Frequency drops rapidly from 130 Hz to 40 Hz (low thud)
                float freq = Mathf.Lerp(130f, 40f, progress);
                float sinVal = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Initial subtle shoe contact noise
                float noise = (progress < 0.2f) ? UnityEngine.Random.Range(-0.25f, 0.25f) : 0f;

                // Exponential decay envelope
                float envelope = Mathf.Exp(-t * 38f);
                samples[i] = (sinVal * 0.75f + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create("SFX_Footstep", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateTypingBlipClip()
        {
            float duration = 0.05f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Upward blip pitch 900 Hz to 1300 Hz
                float freq = Mathf.Lerp(900f, 1300f, progress);
                float sinVal = Mathf.Sin(2f * Mathf.PI * freq * t);
                float squareComp = Mathf.Sign(sinVal) * 0.3f; // 8-bit retro character

                float envelope = 1f - progress;
                samples[i] = (sinVal * 0.7f + squareComp) * (envelope * envelope) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SFX_TypingBlip", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSwordSwingClip()
        {
            float duration = 0.18f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float lastSample = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // White noise filtered with moving cutoff (whoosh)
                float rawNoise = UnityEngine.Random.Range(-1f, 1f);
                float filterAlpha = Mathf.Sin(progress * Mathf.PI) * 0.42f + 0.05f;
                lastSample += filterAlpha * (rawNoise - lastSample);

                // Hump amplitude envelope
                float envelope = Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 1.8f);
                samples[i] = lastSample * envelope * 0.9f;
            }

            AudioClip clip = AudioClip.Create("SFX_SwordSwing", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateParryClip()
        {
            float duration = 0.38f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Inharmonic metallic bell partials (A6, F7, A7, E8)
                float p1 = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.45f;
                float p2 = Mathf.Sin(2f * Mathf.PI * 2793f * t) * 0.30f;
                float p3 = Mathf.Sin(2f * Mathf.PI * 3520f * t) * 0.15f;
                float p4 = Mathf.Sin(2f * Mathf.PI * 5274f * t) * 0.10f;

                // Sharp bell decay
                float envelope = Mathf.Exp(-t * 9.5f);
                samples[i] = (p1 + p2 + p3 + p4) * envelope * 0.85f;
            }

            AudioClip clip = AudioClip.Create("SFX_Parry", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateDamageClip()
        {
            float duration = 0.22f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Low bass impact 90 Hz down to 35 Hz
                float freq = Mathf.Lerp(90f, 35f, progress);
                float bass = Mathf.Sin(2f * Mathf.PI * freq * t);
                float crunchNoise = UnityEngine.Random.Range(-0.45f, 0.45f);

                // Saturated overdrive
                float raw = bass * 0.7f + crunchNoise;
                float saturated = Mathf.Clamp(raw * 1.5f, -0.9f, 0.9f);

                float envelope = Mathf.Exp(-t * 13f);
                samples[i] = saturated * envelope * 0.9f;
            }

            AudioClip clip = AudioClip.Create("SFX_Damage", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateMissClip()
        {
            float duration = 0.16f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Downward sad tone 240 Hz to 65 Hz
                float freq = Mathf.Lerp(240f, 65f, progress);
                float sinVal = Mathf.Sin(2f * Mathf.PI * freq * t);

                float envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress);
                samples[i] = sinVal * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SFX_Miss", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateDoorCreakClip()
        {
            float duration = 0.65f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Low timber frequency with rapid groaning vibrato
                float wobble = Mathf.Sin(38f * t) * 16f;
                float freq = 75f + wobble;
                float saw = (t * freq) % 1f * 2f - 1f;

                float rough = saw + UnityEngine.Random.Range(-0.15f, 0.15f);

                // Envelope: swell then slow decay
                float envelope = Mathf.Sin(progress * Mathf.PI);
                samples[i] = rough * envelope * 0.45f;
            }

            AudioClip clip = AudioClip.Create("SFX_DoorCreak", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateKeyJingleClip()
        {
            float duration = 0.36f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // First tone (C7 = 2093 Hz)
                float chime1 = Mathf.Sin(2f * Mathf.PI * 2093f * t) * Mathf.Exp(-t * 14f);

                // Second tone arpeggio starting after 0.07s (E7 = 2637 Hz)
                float chime2 = 0f;
                if (t >= 0.07f)
                {
                    float t2 = t - 0.07f;
                    chime2 = Mathf.Sin(2f * Mathf.PI * 2637f * t2) * Mathf.Exp(-t2 * 12f) * 1.1f;
                }

                samples[i] = (chime1 + chime2) * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_KeyJingle", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
