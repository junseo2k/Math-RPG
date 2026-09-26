namespace MathRPG
{
    public static class TrainingCourseText
    {
        public static string Objective(TrainingGame game)
        {
            if (game.Rival.InEncounter)
                return game.Rival.SecondPattern ? "라이벌의 검로가 달라졌다\n예고를 읽고 빈틈에 반격" : "라이벌과의 결투\n검격과 같은 흐름의 겨루기";
            if (game.Trials.SparringActive) return "교관과 대련 중\n왼쪽 안전 구역으로 물러나면 중단";
            if (game.player.transform.position.x <= TrainingCourseView.EntranceX)
                return game.Course.Index > 0 ? "[E] 이전 훈련소로 돌아가기" : "첫 훈련소의 입구";
            if (game.player.transform.position.x >= TrainingCourseView.ExitX)
                return !game.StageProgress.cleared ? "아직 닫힌 길\n이곳 교관의 인정을 받아야 합니다" :
                    game.Course.Index == 3 ? (game.Progress.rivalDefeated ? "[E] 정령의 숲으로 이동\n반디샘으로 향하기" : "[E] 숲으로 이어지는 길\n라이벌과의 결투에 진입") : "[E] 다음 훈련소로 이동";
            if (game.NearInstructor)
                return "[E] " + game.Stage.instructorName + "\n" + (game.StageProgress.cleared ? "대화하고 생명·정령의 힘 회복" : "교관의 시험에 도전");
            int chest = game.NearbyChest;
            if (chest >= 0)
                return game.StageProgress.ChestOpen(chest) ? "봉인이 풀린 상자" : "[E] 봉인 상자 " + (chest + 1) + " 열기\n실패해도 생명을 잃지 않습니다";
            if (game.StageProgress.cleared)
                return game.Course.Index < 3 ? "교관의 인정을 받았다\n오른쪽 문에서 E로 다음 훈련소 이동" : game.Progress.rivalDefeated ? "오른쪽 문에서 E로 정령의 숲 이동\n왼쪽 문으로 훈련소 재방문" : "네 훈련소 수련 완료\n오른쪽 문에서 E로 라이벌에게 도전";
            if (!game.StageProgress.trialAccepted) return "왼쪽의 " + game.Stage.instructorName + "에게\n다가가 E로 시험 수락";
            switch (game.Stage.trialKind)
            {
                case InstructorTrialKind.Hunt:
                    return "교관의 사냥 의뢰\n처치 " + game.StageProgress.huntKills + " / " + game.Stage.huntTarget;
                case InstructorTrialKind.Treasure:
                    int count = (game.StageProgress.ChestOpen(0) ? 1 : 0) + (game.StageProgress.ChestOpen(1) ? 1 : 0);
                    return "봉인을 풀어 두 상자 열기\n개봉 " + count + " / 2";
                case InstructorTrialKind.Sparring: return "교관에게 다시 도전할 수 있다\n왼쪽 교관 앞에서 E";
                default: return "교관에게 룬의 흐름 보여주기\n왼쪽 교관 앞에서 E";
            }
        }

        public static string Reward(TrainingGame game)
        {
            int books = game.Progress.skillBooks;
            string growth = books == 1 ? "큰 스킬의 준비가 두 단계에서 한 단계로 짧아집니다." :
                books == game.definition.runeSkill.booksForInstantCast ? "이제 큰 스킬을 문제 없이 즉시 발동할 수 있습니다." : "큰 스킬의 위력이 8 증가했습니다.";
            return growth + "\n" + (game.Course.Index < 3 ? "오른쪽 문에서 E를 눌러 다음 훈련소로 이동하세요." : "네 훈련소 수련을 마쳤습니다. 오른쪽 문 너머에 라이벌이 기다립니다.");
        }
    }
}
