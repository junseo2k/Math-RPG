using UnityEngine;

namespace MathRPG
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        public const float GroundY = -3.2f;
        [Min(1)] public float moveSpeed = 6f;
        [Min(1)] public float jumpSpeed = 11f;
        [Min(1)] public float gravity = 29f;
        public Transform visual;
        public int Facing { get; private set; } = 1;
        public bool Crouching { get; private set; }
        public float Height => transform.position.y - GroundY;
        public bool Grounded => Height <= 0.01f;
        public bool Invulnerable => grace > 0;
        float verticalSpeed, knockback, grace, swing;

        public void Tick(float dt, float axis, bool jump, bool crouch)
        {
            grace = Mathf.Max(0, grace - dt);
            swing = Mathf.Max(0, swing - dt);
            Crouching = crouch && Grounded;
            if (Mathf.Abs(axis) > 0.1f) Facing = axis > 0 ? 1 : -1;
            if (jump && Grounded) { verticalSpeed = jumpSpeed; Crouching = false; }
            verticalSpeed -= gravity * dt;
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + (axis * moveSpeed * (Crouching ? .38f : 1f) + knockback) * dt, -10.5f, 10.5f);
            p.y = Mathf.Max(GroundY, p.y + verticalSpeed * dt);
            if (p.y <= GroundY) verticalSpeed = 0;
            transform.position = p;
            knockback = Mathf.MoveTowards(knockback, 0, dt * 24);
            if (visual != null)
            {
                float bounce = Grounded && axis != 0 && !Crouching ? Mathf.Sin(Time.time * 17) * .045f : 0;
                visual.localPosition = new Vector3(0, bounce, 0);
                visual.localScale = new Vector3(Facing * (swing > 0 ? 1.12f : 1), Crouching ? .52f : 1, 1);
                visual.localRotation = Quaternion.Euler(0, 0, swing > 0 ? -Facing * 8 : 0);
                foreach (var renderer in visual.GetComponentsInChildren<SpriteRenderer>())
                {
                    var c = renderer.color;
                    c.a = grace > 0 && Mathf.FloorToInt(grace * 18) % 2 == 0 ? .35f : 1;
                    renderer.color = c;
                }
            }
        }

        public void Swing() { swing = .16f; }
        public void KnockBack(float direction) { knockback = direction * 5; grace = .7f; }
        public bool Dodges(bool lowSweep) => lowSweep ? Height > .75f : Crouching && Grounded;

        public void ResetAtCheckpoint()
        {
            transform.position = new Vector3(-6, GroundY, 0);
            verticalSpeed = knockback = grace = swing = 0;
            Facing = 1;
            Crouching = false;
            if (visual != null) { visual.localScale = Vector3.one; visual.localRotation = Quaternion.identity; }
        }
    }
}
