using TMPro;
using UnityEngine;

namespace MathRPG.UI
{
    /// <summary>
    /// 피격 지점에서 떠오르며 사라지는 데미지 숫자 하나.
    ///
    /// 스스로 생기지 않는다 — DamagePopupSpawner가 풀에서 꺼내 Play()로 재생하고,
    /// 수명이 끝나면 스스로 비활성화되어 풀로 돌아간다.
    ///
    /// 시간은 Time.deltaTime(스케일 적용)을 쓴다. 히트스톱 동안 숫자도 같이 멈춰야
    /// 타격이 "박히는" 느낌이 살기 때문이다.
    ///
    /// ※ 여기 수치는 전부 M1 연출 튜닝용 임시값이다.
    /// </summary>
    public sealed class DamagePopup : MonoBehaviour
    {
        [SerializeField, Tooltip("숫자를 그릴 텍스트. 비우면 자신에게서 찾는다.")]
        private TextMeshPro label;

        [SerializeField, Min(0.05f), Tooltip("떠올랐다 사라지기까지 걸리는 시간 (초).")]
        private float lifetimeSeconds = 0.6f;

        [SerializeField, Tooltip("위로 떠오르는 거리 (units).")]
        private float riseDistance = 0.9f;

        [SerializeField, Tooltip("맞은 방향으로 흩어지는 거리 (units).")]
        private float driftDistance = 0.35f;

        [SerializeField, Tooltip("시간에 따른 상승 정도. 처음에 빠르게 솟았다가 느려진다.")]
        private AnimationCurve riseCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2.5f, 2.5f),
            new Keyframe(1f, 1f, 0f, 0f));

        [SerializeField, Tooltip("시간에 따른 불투명도. 끝에서만 흐려진다.")]
        private AnimationCurve alphaCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.55f, 1f),
            new Keyframe(1f, 0f));

        [SerializeField, Tooltip("튀어나오는 느낌을 주는 크기 배수.")]
        private AnimationCurve scaleCurve = new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.2f, 1.15f),
            new Keyframe(1f, 1f));

        private Vector3 _origin;
        private Vector3 _baseScale = Vector3.one;
        private Color _color = Color.white;
        private float _elapsed;
        private int _direction = 1;

        /// <summary>지금 재생 중인가. 스포너가 풀에서 고를 때 본다.</summary>
        public bool IsBusy => gameObject.activeSelf;

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponent<TextMeshPro>();
            }

            _baseScale = transform.localScale;
        }

        /// <summary>숫자를 띄운다. direction은 맞은 방향(+1 오른쪽 / -1 왼쪽).</summary>
        public void Play(Vector3 worldPosition, float amount, Color color, int direction)
        {
            if (label == null)
            {
                return;
            }

            _origin = worldPosition;
            _color = color;
            _direction = direction < 0 ? -1 : 1;
            _elapsed = 0f;

            label.text = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(amount))).ToString();

            gameObject.SetActive(true);
            Step(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= lifetimeSeconds)
            {
                gameObject.SetActive(false);
                return;
            }

            Step(_elapsed / lifetimeSeconds);
        }

        private void Step(float t)
        {
            transform.position = _origin
                                 + Vector3.up * (riseDistance * riseCurve.Evaluate(t))
                                 + Vector3.right * (driftDistance * t * _direction);

            transform.localScale = _baseScale * scaleCurve.Evaluate(t);

            Color color = _color;
            color.a = alphaCurve.Evaluate(t);
            label.color = color;
        }
    }
}
