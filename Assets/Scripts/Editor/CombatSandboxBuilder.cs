using System.Collections.Generic;
using System.Reflection;
using MathRPG.Combat;
using MathRPG.Core;
using MathRPG.Data;
using MathRPG.Enemy;
using MathRPG.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 전투 테스트 씬 CombatSandbox를 코드로 생성한다.
    ///
    /// M1 검증용 구성 — Tilemap 소형 레벨, 플레이어 1, 순찰/공격하는 몬스터 1.
    /// 수학 문제 없이 이동·평타·피격·회피 루프의 손맛만 확인한다 (기획서 5-7).
    ///
    /// 씬을 손으로 조립하지 않고 스크립트로 만드는 이유는 MenuScenesBuilder와 같다.
    ///
    /// 실행: 메뉴 MathRPG/Build/Combat Sandbox
    /// 주의: 실행하면 기존 CombatSandbox.unity를 덮어쓴다.
    ///       캐릭터 그림은 Assets/Art/Player.png · Assets/Art/Enemy.png가 있으면 자동으로 쓴다.
    /// </summary>
    public static class CombatSandboxBuilder
    {
        private const string ScenePath = "Assets/Scenes/" + SceneNames.CombatSandbox + ".unity";

        private const string InputReaderPath = "Assets/Settings/Input/PlayerInputReader.asset";
        private const string PlayerTimingPath = "Assets/Scripts/Combat/AttackTiming.asset";
        private const string EnemyTimingPath = "Assets/Scripts/Combat/AttackTiming_Enemy.asset";

        private const string PlaceholderSpritePath = "Assets/Art/Placeholder/White.png";
        private const string PlaceholderTilePath = "Assets/Art/Placeholder/PlaceholderTile.asset";
        private const string PlayerSpritePath = "Assets/Art/Player.png";
        private const string EnemySpritePath = "Assets/Art/Enemy.png";

        // URP 2D 렌더러의 기본 스프라이트 머티리얼. 손으로 만든 기존 씬의 SpriteRenderer들이 쓰던 것과 동일.
        private const string DefaultSpriteMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        private static readonly Color PlayerTint = new Color(0.35f, 0.75f, 1f, 1f);
        private static readonly Color EnemyTint = new Color(0.9f, 0.35f, 0.4f, 1f);
        private static readonly Color GroundTint = new Color(0.24f, 0.27f, 0.33f, 1f);
        private static readonly Color CameraBackground = new Color(0.09f, 0.1f, 0.13f, 1f);

        [MenuItem("MathRPG/Build/Combat Sandbox", priority = 21)]
        public static void Build()
        {
            InputReader inputReader = RequireAsset<InputReader>(InputReaderPath);
            AttackTimingData playerTiming = RequireAsset<AttackTimingData>(PlayerTimingPath);
            Sprite placeholder = RequireAsset<Sprite>(PlaceholderSpritePath);
            if (inputReader == null || playerTiming == null || placeholder == null)
            {
                return;
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (playerLayer < 0 || enemyLayer < 0 || groundLayer < 0)
            {
                Debug.LogError("[CombatSandboxBuilder] 레이어 'Player'/'Enemy'/'Ground' 중 없는 것이 있습니다. " +
                               "ProjectSettings > Tags and Layers에서 추가한 뒤 다시 실행하세요.");
                return;
            }

            AttackTimingData enemyTiming = GetOrCreateEnemyTiming();
            Tile groundTile = GetOrCreatePlaceholderTile(placeholder);

            Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
            if (playerSprite == null)
            {
                playerSprite = placeholder;
            }

            Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnemySpritePath);
            if (enemySprite == null)
            {
                enemySprite = placeholder;
            }

            LayerMask walkableMask = (1 << LayerMask.NameToLayer("Default")) | (1 << groundLayer);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateGameManager();
            CreateLevel(groundTile, groundLayer);

            GameObject player = CreatePlayer(playerSprite, playerSprite == placeholder,
                inputReader, playerTiming, playerLayer, enemyLayer, walkableMask);

            GameObject enemy = CreateEnemy(enemySprite, enemySprite == placeholder,
                enemyTiming, enemyLayer, playerLayer, walkableMask, player.transform);

            Selection.activeGameObject = enemy;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            Debug.Log($"[CombatSandboxBuilder] 생성 완료: {ScenePath}\n" +
                      $"  플레이어({LayerMask.LayerToName(playerLayer)}) · 몬스터({LayerMask.LayerToName(enemyLayer)})\n" +
                      $"  스프라이트: 플레이어={(playerSprite == placeholder ? "플레이스홀더" : PlayerSpritePath)}, " +
                      $"몬스터={(enemySprite == placeholder ? "플레이스홀더" : EnemySpritePath)}\n" +
                      "  조작: 이동 A/D · 점프 Space · 숙이기 S · 평타 좌클릭/J");
        }

        // ---------------------------------------------------------------- 씬 오브젝트

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";

            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            go.transform.position = new Vector3(0f, 2.5f, -10f);
        }

        private static void CreateGameManager()
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        /// <summary>Tilemap 바닥 + 플랫폼 + 벽으로 이뤄진 소형 레벨. 바닥 표면은 y = 0.</summary>
        private static void CreateLevel(Tile tile, int groundLayer)
        {
            // Grid를 먼저 만들고, 자식으로 붙인 다음에 Tilemap 계열 컴포넌트를 추가한다
            // (Tilemap은 부모 GridLayout에 의존하므로 순서가 중요하다).
            var grid = new GameObject("Level", typeof(Grid));

            var tmGo = new GameObject("Tilemap_Ground");
            tmGo.transform.SetParent(grid.transform, false);
            tmGo.layer = groundLayer;

            var tilemap = tmGo.AddComponent<Tilemap>();
            var tmRenderer = tmGo.AddComponent<TilemapRenderer>();
            tmRenderer.sortingOrder = -10;
            tilemap.color = GroundTint;

            string materialPath = AssetDatabase.GUIDToAssetPath(DefaultSpriteMaterialGuid);
            if (!string.IsNullOrEmpty(materialPath))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                {
                    tmRenderer.sharedMaterial = material;
                }
            }

            // 바닥 한 줄 (셀 y=-1 → 표면 y=0), 좌우 벽, 낮은 계단, 떠 있는 발판.
            PaintRect(tilemap, tile, -20, 20, -1, -1);
            PaintRect(tilemap, tile, -14, -14, 0, 4);
            PaintRect(tilemap, tile, 14, 14, 0, 4);
            PaintRect(tilemap, tile, -8, -6, 0, 0);
            PaintRect(tilemap, tile, 4, 8, 2, 2);

            // 타일별 콜라이더. CompositeCollider2D 없이 개별 박스 콜라이더 —
            // 세팅 단순하고 정적 지형으로 확실하게 동작한다. Rigidbody2D 없으면 정적 콜라이더.
            tmGo.AddComponent<TilemapCollider2D>();

            Debug.Log($"[CombatSandboxBuilder] Tilemap 타일 {tilemap.GetUsedTilesCount()}개 배치, 콜라이더 생성.");
        }

        private static void PaintRect(Tilemap tilemap, Tile tile, int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static GameObject CreatePlayer(Sprite sprite, bool isPlaceholder, InputReader inputReader,
                                               AttackTimingData timing, int playerLayer, int enemyLayer,
                                               LayerMask walkableMask)
        {
            var go = new GameObject("Player") { layer = playerLayer, tag = "Player" };
            go.transform.position = new Vector3(-2f, 0.6f, 0f);

            go.AddComponent<Rigidbody2D>().freezeRotation = true;

            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 1.8f);
            collider.offset = new Vector2(0f, 0.9f);
            collider.direction = CapsuleDirection2D.Vertical;

            var locomotion = go.AddComponent<PlayerLocomotion>();
            go.AddComponent<AttackTimeline>();
            var attack = go.AddComponent<PlayerAttack>();
            var hitbox = go.AddComponent<Hitbox>();
            go.AddComponent<Health>();
            go.AddComponent<Hurtbox>();
            go.AddComponent<RespawnOnDeath>();

            SetField(locomotion, "input", inputReader);
            SetField(locomotion, "groundLayers", walkableMask);

            SetField(attack, "input", inputReader);
            SetField(attack, "basicAttackTiming", timing);

            SetField(hitbox, "attacker", go);
            SetField(hitbox, "hittableLayers", (LayerMask)(1 << enemyLayer));

            SpriteRenderer visual = CreateVisual(go.transform, "Visual",
                isPlaceholder ? new Vector3(0.7f, 1.8f, 1f) : Vector3.one,
                isPlaceholder ? PlayerTint : Color.white, sprite, sortingOrder: 10);

            var attackVisuals = visual.gameObject.AddComponent<AttackVisuals>();
            SetField(attackVisuals, "target", visual);
            SetField(attackVisuals, "attacker", go);

            var hitReaction = visual.gameObject.AddComponent<HitReaction>();
            SetField(hitReaction, "target", visual);
            SetField(hitReaction, "owner", go);

            return go;
        }

        private static GameObject CreateEnemy(Sprite sprite, bool isPlaceholder, AttackTimingData timing,
                                              int enemyLayer, int playerLayer, LayerMask walkableMask,
                                              Transform target)
        {
            var go = new GameObject("Enemy") { layer = enemyLayer };
            go.transform.position = new Vector3(5f, 0.6f, 0f);

            go.AddComponent<Rigidbody2D>().freezeRotation = true;

            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.9f, 1.8f);
            collider.offset = new Vector2(0f, 0.9f);
            collider.direction = CapsuleDirection2D.Vertical;

            go.AddComponent<AttackTimeline>();
            var enemyAttack = go.AddComponent<EnemyAttack>();
            var hitbox = go.AddComponent<Hitbox>();
            go.AddComponent<Health>();
            go.AddComponent<Hurtbox>();
            var respawn = go.AddComponent<RespawnOnDeath>();
            var ai = go.AddComponent<EnemyAI>();

            SetField(enemyAttack, "attackTiming", timing);

            SetField(hitbox, "attacker", go);
            SetField(hitbox, "hittableLayers", (LayerMask)(1 << playerLayer));

            SetField(ai, "target", target);
            SetField(ai, "groundLayers", walkableMask);

            SetField(respawn, "disableWhileDead", collider);

            SpriteRenderer visual = CreateVisual(go.transform, "Visual",
                isPlaceholder ? new Vector3(0.9f, 1.8f, 1f) : Vector3.one,
                isPlaceholder ? EnemyTint : Color.white, sprite, sortingOrder: 5);

            var attackVisuals = visual.gameObject.AddComponent<AttackVisuals>();
            SetField(attackVisuals, "target", visual);
            SetField(attackVisuals, "attacker", go);
            SetField(attackVisuals, "windupPullback", 0.32f); // 텔레그래프를 크게

            var hitReaction = visual.gameObject.AddComponent<HitReaction>();
            SetField(hitReaction, "target", visual);
            SetField(hitReaction, "owner", go);

            return go;
        }

        // ---------------------------------------------------------------- 에셋 생성

        private static AttackTimingData GetOrCreateEnemyTiming()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttackTimingData>(EnemyTimingPath);
            if (existing != null)
            {
                return existing;
            }

            var timing = ScriptableObject.CreateInstance<AttackTimingData>();
            timing.name = "AttackTiming_Enemy";
            SetField(timing, "windupSeconds", 0.45f);   // 긴 윈드업 = 전조 동작
            SetField(timing, "activeSeconds", 0f);
            SetField(timing, "recoverySeconds", 0.55f);
            SetField(timing, "hitstopSeconds", 0.06f);
            SetField(timing, "effectLeadSeconds", 0f);

            AssetDatabase.CreateAsset(timing, EnemyTimingPath);
            AssetDatabase.SaveAssets();
            return timing;
        }

        private static Tile GetOrCreatePlaceholderTile(Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(PlaceholderTilePath);
            if (existing != null)
            {
                // 이전에 잘못된 설정으로 만들어졌을 수 있으니 항상 바로잡는다.
                existing.sprite = sprite;
                existing.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "PlaceholderTile";
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;

            AssetDatabase.CreateAsset(tile, PlaceholderTilePath);
            AssetDatabase.SaveAssets();
            return tile;
        }

        // ---------------------------------------------------------------- 헬퍼

        /// <summary>발밑을 원점으로 하는 스프라이트 자식을 만든다 (스프라이트는 중심 피벗 가정).</summary>
        private static SpriteRenderer CreateVisual(Transform parent, string name, Vector3 scale, Color color,
                                                   Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, scale.y * 0.5f, 0f);
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            string materialPath = AssetDatabase.GUIDToAssetPath(DefaultSpriteMaterialGuid);
            if (!string.IsNullOrEmpty(materialPath))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            return renderer;
        }

        /// <summary>
        /// 컴포넌트의 [SerializeField] private 필드에 값을 직접 넣는다.
        /// 씬/에셋에 살아 있는 오브젝트라 저장 시 이 값이 그대로 직렬화된다
        /// (MenuScenesBuilder의 EditorBind와 같은 원리 — SerializedObject보다 확실하다).
        /// </summary>
        private static void SetField(Object component, string fieldName, object value)
        {
            FieldInfo field = component.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (field == null)
            {
                Debug.LogError($"[CombatSandboxBuilder] {component.GetType().Name}에 '{fieldName}' 필드가 없습니다.");
                return;
            }

            field.SetValue(component, value);
            EditorUtility.SetDirty(component);
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[CombatSandboxBuilder] 에셋을 찾지 못했습니다: {path}");
            }

            return asset;
        }

        /// <summary>CombatSandbox를 Build Settings에 (없으면) 추가한다.</summary>
        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
