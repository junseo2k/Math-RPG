using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathRPG
{
    public sealed class BlockCalculatorUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        QuizSession session;
        int stepIndex = -1;
        int[] placed;
        int[] paletteOrder;
        ArithmeticLink[] links;
        int selected;
        bool specialMode;
        readonly Stack<Snapshot> history = new Stack<Snapshot>();
        sealed class Snapshot
        {
            public int[] placed;
            public ArithmeticLink[] links;
            public int selected;
        }

        public BlockCalculatorUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public void Draw(Transform card, QuizSession quiz, string feedback)
        {
            var definition = quiz.Step.calculation;
            if (session != quiz || stepIndex != quiz.StepIndex)
            {
                session = quiz;
                stepIndex = quiz.StepIndex;
                placed = new int[definition.SlotCount];
                paletteOrder = new int[definition.blocks.Length];
                for (int i = 0; i < paletteOrder.Length; i++) paletteOrder[i] = i;
                for (int i = paletteOrder.Length - 1; i > 0; i--)
                {
                    int other = UnityEngine.Random.Range(0, i + 1);
                    int block = paletteOrder[i];
                    paletteOrder[i] = paletteOrder[other];
                    paletteOrder[other] = block;
                }
                links = new ArithmeticLink[definition.function == CalculationFunction.Arithmetic ? placed.Length - 1 : 0];
                ClearSlots();
                history.Clear();
                specialMode = false;
            }
            bool quadratic = definition.function == CalculationFunction.QuadraticFormula;
            ui.Label(card, "블록을 클릭하면 선택한 칸에 들어갑니다. 칸을 클릭해 교체할 수도 있습니다.", 30, 172, 700, 20, 14);
            for (int i = 0; i < 10; i++)
            {
                int digit = definition.blocks.Length + i;
                ui.Button(card, i.ToString(), 30 + i * 70, 194, 64, 36, () => Place(digit),
                    style: UIControlStyle.Block, latched: Array.IndexOf(placed, digit) >= 0);
            }
            int extra = 0;
            for (int i = 0; i < definition.blocks.Length; i++)
            {
                int block = paletteOrder[i];
                var data = definition.blocks[block];
                if (data.kind == CalculationBlockKind.Number && data.number >= 0 && data.number <= 9 &&
                    data.number == Math.Floor(data.number)) continue;
                ui.Button(card, (data.kind == CalculationBlockKind.Number ? "수  " : "식  ") + data.label,
                    30 + extra % 4 * 176, 234 + extra / 4 * 28, 166, 25, () => Place(block),
                    style: UIControlStyle.Block, latched: Array.IndexOf(placed, block) >= 0);
                extra++;
            }
            ui.PanelBackground(card, "Calculator input", 22, 290, 714, 68, recessed: true);
            for (int i = 0; i < placed.Length; i++)
            {
                int slot = i;
                string name = quadratic ? new[] { "a", "b", "c" }[i] : "입력 " + (i + 1);
                ui.Button(card, name + ": " + (placed[i] < 0 ? "빈칸" : definition.GetBlock(placed[i]).label),
                    30 + i * 235, 300, 220, 48, () => { selected = slot; ui.RefreshOverlay(); }, selected == i,
                    style: UIControlStyle.Block, latched: placed[i] >= 0);
            }
            if (!quadratic)
            {
                string[] symbols = { "+", "−", "×", "÷" };
                for (int i = 0; i < links.Length; i++)
                {
                    int connection = i;
                    float x = 30 + i * 354;
                    ui.Label(card, (i + 1) + " → " + (i + 2), x, 360, 72, 38, 15);
                    for (int j = 0; j < symbols.Length; j++)
                    {
                        var operation = (ArithmeticLink)j;
                        ui.Button(card, symbols[j], x + 75 + j * 64, 360, 58, 38,
                            () => Connect(connection, operation), links[i] == operation,
                            style: UIControlStyle.Block, latched: links[i] == operation);
                    }
                }
            }
            else ui.Label(card, "식에서 계수 a·b·c를 골라 배치하세요.", 30, 360, 695, 38, 16);
            ui.Button(card, "처음으로", 30, 414, 165, 42, Reset);
            ui.Button(card, "1단계 취소", 207, 414, 165, 42, Undo);
            ui.Button(card, "특수 계산기", 384, 414, 165, 42,
                () => { specialMode = !specialMode; ui.RefreshOverlay(); }, specialMode, enabled: quadratic);
            if (quadratic && specialMode)
                ui.Button(card, "근의공식", 561, 414, 169, 42, Execute, true,
                    enabled: Array.TrueForAll(placed, value => value >= 0));
            else ui.Label(card, quadratic ? "기능 선택 후 실행" : "사용 가능 특수 기능 없음", 555, 414, 175, 42, 12);
            string result = quiz.LastCalculation == null ? "" : "직전 시도 · " + quiz.LastCalculation.Display + "\n";
            ui.Label(card, result + (feedback ?? ""), 30, 466, 695, 46, 16);
            ui.Label(card, quiz.HintVisible ? "정령: " + quiz.Step.hint :
                quadratic ? "입력 → 결과만 표시됩니다." : "배치 완료 시 자동 계산 · × ÷ 우선 · 결과만 표시", 30, 514, 695, 34, 14);
            ui.Button(card, "H  정령의 도움 (" + game.Hints + ")", 30, 553, 300, 36, game.UseHint,
                enabled: !quiz.HintVisible && game.Hints > 0);
        }

        bool Active => session != null && game.Mode == TrainingMode.Quiz && game.Quiz == session &&
            session.StepIndex == stepIndex && !session.Completed;

        void Remember()
        {
            history.Push(new Snapshot { placed = (int[])placed.Clone(), links = (ArithmeticLink[])links.Clone(), selected = selected });
        }

        void ClearSlots()
        {
            for (int i = 0; i < placed.Length; i++) placed[i] = -1;
            for (int i = 0; i < links.Length; i++) links[i] = (ArithmeticLink)(-1);
            selected = 0;
        }

        void Place(int block)
        {
            if (!Active || placed[selected] == block) return;
            Remember();
            placed[selected] = block;
            for (int offset = 1; offset <= placed.Length; offset++)
            {
                int next = (selected + offset) % placed.Length;
                if (placed[next] < 0) { selected = next; break; }
            }
            Changed();
        }

        void Connect(int connection, ArithmeticLink operation)
        {
            if (!Active || links[connection] == operation) return;
            Remember();
            links[connection] = operation;
            Changed();
        }

        void Changed()
        {
            if (session.Step.calculation.function == CalculationFunction.Arithmetic &&
                Array.TrueForAll(placed, value => value >= 0) && Array.TrueForAll(links, value => (int)value >= 0)) Execute();
            else ui.RefreshOverlay();
        }

        void Execute()
        {
            if (Active) game.SubmitBlocks(new BlockSubmission(placed, links));
        }

        void Reset()
        {
            if (!Active) return;
            Remember();
            ClearSlots();
            ui.RefreshOverlay();
        }

        void Undo()
        {
            if (!Active || history.Count == 0) return;
            var snapshot = history.Pop();
            placed = snapshot.placed;
            links = snapshot.links;
            selected = snapshot.selected;
            ui.RefreshOverlay();
        }
    }
}
