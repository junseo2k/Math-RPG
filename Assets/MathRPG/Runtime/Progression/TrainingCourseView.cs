using System.Collections.Generic;
using UnityEngine;

namespace MathRPG
{
    public sealed class TrainingCourseView : MonoBehaviour
    {
        public const float ExitX = 9.2f;
        public const float EntranceX = -9.9f;
        public static float ChestX(int index) => index == 0 ? -.5f : 4.5f;
        TrainingCourseProgress course;
        TrainingEnemy enemy;
        GameObject courtyard;
        bool regionVisible = true;
        Transform instructor, slimeVisual, sparringVisual, rivalVisual;
        SpriteRenderer nextGate, previousGate;
        readonly Transform[] chests = new Transform[2];
        readonly List<SpriteRenderer> banners = new List<SpriteRenderer>();

        public void Initialize(TrainingCourseProgress progress, TrainingEnemy opponent, Transform instructorObject)
        {
            course = progress;
            enemy = opponent;
            instructor = instructorObject;
            courtyard = GameObject.Find("Training courtyard");
            if (courtyard != null)
                foreach (var sprite in courtyard.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.name == "Banner") banners.Add(sprite);
            slimeVisual = enemy.visual;
            sparringVisual = WorldArt.Character(enemy.transform, true);
            sparringVisual.name = "Sparring instructor visual";
            sparringVisual.gameObject.SetActive(false);
            rivalVisual = WorldArt.Character(enemy.transform);
            rivalVisual.name = "Rival visual";
            foreach (var sprite in rivalVisual.GetComponentsInChildren<SpriteRenderer>())
                if (sprite.name == "Cloak" || sprite.name == "Tunic") sprite.color = new Color(.68f, .26f, .3f);
            rivalVisual.gameObject.SetActive(false);
            nextGate = WorldArt.Shape("Next training gate", new Vector3(9.7f, -1.8f), new Vector2(.45f, 2.5f), Color.gray, "Diamond", transform, 4);
            previousGate = WorldArt.Shape("Previous training gate", new Vector3(-10.25f, -1.8f), new Vector2(.3f, 2.5f), new Color(.4f, .57f, .64f), "Diamond", transform, 4);
            for (int i = 0; i < chests.Length; i++)
            {
                var chest = new GameObject("Rune chest " + (i + 1)).transform;
                chest.SetParent(transform, false);
                chest.localPosition = new Vector3(ChestX(i), PlayerMotor.GroundY, 0);
                WorldArt.Shape("Base", new Vector3(0, .27f), new Vector2(1.05f, .54f), new Color(.48f, .31f, .17f), parent: chest, order: 12);
                WorldArt.Shape("Lid", new Vector3(0, .62f), new Vector2(1.12f, .2f), new Color(.77f, .58f, .28f), parent: chest, order: 13);
                WorldArt.Shape("Seal", new Vector3(0, .38f), new Vector2(.19f, .28f), Color.cyan, "Diamond", chest, 14);
                chests[i] = chest;
            }
            course.Changed += Refresh;
            Refresh();
        }

        public void SetOpponent(bool visible, bool sparring)
        {
            rivalVisual.gameObject.SetActive(false);
            nextGate.gameObject.SetActive(true);
            previousGate.gameObject.SetActive(true);
            slimeVisual.gameObject.SetActive(!sparring);
            sparringVisual.gameObject.SetActive(sparring);
            enemy.visual = sparring ? sparringVisual : slimeVisual;
            enemy.faceTarget = sparring;
            enemy.gameObject.SetActive(visible);
            if (instructor != null) instructor.gameObject.SetActive(!sparring);
            Refresh();
        }

        public void SetRival()
        {
            slimeVisual.gameObject.SetActive(false);
            sparringVisual.gameObject.SetActive(false);
            rivalVisual.gameObject.SetActive(true);
            enemy.visual = rivalVisual;
            enemy.faceTarget = true;
            enemy.gameObject.SetActive(true);
            if (instructor != null) instructor.gameObject.SetActive(false);
            nextGate.gameObject.SetActive(false);
            previousGate.gameObject.SetActive(false);
            foreach (var banner in banners) banner.color = new Color(.4f, .16f, .2f);
        }

        void Refresh()
        {
            nextGate.color = course.State.cleared ? course.Stage.accent : new Color(.32f, .36f, .4f);
            foreach (var banner in banners)
                banner.color = new Color(course.Stage.accent.r * .5f, course.Stage.accent.g * .5f, course.Stage.accent.b * .5f);
            for (int i = 0; i < chests.Length; i++)
            {
                chests[i].gameObject.SetActive(regionVisible && course.Stage.trialKind == InstructorTrialKind.Treasure);
                bool opened = course.State.ChestOpen(i);
                chests[i].Find("Lid").localRotation = Quaternion.Euler(0, 0, opened ? -25 : 0);
                chests[i].Find("Seal").GetComponent<SpriteRenderer>().color = opened ? new Color(.3f, .42f, .43f) : course.Stage.accent;
            }
        }

        public void HideOpponentVisuals()
        {
            slimeVisual.gameObject.SetActive(false);
            sparringVisual.gameObject.SetActive(false);
            rivalVisual.gameObject.SetActive(false);
        }

        public void SetRegionVisible(bool visible)
        {
            regionVisible = visible;
            if (courtyard != null) courtyard.SetActive(visible);
            if (instructor != null) instructor.gameObject.SetActive(visible);
            nextGate.gameObject.SetActive(visible);
            previousGate.gameObject.SetActive(visible);
            if (!visible) HideOpponentVisuals();
            Refresh();
        }

        void OnDestroy() { if (course != null) course.Changed -= Refresh; }
    }
}
