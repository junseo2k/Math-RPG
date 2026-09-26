using System;
using UnityEngine;

namespace MathRPG
{
    public enum CalculationFunction { Arithmetic = 0, QuadraticFormula = 1 }
    public enum CalculationBlockKind { Number = 0, Expression = 1 }
    public enum ArithmeticLink { Add = 0, Subtract = 1, Multiply = 2, Divide = 3 }

    [Serializable]
    public sealed class CalculationBlock
    {
        public string label;
        public CalculationBlockKind kind;
        public double number;
        // Expressions are authored polynomials, never parsed from their display labels.
        public double quadratic, linear, constant;

        public bool TryGetNumber(out double value)
        {
            value = kind == CalculationBlockKind.Number ? number : constant;
            return (kind == CalculationBlockKind.Number ||
                (kind == CalculationBlockKind.Expression && quadratic == 0 && linear == 0)) && Finite(value);
        }

        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    [Serializable]
    public sealed class BlockAnswerPath
    {
        public int[] blocks;
        public ArithmeticLink[] links;
    }

    public sealed class BlockSubmission
    {
        public readonly int[] Blocks;
        public readonly ArithmeticLink[] Links;
        public BlockSubmission(int[] blocks, ArithmeticLink[] links)
        {
            Blocks = blocks == null ? null : (int[])blocks.Clone();
            Links = links == null ? null : (ArithmeticLink[])links.Clone();
        }
    }

    [Serializable]
    public sealed class BlockCalculationDefinition
    {
        public CalculationFunction function;
        public CalculationBlock[] blocks;
        [Range(2, 3)] public int operandCount = 2;
        public BlockAnswerPath[] acceptedPaths;
        public double[] expectedResults;
        [Min(0)] public double tolerance = .000001;
        public int SlotCount => function == CalculationFunction.QuadraticFormula ? 3 : operandCount;

        public CalculationBlock GetBlock(int index)
        {
            if (blocks == null || index < 0) return null;
            if (index < blocks.Length) return blocks[index];
            int digit = index - blocks.Length;
            return digit < 10 ? new CalculationBlock { label = digit.ToString(), number = digit } : null;
        }

        public bool IsValid(out string reason)
        {
            reason = "Invalid block calculation definition.";
            if (!Enum.IsDefined(typeof(CalculationFunction), function) || operandCount < 2 || operandCount > 3 ||
                blocks == null || blocks.Length == 0 || blocks.Length > 8 || acceptedPaths == null || acceptedPaths.Length == 0 ||
                expectedResults == null || expectedResults.Length == 0 || expectedResults.Length > 2 ||
                !CalculationBlock.Finite(tolerance) || tolerance < 0) return false;
            foreach (var block in blocks)
                if (block == null || string.IsNullOrWhiteSpace(block.label) ||
                    !Enum.IsDefined(typeof(CalculationBlockKind), block.kind) || !CalculationBlock.Finite(block.number) ||
                    !CalculationBlock.Finite(block.quadratic) || !CalculationBlock.Finite(block.linear) ||
                    !CalculationBlock.Finite(block.constant)) return false;
            foreach (double value in expectedResults)
                if (!CalculationBlock.Finite(value)) return false;
            foreach (var path in acceptedPaths)
            {
                if (path == null) return false;
                var result = BlockCalculator.Calculate(this, new BlockSubmission(path.blocks, path.links));
                if (!result.Executed || !ResultsMatch(result.Values)) return false;
            }
            reason = null;
            return true;
        }

        internal bool AcceptsPath(BlockSubmission submission)
        {
            foreach (var path in acceptedPaths)
            {
                if (submission.Blocks == null || path.blocks.Length != submission.Blocks.Length) continue;
                bool same = true;
                for (int i = 0; i < path.blocks.Length; i++)
                {
                    int actual = submission.Blocks[i];
                    var expected = GetBlock(path.blocks[i]);
                    var supplied = GetBlock(actual);
                    same &= path.blocks[i] == actual || (actual >= blocks.Length && expected != null &&
                        supplied != null && expected.kind == CalculationBlockKind.Number && expected.number == supplied.number);
                }
                if (function == CalculationFunction.Arithmetic)
                {
                    if (submission.Links == null || path.links.Length != submission.Links.Length) continue;
                    for (int i = 0; i < path.links.Length; i++) same &= path.links[i] == submission.Links[i];
                }
                if (same) return true;
            }
            return false;
        }

        internal bool ResultsMatch(double[] values)
        {
            if (values == null || values.Length != expectedResults.Length) return false;
            var actual = (double[])values.Clone();
            var expected = (double[])expectedResults.Clone();
            Array.Sort(actual);
            Array.Sort(expected);
            for (int i = 0; i < actual.Length; i++)
                if (!CalculationBlock.Finite(actual[i]) || Math.Abs(actual[i] - expected[i]) > tolerance) return false;
            return true;
        }
    }
}
