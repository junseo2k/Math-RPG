using UnityEngine;
using UnityEngine.UI;

namespace MathRPG
{
    public sealed class ChapterFourUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        Button advance;
        RectTransform protagonist;
        Vector2 protagonistOrigin;
        Image rune;
        Image[] magic;

        public string Objective
        {
            get
            {
                switch (game.Progress.chapterFour.beat)
                {
                    case ChapterFourBeat.Reunion: return "라이벌1과 재회";
                    case ChapterFourBeat.Provocation: return "움직이지 않는 몸";
                    case ChapterFourBeat.Collapse: return "시작되지 못한 대결";
                    case ChapterFourBeat.Parents: return "다시 꺼내진 부모님의 이야기";
                    case ChapterFourBeat.Anger: return "억눌렀던 목소리";
                    case ChapterFourBeat.Overload: return "흐려지는 손끝";
                    case ChapterFourBeat.InnerWorld: return "정령의 정체";
                    case ChapterFourBeat.SpiritPast: return "다른 가능세계의 기억";
                    case ChapterFourBeat.Consolation: return "나를 위해 살아간다는 것";
                    case ChapterFourBeat.Question: return "가장 찬란했던 순간을 떠올리기";
                    case ChapterFourBeat.Memory: return "혼자 풀어냈던 기쁨";
                    case ChapterFourBeat.Calm: return "그 감각을 붙잡고 숨 고르기";
                    case ChapterFourBeat.Awakening: return "스스로 일어서다";
                    default: return "재앙 속에 남겨진 이야기";
                }
            }
        }

        public ChapterFourUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public void Tick()
        {
            if (game.Mode != TrainingMode.ChapterFourStory) return;
            if (advance != null) advance.interactable = game.ChapterFourCanAdvance;
            var beat = game.Progress.chapterFour.beat;
            var data = game.ChapterFour;
            float elapsed = game.ChapterFourElapsed;
            bool overload = beat == ChapterFourBeat.Overload;
            float calm = beat == ChapterFourBeat.Calm ? Mathf.Clamp01(elapsed / data.HoldDuration(beat)) : 0;
            if (protagonist != null && (overload || beat == ChapterFourBeat.Calm))
            {
                float amount = Mathf.Clamp(data.overloadShake, 0, 12) * (overload ? 1 : 1 - calm);
                protagonist.anchoredPosition = protagonistOrigin + new Vector2(Mathf.Sin(elapsed * 23), Mathf.Cos(elapsed * 19)) * amount;
            }
            if (magic != null)
            {
                float pulse = .5f + .15f * Mathf.Sin(elapsed * Mathf.PI * 2 * Mathf.Clamp(data.overloadPulseSpeed, .1f, 3));
                foreach (var spark in magic)
                {
                    if (spark == null) continue;
                    var color = data.corruptedMagic;
                    color.a = pulse * (overload ? 1 : 1 - calm);
                    spark.color = color;
                }
            }
            if (rune != null && beat == ChapterFourBeat.Calm) rune.color = Color.Lerp(data.corruptedMagic, data.awakenedMagic, calm);
        }

        public bool Draw(Transform card, string feedback)
        {
            if (game.Mode != TrainingMode.ChapterFourStory) return false;
            var beat = game.Progress.chapterFour.beat;
            var page = game.ChapterFour.Page(beat, game.Progress.chapterFour.storyOrderVersion);
            protagonist = null;
            rune = null;
            magic = null;
            if (game.Progress.chapterFour.InInnerWorld)
            {
                var panel = card.GetComponent<Image>();
                if (panel != null) panel.color = game.ChapterFour.innerWorldSky;
            }
            ui.Label(card, game.Progress.chapterFour.InInnerWorld ? "CHAPTER 04 / 심상세계" : "CHAPTER 04 / 재앙", 32, 18, 696, 28, 15);
            ui.Label(card, page.title, 32, 53, 696, 44, 29);
            DrawScene(card, beat);
            ui.Label(card, page.body, 32, 200, 696, 220, 19, alignment: TextAnchor.UpperLeft);
            ui.Label(card, feedback ?? (game.Progress.chapterFour.Awakened ? "스스로 일어선 순간이 기억에 남았다." : "E  계속 · F5  기억 남기기 · F9  기억 불러오기"), 32, 437, 696, 34, 14);
            advance = ui.Button(card, game.Progress.chapterFour.Awakened ? "재앙을 향해 출발한다" : page.action, 32, 480, 696, 48, game.AdvanceChapterFour, true, game.ChapterFourCanAdvance);
            Tick();
            return true;
        }

