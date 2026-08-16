using System;
using System.IO;
using UnityEngine;

namespace MathRPG.Core
{
    /// <summary>
    /// 슬롯 단위 세이브 파일 읽기/쓰기/삭제.
    /// 파일은 <c>{persistentDataPath}/Saves/slot_0.json</c> 형태로 저장된다.
    ///
    /// 쓰기는 임시 파일에 먼저 기록한 뒤 교체하는 방식이라,
    /// 저장 도중 게임이 죽어도 기존 세이브가 깨지지 않는다.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>세이브 슬롯 개수.</summary>
        public const int SlotCount = 5;

        private const string SaveFolderName = "Saves";
        private const string FileExtension = ".json";
        private const string TempExtension = ".tmp";

        /// <summary>슬롯이 저장/삭제될 때 발생. UI가 목록을 갱신하는 데 쓴다.</summary>
        public static event Action<int> SlotChanged;

        public static string SaveDirectory
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFolderName); }
        }

        public static bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < SlotCount;
        }

        public static string GetSlotPath(int slot)
        {
            return Path.Combine(SaveDirectory, "slot_" + slot + FileExtension);
        }

        public static bool Exists(int slot)
        {
            return IsValidSlot(slot) && File.Exists(GetSlotPath(slot));
        }

        /// <summary>
        /// 슬롯을 읽는다. 파일이 없거나 내용이 깨졌으면 null을 반환한다.
        /// 깨진 파일을 조용히 새 세이브로 덮어쓰지 않는다 — 플레이어의 진행이 말없이 사라지는 게 최악이다.
        /// </summary>
        public static SaveData Load(int slot)
        {
            if (!Exists(slot))
            {
                return null;
            }

            string path = GetSlotPath(slot);
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    Debug.LogError("[SaveSystem] 슬롯 " + slot + " 파싱 결과가 비어 있습니다: " + path);
                    return null;
                }

                return Migrate(data, slot);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] 슬롯 " + slot + " 읽기 실패: " + e.Message);
                return null;
            }
        }

        public static bool Save(int slot, SaveData data)
        {
            if (!IsValidSlot(slot))
            {
                Debug.LogError("[SaveSystem] 잘못된 슬롯 번호: " + slot);
                return false;
            }

            if (data == null)
            {
                Debug.LogError("[SaveSystem] 저장할 데이터가 null입니다. 슬롯 " + slot);
                return false;
            }

            try
            {
                Directory.CreateDirectory(SaveDirectory);

                data.version = SaveData.Version;
                data.StampSaveTime();

                string path = GetSlotPath(slot);
                string tempPath = path + TempExtension;

                File.WriteAllText(tempPath, JsonUtility.ToJson(data, prettyPrint: true));

                // 임시 파일 → 실제 파일 교체. 기존 파일이 있으면 덮어쓴다.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);

                RaiseSlotChanged(slot);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] 슬롯 " + slot + " 저장 실패: " + e.Message);
                return false;
            }
        }

        public static bool Delete(int slot)
        {
            if (!Exists(slot))
            {
                return false;
            }

            try
            {
                File.Delete(GetSlotPath(slot));
                RaiseSlotChanged(slot);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] 슬롯 " + slot + " 삭제 실패: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 구버전 세이브를 현재 포맷으로 올린다.
        /// 지금은 버전이 1뿐이라 할 일이 없지만, 필드를 추가할 때 여기에 단계별 변환을 넣는다.
        /// </summary>
        private static SaveData Migrate(SaveData data, int slot)
        {
            if (data.version > SaveData.Version)
            {
                Debug.LogWarning("[SaveSystem] 슬롯 " + slot + "의 세이브 버전(" + data.version +
                                 ")이 현재 지원 버전(" + SaveData.Version + ")보다 높습니다. 그대로 읽습니다.");
            }

            return data;
        }

        private static void RaiseSlotChanged(int slot)
        {
            Action<int> handler = SlotChanged;
            if (handler != null)
            {
                handler.Invoke(slot);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            SlotChanged = null;
        }
    }
}
