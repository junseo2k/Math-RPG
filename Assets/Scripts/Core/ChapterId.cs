namespace MathRPG.Core
{
    /// <summary>
    /// 기획서 3장 챕터 구조. 고3 범위는 스코프에서 제외되어 챕터3(고2)이 마지막이다.
    /// 세이브 슬롯 표시와 진행도 저장의 기준 단위이며,
    /// M4에서 개념 트리·단원표의 공통 ID 체계와 연결된다 (CLAUDE.md 4-4).
    /// </summary>
    public enum ChapterId
    {
        Tutorial = 0,
        Chapter1 = 1,
        Chapter2 = 2,
        Chapter3 = 3
    }

    public static class ChapterIdExtensions
    {
        /// <summary>세이브 슬롯 등 UI에 표시할 이름.</summary>
        public static string ToDisplayName(this ChapterId chapter)
        {
            switch (chapter)
            {
                case ChapterId.Tutorial: return "튜토리얼";
                case ChapterId.Chapter1: return "챕터 1";
                case ChapterId.Chapter2: return "챕터 2";
                case ChapterId.Chapter3: return "챕터 3";
                default: return "알 수 없음";
            }
        }

        /// <summary>기획서 3장의 학년 범위. 슬롯 부가 정보용.</summary>
        public static string ToGradeRange(this ChapterId chapter)
        {
            switch (chapter)
            {
                case ChapterId.Tutorial: return "실력 진단";
                case ChapterId.Chapter1: return "중1~중2";
                case ChapterId.Chapter2: return "중3~고1";
                case ChapterId.Chapter3: return "고2";
                default: return string.Empty;
            }
        }
    }
}
