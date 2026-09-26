using UnityEngine;

namespace MathRPG
{
    public sealed class HumanVillageView : MonoBehaviour
    {
        TrainingEnemy enemy;
        Transform environment, resident, boss, ridge, market, rubbleArmor, climaxBoss, rivalSecond;

        public void Initialize(TrainingEnemy opponent)
        {
            enemy = opponent;
            environment = new GameObject("Ash ferry village").transform;
            environment.SetParent(transform, false);
            WorldArt.Shape("Overcast sky", Vector3.zero, new Vector2(30, 18), new Color(.22f, .23f, .27f), parent: environment, order: -30);
            WorldArt.Shape("River", new Vector3(0, -2.7f), new Vector2(30, 3), new Color(.18f, .28f, .32f), parent: environment, order: -21);
            ridge = new GameObject("Wind ridge").transform;
            ridge.SetParent(environment, false);
            for (int i = 0; i < 3; i++)
                WorldArt.Shape("Ridge", new Vector3(-10 + i * 10, 2), new Vector2(13, 9), new Color(.3f, .32f, .36f), "Triangle", ridge, -25);
            ridge.gameObject.SetActive(false);
            market = new GameObject("Stone market").transform;
            market.SetParent(environment, false);
            WorldArt.Shape("Storehouse", new Vector3(7, .5f), new Vector2(5, 6), new Color(.39f, .4f, .42f), parent: market, order: -17);
            WorldArt.Shape("Storehouse door", new Vector3(7, -1), new Vector2(1.8f, 3), new Color(.17f, .18f, .2f), parent: market, order: -16);
            for (int i = 0; i < 3; i++)
            {
                float x = -5 + i * 3.5f;
                WorldArt.Shape("Empty counter", new Vector3(x, -2), new Vector2(2.4f, .8f), new Color(.42f, .34f, .26f), parent: market, order: -15);
                WorldArt.Shape("Awning", new Vector3(x, 0), new Vector2(2.8f, .5f), new Color(.44f, .32f, .28f), parent: market, order: -15);
                WorldArt.Shape("Awning post", new Vector3(x - 1, -1), new Vector2(.13f, 2), new Color(.3f, .25f, .2f), parent: market, order: -15);
            }
            market.gameObject.SetActive(false);
            WorldArt.Shape("Stone path", new Vector3(0, -4.5f), new Vector2(30, 2.6f), new Color(.3f, .28f, .26f), parent: environment, order: -10);
            for (int i = 0; i < 4; i++)
            {
                float x = -9 + i * 5.5f;
                WorldArt.Shape("House wall", new Vector3(x, -.5f), new Vector2(3.2f, 3.8f), new Color(.37f, .34f, .31f), parent: environment, order: -20);
                WorldArt.Shape("House roof", new Vector3(x, 1.8f), new Vector2(4.1f, 1.8f), new Color(.24f, .2f, .22f), "Triangle", environment, -19);
                WorldArt.Shape("Window", new Vector3(x - .65f, -.1f), new Vector2(.6f, .75f), new Color(.72f, .59f, .35f), parent: environment, order: -18);
            }
            WorldArt.Shape("Forest exit", new Vector3(-10.25f, -1.8f), new Vector2(.3f, 2.5f), Color.cyan, "Diamond", environment, 4);
            WorldArt.Shape("Ferry gate", new Vector3(9.7f, -1.8f), new Vector2(.4f, 2.5f), new Color(.87f, .6f, .36f), "Diamond", environment, 4);
            resident = new GameObject("Ferry keeper").transform;
            resident.SetParent(environment, false);
            resident.localPosition = new Vector3(-8.7f, PlayerMotor.GroundY);
            WorldArt.Character(resident, true);
            boss = new GameObject("Murk devourer").transform;
            boss.SetParent(enemy.transform, false);
            WorldArt.Shape("Body", new Vector3(0, .8f), new Vector2(2.1f, 1.6f), new Color(.2f, .21f, .31f), "Disc", boss, 12);
            WorldArt.Shape("Maw", new Vector3(0, .65f), new Vector2(1.05f, .5f), new Color(.07f, .08f, .12f), "Disc", boss, 13);
            WorldArt.Shape("Eye left", new Vector3(-.4f, 1.15f), new Vector2(.17f, .23f), new Color(.99f, .64f, .31f), "Diamond", boss, 14);
            WorldArt.Shape("Eye right", new Vector3(.4f, 1.15f), new Vector2(.17f, .23f), new Color(.99f, .64f, .31f), "Diamond", boss, 14);
            rubbleArmor = new GameObject("Rubble armor").transform;
            rubbleArmor.SetParent(boss, false);
            WorldArt.Shape("Left slab", new Vector3(-.9f, .6f), new Vector2(.65f, 1.2f), new Color(.48f, .45f, .4f), "Diamond", rubbleArmor, 13);
            WorldArt.Shape("Right slab", new Vector3(.9f, .6f), new Vector2(.65f, 1.2f), new Color(.48f, .45f, .4f), "Diamond", rubbleArmor, 13);
            WorldArt.Shape("Crown slab", new Vector3(0, 1.65f), new Vector2(.9f, .7f), new Color(.48f, .45f, .4f), "Diamond", rubbleArmor, 13);
            rubbleArmor.gameObject.SetActive(false);
            climaxBoss = new GameObject("Black flood").transform;
            climaxBoss.SetParent(enemy.transform, false);
            WorldArt.Shape("Flood body", new Vector3(0, 1), new Vector2(3.4f, 2.4f), new Color(.12f, .1f, .19f), "Disc", climaxBoss, 12);
            WorldArt.Shape("Flood crest", new Vector3(-.65f, 1.9f), new Vector2(1.7f, 1.5f), new Color(.2f, .16f, .29f), "Triangle", climaxBoss, 13);
            WorldArt.Shape("Flood eye", new Vector3(.45f, 1.35f), new Vector2(.55f, .3f), new Color(.92f, .58f, .25f), "Diamond", climaxBoss, 14);
            WorldArt.Shape("Flood core", new Vector3(0, .6f), new Vector2(.55f, .65f), new Color(.66f, .45f, .26f), "Diamond", climaxBoss, 14);
            rivalSecond = new GameObject("Second rival").transform;
            rivalSecond.SetParent(enemy.transform, false);
            WorldArt.Character(rivalSecond, true);
            WorldArt.Shape("Rival sash", new Vector3(0, .85f), new Vector2(.85f, .2f), new Color(.65f, .18f, .22f), parent: rivalSecond, order: 20);
            WorldArt.Shape("Rival blade", new Vector3(-.65f, .8f), new Vector2(.12f, 1.5f), new Color(.8f, .85f, .9f), parent: rivalSecond, order: 20);
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            environment.gameObject.SetActive(visible);
            resident.gameObject.SetActive(true);
            boss.gameObject.SetActive(false);
            climaxBoss.gameObject.SetActive(false);
            rivalSecond.gameObject.SetActive(false);
        }

