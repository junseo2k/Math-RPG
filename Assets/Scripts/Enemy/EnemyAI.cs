using MathRPG.Combat;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Enemy
{
    /// <summary>
    /// 테스트용 몬스터 행동. 순찰 → 추격 → 공격 → 쿨다운의 단순 상태 기계.
    /// AI · 텔레그래프 · 패턴 1종만 있는 M1-D 수준 (기획서 5-7: "회피가 반사가 아니라 판단").
    ///
    /// 이동만 담당하고, 공격 실행/판정/연출은 EnemyAttack + 기존 전투 시스템에 넘긴다.
    /// 죽음/부활은 Combat/RespawnOnDeath가 맡는다 — 여기서는 상태만 전환한다.
    ///
    /// ※ 여기 수치는 전부 M1 튜닝용 임시값이다 (기획서 7장 미정 수치 아님).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyAI : MonoBehaviour
    {
        private enum State { Patrol, Chase, Attack, Cooldown, Dead }

        [Header("타깃")]
        [SerializeField, Tooltip("추격 대상. 비우면 태그 'Player'로 찾는다.")]
        private Transform target;

        [Header("이동 (임시값)")]
        [SerializeField, Min(0f)] private float patrolSpeed = 1.6f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.4f;
        [SerializeField, Min(0f), Tooltip("시작 지점 기준 좌우 왕복 폭 (units).")]
        private float patrolHalfWidth = 3f;
        [SerializeField, Tooltip("중력 배수.")] private float gravityScale = 4f;

        [Header("감지 (임시값)")]
        [SerializeField, Min(0f)] private float aggroRange = 6.5f;
        [SerializeField, Min(0f), Tooltip("이보다 멀어지면 추격을 포기한다 (히스테리시스).")]
        private float deAggroRange = 9.5f;
        [SerializeField, Min(0f), Tooltip("이 거리 안이면 공격을 시도한다.")]
        private float attackRange = 1.35f;
        [SerializeField, Min(0f), Tooltip("타깃과의 높이차가 이보다 크면 무시한다.")]
        private float verticalTolerance = 2.2f;

        [Header("공격 (임시값)")]
        [SerializeField, Min(0f), Tooltip("공격 후 다음 공격까지 대기 (초).")]
        private float attackCooldown = 1.3f;

        [Header("접지·절벽 판정")]
        [SerializeField, Tooltip("바닥/벽으로 취급할 레이어.")]
        private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float edgeCheckAhead = 0.55f;
        [SerializeField, Min(0f)] private float edgeProbeDepth = 0.6f;
        [SerializeField, Min(0f)] private float wallCheckDistance = 0.5f;

        [Header("디버그")]
        [SerializeField] private bool drawGizmos = true;

        private Rigidbody2D _rb;
        private EnemyAttack _attack;
        private State _state = State.Patrol;
        private float _homeX;
        private int _facing = 1;
        private float _cooldownTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _attack = GetComponent<EnemyAttack>();

            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _homeX = transform.position.x;

            if (target == null)
            {
                GameObject tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                {
                    target = tagged.transform;
                }
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CharacterDiedEvent>(OnDied);
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
        }

        private void FixedUpdate()
        {
            if (_state == State.Dead)
            {
                return;
            }

            float toTargetX = target != null ? target.position.x - transform.position.x : 0f;
            float horizDist = Mathf.Abs(toTargetX);
            float vertDist = target != null ? Mathf.Abs(target.position.y - transform.position.y) : Mathf.Infinity;
            bool inSight = target != null && horizDist <= aggroRange && vertDist <= verticalTolerance;

            switch (_state)
            {
                case State.Patrol:
                    TickPatrol(inSight);
                    break;
                case State.Chase:
                    TickChase(toTargetX, horizDist, vertDist);
                    break;
                case State.Attack:
                    SetVelocityX(0f);
                    if (_attack == null || !_attack.IsAttacking)
                    {
                        _cooldownTimer = attackCooldown;
                        _state = State.Cooldown;
                    }
                    break;
                case State.Cooldown:
                    SetVelocityX(0f);
                    FaceTarget(toTargetX);
                    _cooldownTimer -= Time.fixedDeltaTime;
                    if (_cooldownTimer <= 0f)
                    {
                        _state = State.Chase;
                    }
                    break;
            }
        }

        private void TickPatrol(bool inSight)
        {
            if (inSight)
            {
                _state = State.Chase;
                return;
            }

            // 왕복 폭 끝 · 절벽 · 벽에서 방향을 튼다.
            float offset = transform.position.x - _homeX;
            if ((offset > patrolHalfWidth && _facing > 0) || (offset < -patrolHalfWidth && _facing < 0))
            {
                _facing = -_facing;
            }
            else if (!HasGroundAhead() || HasWallAhead())
            {
                _facing = -_facing;
            }

            SetVelocityX(_facing * patrolSpeed);
        }

        private void TickChase(float toTargetX, float horizDist, float vertDist)
        {
            if (horizDist > deAggroRange || vertDist > verticalTolerance)
            {
                _state = State.Patrol;
                return;
            }

            FaceTarget(toTargetX);

            if (horizDist <= attackRange)
            {
                SetVelocityX(0f);
                if (_attack != null && _attack.TryAttack(_facing))
                {
                    _state = State.Attack;
                }

                return;
            }

            // 사거리 밖이면 접근하되, 절벽/벽 앞에서는 멈춘다.
            if (!HasGroundAhead() || HasWallAhead())
            {
                SetVelocityX(0f);
                return;
            }

            SetVelocityX(_facing * chaseSpeed);
        }

        private void FaceTarget(float toTargetX)
        {
            if (Mathf.Abs(toTargetX) > 0.05f)
            {
                _facing = toTargetX > 0f ? 1 : -1;
            }
        }

        private void SetVelocityX(float value)
        {
            if (!_rb.simulated)
            {
                return;
            }

            Vector2 v = _rb.linearVelocity;
            v.x = value;
            _rb.linearVelocity = v;
        }

        private bool HasGroundAhead()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(_facing * edgeCheckAhead, 0.1f);
            return Physics2D.Raycast(origin, Vector2.down, edgeProbeDepth + 0.1f, groundLayers);
        }

        private bool HasWallAhead()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, 0.6f);
            return Physics2D.Raycast(origin, new Vector2(_facing, 0f), wallCheckDistance, groundLayers);
        }

        private void OnDied(CharacterDiedEvent e)
        {
            if (e.Victim != gameObject)
            {
                return;
            }

            _state = State.Dead;
        }

        private void OnHealthChanged(HealthChangedEvent e)
        {
            if (e.Owner != gameObject)
            {
                return;
            }

            // RespawnOnDeath가 체력을 되채우면 순찰부터 다시 시작한다.
            if (_state == State.Dead && e.Current >= e.Max)
            {
                _homeX = transform.position.x;
                _state = State.Patrol;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Vector3 p = transform.position;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(p, aggroRange);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(p, attackRange);

            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
            float home = Application.isPlaying ? _homeX : p.x;
            Gizmos.DrawLine(new Vector3(home - patrolHalfWidth, p.y - 0.5f, 0f),
                            new Vector3(home - patrolHalfWidth, p.y + 0.5f, 0f));
            Gizmos.DrawLine(new Vector3(home + patrolHalfWidth, p.y - 0.5f, 0f),
                            new Vector3(home + patrolHalfWidth, p.y + 0.5f, 0f));
        }
#endif
    }
}