        void DrawScene(Transform card, ChapterFourBeat beat)
        {
            var data = game.ChapterFour;
            bool innerWorld = game.Progress.chapterFour.InInnerWorld;
            var scene = ui.Block(card, "Chapter four scene", 32, 107, 696, 80,
                beat == ChapterFourBeat.Memory ? data.memorySky : innerWorld ? data.innerWorldSky : data.sky).transform;
            if (innerWorld)
            {
                DrawInnerWorld(scene, beat);
                return;
            }
            ui.Block(scene, "Broken earth", 0, 67, 696, 13, data.ground);
            if (beat == ChapterFourBeat.Letter || beat == ChapterFourBeat.Torn)
            {
                bool torn = beat == ChapterFourBeat.Torn;
                for (int i = 0; i < 4; i++)
                {
                    var paper = ui.Block(scene, "Letter fragment", 301 + i * (torn ? 31 : 23), torn ? 13 + i % 2 * 14 : 12, 23, 51, new Color(.81f, .77f, .65f));
                    if (torn) paper.rectTransform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -16 : 19);
                }
                return;
            }
            if (beat <= ChapterFourBeat.Defeat)
            {
                for (int i = 0; i < 6; i++)
                {
                    var fracture = ui.Block(scene, "Changed magic", 65 + i * 106, 8 + i % 3 * 12, 9, 42, data.corruptedMagic);
                    fracture.rectTransform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 24 : -24);
                }
            }
            else
            {
                bool fallen = beat == ChapterFourBeat.Collapse || beat == ChapterFourBeat.Parents;
                bool standing = beat == ChapterFourBeat.Anger || beat == ChapterFourBeat.Overload || beat >= ChapterFourBeat.Calm;
                protagonist = ui.Block(scene, "Protagonist", 327, fallen ? 56 : standing ? 19 : 41,
                    fallen ? 43 : 24, fallen ? 11 : standing ? 49 : 27, new Color(.37f, .44f, .49f)).rectTransform;
                protagonistOrigin = protagonist.anchoredPosition;
                if (beat >= ChapterFourBeat.Reunion)
                {
                    ui.Block(scene, "Rival one", 465, 19, 23, 49, new Color(.68f, .53f, .34f));
                    if (game.Progress.chapterFour.LetterTorn)
                        for (int i = 0; i < 3; i++) ui.Block(scene, "Torn letter", 285 + i * 15, 71, 9, 3, new Color(.81f, .77f, .65f));
                }
                if (beat == ChapterFourBeat.Overload || beat >= ChapterFourBeat.Calm)
                {
                    rune = ui.Block(protagonist, "Rune glove", 20, 21, 8, 10,
                        beat == ChapterFourBeat.Awakening ? data.awakenedMagic : data.corruptedMagic);
                    if (beat != ChapterFourBeat.Awakening)
                    {
                        magic = new Image[5];
                        for (int i = 0; i < magic.Length; i++)
                        {
                            magic[i] = ui.Block(scene, "Escaping magic", 295 + i * 19, 8 + i % 2 * 18, 4, 28, data.corruptedMagic);
                            magic[i].rectTransform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 27 : -27);
                        }
                    }
                }
            }
        }

        void DrawInnerWorld(Transform scene, ChapterFourBeat beat)
        {
            var data = game.ChapterFour;
            if (beat == ChapterFourBeat.Memory)
            {
                ui.Block(scene, "Remembered self", 107, 20, 24, 42, new Color(.59f, .58f, .47f));
                ui.Block(scene, "Quiet desk", 143, 40, 43, 7, new Color(.49f, .36f, .24f));
                ui.Label(scene, "2x + 3 = 9    →    2x = 6    →    x = 3", 214, 12, 460, 32, 20, data.awakenedMagic);
                ui.Label(scene, "…됐다.", 214, 45, 440, 24, 17);
                return;
            }
            ui.Block(scene, "Self", 249, 17, 24, 45, new Color(.37f, .44f, .49f));
            var reflection = data.awakenedMagic;
            reflection.a = .4f;
            ui.Block(scene, "Another self", 422, 17, 24, 45, reflection);
            ui.Block(scene, "Companion", 428, 27, 12, 14, data.awakenedMagic);
            ui.Block(scene, "Still horizon", 112, 69, 472, 2, reflection);
        }
    }
}
