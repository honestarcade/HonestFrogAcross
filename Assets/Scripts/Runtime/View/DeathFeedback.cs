using System.Collections;
using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.View
{
    /// <summary>
    /// #54: per-cause death presentation inside the respawn window (&lt;1s):
    /// road/tracks → squash splat; water/gator → sink+fade ripple; edge drift →
    /// quick slide-out fade. Purely visual — timing fits SimConfig.RespawnDelayTicks.
    /// </summary>
    public sealed class DeathFeedback : MonoBehaviour
    {
        private SpriteRenderer _fx;

        public void Play(DeathCause cause, Vector3 at, Sprite characterSprite)
        {
            if (_fx == null)
            {
                var go = new GameObject("death-fx");
                go.transform.SetParent(transform, false);
                _fx = go.AddComponent<SpriteRenderer>();
                _fx.sortingOrder = 40;
            }
            _fx.sprite = characterSprite;
            _fx.transform.position = at + new Vector3(0f, 0f, -0.6f);
            float s = 0.9f / Mathf.Max(0.1f, characterSprite.bounds.size.x);
            _fx.transform.localScale = Vector3.one * s;
            _fx.enabled = true;
            StopAllCoroutines();
            StartCoroutine(Animate(cause, s));
        }

        private IEnumerator Animate(DeathCause cause, float baseScale)
        {
            float dur = SimConfig.RespawnDelayTicks / (float)SimConfig.TicksPerSecond * 0.9f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                switch (cause)
                {
                    case DeathCause.Water or DeathCause.Gator:
                        _fx.transform.localScale = Vector3.one * (baseScale * (1f - 0.7f * k));
                        _fx.color = new Color(0.6f, 0.8f, 1f, 1f - k);
                        break;
                    case DeathCause.EdgeDrift:
                        _fx.transform.position += new Vector3(Time.deltaTime * 2f, 0f, 0f);
                        _fx.color = new Color(1f, 1f, 1f, 1f - k);
                        break;
                    default: // Vehicle, Train: the classic splat
                        _fx.transform.localScale = new Vector3(
                            baseScale * (1f + 0.5f * k), baseScale * (1f - 0.8f * k), 1f);
                        _fx.color = new Color(1f, 1f - 0.4f * k, 1f - 0.4f * k, 1f - k * k);
                        break;
                }
                yield return null;
            }
            _fx.enabled = false;
        }
    }
}
