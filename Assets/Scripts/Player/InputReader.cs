using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG.Player
{
    /// <summary>
    /// 입력 장치와 게임 로직 사이의 단일 경계면.
    /// 다른 시스템은 Input System을 직접 참조하지 않고 이 ScriptableObject의 이벤트만 구독한다
    /// — 나중에 키 리바인딩이나 모바일 가상 패드를 붙일 때 이 파일만 고치면 된다.
    ///
    /// 액션 맵 구성 (기획서 5-1: "조작은 쉽게, 두뇌는 수학에 집중"):
    ///   이동    좌우 2방향만 (2D 횡스크롤)
    ///   점프    회피 수단 1
    ///   숙이기  회피 수단 2
    ///   평타    문제 없이 즉시 발동      → M1에서 사용
    ///   스킬    문제 발생 + 마나 소모    → M2~M3에서 사용
    ///
    /// M0 현재는 바인딩을 코드에 직접 정의한다. 리바인딩 UI가 필요해지는 시점에
    /// .inputactions 에셋으로 옮기되, 이 클래스의 공개 API는 그대로 유지할 것.
    /// </summary>
    [CreateAssetMenu(menuName = "MathRPG/Input Reader", fileName = "InputReader")]
    public sealed class InputReader : ScriptableObject
    {
        /// <summary>스킬 슬롯 개수. 기획서에 확정 수치가 없어 임시로 3칸.</summary>
        public const int SkillSlotCount = 3;

        public event Action JumpPressed;
        public event Action CrouchPressed;
        public event Action CrouchReleased;
        public event Action AttackPressed;
        public event Action<int> SkillPressed;

        /// <summary>-1(좌) ~ +1(우). 매 프레임 폴링해서 읽는다.</summary>
        public float MoveAxis => _move != null && _move.enabled ? _move.ReadValue<float>() : 0f;

        public bool IsCrouchHeld => _crouch != null && _crouch.enabled && _crouch.IsPressed();

        /// <summary>점프 버튼을 누르고 있는지. 짧게 누르면 낮게 뛰는 가변 점프에 쓰인다.</summary>
        public bool IsJumpHeld => _jump != null && _jump.enabled && _jump.IsPressed();

        private InputAction _move;
        private InputAction _jump;
        private InputAction _crouch;
        private InputAction _attack;
        private InputAction[] _skills;

        private bool _built;

        private void OnEnable()
        {
            BuildActions();
        }

        private void OnDisable()
        {
            DisableGameplay();
            DisposeActions();
        }

        /// <summary>게임플레이 입력을 켠다. 플레이어 컨트롤러의 OnEnable에서 호출.</summary>
        public void EnableGameplay()
        {
            BuildActions();

            _move.Enable();
            _jump.Enable();
            _crouch.Enable();
            _attack.Enable();
            for (int i = 0; i < _skills.Length; i++)
            {
                _skills[i].Enable();
            }
        }

        /// <summary>게임플레이 입력을 끈다. 컷신·문제 UI·일시정지 진입 시 호출.</summary>
        public void DisableGameplay()
        {
            if (!_built)
            {
                return;
            }

            _move.Disable();
            _jump.Disable();
            _crouch.Disable();
            _attack.Disable();
            for (int i = 0; i < _skills.Length; i++)
            {
                _skills[i].Disable();
            }
        }

        private void BuildActions()
        {
            if (_built)
            {
                return;
            }

            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("1DAxis")
                 .With("Negative", "<Keyboard>/a")
                 .With("Positive", "<Keyboard>/d");
            _move.AddCompositeBinding("1DAxis")
                 .With("Negative", "<Keyboard>/leftArrow")
                 .With("Positive", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick/x");

            _jump = new InputAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");

            _crouch = new InputAction("Crouch", InputActionType.Button);
            _crouch.AddBinding("<Keyboard>/s");
            _crouch.AddBinding("<Keyboard>/downArrow");
            _crouch.AddBinding("<Gamepad>/buttonEast");

            _attack = new InputAction("Attack", InputActionType.Button);
            _attack.AddBinding("<Mouse>/leftButton");
            _attack.AddBinding("<Keyboard>/j");
            _attack.AddBinding("<Gamepad>/buttonWest");

            _skills = new InputAction[SkillSlotCount];
            string[] skillKeys = { "<Keyboard>/1", "<Keyboard>/2", "<Keyboard>/3" };
            string[] skillPadButtons = { "<Gamepad>/leftShoulder", "<Gamepad>/rightShoulder", "<Gamepad>/buttonNorth" };
            for (int i = 0; i < SkillSlotCount; i++)
            {
                var action = new InputAction($"Skill{i + 1}", InputActionType.Button);
                action.AddBinding(skillKeys[i]);
                action.AddBinding(skillPadButtons[i]);
                _skills[i] = action;
            }

            _jump.performed += OnJumpPerformed;
            _crouch.performed += OnCrouchPerformed;
            _crouch.canceled += OnCrouchCanceled;
            _attack.performed += OnAttackPerformed;
            for (int i = 0; i < SkillSlotCount; i++)
            {
                int slot = i; // 클로저가 루프 변수를 캡처하지 않도록 복사
                _skills[i].performed += _ => SkillPressed?.Invoke(slot);
            }

            _built = true;
        }

        private void DisposeActions()
        {
            if (!_built)
            {
                return;
            }

            _jump.performed -= OnJumpPerformed;
            _crouch.performed -= OnCrouchPerformed;
            _crouch.canceled -= OnCrouchCanceled;
            _attack.performed -= OnAttackPerformed;

            _move.Dispose();
            _jump.Dispose();
            _crouch.Dispose();
            _attack.Dispose();
            for (int i = 0; i < _skills.Length; i++)
            {
                _skills[i].Dispose();
            }

            _built = false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext _) => JumpPressed?.Invoke();
        private void OnCrouchPerformed(InputAction.CallbackContext _) => CrouchPressed?.Invoke();
        private void OnCrouchCanceled(InputAction.CallbackContext _) => CrouchReleased?.Invoke();
        private void OnAttackPerformed(InputAction.CallbackContext _) => AttackPressed?.Invoke();
    }
}
