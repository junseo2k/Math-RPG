using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public FinalCalamityEncounter Calamity { get; private set; }
        FinalCalamityView calamityView;
        public bool InCalamity => Progress.chapterFour.calamity != CalamityProgress.None;

        void InitializeCalamity()
        {
            Calamity = new FinalCalamityEncounter(this, Resources.Load<FinalCalamityDefinition>("MathRPG/FinalCalamity"));
            calamityView = gameObject.AddComponent<FinalCalamityView>();
            calamityView.Initialize(this);
        }

        internal bool SaveCalamity(CalamityProgress next)
        {
            var previous = Progress.chapterFour.calamity;
            Progress.chapterFour.calamity = next;
            if (CheckpointStore.Save(SavePath, Progress, out var error)) return true;
            Progress.chapterFour.calamity = previous;
            Notify("기억을 남기지 못했습니다: " + error, 8);
            UI.RefreshOverlay(Notice);
            return false;
        }

        internal void PrepareCalamity(bool companionPresent)
        {
            ResetArena();
            courseView.SetRegionVisible(false);
            courseView.HideOpponentVisuals();
            if (instructor != null) instructor.gameObject.SetActive(false);
            player.transform.position = new Vector3(-3, PlayerMotor.GroundY, 0);
            enemy.gameObject.SetActive(true);
            enemy.transform.position = new Vector3(3, PlayerMotor.GroundY, 0);
            SetCalamitySpiritVisible(companionPresent);
            ambientPlayed = true;
            Ambient = Notice = "";
            calamityView.Show();
        }

        internal void ShowCalamityMode(TrainingMode mode) => SetMode(mode);
        internal void SetCalamitySpiritVisible(bool visible)
        {
            if (spirit != null) spirit.gameObject.SetActive(visible);
        }
        internal void HideCalamityBoss()
        {
            enemy.gameObject.SetActive(false);
            calamityView.HideBoss();
            SetCalamitySpiritVisible(false);
        }

        void TickCalamityInput(float dt)
        {
            if (Mode != TrainingMode.CalamityChallenge) return;
            Calamity.TickQuestion(dt);
            if (Mode != TrainingMode.CalamityChallenge) return;
            if (GameInput.Pressed(Key.R)) { Calamity.Rethink(); return; }
            if (GameInput.Pressed(Key.Digit1)) Calamity.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) Calamity.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) Calamity.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) Calamity.Submit(3);
        }

        void ClampCalamityPlayer()
        {
            if (!Calamity.Active) return;
            var point = player.transform.position;
            point.x = Mathf.Clamp(point.x, -Calamity.Definition.arenaHalfWidth, Calamity.Definition.arenaHalfWidth);
            player.transform.position = point;
        }

        internal void MoveCalamity(float dt)
        {
            float delta = player.transform.position.x - enemy.transform.position.x;
            if (Mathf.Abs(delta) <= 1.8f) return;
            var target = enemy.transform.position;
            target.x = Mathf.Clamp(player.transform.position.x - Mathf.Sign(delta) * 1.8f, -Calamity.Definition.arenaHalfWidth, Calamity.Definition.arenaHalfWidth);
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, target, Calamity.Definition.moveSpeed * dt);
        }

        internal void StrikeCalamity(bool low)
        {
            float delta = player.transform.position.x - enemy.transform.position.x;
            Effects.Slash(enemy.transform.position + new Vector3(0, low ? .3f : 1.35f), delta < 0 ? -1 : 1, true);
            if (Mathf.Abs(delta) > Calamity.Definition.attackRange || player.Dodges(low) || player.Invulnerable) return;
            player.KnockBack(Mathf.Sign(delta));
            Calamity.Hurt(Calamity.Definition.swordDamage + Calamity.Rage * Calamity.Definition.rageDamage,
                low ? "낮은 검격에 맞았다 — 점프로 넘을 수 있다" : "높은 검격에 맞았다 — 숙여서 피할 수 있다", false);
        }

        internal void ResolveCalamityHazard()
        {
            Effects.Impact(new Vector3(Calamity.HazardX, PlayerMotor.GroundY + .3f, 0), true);
            if (Mathf.Abs(player.transform.position.x - Calamity.HazardX) > Calamity.HazardRadius) return;
            Calamity.Hurt(Health, "즉사 장판 안에 머물렀다 — 점프가 아닌 좌우 이동으로 표시 밖까지 벗어나야 한다", false);
        }

        internal void ResolveCalamityRune()
        {
            Effects.Charge(player.transform.position + Vector3.up);
            int damage = Calamity.ReceiveHit(Calamity.Definition.runeDamage);
            if (damage > 0) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
        }
    }
}
