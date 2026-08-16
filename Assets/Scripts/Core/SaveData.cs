using System;

namespace MathRPG.Core
{
    /// <summary>
    /// 세이브 파일 1개의 내용. JsonUtility로 직렬화되므로
    /// public 필드만 쓰고, 프로퍼티나 딕셔너리는 넣지 않는다.
    ///
    /// 지금은 진행도 뼈대만 있다. 마나/숙련도/개념 트리 진행/계산기 특수 기능 잔여량 등은
    /// 해당 시스템이 생기는 마일스톤에서 필드를 추가하며, 그때 <see cref="Version"/>을 올리고
    /// <see cref="SaveSystem"/>에 마이그레이션을 추가한다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>세이브 포맷 버전. 필드를 추가/변경할 때마다 올린다.</summary>
        public const int Version = 1;

        public int version = Version;

        /// <summary>현재 챕터. 슬롯 목록에 "챕터 1"처럼 표시된다.</summary>
        public int chapter = (int)ChapterId.Tutorial;

        /// <summary>챕터 안에서의 진행 노드 번호. M5에서 실제 맵 노드 구조와 연결한다.</summary>
        public int nodeIndex = 1;

        /// <summary>누적 플레이 시간(초).</summary>
        public double playTimeSeconds;

        /// <summary>마지막 저장 시각. ISO 8601 UTC 문자열로 보관한다.</summary>
        public string lastSavedUtc = string.Empty;

        public ChapterId ChapterId
        {
            get { return (ChapterId)chapter; }
            set { chapter = (int)value; }
        }

        public DateTime LastSavedUtc
        {
            get
            {
                DateTime parsed;
                bool ok = DateTime.TryParse(
                    lastSavedUtc,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out parsed);
                return ok ? parsed : DateTime.MinValue;
            }
        }

        public void StampSaveTime()
        {
            lastSavedUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>새 게임의 초기 상태.</summary>
        public static SaveData CreateNew()
        {
            var data = new SaveData
            {
                version = Version,
                chapter = (int)Core.ChapterId.Tutorial,
                nodeIndex = 1,
                playTimeSeconds = 0d
            };
            data.StampSaveTime();
            return data;
        }
    }
}
