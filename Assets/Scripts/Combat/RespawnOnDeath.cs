using System.Collections;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 죽으면 잠깐 뒤에 되살아난다. 플레이어 · 더미 몬스터 공용 (테스트 편의).
    ///
    /// 죽음/부활 자체의 연출은 하지 않는다 — HitReaction이 사망 틴트를,
    /// 되살아나면 원래대로 되돌린다. 이 컴포넌트는 위치 복구 + 체력 회복만 맡는다.
    ///
    /// ※ respawnDelay는 기획서 7장 미정 수치가 아니라 M1 테스트 편의값이다.
    ///   실제 리스폰/재도전 규칙(기획서 9장, 진행 유지)은 M5에서 다룬다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class RespawnOnDeath : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("죽은 뒤 되살아나기까지의 시간 (초).")]
        private float respawnDelay = 2f;

        [SerializeField, Tooltip("되살아날 때 처음 위치로 되돌릴지 여부.")]
        private bool returnToStartPosition = true;

        [SerializeField, Tooltip("죽어 있는 동안 끌 콜라이더 (몬스터 시체를 통과 가능하게). 비워도 됨.")]
        private Collider2D disableWhileDead;

        [SerializeField, Tooltip("죽어 있는 동안 멈출 Rigidbody2D. 비우면 이 오브젝트에서 찾는다.")]
        private Rigidbody2D bodyToFreeze;

        private Health _health;
        private Vector3 _startPosition;
        private Coroutine _pending;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _startPosition = transform.position;

            if (bodyToFreeze == null)
            {
                bodyToFreeze = GetComponent<Rigidbody2D>();
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CharacterDiedEvent>(OnDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);

            if (_pending != null)
            {
                StopCoroutine(_pending);
                _pending = null;
            }
        }

        private void OnDied(CharacterDiedEvent e)
        {
            if (e.Victim != gameObject || _pending != null)
            {
                return;
            }

            _pending = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            if (disableWhileDead != null)
            {
                disableWhileDead.enabled = false;
            }

            if (bodyToFreeze != null)
            {
                bodyToFreeze.linearVelocity = Vector2.zero;
                bodyToFreeze.simulated = false;
            }

            // 히트스톱으로 timeScale이 0일 수 있으니 실제 시간으로 센다.
            yield return new WaitForSecondsRealtime(respawnDelay);

            if (returnToStartPosition)
            {
                transform.position = _startPosition;
            }

            if (bodyToFreeze != null)
            {
                bodyToFreeze.simulated = true;
            }

            if (disableWhileDead != null)
            {
                disableWhileDead.enabled = true;
            }

            _health.Revive();
            _pending = null;
        }
    }
}
