using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRPG.Editor
{
    public static class PrototypeBuilder
    {
        public const string ScenePath = "Assets/MathRPG/Scenes/TrainingPrototype.unity";
        const string DataPath = "Assets/MathRPG/Content/";

        [MenuItem("Math RPG/Create Training Prototype")]
        public static void Create()
        {
            HuntingGroundContent.Ensure();
            if (File.Exists(ScenePath))
            {
                var existingTraining = AssetDatabase.LoadAssetAtPath<TrainingDefinition>(DataPath + "Training.asset");
                if (existingTraining != null) TrainingCourseContent.Ensure(existingTraining);
                Debug.Log("Training scene already exists. Open it with Math RPG/Open Training Prototype.");
                return;
            }
            Directory.CreateDirectory("Assets/MathRPG/Scenes");
            Directory.CreateDirectory("Assets/MathRPG/Prefabs");
            Directory.CreateDirectory(DataPath);
            CreateShapes();
            var definition = AssetDatabase.LoadAssetAtPath<TrainingDefinition>(DataPath + "Training.asset");
            if (definition == null) definition = CreateContent();
            TrainingCourseContent.Ensure(definition);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 6.75f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f, .08f, .12f);
            var light = new GameObject("Main Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = .6f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var environment = new GameObject("Training courtyard");
            WorldArt.BuildEnvironment(environment.transform);

            var playerObject = new GameObject("Player");
            playerObject.transform.position = new Vector3(-6, PlayerMotor.GroundY, 0);
            var player = playerObject.AddComponent<PlayerMotor>();
            player.visual = WorldArt.Character(playerObject.transform);
            PrefabUtility.SaveAsPrefabAssetAndConnect(playerObject, "Assets/MathRPG/Prefabs/Player.prefab", InteractionMode.AutomatedAction);

            var instructor = new GameObject("Instructor");
            instructor.transform.position = new Vector3(-8.7f, PlayerMotor.GroundY, 0);
            WorldArt.Character(instructor.transform, true);

            var enemyObject = new GameObject("Training wisp");
            enemyObject.transform.position = new Vector3(5, PlayerMotor.GroundY, 0);
            var enemy = enemyObject.AddComponent<TrainingEnemy>();
            enemy.visual = new GameObject("Visual").transform;
            enemy.visual.SetParent(enemyObject.transform, false);
            WorldArt.Shape("Shadow", new Vector3(0, .04f), new Vector2(1.7f, .2f), new Color(.03f, .08f, .13f, .8f), "Disc", enemy.visual, 8);
            WorldArt.Shape("Slime", new Vector3(0, .61f), new Vector2(1.65f, 1.22f), new Color(.65f, .28f, .37f), "Disc", enemy.visual, 12);
            WorldArt.Shape("Slime light", new Vector3(-.17f, .79f), new Vector2(1.05f, .71f), new Color(.84f, .42f, .46f), "Disc", enemy.visual, 13);
            WorldArt.Shape("Eye left", new Vector3(-.28f, .69f), new Vector2(.12f, .2f), new Color(.1f, .12f, .19f), "Diamond", enemy.visual, 14);
            WorldArt.Shape("Eye right", new Vector3(.28f, .69f), new Vector2(.12f, .2f), new Color(.1f, .12f, .19f), "Diamond", enemy.visual, 14);
            WorldArt.Shape("Rune crown", new Vector3(0, 1.49f), new Vector2(.28f, .38f), new Color(1, .77f, .44f), "Diamond", enemy.visual, 14);
            enemy.warning = WorldArt.Shape("Attack telegraph", Vector3.zero, Vector2.one, Color.red, parent: enemyObject.transform, order: 5);
            enemy.warning.enabled = false;
            var hpRoot = new GameObject("Health bar").transform;
            hpRoot.SetParent(enemyObject.transform, false);
            hpRoot.localPosition = new Vector3(0, 1.98f, 0);
            WorldArt.Shape("Health background", Vector3.zero, new Vector2(1.7f, .12f), new Color(.12f, .2f, .25f), parent: hpRoot, order: 19);
            enemy.healthFill = WorldArt.Shape("Health fill", Vector3.zero, new Vector2(1.6f, .065f), new Color(.96f, .61f, .49f), parent: hpRoot, order: 20);
            PrefabUtility.SaveAsPrefabAssetAndConnect(enemyObject, "Assets/MathRPG/Prefabs/TrainingEnemy.prefab", InteractionMode.AutomatedAction);

            var spirit = new GameObject("Companion spirit").transform;
            spirit.position = new Vector3(-7, -.9f, 0);
            WorldArt.Shape("Glow", Vector3.zero, new Vector2(.65f, .65f), new Color(.37f, .89f, .9f, .25f), "Disc", spirit, 16);
            WorldArt.Shape("Core", Vector3.zero, new Vector2(.26f, .37f), new Color(.7f, 1, .96f), "Diamond", spirit, 17);

            var game = new GameObject("Training Game").AddComponent<TrainingGame>();
            game.definition = definition;
            game.player = player;
            game.enemy = enemy;
            game.spirit = spirit;
            game.worldCamera = camera;
            game.instructor = instructor.transform;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Training prototype created: " + ScenePath);
        }

        [MenuItem("Math RPG/Open Training Prototype")]
        public static void Open()
        {
            Create();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void CreateBatch()
        {
            Create();
            Debug.Log("MATH_RPG_BUILD_SUCCESS");
        }

        static void CreateShapes()
        {
            string folder = "Assets/MathRPG/Resources/MathRPG/";
            Directory.CreateDirectory(folder);
            foreach (var shape in new[] { "Square", "Disc", "Diamond", "Triangle" })
            {
                string path = folder + shape + ".png";
                if (File.Exists(path)) continue;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1;
                        bool inside = shape == "Square" || (shape == "Disc" && u * u + v * v <= 1) ||
                            (shape == "Diamond" && Mathf.Abs(u) + Mathf.Abs(v) <= 1) ||
                            (shape == "Triangle" && Mathf.Abs(u) <= (1 - v) / 2);
                        texture.SetPixel(x, y, new Color(1, 1, 1, inside ? 1 : 0));
                    }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = size;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        static T Asset<T>(string file) where T : ScriptableObject
        {
            string path = DataPath + file + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static QuestionStep Step(string phase, string prompt, string[] choices, int correct, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = correct, hint = hint };

        static TrainingDefinition CreateContent()
        {
            var first = Asset<QuestionDefinition>("01_NumberFlow");
            first.conceptId = "integer-addition";
            first.title = "서로 다른 방향의 힘";
            first.steps = new[]
            {
                Step("조준", "오른쪽으로 5, 왼쪽으로 3의 힘이 겹친다.\n남은 힘을 읽는 식은?", new[] { "5 + 3", "5 - 3", "3 - 5", "5 × 3" }, 1, "서로 반대인 힘은 상쇄돼. 더 큰 힘에서 작은 힘을 빼보자."),
                Step("차징", "5 - 3의 힘이 남았다. 룬은 어느 쪽으로 흐를까?", new[] { "왼쪽으로 2", "오른쪽으로 8", "오른쪽으로 2", "멈춘다" }, 2, "오른쪽 힘이 더 강했지. 크기는 5에서 3을 뺀 만큼이야.")
            };
            var second = Asset<QuestionDefinition>("02_EqualBalance");
            second.conceptId = "equation-balance";
            second.title = "균형을 지키는 룬";
            second.steps = new[]
            {
                Step("조준", "x + 3 = 7. x만 남기면서 균형을 지키려면?", new[] { "양쪽에서 3을 뺀다", "왼쪽에만 3을 더한다", "양쪽을 3으로 나눈다", "오른쪽에서만 3을 뺀다" }, 0, "양쪽에 같은 일을 해야 균형이 유지돼. +3을 지우는 일을 골라봐."),
                Step("차징", "양쪽에서 3을 빼면 x = 7 - 3.\n룬에 담을 x의 값은?", new[] { "10", "3", "-4", "4" }, 3, "7에서 3을 덜어낸 값이야. x + 3에 다시 넣어 확인해도 좋아.")
            };
            var third = Asset<QuestionDefinition>("03_PrimeCore");
            third.conceptId = "prime-factorization";
            third.title = "힘의 작은 조각";
            third.steps = new[]
            {
                Step("조준", "12의 힘을 소수 조각으로만 나누려 한다.\n어떤 묶음을 고를까?", new[] { "3 × 4", "2 × 6", "2 × 2 × 3", "1 × 12" }, 2, "소수는 1과 자기 자신으로만 나누어져. 4와 6은 아직 더 나눌 수 있어."),
                Step("차징", "2 × 2 × 3에서 같은 조각을 지수로 묶으면?", new[] { "2 × 3²", "2² × 3", "2³", "6²" }, 1, "2가 두 번 곱해졌어. 지수는 같은 수를 곱한 횟수를 적는 표시야.")
            };
            var skill = Asset<SkillDefinition>("DawnSlash");
            var dialogue = Asset<DialogueDefinition>("TrainingDialogue");
            dialogue.introduction = new[]
            {
                "왕은 정령의 숲에서 자신의 인장을 찾아오는 자에게\n총사령관의 자리를 내리겠다고 공표했다.\n\n숲으로 향하기 전, 나는 왕국의 훈련소에 들어섰다.",
                "교관: 보급받은 룬 장갑은 장식이 아니다.\n검격은 손으로, 큰 기술은 정확한 생각으로 움직인다.\n\n주인공: …나는 칼이 먼저라고 생각했는데.",
                "정령: 손에 든 빛이 보이지? 흐름을 함께 읽어보자.\n나도 여기서 참 많이 넘어졌어.\n\n교관은 왼쪽에 있다. 오른쪽에서는 자유롭게 검을 시험해라."
            };
            dialogue.ambient = new[] { "훈련생: 아무도 안 보던 새벽에\n처음 검을 끝까지 휘둘렀지.\n난 그때가 제일 선명해." };
            dialogue.rewind = new[] { "정령: 괜찮아. 이번엔 발밑부터 볼까?\n내가 조금 전으로 돌려줄게.", "정령: 나도 여기서 참 많이 넘어졌어.\n다시 일어나면 되는 거야.", "정령: 급할 것 없어. 검이 오기 전에 빛이 보였지?\n이번엔 그 순간을 기다려보자." };
            dialogue.instructorGreeting = "교관: 서두르지 마라. 힘이 어디로 흐르는지 먼저 읽어라.";
            dialogue.instructorReward = "교관: 이제 처음의 망설임은 덜었군. 이 책을 받아라.\n\n주인공: …방금은, 내 손으로 해냈다.";
            var definition = Asset<TrainingDefinition>("Training");
            definition.questions = new[] { first, second, third };
            definition.runeSkill = skill;
            definition.dialogue = dialogue;
            foreach (var obj in new UnityEngine.Object[] { first, second, third, skill, dialogue, definition }) EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            return definition;
        }
    }
}
