using System.Collections.Generic;
using UnityEngine;

namespace MathRPG
{
    [DefaultExecutionOrder(1000)]
    public sealed class CombatEffects : MonoBehaviour
    {
        [SerializeField, Min(0)] float lightShakeStrength = .06f;
        [SerializeField, Min(0)] float heavyShakeStrength = .16f;
        [SerializeField, Min(.01f)] float shakeDuration = .2f;
        [SerializeField, Min(0)] float lightHitStop = .045f;
        [SerializeField, Min(0)] float heavyHitStop = .095f;
        [SerializeField] Color hitFlashColor = Color.white;
        [SerializeField, Min(0)] float flashDuration = .1f;
        [SerializeField, Min(0)] float scaleDuration = .16f;
        [SerializeField, Range(0, .5f)] float scaleStrength = .18f;
        sealed class Spark { public Transform body; public Vector3 velocity; public float life, duration; public SpriteRenderer sprite; }
        readonly List<Spark> sparks = new List<Spark>();
        AudioSource source;
        AudioClip lightHit, heavyHit, hurt, cast;
        public float FreezeUntil { get; private set; }
        public float Shake { get; private set; }
        float shakeRemaining, shakePeak, hitStarted;
        Transform hitTarget;
        SpriteRenderer[] hitSprites;
        Color[] originalColors;
        Vector3 originalScale;
        bool hitApplied;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .35f;
            lightHit = Tone(180, .09f, .45f);
            heavyHit = Tone(75, .23f, .65f);
            hurt = Tone(110, .17f, .8f);
            cast = Tone(620, .24f, .08f);
        }

        static AudioClip Tone(float frequency, float duration, float noise)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * duration)];
            var random = new System.Random(49);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Pow(1 - (float)i / samples.Length, 2);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * frequency * t * (1 - t)) * (1 - noise) + (float)(random.NextDouble() * 2 - 1) * noise) * envelope;
            }
            var clip = AudioClip.Create("Synthesized training sound", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Charge(Vector3 position)
        {
            source.PlayOneShot(cast, .65f);
            Burst(position, new Color(.38f, .95f, 1), 14, 3.2f);
        }

        public void Impact(Vector3 position, bool heavy, bool playerHit = false, Transform hitVisual = null)
        {
            FreezeUntil = Mathf.Max(FreezeUntil, Time.unscaledTime + (heavy ? heavyHitStop : lightHitStop));
            shakePeak = Mathf.Max(Shake, heavy ? heavyShakeStrength : lightShakeStrength);
            Shake = shakePeak;
            shakeRemaining = shakeDuration;
            if (hitVisual != null)
            {
                RestoreHitVisual();
                if (hitTarget != hitVisual)
                {
                    hitTarget = hitVisual;
                    hitSprites = hitVisual.GetComponentsInChildren<SpriteRenderer>(true);
                    originalColors = new Color[hitSprites.Length];
                }
                hitStarted = Time.unscaledTime;
            }
            source.PlayOneShot(playerHit ? hurt : heavy ? heavyHit : lightHit);
            if (heavy) source.PlayOneShot(lightHit, .45f);
            Burst(position, playerHit ? new Color(1, .37f, .4f) : heavy ? new Color(.5f, 1, 1) : new Color(1, .86f, .5f), heavy ? 22 : 9, heavy ? 7 : 4);
        }

        public void Slash(Vector3 position, int direction, bool heavy, float strength = 1f)
        {
            var sprite = WorldArt.Shape("Slash", position + Vector3.right * direction * .6f, new Vector2(heavy ? 3.2f : 1.4f, heavy ? .16f : .09f) * strength, new Color(.72f, .95f, 1), "Diamond", transform, 25);
            sprite.transform.rotation = Quaternion.Euler(0, 0, direction * 23);
            sparks.Add(new Spark { body = sprite.transform, velocity = Vector3.right * direction * 3, life = .18f, duration = .18f, sprite = sprite });
        }

        void Burst(Vector3 position, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var sprite = WorldArt.Shape("Spark", position, new Vector2(.12f, .055f), color, "Diamond", transform, 30);
                Vector2 velocity = Random.insideUnitCircle.normalized * Random.Range(speed * .3f, speed);
                sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
                float life = Random.Range(.17f, .4f);
                sparks.Add(new Spark { body = sprite.transform, velocity = velocity, life = life, duration = life, sprite = sprite });
            }
        }

        public void Tick(float dt)
        {
            // Restore before gameplay updates so animation and phase colors remain the baseline.
            RestoreHitVisual();
            shakeRemaining = Mathf.Max(0, shakeRemaining - dt);
            Shake = shakeDuration > 0 ? shakePeak * Mathf.Clamp01(shakeRemaining / shakeDuration) : 0;
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var spark = sparks[i];
                spark.life -= dt;
                if (spark.life <= 0) { Destroy(spark.body.gameObject); sparks.RemoveAt(i); continue; }
                spark.body.position += spark.velocity * dt;
                spark.velocity += Vector3.down * (dt * 6);
                var color = spark.sprite.color;
                color.a = spark.life / spark.duration;
                spark.sprite.color = color;
            }
        }

        void LateUpdate()
        {
            if (hitTarget == null) return;
            float elapsed = Time.unscaledTime - hitStarted;
            if (!hitTarget.gameObject.activeInHierarchy || elapsed >= Mathf.Max(flashDuration, scaleDuration))
            {
                RestoreHitVisual();
                hitTarget = null;
                return;
            }
            RestoreHitVisual();
            originalScale = hitTarget.localScale;
            float amount = scaleDuration > 0 ? scaleStrength * Mathf.Clamp01(1 - elapsed / scaleDuration) : 0;
            hitTarget.localScale = Vector3.Scale(originalScale, new Vector3(1 + amount, 1 - amount, 1));
            for (int i = 0; i < hitSprites.Length; i++)
            {
                if (hitSprites[i] == null) continue;
                originalColors[i] = hitSprites[i].color;
                if (elapsed >= flashDuration) continue;
                var color = hitFlashColor;
                color.a = originalColors[i].a;
                hitSprites[i].color = color;
            }
            hitApplied = true;
        }

        void RestoreHitVisual()
        {
            if (!hitApplied) return;
            if (hitTarget != null) hitTarget.localScale = originalScale;
            for (int i = 0; i < hitSprites.Length; i++)
                if (hitSprites[i] != null) hitSprites[i].color = originalColors[i];
            hitApplied = false;
        }

        public void ResetFeedback()
        {
            RestoreHitVisual();
            hitTarget = null;
            Shake = shakePeak = shakeRemaining = FreezeUntil = 0;
        }

        void OnDisable() { ResetFeedback(); }

        void OnDestroy()
        {
            if (lightHit != null) Destroy(lightHit);
            if (heavyHit != null) Destroy(heavyHit);
            if (hurt != null) Destroy(hurt);
            if (cast != null) Destroy(cast);
        }
    }
}
