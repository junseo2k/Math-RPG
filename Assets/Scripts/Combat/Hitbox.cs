using System.Collections.Generic;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 공격이 닿는 범위. AttackTimeline이 타격 순간(AttackHitEvent)을 알리면,
    /// 그 순간 캐릭터 앞쪽 상자를 한 번 겹침 검사해서 잡힌 Hurtbox에 데미지를 넣는다.
    ///
    /// 타이밍은 전혀 계산하지 않는다 — 언제 때리는지는 AttackTimeline / AttackTimingData가
    /// 정하고, 이 컴포넌트는 통보받은 순간에 "어디를" 때리는지만 안다.
    /// AttackVisuals가 그림을, 이 클래스가 판정을 맡는 대칭 구조.
    ///
    /// 붙이는 위치: AttackTimeline과 같은 GameObject (보통 플레이어 루트).
    ///
    /// ※ damage · 상자 크기 · 오프셋은 기획서 7장 미정 수치가 아니라 M1 타격감 튜닝용 임시값이다.
    /// </summary>
    public sealed class Hitbox : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("이 히트박스의 주인. 비우면 같은/부모 오브젝트의 AttackTimeline을 쓴다.")]
        private GameObject attacker;

        [Header("판정 범위 (캐릭터 기준 로컬)")]
        [SerializeField, Tooltip("바라보는 방향으로 상자 중심을 얼마나 앞에 둘지 (units).")]
        private float forwardOffset = 0.9f;

        [SerializeField, Tooltip("상자 중심의 높이 오프셋 (units). 발밑 원점 기준.")]
        private float verticalOffset = 0.9f;

        [SerializeField, Tooltip("판정 상자 크기 (units).")]
        private Vector2 boxSize = new Vector2(1.4f, 1.2f);

        [SerializeField, Tooltip("이 레이어에 있는 Hurtbox만 맞힌다.")]
        private LayerMask hittableLayers;

        [Header("피해 (임시값)")]
        [SerializeField, Min(0f), Tooltip("한 번 맞힐 때 주는 피해량. 타격감 검증용 임시값.")]
        private float damage = 12f;

        /// <summary>바라보는 방향. +1 오른쪽 / -1 왼쪽. 공격 직전에 PlayerAttack이 갱신한다.</summary>
        public int Facing { get; set; } = 1;

        private readonly List<Collider2D> _overlap = new List<Collider2D>();
        private readonly HashSet<Hurtbox> _hitThisSwing = new HashSet<Hurtbox>();
        private ContactFilter2D _filter;

        private void Awake()
        {
            if (attacker == null)
            {
                var timeline = GetComponentInParent<AttackTimeline>();
                attacker = timeline != null ? timeline.gameObject : transform.root.gameObject;
            }

            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = hittableLayers,
                useTriggers = true
            };
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AttackWindupStartedEvent>(OnWindupStarted);
            EventBus.Subscribe<AttackHitEvent>(OnHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AttackWindupStartedEvent>(OnWindupStarted);
            EventBus.Unsubscribe<AttackHitEvent>(OnHit);
        }

        private bool IsMine(GameObject eventAttacker) => eventAttacker == attacker;

        // 새 스윙이 시작되면 "이번에 이미 맞힌 대상" 목록을 비운다.
        private void OnWindupStarted(AttackWindupStartedEvent e)
        {
            if (IsMine(e.Attacker))
            {
                _hitThisSwing.Clear();
            }
        }

        private void OnHit(AttackHitEvent e)
        {
            if (!IsMine(e.Attacker))
            {
                return;
            }

            Vector2 center = GetBoxCenter();

            _overlap.Clear();
            int count = Physics2D.OverlapBox(center, boxSize, 0f, _filter, _overlap);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlap[i];
                if (col == null)
                {
                    continue;
                }

                var hurtbox = col.GetComponentInParent<Hurtbox>();
                if (hurtbox == null || hurtbox.IsDead || !_hitThisSwing.Add(hurtbox))
                {
                    continue;
                }

                var info = new DamageInfo(attacker, damage, col.ClosestPoint(center), Facing);
                hurtbox.Receive(info);
            }
        }

        private Vector2 GetBoxCenter()
        {
            Vector2 origin = transform.position;
            return origin + new Vector2(forwardOffset * Mathf.Sign(Facing), verticalOffset);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
            int facing = Application.isPlaying ? Facing : 1;
            Vector2 center = (Vector2)transform.position
                             + new Vector2(forwardOffset * Mathf.Sign(facing), verticalOffset);
            Gizmos.DrawWireCube(center, boxSize);
        }
#endif
    }
}
