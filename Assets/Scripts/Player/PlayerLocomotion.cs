using System.Collections.Generic;
using UnityEngine;

namespace MathRPG.Player
{
    /// <summary>
    /// 플레이어의 이동 · 점프 · 숙이기.
    /// 전투(평타/스킬)는 이 클래스의 책임이 아니다 — M1에서 별도 컴포넌트로 추가한다.
    ///
    /// 트랜스폼 기준: 오브젝트의 원점(pivot)이 발밑이라고 가정한다.
    /// 콜라이더 오프셋은 이 가정에 맞춰 코드가 직접 계산하므로 인스펙터에서 건드리지 않아도 된다.
    ///
    /// ※ 여기 수치들은 기획서 7장의 "미정 수치"(마나·문제·보스 관련)가 아니라
    ///   조작 체감용 임시값이다. M1 타격감 검증 단계에서 플레이하며 조정할 것.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class PlayerLocomotion : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField, Tooltip("InputReader 에셋. 비어 있으면 이동이 동작하지 않는다.")]
        private InputReader input;

        [Header("이동")]
        [SerializeField, Tooltip("최대 이동 속도 (units/sec).")]
        private float moveSpeed = 7f;

        [SerializeField, Tooltip("가속도. 높을수록 조작이 즉각적으로 느껴진다.")]
        private float acceleration = 70f;

        [SerializeField, Tooltip("감속도. 높을수록 즉시 멈춘다.")]
        private float deceleration = 90f;

        [SerializeField, Range(0f, 1f), Tooltip("숙인 상태의 이동 속도 배수.")]
        private float crouchSpeedMultiplier = 0.4f;

        [SerializeField, Range(0f, 1f), Tooltip("공중에서의 방향 전환 정도. 1이면 지상과 동일.")]
        private float airControl = 0.65f;

        [Header("점프")]
        [SerializeField, Tooltip("점프로 도달할 최고 높이 (units). 속도가 아니라 높이로 지정한다.")]
        private float jumpHeight = 2.6f;

        [SerializeField, Tooltip("중력 배수. 클수록 묵직하고 빠릿하게 떨어진다.")]
        private float gravityScale = 4f;

        [SerializeField, Tooltip("하강 시 추가 중력 배수. 점프 궤적의 '떨어지는 맛'을 만든다.")]
        private float fallGravityMultiplier = 1.5f;

        [SerializeField, Range(0f, 1f), Tooltip("점프 버튼을 일찍 떼면 상승 속도를 이 비율로 줄인다 (가변 점프).")]
        private float jumpCutMultiplier = 0.45f;

        [SerializeField, Tooltip("발판에서 떨어진 뒤에도 점프를 허용하는 유예 시간 (초). 코요테 타임.")]
        private float coyoteTime = 0.1f;

        [SerializeField, Tooltip("착지 직전에 누른 점프를 기억하는 시간 (초). 입력 선행 버퍼.")]
        private float jumpBufferTime = 0.12f;

        [SerializeField, Tooltip("하강 속도 상한. 너무 빨리 떨어져 조작 불능이 되는 것을 막는다.")]
        private float maxFallSpeed = 22f;

        [Header("콜라이더 (서 있을 때 / 숙였을 때)")]
        [SerializeField] private Vector2 standingSize = new Vector2(0.7f, 1.8f);
        [SerializeField] private Vector2 crouchingSize = new Vector2(0.7f, 1.0f);

        [Header("접지 판정")]
        [SerializeField, Tooltip("지면으로 취급할 레이어.")]
        private LayerMask groundLayers = ~0;

        [SerializeField, Tooltip("발밑 판정 박스의 두께.")]
        private float groundCheckThickness = 0.12f;

        [Header("디버그")]
        [SerializeField, Tooltip("씬 뷰에 접지/머리 판정 박스를 그린다.")]
        private bool drawDebugGizmos = true;

        /// <summary>지면에 닿아 있는가. M1의 회피·콤보 판정이 참조한다.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>숙이고 있는가. M1에서 상단 공격 회피 판정에 쓰인다.</summary>
        public bool IsCrouching { get; private set; }

        /// <summary>바라보는 방향. +1은 오른쪽, -1은 왼쪽.</summary>
        public int FacingDirection { get; private set; } = 1;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _collider;

