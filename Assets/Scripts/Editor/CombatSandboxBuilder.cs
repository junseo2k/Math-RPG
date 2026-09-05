using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MathRPG.Combat;
using MathRPG.Core;
using MathRPG.Data;
using MathRPG.Enemy;
using MathRPG.Player;
using MathRPG.UI;
using TMPro;
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
        private const string PlatformSpritePath = "Assets/Art/Placeholder/WhiteThin.png";
        private const string PlatformTilePath = "Assets/Art/Placeholder/PlatformTile.asset";
        private const string PlayerSpritePath = "Assets/Art/Player.png";
        private const string EnemySpritePath = "Assets/Art/Enemy.png";

        // URP 2D 렌더러의 기본 스프라이트 머티리얼. 손으로 만든 기존 씬의 SpriteRenderer들이 쓰던 것과 동일.
        private const string DefaultSpriteMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        private static readonly Color PlayerTint = new Color(0.35f, 0.75f, 1f, 1f);
        private static readonly Color EnemyTint = new Color(0.9f, 0.35f, 0.4f, 1f);
        private static readonly Color GroundTint = new Color(0.24f, 0.27f, 0.33f, 1f);
        private static readonly Color PlatformTint = new Color(0.47f, 0.53f, 0.63f, 1f);
        private static readonly Color CameraBackground = new Color(0.09f, 0.1f, 0.13f, 1f);

        /// <summary>지면을 아래로 채우는 마지막 셀. 카메라 화면 밖까지 내려가야 "땅"으로 보인다.</summary>
        private const int GroundFillBottom = -6;

        /// <summary>공중 발판의 두께 (units). 1셀보다 얇아야 지면과 구분된다.</summary>
        private const float PlatformThickness = 0.32f;

        // ==================================================================
        //  ★ 캐릭터 크기 · 위치 튜닝 블록 — 여기만 고치면 된다 ★
        //
        //  씬에서 손으로 맞춘 값은 리빌드할 때 전부 날아간다. 영구 수정은 반드시 여기서.
        //  숫자를 바꾼 뒤 메뉴 MathRPG/Build/Combat Sandbox를 다시 실행하면 반영된다.
        // ==================================================================

        /// <summary>
        /// 플레이어 그림의 세로 길이 (units). 그림 원본이 몇 픽셀이든, PPU가 얼마든
        /// CreateVisual이 이 높이에 맞춰 스케일을 역산한다. <b>크기는 이 숫자로 조절.</b>
        /// 참고: 콜라이더 높이는 1.8이다 (PlayerLocomotion.standingSize와 아래 CreatePlayer).
        /// </summary>
        private const float PlayerVisualHeight = 2.2f;

        /// <summary>
        /// 플레이어 그림의 위치 미세 보정 (units). <b>위치는 이 숫자로 조절.</b>
        /// 기본 정렬은 "그림의 아래끝을 발밑(y = 0)에 맞추고 가로는 가운데"인데,
        /// 그림 여백이 비대칭이거나 캐릭터가 캔버스 가운데에 있지 않으면 어긋나 보인다.
        /// y를 올리면 위로, x를 올리면 오른쪽으로 밀린다. (예: 캐릭터가 떠 보이면 y를 음수로)
        /// </summary>
        private static readonly Vector2 PlayerVisualOffset = new Vector2(-0.2f, -0.2f);

        /// <summary>몬스터 그림의 세로 길이 (units).</summary>
        private const float EnemyVisualHeight = 1.8f;

        /// <summary>몬스터 그림의 위치 미세 보정 (units). PlayerVisualOffset와 같은 의미.</summary>
        private static readonly Vector2 EnemyVisualOffset = new Vector2(0f, 0f);

        /// <summary>플레이어가 처음 서 있을 위치. 발밑 기준이라 y는 지면보다 살짝 위면 된다.</summary>
        private static readonly Vector3 PlayerSpawn = new Vector3(-2f, 0.6f, 0f);

        /// <summary>몬스터가 처음 서 있을 위치. 순찰 왕복의 중심이 된다.</summary>
        private static readonly Vector3 EnemySpawn = new Vector3(5f, 0.6f, 0f);

        /// <summary>체력바를 그림 머리 위로 띄우는 여유 (units). 그림 높이에 더해진다.</summary>
        private const float HealthBarMargin = 0.3f;

        /// <summary>공격 쿨다운 바를 체력바 아래로 내리는 간격 (units). 체력바 전체 높이(0.23)보다 조금 크게.</summary>
        private const float AttackBarGap = 0.26f;

        /// <summary>몬스터 몸에 닿았을 때 들어가는 피해량. 평타(12)보다 작게 둔다.</summary>
        private const float EnemyContactDamage = 6f;

        /// <summary>계속 닿아 있을 때 접촉 피해가 반복되는 간격 (초).</summary>
        private const float EnemyContactInterval = 0.8f;

        /// <summary>
        /// 플레이어가 피해를 받은 뒤 무적인 시간 (초).
        /// 몬스터에는 주지 않는다 — 여기에 값을 주면 플레이어의 연타가 씹혀 전투가 답답해진다.
        /// </summary>
        private const float PlayerInvulnerableSeconds = 1f;

        // ================= 튜닝 블록 끝 =====================================

        [MenuItem("MathRPG/Build/Combat Sandbox", priority = 21)]
        public static void Build()
        {
            // 에셋 존재 확인은 씬을 새로 만들기 전에 끝낸다 — 빠진 에셋이 있으면
            // 열려 있던 씬을 날리지 않고 중단할 수 있어야 하므로.
            if (RequireAsset<InputReader>(InputReaderPath) == null ||
                RequireAsset<AttackTimingData>(PlayerTimingPath) == null ||
                RequireAsset<Sprite>(PlaceholderSpritePath) == null)
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

            // 없으면 만들어 두기만 한다. 실제로 쓸 참조는 씬을 만든 뒤에 다시 잡는다.
            GetOrCreateEnemyTiming();
            GetOrCreatePlaceholderTile(AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath));
            GetOrCreatePlatformTile(EnsurePlatformSprite());

            LayerMask walkableMask = (1 << LayerMask.NameToLayer("Default")) | (1 << groundLayer);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 에셋 로드는 반드시 NewScene '이후'에 한다.
            // NewScene은 이전 씬을 언로드하면서 참조가 남지 않은 에셋도 같이 언로드하고,
            // 그러면 씬 생성 전에 받아둔 참조는 파괴된 상태(Unity의 "가짜 null")가 된다.
            // 특히 Tile이 그렇다 — 파괴된 Tile로 SetTile을 호출하면 "타일 지우기"로 처리돼서
            // 오류도 경고도 없이 타일이 0개가 된다.
            InputReader inputReader = AssetDatabase.LoadAssetAtPath<InputReader>(InputReaderPath);
            AttackTimingData playerTiming = AssetDatabase.LoadAssetAtPath<AttackTimingData>(PlayerTimingPath);
            AttackTimingData enemyTiming = AssetDatabase.LoadAssetAtPath<AttackTimingData>(EnemyTimingPath);
            Sprite placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            Tile groundTile = AssetDatabase.LoadAssetAtPath<Tile>(PlaceholderTilePath);
            Tile platformTile = AssetDatabase.LoadAssetAtPath<Tile>(PlatformTilePath);

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

            CreateCamera();
            CreateGameManager();
            CreateLevel(groundTile, platformTile, groundLayer);

            GameObject player = CreatePlayer(playerSprite, playerSprite == placeholder,
                inputReader, playerTiming, playerLayer, enemyLayer, walkableMask);

            GameObject enemy = CreateEnemy(enemySprite, enemySprite == placeholder,
                enemyTiming, enemyLayer, playerLayer, walkableMask, player.transform);

            // 체력바는 캐릭터의 자식이라 따로 따라다니는 로직이 없다. 머리 위 높이는
            // 스프라이트가 플레이스홀더냐 실제 그림이냐에 따라 다르다.
            // 바 높이는 각 캐릭터의 그림 높이에서 파생시킨다. 예전에는 플레이스홀더 2.1 /
            // 실제 그림 2.6으로 갈라 두었는데, 그건 그림 크기를 손으로 맞춘다는 전제의 값이었다.
            // 이제 그림이 항상 지정 높이로 정규화되므로 분기가 필요 없고,
            // 튜닝 블록에서 그림 높이를 바꾸면 바도 알아서 따라 올라간다.
            float playerBarY = PlayerVisualHeight + HealthBarMargin;
            float enemyBarY = EnemyVisualHeight + HealthBarMargin;
            CreateHealthBar(player, playerBarY, 1.4f, placeholder);
            CreateHealthBar(enemy, enemyBarY, 1.1f, placeholder);

            // 공격 쿨다운 바는 체력바 '바로 아래'. 폭은 각자의 체력바와 맞춘다.
            //
            // 몬스터 쪽 바는 조작 피드백이 아니라 읽을거리다 — 윈드업 동안 바가 차 있으니
            // "지금 공격 모션 중"이 한눈에 보이고, 바가 사라지는 순간이 곧 다음 공격이
            // 가능해지는 시점이다. 스프라이트 뒤로 빼기(텔레그래프)와 겹쳐서 읽힌다.
            CreateAttackBar(player, playerBarY - AttackBarGap, 1.4f, placeholder);
            CreateAttackBar(enemy, enemyBarY - AttackBarGap, 1.1f, placeholder);
            CreateDamagePopups(player);

            Selection.activeGameObject = enemy;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            Debug.Log($"[CombatSandboxBuilder] 생성 완료: {ScenePath}\n" +
                      $"  플레이어({LayerMask.LayerToName(playerLayer)}) · 몬스터({LayerMask.LayerToName(enemyLayer)})\n" +
                      $"  스프라이트: 플레이어={(playerSprite == placeholder ? "플레이스홀더" : PlayerSpritePath)}, " +
                      $"몬스터={(enemySprite == placeholder ? "플레이스홀더" : EnemySpritePath)}\n" +
                      "  체력바: 캐릭터 머리 위 · 공격 쿨다운 바: 체력바 아래(플레이어 · 몬스터) · 데미지 숫자: 피격 지점\n" +
                      "  타격감: 히트스톱(맞았을 때만) · 실제 넉백 · 카메라 흔들림(플레이어 피격 시에만) · 몬스터 접촉 피해\n" +
                      "  무적: 플레이어만 피격 후 1초 (깜빡임으로 표시)\n" +
                      "  조작: 이동 A/D · 점프 Space · 숙이기 S · 평타 좌클릭/J");
        }

        // ---------------------------------------------------------------- 씬 오브젝트

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";

            // 전투 반응(흔들림 · 줌 펀치). 값은 컴포넌트 기본값을 그대로 쓰고 인스펙터에서 튜닝한다.
            go.AddComponent<CombatCamera>();

            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            camera.orthographic = true;

            // 지형과 캐릭터가 화면을 채우도록 당겨 잡는다. 예전 값(6 / y=2.5)은
            // 위쪽 절반이 빈 하늘이라 바닥이 화면 가운데 뜬 띠처럼 보였다.
            camera.orthographicSize = 4.5f;
            go.transform.position = new Vector3(0f, 1.6f, -10f);
        }

        private static void CreateGameManager()
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        /// <summary>Tilemap 지형. 지면 표면은 y = 0.</summary>
        /// <remarks>
        /// 지면과 공중 발판을 서로 다른 Tilemap으로 나눈다 — 같은 두께에 같은 색이면
        /// 어느 쪽이 밟고 선 땅인지 한눈에 구분되지 않기 때문이다. 지면은 화면 아래까지
        /// 꽉 채운 어두운 덩어리로, 발판은 밝고 얇은 판으로 만든다.
        /// </remarks>
        private static void CreateLevel(Tile groundTile, Tile platformTile, int groundLayer)
        {
            // Grid를 먼저 만들고, 자식으로 붙인 다음에 Tilemap 계열 컴포넌트를 추가한다
            // (Tilemap은 부모 GridLayout에 의존하므로 순서가 중요하다).
            var grid = new GameObject("Level", typeof(Grid));

            Tilemap ground = CreateTilemap(grid.transform, "Tilemap_Ground", groundLayer, GroundTint, -10);
            Tilemap platform = CreateTilemap(grid.transform, "Tilemap_Platform", groundLayer, PlatformTint, -9);

            // 지면 — 표면(셀 y=-1)에서 화면 밖까지 아래로 꽉 채운다.
            PaintRect(ground, groundTile, -20, 20, GroundFillBottom, -1);

            // 좌우 벽 — 순찰 범위를 막는 경계.
            PaintRect(ground, groundTile, -14, -14, 0, 4);
            PaintRect(ground, groundTile, 14, 14, 0, 4);

            // 낮은 계단은 지면 쪽에 둔다 — 땅에 붙어 있는 턱이라 얇은 판으로 그리면
            // 지면과 사이가 뜬 선반처럼 보인다.
            PaintRect(ground, groundTile, -8, -6, 0, 0);

            // 공중 발판 — 얇고 밝은 판. 밟는 면은 셀 윗면(y=3)이다.
            PaintRect(platform, platformTile, 4, 8, 2, 2);

            // 타일별 콜라이더. CompositeCollider2D 없이 개별 박스 콜라이더 —
            // 세팅 단순하고 정적 지형으로 확실하게 동작한다. Rigidbody2D 없으면 정적 콜라이더.
            // 발판도 셀 단위 콜라이더라 밟는 면이 보이는 판의 윗면과 정확히 일치한다.
            ground.gameObject.AddComponent<TilemapCollider2D>();
            platform.gameObject.AddComponent<TilemapCollider2D>();

            int usedTiles = ground.GetUsedTilesCount() + platform.GetUsedTilesCount();
            if (usedTiles == 0)
            {
                Debug.LogError("[CombatSandboxBuilder] 타일이 하나도 배치되지 않았습니다. " +
                               "Tile 에셋 참조가 파괴된 상태일 수 있습니다 — 에셋은 NewScene 이후에 로드해야 합니다.");
                return;
            }

            Debug.Log($"[CombatSandboxBuilder] Tilemap 지면 {ground.cellBounds.size} · 발판 {platform.cellBounds.size}, 콜라이더 생성.");
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int layer, Color tint, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;

            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            tilemap.color = tint;

            ApplyDefaultSpriteMaterial(renderer);
            return tilemap;
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
            go.transform.position = PlayerSpawn;

            go.AddComponent<Rigidbody2D>().freezeRotation = true;

            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 1.8f);
            collider.offset = new Vector2(0f, 0.9f);
            collider.direction = CapsuleDirection2D.Vertical;

            var locomotion = go.AddComponent<PlayerLocomotion>();
            go.AddComponent<AttackTimeline>();
            var attack = go.AddComponent<PlayerAttack>();
            var hitbox = go.AddComponent<Hitbox>();

            // 무적 프레임은 플레이어에게만. 모든 피해가 Health.ApplyDamage 하나를 거치므로
            // 평타든 접촉 피해든 전부 이 한 값으로 막힌다.
            var health = go.AddComponent<Health>();
            SetField(health, "invulnerableSeconds", PlayerInvulnerableSeconds);
            go.AddComponent<Hurtbox>();
            go.AddComponent<RespawnOnDeath>();
            var knockback = go.AddComponent<KnockbackReceiver>();

            // 플레이어는 몬스터보다 덜 밀리고 덜 굳는다 — 조작이 오래 끊기면 답답하다.
            SetField(knockback, "owner", go);
            SetField(knockback, "speed", 5f);
            SetField(knockback, "staggerSeconds", 0.14f);

            SetField(locomotion, "input", inputReader);
            SetField(locomotion, "groundLayers", walkableMask);

            SetField(attack, "input", inputReader);
            SetField(attack, "basicAttackTiming", timing);

            SetField(hitbox, "attacker", go);
            SetField(hitbox, "hittableLayers", (LayerMask)(1 << enemyLayer));

            SpriteRenderer visual = CreateVisual(go.transform, "Visual", sprite,
                PlayerVisualHeight,
                stretchWidth: isPlaceholder ? 0.7f : 0f,   // 플레이스홀더만 콜라이더 폭으로 늘린다
                isPlaceholder ? Vector2.zero : PlayerVisualOffset,
                isPlaceholder ? PlayerTint : Color.white, sortingOrder: 10);

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
            go.transform.position = EnemySpawn;

            go.AddComponent<Rigidbody2D>().freezeRotation = true;

            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.9f, 1.8f);
            collider.offset = new Vector2(0f, 0.9f);
            collider.direction = CapsuleDirection2D.Vertical;

            go.AddComponent<AttackTimeline>();
            var enemyAttack = go.AddComponent<EnemyAttack>();
            var hitbox = go.AddComponent<Hitbox>();
            var health = go.AddComponent<Health>();
            go.AddComponent<Hurtbox>();
            var respawn = go.AddComponent<RespawnOnDeath>();
            var ai = go.AddComponent<EnemyAI>();
            var knockback = go.AddComponent<KnockbackReceiver>();

            // 몸에 닿기만 해도 아프다. 공격 타이밍과 무관한 별도 판정이라
            // 몬스터가 후딜 중이거나 순찰 중이어도 부딪히면 피해가 들어간다.
            var contact = go.AddComponent<ContactDamage>();
            SetField(contact, "source", go);
            SetField(contact, "selfHealth", health);
            SetField(contact, "damage", EnemyContactDamage);
            SetField(contact, "interval", EnemyContactInterval);
            SetField(contact, "hittableLayers", (LayerMask)(1 << playerLayer));

            // 몬스터는 크게 밀린다 — 내가 때린 결과가 눈에 보여야 한다.
            SetField(knockback, "owner", go);
            SetField(knockback, "speed", 7.5f);
            SetField(knockback, "staggerSeconds", 0.18f);

            SetField(enemyAttack, "attackTiming", timing);

            SetField(hitbox, "attacker", go);
            SetField(hitbox, "hittableLayers", (LayerMask)(1 << playerLayer));

            SetField(ai, "target", target);
            SetField(ai, "groundLayers", walkableMask);

            SetField(respawn, "disableWhileDead", collider);

            SpriteRenderer visual = CreateVisual(go.transform, "Visual", sprite,
                EnemyVisualHeight,
                stretchWidth: isPlaceholder ? 0.9f : 0f,
                isPlaceholder ? Vector2.zero : EnemyVisualOffset,
                isPlaceholder ? EnemyTint : Color.white, sortingOrder: 5);

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

        /// <summary>
        /// 공중 발판용 얇은 흰색 스프라이트를 만든다 (없을 때만).
        /// 지면과 같은 1×1 그림을 쓰면 발판도 한 셀 두께가 되어 어느 쪽이 땅인지 구분되지 않는다.
        /// </summary>
        private static Sprite EnsurePlatformSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlatformSpritePath);
            if (existing != null)
            {
                return existing;
            }

            const int pixelsPerUnit = 100;
            int height = Mathf.RoundToInt(PlatformThickness * pixelsPerUnit);

            var texture = new Texture2D(pixelsPerUnit, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[pixelsPerUnit * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(PlatformSpritePath));
            File.WriteAllBytes(PlatformSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(PlatformSpritePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlatformSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlatformSpritePath);
        }

        /// <summary>
        /// 발판 타일. 얇은 스프라이트를 셀 위쪽에 붙여 그린다 — 콜라이더는 셀 전체(Grid)라
        /// 밟는 면이 눈에 보이는 판의 윗면과 정확히 일치한다.
        /// </summary>
        private static Tile GetOrCreatePlatformTile(Sprite sprite)
        {
            // 얇은 판은 셀 한가운데에 그려지므로 셀 윗면까지 끌어올린다.
            Matrix4x4 offset = Matrix4x4.Translate(new Vector3(0f, 0.5f - PlatformThickness * 0.5f, 0f));

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(PlatformTilePath);
            if (existing != null)
            {
                existing.sprite = sprite;
                existing.colliderType = Tile.ColliderType.Grid;
                existing.transform = offset;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "PlatformTile";
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            tile.transform = offset;

            AssetDatabase.CreateAsset(tile, PlatformTilePath);
            AssetDatabase.SaveAssets();
            return tile;
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
        /// <summary>
        /// 캐릭터 그림을 만든다. <b>스케일과 위치를 스프라이트 원본 크기에서 역산</b>하므로,
        /// 아트를 교체하거나 씬을 다시 만들어도 손으로 크기·위치를 맞출 필요가 없다.
        /// </summary>
        /// <remarks>
        /// 예전에는 스케일을 호출하는 쪽이 직접 넘겼고, 실제 아트에는 Vector3.one을 줬다.
        /// 그런데 Player.png는 1047×1385px에 PPU 100이라 스케일 1이면 13.85유닛 높이가 된다
        /// (카메라 화면이 9유닛). 게다가 피벗이 한가운데라 "원점 = 발밑" 규칙과도 어긋나서,
        /// 리빌드할 때마다 씬에서 손으로 줄이고 올려야 했다. 그 계산을 여기로 옮긴 것이다.
        ///
        /// 두 가지를 스프라이트에서 읽는다:
        /// - <c>sprite.bounds.size</c> — PPU가 이미 반영된 월드 단위 크기. 픽셀 수를 몰라도 된다.
        /// - <c>sprite.bounds.min.y</c> — 피벗에서 그림 아래끝까지의 거리. 피벗이 가운데든
        ///   아래든 이 값만 쓰면 발밑이 부모 원점에 정확히 맞는다.
        /// </remarks>
        /// <param name="targetHeight">그림이 차지할 세로 길이 (units).</param>
        /// <param name="stretchWidth">
        /// 0보다 크면 가로를 이 길이로 <b>늘린다</b> — 1×1 사각형을 콜라이더 모양 상자로 만드는
        /// 플레이스홀더용이다. 0이면 <b>종횡비를 유지</b>한 채 targetHeight에 맞춘다 (실제 아트용).
        /// </param>
        /// <param name="offset">
        /// 자동 정렬 위에 얹는 미세 보정 (units). 그림 여백이 비대칭이라 자동 정렬만으로
        /// 어긋나 보일 때 쓴다. 튜닝 블록의 PlayerVisualOffset / EnemyVisualOffset.
        /// </param>
        private static SpriteRenderer CreateVisual(Transform parent, string name, Sprite sprite,
                                                   float targetHeight, float stretchWidth,
                                                   Vector2 offset, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            ApplyDefaultSpriteMaterial(renderer);

            Bounds bounds = sprite != null ? sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            if (bounds.size.y <= 0f || bounds.size.x <= 0f)
            {
                // 스프라이트를 못 읽었을 때의 안전값 — 적어도 화면을 뒤덮지는 않게.
                Debug.LogWarning($"[CombatSandboxBuilder] '{name}'의 스프라이트 크기를 읽지 못해 스케일 1로 둡니다.");
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = new Vector3(offset.x, targetHeight * 0.5f + offset.y, 0f);
                return renderer;
            }

            float scaleY = targetHeight / bounds.size.y;
            float scaleX = stretchWidth > 0f ? stretchWidth / bounds.size.x : scaleY;

            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // 발밑을 부모 원점(y = 0)에 맞춘다. bounds.min.y는 보통 음수이므로 부호를 뒤집는다.
            // 그 위에 튜닝 블록의 보정값을 더한다.
            go.transform.localPosition = new Vector3(
                offset.x,
                -bounds.min.y * scaleY + offset.y,
                0f);

            return renderer;
        }

        private static void ApplyDefaultSpriteMaterial(Renderer renderer)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(DefaultSpriteMaterialGuid);
            if (string.IsNullOrEmpty(materialPath))
            {
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        // ---------------------------------------------------------------- 전투 UI

        /// <summary>캐릭터 머리 위 체력바. 캐릭터의 자식이라 따라다니는 로직이 필요 없다.</summary>
        private static void CreateHealthBar(GameObject owner, float yOffset, float width, Sprite sprite)
        {
            const float height = 0.14f;
            const float border = 0.045f;

            var barGo = new GameObject("HealthBar");
            barGo.transform.SetParent(owner.transform, false);
            barGo.transform.localPosition = new Vector3(0f, yOffset, 0f);

            CreateBarPiece(barGo.transform, "Back",
                new Vector3(width + border * 2f, height + border * 2f, 1f),
                new Color(0.05f, 0.06f, 0.08f, 0.9f), sprite, sortingOrder: 100);

            SpriteRenderer fill = CreateBarPiece(barGo.transform, "Fill",
                new Vector3(width, height, 1f), Color.white, sprite, sortingOrder: 101);

            var bar = barGo.AddComponent<WorldHealthBar>();
            SetField(bar, "owner", owner);
            SetField(bar, "fill", fill.transform);
            SetField(bar, "fillRenderer", fill);
            SetField(bar, "width", width);
        }

        /// <summary>
        /// 공격 쿨다운 바. 체력바와 같은 조각을 쓰되 더 얇게 만들어 한눈에 구분되게 한다.
        /// 쉬는 동안에는 WorldAttackBar가 렌더러를 꺼서 통째로 사라진다.
        /// </summary>
        private static void CreateAttackBar(GameObject owner, float yOffset, float width, Sprite sprite)
        {
            const float height = 0.09f;   // 체력바(0.14)보다 얇게
            const float border = 0.035f;

            var barGo = new GameObject("AttackBar");
            barGo.transform.SetParent(owner.transform, false);
            barGo.transform.localPosition = new Vector3(0f, yOffset, 0f);

            SpriteRenderer back = CreateBarPiece(barGo.transform, "Back",
                new Vector3(width + border * 2f, height + border * 2f, 1f),
                new Color(0.05f, 0.06f, 0.08f, 0.9f), sprite, sortingOrder: 100);

            SpriteRenderer fill = CreateBarPiece(barGo.transform, "Fill",
                new Vector3(width, height, 1f), Color.white, sprite, sortingOrder: 101);

            var bar = barGo.AddComponent<WorldAttackBar>();
            SetField(bar, "owner", owner);
            SetField(bar, "fill", fill.transform);
            SetField(bar, "fillRenderer", fill);
            SetField(bar, "backRenderer", back);
            SetField(bar, "width", width);
        }

        /// <summary>체력바 조각. CreateVisual과 달리 원점을 발밑이 아니라 한가운데에 둔다.</summary>
        private static SpriteRenderer CreateBarPiece(Transform parent, string name, Vector3 scale, Color color,
                                                     Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            ApplyDefaultSpriteMaterial(renderer);
            return renderer;
        }

        /// <summary>피격 지점에 뜨는 데미지 숫자. 비활성 템플릿 하나를 두고 스포너가 복제해 쓴다.</summary>
        private static void CreateDamagePopups(GameObject player)
        {
            var go = new GameObject("DamagePopups");
            var spawner = go.AddComponent<DamagePopupSpawner>();

            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(go.transform, false);

            var text = templateGo.AddComponent<TextMeshPro>();
            text.text = "0";
            text.fontSize = 4f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.sortingOrder = 200;

            // 기본 RectTransform은 폭이 좁아 두 자리 숫자에서 줄이 바뀐다.
            var rect = templateGo.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(4f, 1f);
            }

            if (text.font == null)
            {
                Debug.LogWarning("[CombatSandboxBuilder] TMP 기본 폰트가 없습니다. " +
                                 "Window > TextMeshPro > Import TMP Essential Resources를 먼저 실행하세요.");
            }

            var popup = templateGo.AddComponent<DamagePopup>();
            SetField(popup, "label", text);

            templateGo.SetActive(false);

            SetField(spawner, "template", popup);
            SetField(spawner, "highlightVictim", player);
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
