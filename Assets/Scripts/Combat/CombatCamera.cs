using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 전투에 반응하는 카메라 — 화면 흔들림 + 줌 펀치.
    ///
    /// <b>흔들림은 플레이어가 맞았을 때만</b> 걸린다. 카메라는 플레이어의 시점이므로,
    /// 흔들림은 "내가 충격을 받았다"는 신호여야 한다. 내가 때릴 때마다 흔들면 그 의미가
    /// 희석되고, 평타를 연타하는 동안 화면이 계속 떨려 오히려 보기 불편해진다.
    /// 몬스터가 쓰러질 때도 흔들지 않는다 — 내 몸이 받은 충격이 아니기 때문이다.
    ///
    /// 반면 <b>줌 펀치는 타격이 성사되면 항상</b> 작동한다. 흔들림과 달리 화면을 어지럽히지
    /// 않으면서 "꽂혔다"를 짧게 짚어주는 역할이라, 내 공격이 맞았을 때도 켜두는 편이 낫다.
    /// 원하지 않으면 zoomPunch를 0으로 두면 된다.
    ///
    /// 흔들림은 트라우마 모델이다 — 피격 때 _trauma를 올리고 매 프레임 일정 속도로 깎으며,
    /// 실제 흔들림 세기는 trauma²을 쓴다. 제곱을 쓰는 이유는 감쇠의 끝자락이 부드럽게
    /// 사라지기 때문 — 선형으로 깎으면 흔들림이 뚝 끊긴 것처럼 보인다.
    /// 무작위 값이 아니라 Perlin 노이즈를 쓰는 것도 같은 이유로, 프레임마다 튀지 않고
    /// 연속적인 궤적을 그려야 '진동'으로 읽힌다.
    ///
    /// 붙이는 위치: Main Camera (직교 투영).
    ///
    /// ※ 여기 수치는 기획서 7장 미정 수치가 아니라 M1 타격감 튜닝용 임시값이다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CombatCamera : MonoBehaviour
    {
        [Header("흔들림 세기 (임시값)")]
        [SerializeField, Min(0f), Tooltip("피해량 1당 쌓이는 트라우마. 트라우마는 0~1로 잘린다.")]
        private float traumaPerDamage = 0.06f;

        [SerializeField, Range(0f, 1f), Tooltip("한 방으로 쌓을 수 있는 트라우마 상한. 연타로 화면이 폭주하는 것을 막는다.")]
        private float traumaPerHitCap = 0.85f;

        [SerializeField, Min(0f), Tooltip("트라우마가 초당 깎이는 양. 클수록 빨리 잦아든다.")]
        private float traumaDecayPerSecond = 2.2f;

        [Header("흔들림 모양")]
        [SerializeField, Min(0f), Tooltip("최대 흔들림 거리 (units).")]
        private float maxOffset = 0.35f;

        [SerializeField, Min(0f), Tooltip("최대 흔들림 회전 (도). 0이면 회전 없이 평행 이동만.")]
        private float maxAngle = 2.5f;

        [SerializeField, Min(0f), Tooltip("떨림의 빠르기. 클수록 잘게 떤다.")]
        private float frequency = 26f;

        [Header("누가 맞았을 때 흔들 것인가")]
        [SerializeField, Tooltip("이 태그를 가진 대상이 맞았을 때만 흔든다. 비우면 누가 맞든 흔든다.")]
        private string shakeVictimTag = "Player";

        [SerializeField, Min(0f), Tooltip("위 대상이 쓰러질 때 쌓이는 트라우마. 0이면 사망 시 흔들지 않는다. " +
                                          "몬스터 사망은 태그가 달라 어차피 걸러진다.")]
        private float deathTrauma = 0.5f;

        [Header("줌 펀치")]
        [SerializeField, Range(0f, 0.3f), Tooltip("타격 순간 화면이 확 당겨지는 비율. 0이면 줌 없음.")]
        private float zoomPunch = 0.035f;

        [SerializeField, Min(0f), Tooltip("당겨진 화면이 원래 크기로 돌아오는 속도 (초당 비율).")]
        private float zoomRecoverPerSecond = 4f;

        [Header("시간")]
        [SerializeField, Tooltip("켜면 히트스톱(timeScale = 0) 동안에도 흔들린다. " +
                                 "끄면 화면이 완전히 얼었다가 풀리는 순간 터진다 — 어느 쪽이 나은지는 직접 눌러보고 정할 것.")]
        private bool shakeDuringHitstop = false;

        private Camera _camera;
        private float _baseOrthographicSize;
        private Quaternion _baseRotation;

        private float _trauma;
        private float _zoom;
        private float _noiseTime;

        // 이번 프레임에 우리가 더한 오프셋. 다음 프레임에 그대로 빼서 원위치를 복원한다.
        // 위치를 Awake 시점 값으로 기억하지 않는 이유는, 나중에 카메라 추적 스크립트가
        // 붙어도 이 컴포넌트를 고치지 않아도 되게 하기 위함이다.
        private Vector3 _appliedOffset;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseOrthographicSize = _camera.orthographicSize;
            _baseRotation = transform.localRotation;
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

            // 흔들리던 도중 꺼지면 카메라가 비뚤어진 채로 굳는다.
            transform.position -= _appliedOffset;
            _appliedOffset = Vector3.zero;
            transform.localRotation = _baseRotation;
            _camera.orthographicSize = _baseOrthographicSize;
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            // 줌 펀치는 타격이 성사되면 누가 맞았든 작동한다 — 내 공격이 꽂힌 것도 짚어줘야 한다.
            _zoom = Mathf.Max(_zoom, 1f);

            if (!ShouldShakeFor(e.Victim))
            {
                return;
            }

            AddTrauma(Mathf.Min(e.Amount * traumaPerDamage, traumaPerHitCap));
        }

        private void OnDied(CharacterDiedEvent e)
        {
            if (deathTrauma <= 0f || !ShouldShakeFor(e.Victim))
            {
                return;
            }

            AddTrauma(deathTrauma);
        }

        /// <summary>이 대상이 맞은 것에 카메라가 흔들려야 하는가. 기본은 플레이어일 때만.</summary>
        private bool ShouldShakeFor(GameObject victim)
        {
            if (string.IsNullOrEmpty(shakeVictimTag))
            {
                return true;
            }

            return victim != null && victim.CompareTag(shakeVictimTag);
        }

        /// <summary>외부에서도 흔들 수 있게 열어둔다 (착지, 보스 등장, 즉사 장판 등).</summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
        }

        // LateUpdate에서 처리한다 — 이동·추적이 카메라 위치를 정한 뒤에 흔들림을 얹어야 한다.
        private void LateUpdate()
        {
            // 지난 프레임의 흔들림을 먼저 되돌린다. 그래야 오프셋이 누적되지 않는다.
            transform.position -= _appliedOffset;
            _appliedOffset = Vector3.zero;

            float dt = shakeDuringHitstop ? Time.unscaledDeltaTime : Time.deltaTime;

            _trauma = Mathf.Max(0f, _trauma - traumaDecayPerSecond * dt);
            _zoom = Mathf.Max(0f, _zoom - zoomRecoverPerSecond * dt);

            if (_trauma > 0f)
            {
                _noiseTime += dt * frequency;
                float shake = _trauma * _trauma;

                // 축마다 노이즈 좌표를 다른 곳에서 샘플링해야 x·y가 같이 움직이지 않는다
                // (같으면 대각선으로만 흔들려 '진동'이 아니라 '미끄러짐'으로 보인다).
                float x = (Mathf.PerlinNoise(_noiseTime, 0.31f) * 2f - 1f) * maxOffset * shake;
                float y = (Mathf.PerlinNoise(0.73f, _noiseTime) * 2f - 1f) * maxOffset * shake;
                float angle = (Mathf.PerlinNoise(_noiseTime, 9.21f) * 2f - 1f) * maxAngle * shake;

                _appliedOffset = new Vector3(x, y, 0f);
                transform.position += _appliedOffset;
                transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                transform.localRotation = _baseRotation;
            }

            _camera.orthographicSize = _baseOrthographicSize * (1f - zoomPunch * _zoom);
        }
    }
}
