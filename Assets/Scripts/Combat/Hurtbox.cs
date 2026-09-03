using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// "여기를 때리면 맞는다"를 나타내는 콜라이더. 상대 Hitbox의 OverlapBox에 잡히는 쪽.
    ///
    /// 트리거 이벤트를 쓰지 않는다 — Hitbox가 타격 순간에 능동적으로 겹침 검사를 하고,
    /// 잡힌 콜라이더에서 이 컴포넌트를 찾아 ApplyDamage를 호출한다. 그래서 이 콜라이더는
    /// 지형·이동 충돌용으로도 그대로 쓸 수 있다 (트리거일 필요 없음).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField, Tooltip("피해를 전달할 대상. 비우면 자신·부모에서 Health를 찾는다.")]
        private Health target;

        [SerializeField, Tooltip("피격 이벤트에 실릴 루트 오브젝트. 비우면 target의 오브젝트.")]
        private GameObject owner;

        /// <summary>피격 연출이 "내가 맞았는지" 거르는 데 쓰는 기준 오브젝트.</summary>
        public GameObject Owner => owner;

        public bool IsDead => _damageable == null || _damageable.IsDead;

        private IDamageable _damageable;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponentInParent<Health>();
            }

            _damageable = target;

            if (_damageable == null)
            {
                Debug.LogError($"[{nameof(Hurtbox)}] Health를 찾지 못했습니다. 인스펙터에서 지정하세요.", this);
                enabled = false;
                return;
            }

            if (owner == null)
            {
                owner = target.gameObject;
            }
        }

        /// <summary>Hitbox가 호출한다. 데미지를 대상 Health로 넘긴다.</summary>
        public void Receive(in DamageInfo info)
        {
            if (!enabled || _damageable == null || _damageable.IsDead)
            {
                return;
            }

            _damageable.ApplyDamage(info);
        }
    }
}
