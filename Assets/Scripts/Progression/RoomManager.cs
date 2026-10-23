using System.Collections.Generic;
using UnityEngine;

// 스테이지마다 방을 코드로 생성한다. 규칙: Docs/SliceA/NormalRoom.md
public class RoomManager : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public int alive;
        public bool exitOpen;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<RoomManager>()) return;
        new GameObject("RoomManager").AddComponent<RoomManager>();
    }

    [SerializeField] int[] enemiesPerRoom = { 2, 3, 3, 4, 4, 5 };
    [SerializeField] float safeDistanceFromEntrance = 10f;

    [System.Serializable]
    struct AnimSpeed
    {
        public string state;
        public float fps;
    }

    [Header("플레이어 애니메이션 속도 (초당 프레임)")]
    [SerializeField] AnimSpeed[] playerAnimSpeeds =
    {
        new() { state = "idle", fps = 5f },
        new() { state = "run", fps = 10f },
    };

    static Sprite square;
    public static Sprite Square => square
        ? square
        : square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);

    static readonly Color GroundColor = Color.gray;
    static readonly Color FloorColor = new(0.18f, 0.18f, 0.2f);
    static readonly Color EnemyColor = new(0.55f, 0.6f, 0.85f);

    public Health CurrentBoss { get; private set; }
    public string CurrentBossName { get; private set; }
    // 보스 처치·일반 스테이지 클리어 확정. 이후 역행 불가 (CombatDecisions 8장)
    public bool CombatConfirmed => flow && flow.Stage > 0 &&
        (flow.IsBossStage ? !CurrentBoss || CurrentBoss.IsDead : alive <= 0);

    GameFlow flow;
    PlayerController player;
    Rigidbody2D playerBody;
    CameraFollow follow;
    Transform room;
    RoomExit exit;
    RoomLayout currentLayout;
    int alive;
    int normalRoomsEntered;
    readonly List<RoomLayout> bag = new();
    readonly List<Collider2D> enemyColliders = new();

    void Awake()
    {
        player = CreatePlayer(RoomLayouts.Arena.Entrance);
        playerBody = player.GetComponent<Rigidbody2D>();

        var cam = Camera.main;
        if (cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            follow = cam.GetComponent<CameraFollow>();
            if (!follow) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = player.transform;
        }
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow)
        {
            flow.StageChanged += OnStageChanged;
            flow.RunReset += OnRunReset;
        }
        // 시작 능력 선택이 없어져 GameFlow가 먼저 1스테이지에 들어갔을 수 있음
        if (flow && flow.Stage > 0) OnStageChanged(flow.Stage);
        else Build(RoomLayouts.Arena);
    }

    public object Capture() => new Snapshot { alive = alive, exitOpen = exit && exit.IsOpen };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        alive = s.alive;
        if (!exit) return;
        exit.Remaining = alive;
        exit.SetOpen(s.exitOpen);
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (!flow) return;
        flow.StageChanged -= OnStageChanged;
        flow.RunReset -= OnRunReset;
    }

    void OnRunReset()
    {
        bag.Clear();
        normalRoomsEntered = 0;
        Build(RoomLayouts.Arena);
    }

    void OnStageChanged(int stage)
    {
        if (flow.IsBossStage) BuildBossRoom();
        else BuildNormalRoom();
    }

    void BuildNormalRoom()
    {
        var layout = NextLayout();
        Build(layout);

        int count = enemiesPerRoom[Mathf.Min(normalRoomsEntered, enemiesPerRoom.Length - 1)];
        normalRoomsEntered++;

        var spots = new List<Vector2>();
        foreach (var s in layout.Spawns)
            if (Vector2.Distance(s, layout.Entrance) >= safeDistanceFromEntrance) spots.Add(s);
        Shuffle(spots);
        count = Mathf.Min(count, spots.Count);

        enemyColliders.Clear();
        for (int i = 0; i < count; i++) SpawnEnemy(spots[i]);

        alive = count;
        currentLayout = layout;
        exit = CreateExit(layout);
        exit.Remaining = alive;
        if (alive == 0) OnRoomCleared();
    }

    // 클리어 확정: 출구 개방, 보스 직전 일반 스테이지면 숨겨진 시간의 통로 개방 (CombatDecisions 11장)
    void OnRoomCleared()
    {
        if (exit) exit.SetOpen(true);
        if (!flow || flow.IsBossStage || !flow.IsBossStageAt(flow.Stage + 1) || currentLayout == null) return;
        // 주 경로(입구 → 출구)에서 벗어난 입구 쪽 위 구석
        var go = Sized("ShopPassage", currentLayout.Entrance + new Vector2(-0.5f, currentLayout.Height / 2f - 1.5f), new Vector2(1f, 2f), Square, ShopPassage.HiddenColor);
        go.AddComponent<ShopPassage>();
    }

    void BuildBossRoom()
    {
        Build(RoomLayouts.Arena);
        switch (flow.BossIndex)
        {
            case 0:
                // 시험판 A 보스. 기존 축적 보스는 코드만 남김
                SpawnBoss<SliceBoss>("시험판 보스", "boss_slice", new Color(0.7f, 0.35f, 0.6f));
                break;
            case 1:
                SpawnBoss<DeferralBoss>("시간 유예", "boss_deferral", new Color(0.85f, 0.6f, 0.25f));
                break;
            case 2:
                SpawnBoss<ReversalBoss>("시간 역행", "boss_reversal", new Color(0.3f, 0.7f, 0.75f));
                break;
            default:
                // 1장 보스는 3명. 그 밖의 설정에서는 출구만 열어 둠
                exit = CreateExit(RoomLayouts.Arena);
                exit.SetOpen(true);
                break;
        }
    }

    void SpawnBoss<T>(string displayName, string art, Color color) where T : Component
    {
        var boss = Block(typeof(T).Name, new Vector2(6f, 0f), new Vector2(2f, 2f), color);
        boss.GetComponent<BoxCollider2D>().isTrigger = true;
        AssetLibrary.ApplyCharacter(boss.GetComponent<SpriteRenderer>(), art + "_idle");
        boss.AddComponent<SpriteAnimator>().BaseName = art;
        boss.AddComponent<T>();
        boss.AddComponent<HitFlash>();
        CurrentBoss = boss.GetComponent<Health>();
        CurrentBossName = displayName;
    }

    // 모든 형태를 한 번씩 쓴 뒤에 다시 섞는다
    RoomLayout NextLayout()
    {
        if (bag.Count == 0)
        {
            bag.AddRange(RoomLayouts.Normal);
            Shuffle(bag);
        }
        var layout = bag[0];
        bag.RemoveAt(0);
        return layout;
    }

    void Build(RoomLayout layout)
    {
        if (room) Destroy(room.gameObject);
        room = new GameObject($"Room_{layout.Name}").transform;
        exit = null;

        float halfW = layout.Length / 2f;
        float halfH = layout.Height / 2f;
        Floor(new Vector2(layout.Length, layout.Height));
        Terrain("WallL", new Vector2(-halfW - 0.5f, 0f), new Vector2(1f, layout.Height + 2f));
        Terrain("WallR", new Vector2(halfW + 0.5f, 0f), new Vector2(1f, layout.Height + 2f));
        Terrain("WallT", new Vector2(0f, halfH + 0.5f), new Vector2(layout.Length, 1f));
        Terrain("WallB", new Vector2(0f, -halfH - 0.5f), new Vector2(layout.Length, 1f));
        foreach (var (center, size) in layout.Blocks) Terrain("Block", center, size);

        player.transform.position = layout.Entrance;
        playerBody.linearVelocity = Vector2.zero;
        if (follow)
        {
            follow.MinX = -halfW;
            follow.MaxX = halfW;
            follow.MinY = -halfH;
            follow.MaxY = halfH;
        }
    }

    void SpawnEnemy(Vector2 pos)
    {
        var go = Block("PatrolEnemy", pos, Vector2.one, EnemyColor);
        AssetLibrary.ApplyCharacter(go.GetComponent<SpriteRenderer>(), "enemy_sentinel_idle");
        go.AddComponent<SpriteAnimator>().BaseName = "enemy_sentinel";
        var enemy = go.AddComponent<PatrolEnemy>();
        enemy.Removed += OnEnemyRemoved;

        // 적끼리, 적과 플레이어는 서로 밀치지 않음(접촉 피해 없음 규칙)
        var col = go.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(col, player.GetComponent<Collider2D>());
        foreach (var other in enemyColliders) Physics2D.IgnoreCollision(col, other);
        enemyColliders.Add(col);
    }

    void OnEnemyRemoved()
    {
        alive--;
        if (!exit) return;
        exit.Remaining = alive;
        if (alive <= 0) OnRoomCleared();
    }

    RoomExit CreateExit(RoomLayout layout)
    {
        var go = Sized("Exit", layout.ExitPos, new Vector2(1f, 2f), RoomManager.Square, Color.white);
        return go.AddComponent<RoomExit>();
    }

    PlayerController CreatePlayer(Vector2 pos)
    {
        var go = new GameObject("Player");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        sr.color = new Color(1f, 0.8f, 0.3f);
        sr.sortingOrder = 1;
        if (!AssetLibrary.ApplyCharacter(sr, "chr_player_idle_s")) AssetLibrary.ApplyCharacter(sr, "chr_player_idle");
        var anim = go.AddComponent<SpriteAnimator>();
        anim.BaseName = "chr_player";
        foreach (var s in playerAnimSpeeds) anim.SetFps(s.state, s.fps);
        // 그림(48px, 투명 여백 포함) 크기를 따라가지 않게 기존 판정 1x1 유지
        go.AddComponent<BoxCollider2D>().size = Vector2.one;
        var controller = go.AddComponent<PlayerController>();
        go.AddComponent<PlayerAttack>();
        // 시험판 A: 시작 능력(AbilityEffects) 미사용. 우클릭은 역행
        go.AddComponent<DeferredDamage>();
        go.AddComponent<PotionUser>();
        go.AddComponent<RewardEffects>();
        go.AddComponent<HitFlash>();
        return controller;
    }

    GameObject Block(string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(room);
        go.transform.position = pos;
        go.transform.localScale = size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        sr.color = color;
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    // 지형(벽·기둥): 크기를 스케일이 아니라 충돌·타일 크기로 지정(타일 그림이 늘어나지 않게)
    void Terrain(string name, Vector2 pos, Vector2 size)
    {
        var tile = AssetLibrary.LoadSprite("tile_wall_ch1");
        Sized(name, pos, size, tile ? tile : Square, tile ? Color.white : GroundColor);
    }

    // 바닥: 충돌 없음, 모든 것 아래에 그림
    void Floor(Vector2 size)
    {
        var tile = AssetLibrary.LoadSprite("tile_ground_ch1");
        var go = Sized("Floor", Vector2.zero, size, tile ? tile : Square, tile ? Color.white : FloorColor);
        Destroy(go.GetComponent<BoxCollider2D>());
        go.GetComponent<SpriteRenderer>().sortingOrder = -10;
    }

    GameObject Sized(string name, Vector2 pos, Vector2 size, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(room);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        go.AddComponent<BoxCollider2D>().size = size;
        return go;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