        private readonly List<Collider2D> _overlapResults = new List<Collider2D>();
        private ContactFilter2D _groundFilter;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isRising;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CapsuleCollider2D>();

            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayers,
                useTriggers = false
            };

            ApplyColliderShape(standing: true);
        }

        private void OnEnable()
        {
            if (input == null)
            {
                Debug.LogError($"[{nameof(PlayerLocomotion)}] InputReader가 할당되지 않았습니다. 인스펙터에서 지정하세요.", this);
                return;
            }

            input.EnableGameplay();
            input.JumpPressed += OnJumpPressed;
            input.CrouchPressed += OnCrouchPressed;
            input.CrouchReleased += OnCrouchReleased;
        }

        private void OnDisable()
        {
            if (input == null)
            {
                return;
            }

            input.JumpPressed -= OnJumpPressed;
            input.CrouchPressed -= OnCrouchPressed;
            input.CrouchReleased -= OnCrouchReleased;
            input.DisableGameplay();
        }

        private void Update()
        {
            // 타이머는 프레임 단위로 줄인다 — 입력 반응성이 물리 스텝에 묶이지 않게.
            _coyoteTimer -= Time.deltaTime;
            _jumpBufferTimer -= Time.deltaTime;

            // 점프 버튼을 일찍 떼면 상승을 잘라 낮게 뛴다 (가변 점프).
            if (_isRising && input != null && !input.IsJumpHeld)
            {
                if (_rb.linearVelocityY > 0f)
                {
                    _rb.linearVelocityY *= jumpCutMultiplier;
                }
                _isRising = false;
            }
        }

        private void FixedUpdate()
        {
            UpdateGrounded();
            ApplyHorizontalMovement();
            ApplyJump();
            ApplyGravityFeel();
        }

        private void UpdateGrounded()
        {
            bool wasGrounded = IsGrounded;

            Vector2 center = (Vector2)transform.position + Vector2.down * (groundCheckThickness * 0.5f);
            Vector2 size = new Vector2(_collider.size.x * 0.9f, groundCheckThickness);
            IsGrounded = OverlapIgnoringSelf(center, size);

            if (IsGrounded)
            {
                _coyoteTimer = coyoteTime;
                if (!wasGrounded)
                {
                    _isRising = false;
                }
            }
        }

        private void ApplyHorizontalMovement()
        {
            float axis = input != null ? input.MoveAxis : 0f;

            if (!Mathf.Approximately(axis, 0f))
            {
                FacingDirection = axis > 0f ? 1 : -1;
            }

            float targetSpeed = axis * moveSpeed * (IsCrouching ? crouchSpeedMultiplier : 1f);
            float rate = Mathf.Approximately(axis, 0f) ? deceleration : acceleration;
            if (!IsGrounded)
            {
                rate *= airControl;
            }

            float newX = Mathf.MoveTowards(_rb.linearVelocityX, targetSpeed, rate * Time.fixedDeltaTime);
            _rb.linearVelocityX = newX;
        }

        private void ApplyJump()
        {
            bool canJump = _jumpBufferTimer > 0f && _coyoteTimer > 0f && !IsCrouching;
            if (!canJump)
            {
                return;
            }

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _isRising = true;

            // v = sqrt(2 * g * h) — 원하는 도달 높이를 초기 속도로 환산한다.
            float gravity = Mathf.Abs(Physics2D.gravity.y) * _rb.gravityScale;
            _rb.linearVelocityY = Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        private void ApplyGravityFeel()
        {
            _rb.gravityScale = _rb.linearVelocityY < 0f
                ? gravityScale * fallGravityMultiplier
                : gravityScale;

            if (_rb.linearVelocityY < -maxFallSpeed)
            {
                _rb.linearVelocityY = -maxFallSpeed;
            }
        }

        private void OnJumpPressed()
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        private void OnCrouchPressed()
        {
            if (IsCrouching)
            {
                return;
            }

            IsCrouching = true;
            ApplyColliderShape(standing: false);
        }

        private void OnCrouchReleased()
        {
            if (!IsCrouching || !HasHeadroom())
            {
                // 머리 위가 막혀 있으면 일어서지 않는다 — 벽에 끼는 것을 방지.
                return;
            }

            IsCrouching = false;
            ApplyColliderShape(standing: true);
        }

        private void ApplyColliderShape(bool standing)
        {
            Vector2 size = standing ? standingSize : crouchingSize;
            _collider.size = size;
            _collider.offset = new Vector2(0f, size.y * 0.5f); // 발밑이 원점
        }

        /// <summary>숙인 상태에서 일어설 공간이 있는지 검사한다.</summary>
        private bool HasHeadroom()
        {
            float extraHeight = standingSize.y - crouchingSize.y;
            if (extraHeight <= 0f)
            {
                return true;
            }

            Vector2 center = (Vector2)transform.position + Vector2.up * (crouchingSize.y + extraHeight * 0.5f);
            Vector2 size = new Vector2(standingSize.x * 0.9f, extraHeight);
            return !OverlapIgnoringSelf(center, size);
        }

        /// <summary>자기 자신의 콜라이더를 제외하고 겹침을 검사한다.</summary>
        private bool OverlapIgnoringSelf(Vector2 center, Vector2 size)
        {
            _overlapResults.Clear();
            Physics2D.OverlapBox(center, size, 0f, _groundFilter, _overlapResults);

            for (int i = 0; i < _overlapResults.Count; i++)
            {
                if (_overlapResults[i].attachedRigidbody != _rb)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Gizmos.color = Color.green;
            Vector2 groundCenter = (Vector2)transform.position + Vector2.down * (groundCheckThickness * 0.5f);
            float width = (Application.isPlaying && _collider != null ? _collider.size.x : standingSize.x) * 0.9f;
            Gizmos.DrawWireCube(groundCenter, new Vector3(width, groundCheckThickness, 0f));

            Gizmos.color = Color.cyan;
            float extraHeight = standingSize.y - crouchingSize.y;
            if (extraHeight > 0f)
            {
                Vector2 headCenter = (Vector2)transform.position + Vector2.up * (crouchingSize.y + extraHeight * 0.5f);
                Gizmos.DrawWireCube(headCenter, new Vector3(standingSize.x * 0.9f, extraHeight, 0f));
            }
        }
#endif
    }
}
