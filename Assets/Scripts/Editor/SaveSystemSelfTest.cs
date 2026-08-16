using System.Collections.Generic;
using System.Text;
using MathRPG.Core;
using UnityEditor;
using UnityEngine;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 세이브 시스템이 실제로 디스크에 쓰고 다시 읽는지 확인하는 자체 검사.
    ///
    /// 비어 있는 슬롯 하나를 빌려 쓰고 끝나면 반드시 지운다.
    /// 빈 슬롯이 없으면 아무것도 건드리지 않고 중단한다 — 검사 때문에 진행이 날아가면 안 된다.
    /// </summary>
    public static class SaveSystemSelfTest
    {
        [MenuItem("MathRPG/Diagnostics/Run Save System Self-Test", priority = 91)]
        public static void Run()
        {
            int slot = FindEmptySlot();
            if (slot < 0)
            {
                Debug.LogWarning("[SaveSystemSelfTest] 빈 슬롯이 없어 검사를 건너뜁니다. " +
                                 "기존 세이브를 건드리지 않기 위한 안전장치입니다.");
                return;
            }

            var failures = new List<string>();
            var log = new StringBuilder();
            log.AppendLine("[SaveSystemSelfTest] 슬롯 " + slot + "로 검사 시작");
            log.AppendLine("  저장 위치: " + SaveSystem.SaveDirectory);

            try
            {
                // 1) 새 게임 생성
                Check(failures, GameSession.StartNew(slot), "새 게임 생성");
                Check(failures, SaveSystem.Exists(slot), "생성 후 파일 존재");
                Check(failures, GameSession.ActiveSlot == slot, "활성 슬롯 번호 일치");
                Check(failures, GameSession.Current != null && GameSession.Current.ChapterId == ChapterId.Tutorial,
                    "새 게임의 시작 챕터는 튜토리얼");
                Check(failures, GameSession.Current != null && GameSession.Current.nodeIndex == 1,
                    "새 게임의 시작 노드는 1");

                // 2) 진행도 변경 후 저장 → 다시 읽기
                GameSession.Current.ChapterId = ChapterId.Chapter2;
                GameSession.Current.nodeIndex = 7;
                GameSession.Current.playTimeSeconds = 3725d;
                Check(failures, GameSession.SaveNow(), "진행도 저장");

                SaveData reloaded = SaveSystem.Load(slot);
                Check(failures, reloaded != null, "저장한 세이브 다시 읽기");

                if (reloaded != null)
                {
                    Check(failures, reloaded.ChapterId == ChapterId.Chapter2, "챕터 왕복 일치 (Chapter2)");
                    Check(failures, reloaded.nodeIndex == 7, "노드 왕복 일치 (7)");
                    Check(failures, Mathf.Abs((float)(reloaded.playTimeSeconds - 3725d)) < 0.001f,
                        "플레이 시간 왕복 일치 (3725초)");
                    Check(failures, reloaded.version == SaveData.Version, "세이브 버전 기록");
                    Check(failures, reloaded.LastSavedUtc.Year > 2000, "저장 시각 기록");
                    log.AppendLine("  표시 예시: 현재 스테이지: " + reloaded.ChapterId.ToDisplayName()
                                   + " · 노드 " + reloaded.nodeIndex);
                }

                // 3) 이어하기
                GameSession.End();
                Check(failures, !GameSession.HasActiveSession, "세션 종료 후 활성 세션 없음");
                Check(failures, GameSession.Continue(slot), "이어하기");
                Check(failures, GameSession.Current != null && GameSession.Current.nodeIndex == 7,
                    "이어하기 후 진행도 유지");

                // 4) 삭제
                Check(failures, SaveSystem.Delete(slot), "삭제 실행");
                Check(failures, !SaveSystem.Exists(slot), "삭제 후 파일 없음");
                Check(failures, SaveSystem.Load(slot) == null, "삭제된 슬롯 읽기는 null");
            }
            finally
            {
                // 검사가 중간에 실패해도 흔적을 남기지 않는다.
                if (SaveSystem.Exists(slot))
                {
                    SaveSystem.Delete(slot);
                }
                GameSession.End();
            }

            if (failures.Count == 0)
            {
                log.AppendLine("  결과: 전체 통과");
                Debug.Log(log.ToString());
            }
            else
            {
                log.AppendLine("  결과: " + failures.Count + "건 실패");
                for (int i = 0; i < failures.Count; i++)
                {
                    log.AppendLine("    ✗ " + failures[i]);
                }
                Debug.LogError(log.ToString());
            }
        }

        private static int FindEmptySlot()
        {
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                if (!SaveSystem.Exists(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Check(List<string> failures, bool condition, string description)
        {
            if (!condition)
            {
                failures.Add(description);
            }
        }
    }
}
