namespace MathRPG
{
    public sealed class HumanClimaxAttackPattern
    {
        readonly HumanClimaxDefinition definition;
        float remaining;
        int cursor;
        bool shifted, shiftPending;
        public bool WindingUp { get; private set; }
        public bool LowSweep { get; private set; }

        public HumanClimaxAttackPattern(HumanClimaxDefinition definition)
        { this.definition = definition; remaining = definition.attackInterval; }

        public void ChangePattern() => shiftPending = true;

        public bool Tick(float dt)
        {
            remaining -= dt;
            if (remaining > 0) return false;
            if (WindingUp)
            {
                WindingUp = false;
                remaining = definition.attackInterval;
                return true;
            }
            // Preserve an attack already telegraphed; switch only before the next warning.
            if (shiftPending) { shifted = true; shiftPending = false; cursor = 0; }
            var sequence = shifted ? definition.secondPattern : definition.firstPattern;
            LowSweep = sequence[cursor++ % sequence.Length];
            WindingUp = true;
            remaining = definition.attackWindup;
            return false;
        }
    }
}
