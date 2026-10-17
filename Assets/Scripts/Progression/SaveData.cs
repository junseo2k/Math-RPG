using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 영구 성장 로컬 저장. 손상·없음이면 초기 상태 (CombatDecisions 12장)
public static class SaveData
{
    [Serializable]
    class SaveFile
    {
        public int memoryShards;
        public int invested;
        public List<int> growthLevels = new();
    }

    static string FilePath => Path.Combine(Application.persistentDataPath, "slice_a_save.json");

    public static PersistentState Load()
    {
        var state = new PersistentState();
        try
        {
            if (!File.Exists(FilePath)) return state;
            var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
            if (file == null) return state;
            state.memoryShards = Mathf.Max(file.memoryShards, 0);
            state.invested = Mathf.Max(file.invested, 0);
            for (int i = 0; i < state.growthLevels.Count && i < file.growthLevels.Count; i++)
                state.growthLevels[i] = Mathf.Max(file.growthLevels[i], 0);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"저장 파일을 읽지 못해 초기 상태로 시작: {e.Message}");
            return new PersistentState();
        }
        return state;
    }

    public static void Save(PersistentState state)
    {
        try
        {
            var file = new SaveFile { memoryShards = state.memoryShards, invested = state.invested, growthLevels = new List<int>(state.growthLevels) };
            File.WriteAllText(FilePath, JsonUtility.ToJson(file));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"저장 실패: {e.Message}");
        }
    }
}
