using System.Collections.Generic;
using UnityEngine;

// 스테이지마다 방을 코드로 생성한다. 규칙: Docs/NormalRoom.md
public class RoomManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<RoomManager>()) return;
        new GameObject("RoomManager").AddComponent<RoomManager>();
    }

    [SerializeField] int[] enemiesPerRoom = { 2, 3, 3, 4, 4, 5 };
    [SerializeField] float safeDistanceFromEntrance = 21f;

    static Sprite square;
    public static Sprite Square => square
        ? square
        : square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);

    static readonly Color GroundColor = Color.gray;
    static readonly Color EnemyColor = new(0.55f, 0.6f, 0.85f);

    public Health CurrentBoss { get; private set; }
    public string CurrentBossName { get; private set; }

    GameFlow flow;
    PlayerController player;
    Rigidbody2D playerBody;
    CameraFollow follow;
    Transform room;
    RoomExit exit;
    int alive;
    int normalRoomsEntered;
    readonly List<RoomLayout> bag = new();
    readonly List<Collider2D> enemyColliders = new();

    void Awake()
    {
        player = CreatePlayer(new Vector2(RoomLayouts.Arena.EntranceX, 0.5f));
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
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow)
        {
            flow.StageChanged += OnStageChanged;
            flow.RunReset += OnRunReset;
        }
        Build(RoomLayouts.Arena);
    }

    void OnDestroy()
    {
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
            if (s.x >= layout.EntranceX + safeDistanceFromEntrance) spots.Add(s);
        Shuffle(spots);
        count = Mathf.Min(count, spots.Count);

        enemyColliders.Clear();
        for (int i = 0; i < count; i++) SpawnEnemy(spots[i]);

        alive = count;
        exit = CreateExit(layout);
        exit.Remaining = alive;
        if (alive == 0) exit.SetOpen(true);
    }

    void BuildBossRoom()
    {
        Build(RoomLayouts.Arena);
        switch (flow.BossIndex)
        {
            case 0:
                SpawnBoss<AccumulationBoss>("시간 축적", "boss_accumulation", new Color(0.85f, 0.3f, 0.3f));
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
        var boss = Block(typeof(T).Name, new Vector2(6f, 1f), new Vector2(2f, 2f), color);
        boss.GetComponent<BoxCollider2D>().isTrigger = true;
        AssetLibrary.ApplyCharacter(boss.GetComponent<SpriteRenderer>(), art + "_idle");
        boss.AddComponent<SpriteAnimator>().BaseName = art;
        boss.AddComponent<T>();
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

        float half = layout.Length / 2f;
        Terrain("Floor", new Vector2(0f, -0.5f), new Vector2(layout.Length + 2f, 1f));
        Terrain("WallL", new Vector2(-half - 0.5f, 5f), new Vector2(1f, 12f));
        Terrain("WallR", new Vector2(half + 0.5f, 5f), new Vector2(1f, 12f));
        foreach (var (center, size) in layout.Blocks) Terrain("Ground", center, size);

        player.transform.position = new Vector2(layout.EntranceX, 0.5f);
        playerBody.linearVelocity = Vector2.zero;
        if (follow)
        {
            follow.MinX = -half;
            follow.MaxX = half;
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
        if (alive <= 0) exit.SetOpen(true);
    }

    RoomExit CreateExit(RoomLayout layout)
    {
        var go = Sized("Exit", new Vector2(layout.ExitX, 1f), new Vector2(1f, 2f), RoomManager.Square, Color.white);
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
        AssetLibrary.ApplyCharacter(sr, "chr_player_idle");
        go.AddComponent<SpriteAnimator>().BaseName = "chr_player";
        go.AddComponent<BoxCollider2D>();
        var controller = go.AddComponent<PlayerController>();
        go.AddComponent<PlayerAttack>();
        go.AddComponent<AbilityEffects>();
        go.AddComponent<DeferredDamage>();
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

    // 지형: 크기를 스케일이 아니라 충돌·타일 크기로 지정(타일 그림이 늘어나지 않게)
    void Terrain(string name, Vector2 pos, Vector2 size)
    {
        var tile = AssetLibrary.LoadSprite("tile_ground_ch1");
        Sized(name, pos, size, tile ? tile : Square, tile ? Color.white : GroundColor);
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
