using System;
using System.Globalization;

namespace MathRPG
{
    public sealed class BlockCalculationResult
    {
        public bool Executed { get; }
        public double[] Values { get; }
        public string Display { get; }

        internal BlockCalculationResult(string error) { Display = error; }
        internal BlockCalculationResult(double[] values, bool roots)
        {
            Executed = true;
            Values = values;
            Display = values.Length == 0 ? "실수인 근 없음" : (roots ? "x = " : "결과: ") + string.Join(", ", Array.ConvertAll(values,
                value => value.ToString("G8", CultureInfo.InvariantCulture)));
        }
    }

    public static class BlockCalculator
    {
        public static BlockCalculationResult Calculate(BlockCalculationDefinition definition, BlockSubmission input)
        {
            if (definition == null || input == null || input.Blocks == null || input.Blocks.Length != definition.SlotCount)
                return new BlockCalculationResult("실행 불가: 입력 슬롯을 채워주세요.");
            var values = new double[input.Blocks.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int index = input.Blocks[i];
                var block = definition.GetBlock(index);
                if (block == null)
                    return new BlockCalculationResult("실행 불가: 입력 슬롯을 채워주세요.");
                if (!block.TryGetNumber(out values[i]))
                    return new BlockCalculationResult("실행 불가: 이 슬롯에는 수치가 필요합니다.");
            }
            if (definition.function == CalculationFunction.QuadraticFormula)
            {
                double a = values[0], b = values[1], c = values[2];
                if (a == 0) return new BlockCalculationResult("실행 불가: 이차식이 아닙니다.");
                // Normalize before computing the discriminant to avoid unnecessary overflow.
                double scale = Math.Max(Math.Abs(a), Math.Max(Math.Abs(b), Math.Abs(c)));
                a /= scale; b /= scale; c /= scale;
                if (a == 0) return new BlockCalculationResult("실행 불가: 계산 가능한 수의 범위를 벗어났습니다.");
                double discriminant = b * b - 4 * a * c;
                if (discriminant < 0) return new BlockCalculationResult(Array.Empty<double>(), true);
                if (discriminant == 0) return Finish(new[] { -b / (2 * a) }, true);
                double q = -.5 * (b + (b >= 0 ? 1 : -1) * Math.Sqrt(discriminant));
                var roots = new[] { q / a, c / q };
                Array.Sort(roots);
                return Finish(roots, true);
            }
            if (definition.function != CalculationFunction.Arithmetic || input.Links == null || input.Links.Length != values.Length - 1)
                return new BlockCalculationResult("실행 불가: 연산 연결을 선택해주세요.");
            double total = 0, term = values[0];
            for (int i = 0; i < input.Links.Length; i++)
            {
                switch (input.Links[i])
                {
                    case ArithmeticLink.Add: total += term; term = values[i + 1]; break;
                    case ArithmeticLink.Subtract: total += term; term = -values[i + 1]; break;
                    case ArithmeticLink.Multiply: term *= values[i + 1]; break;
                    case ArithmeticLink.Divide:
                        if (values[i + 1] == 0) return new BlockCalculationResult("실행 불가: 0으로 나눌 수 없습니다.");
                        term /= values[i + 1]; break;
                    default: return new BlockCalculationResult("실행 불가: 연산 연결을 선택해주세요.");
                }
            }
            return Finish(new[] { total + term }, false);
        }

        static BlockCalculationResult Finish(double[] values, bool roots)
        {
            foreach (double value in values)
                if (!CalculationBlock.Finite(value)) return new BlockCalculationResult("실행 불가: 계산 가능한 수의 범위를 벗어났습니다.");
            return new BlockCalculationResult(values, roots);
        }
    }
}
