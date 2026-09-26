using UnityEngine;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public ForestClimaxEncounter Guardian { get; private set; }
        ForestClimaxView climaxView;

        void InitializeClimax()
        {
            var data = definition.forestClimax != null ? definition.forestClimax : Resources.Load<ForestClimaxDefinition>("MathRPG/ForestClimax");
            Guardian = new ForestClimaxEncounter(this, data);
            climaxView = gameObject.AddComponent<ForestClimaxView>();
            climaxView.Initialize(enemy);
        }

        internal void PrepareForestClimax()
        {
            PrepareForestBoss();
            enemy.ResetEnemy(Guardian.Definition.health, Guardian.Definition.openingWindup);
            forestView.SetBossMode(false);
            forestView.SetGuideVisible(false);
            climaxView.SetBoss(true);
        }

        internal void RefreshClimaxVisuals()
        { climaxView.SetState(Guardian.Stage, Guardian.SplitSpirits, Guardian.CloneCount); }

        internal void ResolveClimaxRune(bool release)
        {
            var data = Guardian.Definition;
            int damage = Guardian.ModifyIncomingDamage(data.runeDamage);
            Effects.Charge(player.transform.position + Vector3.up);
            if (damage > 0 && enemy.Phase != EnemyPhase.Defeated) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            enemy.ReceiveDamage(damage, enemy.transform.position.x >= player.transform.position.x ? 1 : -1, true);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
            Health = Mathf.Min(definition.playerHealth, Health + data.correctHeal);
            Notify("집합체의 약한 결속을 룬이 파고들었다.", 4);
        }

        internal void RecordClimaxVictory()
        {
            PrepareForestVillage();
            WriteCheckpoint();
            SetMode(TrainingMode.ForestChapterClear);
        }
    }
}
