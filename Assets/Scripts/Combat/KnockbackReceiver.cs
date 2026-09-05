using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 맞으면 <b>몸이 실제로 밀려나는</b> 넉백. DamageDealtEvent를 구독해 Rigidbody2D에 속도를 준다.
    ///
    /// HitReaction의 넉백과는 다르다 — 저건 Visual의 로컬 오프셋이라 갔다가 제자리로
    /// 돌아왔다. 그림만 흔들리고 몸은 그 자리에 있으니 "때렸는데 세계가 반응하지 않는"
    /// 느낌이 났다. 이 컴포넌트는 루트 트랜스폼을 물리로 밀어서 실제로 거리를 벌린다.
    ///
    /// 밀리는 동안(<see cref="IsStaggered"/>)은 이동 로직이 속도를 덮어쓰면 안 된다 —
    /// PlayerLocomotion · EnemyAI가 이 값을 보고 그 프레임의 이동 제어를 건너뛴다.
    /// 그게 없으면 다음 FixedUpdate에서 속도가 즉시 0으로 덮여 넉백이 사라진다.
    ///
    /// 시간은 Time.fixedDeltaTime(스케일 적용)을 쓴다 — 히트스톱 동안 timeScale이 0이면
    /// FixedUpdate 자체가 돌지 않아 자연히 같이 얼고, 시간이 풀리는 순간 튕겨 나간다.
    ///
    /// 붙이는 위치: Rigidbody2D가 있는 루트 (플레이어 · 몬스터).
    ///
    /// ※ 여기 수치는 기획서 7장 미정 수치가 아니라 M1 타격감 튜닝용 임시값이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class KnockbackReceiver : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("이 넉백이 반응할 피격 대상. 비우면 이 오브젝트.")]
        private GameObject owner;

        [Header("세기 (임시값)")]
        [SerializeField, Min(0f), Tooltip("맞은 순간 뒤로 밀리는 초기 속도 (units/sec).")]
        private float speed = 6f;

        [SerializeField, Min(0f), Tooltip("맞은 순간 살짝 띄우는 속도 (units/sec). 0이면 띄우지 않는다.")]
        private float liftSpeed = 0f;

        [SerializeField, Min(0.01f), Tooltip("밀려나며 조작·AI가 멈춰 있는 시간 (초). 곧 경직 시간이다.")]
        private float staggerSeconds = 0.16f;

        [SerializeField, Tooltip("시간에 따른 속도 감쇠. 1에서 시작해 0으로 떨어질수록 '탁' 밀리고 멈춘다.")]
        private AnimationCurve decayCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -2.5f),
            new Keyframe(1f, 0f, -2.5f, 0f));

        /// <summary>넉백으로 밀려나는 중인가. 이동 로직이 속도 제어를 넘겨줘야 하는 구간.</summary>
        public bool IsStaggered => _timer > 0f;

        private Rigidbody2D _rb;
        private float _timer;
        private int _direction = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            if (owner == null)
            {
                owner = gameObject;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<CharacterDiedEvent>(OnDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);

            // 밀려나던 도중 꺼지면 IsStaggered가 true로 남아 이동이 영영 막힌다.
            _timer = 0f;
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            if (e.Victim != owner || !isActiveAndEnabled)
            {
                return;
            }

            // RespawnOnDeath가 시뮬레이션을 꺼둔 시체는 밀지 않는다.
            if (_rb == null || !_rb.simulated)
            {
                return;
            }

            _direction = e.HitDirection < 0 ? -1 : 1;
            _timer = staggerSeconds;

            Vector2 v = _rb.linearVelocity;
            v.x = speed * _direction;
            if (liftSpeed > 0f)
            {
                v.y = liftSpeed;
            }

            _rb.linearVelocity = v;
        }

        private void OnDied(CharacterDiedEvent e)
        {
            // 죽는 순간 경직을 풀어둔다. 안 그러면 리스폰 직후까지 조작이 막힌 채로 남는다.
            if (e.Victim == owner)
            {
                _timer = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (_timer <= 0f)
            {
                return;
            }

            _timer -= Time.fixedDeltaTime;

            if (_rb == null || !_rb.simulated)
            {
                _timer = 0f;
                return;
            }

            float elapsed = staggerSeconds - Mathf.Max(0f, _timer);
            float amount = decayCurve.Evaluate(Mathf.Clamp01(elapsed / staggerSeconds));

            Vector2 v = _rb.linearVelocity;
            v.x = speed * _direction * amount;
            _rb.linearVelocity = v;

            if (_timer <= 0f)
            {
                _timer = 0f;
            }
        }
    }
}
