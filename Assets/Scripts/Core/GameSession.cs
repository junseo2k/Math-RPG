using UnityEngine;

namespace MathRPG.Core
{
    /// <summary>
    /// 지금 플레이 중인 세이브 슬롯을 들고 있는 런타임 상태.
    /// 씬이 바뀌어도 유지되어야 하므로 정적 클래스로 둔다.
    ///
    /// 게임 로직은 이 클래스의 <see cref="Current"/>를 직접 수정하고,
    /// 저장 시점(세이브 포인트 등)에 <see cref="SaveNow"/>를 호출한다.
    /// </summary>
    public static class GameSession
    {
        public const int NoSlot = -1;

        public static int ActiveSlot { get; private set; } = NoSlot;
        public static SaveData Current { get; private set; }

        public static bool HasActiveSession
        {
            get { return Current != null && SaveSystem.IsValidSlot(ActiveSlot); }
        }

        /// <summary>기존 세이브를 이어서 시작한다.</summary>
        public static bool Continue(int slot)
        {
            SaveData data = SaveSystem.Load(slot);
            if (data == null)
            {
                Debug.LogError("[GameSession] 슬롯 " + slot + "을 불러올 수 없어 세션을 시작하지 못했습니다.");
                return false;
            }

            ActiveSlot = slot;
            Current = data;
            return true;
        }

        /// <summary>새 게임을 시작한다. 슬롯에 즉시 초기 세이브를 기록한다.</summary>
        public static bool StartNew(int slot)
        {
            if (!SaveSystem.IsValidSlot(slot))
            {
                Debug.LogError("[GameSession] 잘못된 슬롯 번호: " + slot);
                return false;
            }

            SaveData data = SaveData.CreateNew();
            if (!SaveSystem.Save(slot, data))
            {
                return false;
            }

            ActiveSlot = slot;
            Current = data;
            return true;
        }

        /// <summary>현재 세션을 활성 슬롯에 기록한다.</summary>
        public static bool SaveNow()
        {
            if (!HasActiveSession)
            {
                Debug.LogWarning("[GameSession] 활성 세션이 없어 저장을 건너뜁니다.");
                return false;
            }

            return SaveSystem.Save(ActiveSlot, Current);
        }

        /// <summary>메인 메뉴로 돌아갈 때 등, 세션을 비운다. 자동 저장하지 않는다.</summary>
        public static void End()
        {
            ActiveSlot = NoSlot;
            Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            ActiveSlot = NoSlot;
            Current = null;
        }
    }
}