        public void SetVillage(int index)
        {
            ridge.gameObject.SetActive(index == 1);
            market.gameObject.SetActive(index == 2);
            rubbleArmor.gameObject.SetActive(index == 2);
            environment.Find("River").gameObject.SetActive(index == 0);
            boss.localScale = index == 1 ? new Vector3(.8f, 1.35f, 1) : Vector3.one;
        }

        public void SetBossMode()
        {
            resident.gameObject.SetActive(false);
            boss.gameObject.SetActive(true);
            enemy.visual = boss;
            enemy.faceTarget = false;
            enemy.gameObject.SetActive(true);
        }

        public void SetClimaxMode()
        {
            resident.gameObject.SetActive(false);
            boss.gameObject.SetActive(false);
            climaxBoss.gameObject.SetActive(true);
            enemy.visual = climaxBoss;
            enemy.faceTarget = false;
            enemy.gameObject.SetActive(true);
        }

        public void SetRivalSecondMode()
        {
            resident.gameObject.SetActive(false);
            boss.gameObject.SetActive(false);
            climaxBoss.gameObject.SetActive(false);
            rivalSecond.gameObject.SetActive(true);
            enemy.visual = rivalSecond;
            enemy.faceTarget = false;
            enemy.gameObject.SetActive(true);
        }
    }
}
