using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

using static KamuraPrime.Constants;
using static KamuraPrime.Misc.BehaviorTreeUtils;

namespace KamuraPrime;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }

    private Harmony harmony;
    private ConfigEntry<bool> spawnStickyBombP2;
    private ConfigEntry<bool> disableDarknessP2;
    private ConfigEntry<bool> goodtimefrog;

    private BehaviourTreeRunner runner;
    private BossBlackboard blackboard;
    private GameObject lights;
    private BehaviourTree tree;

    public bool IsP2;

    public bool GoodTimeFrog => goodtimefrog.Value;
    public bool IsDebug = false;

    public void Awake()
    {
        Instance = this;
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);

        spawnStickyBombP2 = Config.Bind("General", "SpawnExtraStickyBombP2", true, "If enabled, an extra bomb is spawned on top of the player when on Phase 2");
        disableDarknessP2 = Config.Bind("General", "DisableDarknessP2", false, "If enabled, disables the blackout effect on Phase 2");

        goodtimefrog = Config.Bind("goodtimefrog", "goodtimefrog", false, "goodtimefrog");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        SceneManager.sceneLoaded += CheckBossScene;
    }

    public void Start()
    {
        if (IsDebug && SceneManager.GetActiveScene().name == "MainMenu")
            CustomUIManager.AddBossChallengeButton();

        StartCoroutine(StoreLights());
    }

    public void Update()
    {
        if (!IsDebug) return;

        if (Input.GetKeyDown(KeyCode.G))
            AttackManager.ForceDebugNode();

        if (Input.GetKeyDown(KeyCode.H))
        {
            blackboard.Health = blackboard.GetMaxHealth() / 2f;
            TryTriggerPhase2();
        }
    }

    public void OnDestroy()
    {
        harmony?.UnpatchSelf();
        lights?.SetActive(true);
        SceneManager.sceneLoaded -= CheckBossScene;
        TryCleanUp(); 
    }

    public void CheckBossScene(Scene scene, LoadSceneMode mode)
    {
        TryCleanUp();

        if (scene.name == "MainMenu")
            CustomUIManager.AddBossChallengeButton();

        if (scene.name == "Scene_GameDesign_Boss_ART")
            InitializeBoss();
    }

    public void SpawnStickyBomb()
    {
        if (runner == null) return;

        var poolManager = GameManager.Instance.ObjectPoolingManager.ProjectilePoolingManager;

        var bomb = poolManager.Pop(STICKY_BOMB_PROJECTILE_NAME);
        bomb.transform.position = runner.blackboard.transform.position;
        bomb.GetComponent<StickyBomb>().Shoot(9999, Vector2.zero, 10f, runner.blackboard);
   
        if (!IsP2 || !spawnStickyBombP2.Value) return;

        var playerBomb = poolManager.Pop(STICKY_BOMB_PROJECTILE_NAME);
        playerBomb.transform.position = GameManager.Instance.PlayerStateMachine.transform.position;
        playerBomb.GetComponent<StickyBomb>().Shoot(9999, Vector2.zero, 10f, runner.blackboard);
    }

    public void InitializeBoss()
    {
        IsP2 = false;

        runner = FindFirstObjectByType<BehaviourTreeRunner>(FindObjectsInactive.Include);
        runner.blackboard.Animator.speed = 1.5f;
        var bossBlackboard = runner.blackboard as BossBlackboard;

        blackboard = bossBlackboard;

        TryCloneTree(runner, runner.tree, bossBlackboard);
        ModifyHP(bossBlackboard);
        InitializeEventListeners(bossBlackboard);
        SetInfiniteStamina(bossBlackboard);
        ChangeHeavyModifiers(bossBlackboard);
        ApplySpriteEffects(bossBlackboard);
        ApplyLightningEffects(.8f, new Color(0.58f, 0.3339f, 0.46f, 0.5396f)); 
        AttackManager.Initialize(tree, runner, bossBlackboard, runner.blackboard.Animator);
    }

    private void TryCloneTree(BehaviourTreeRunner runner, BehaviourTree incomingTree, Blackboard blackboard)
    {
        if (!tree)
        {
            Log("Creating new Tree");
            tree = CloneTree(incomingTree);
        }
        tree.Bind(blackboard);
        runner.tree = tree;
    }

    private void ModifyHP(BossBlackboard board)
    {
        board.Profile.MaxHealth = MAX_HP;
        board.Profile.PhaseChangeHealthPercentages = PHASE_THRESHOLDS.ToList();
    }

    private void InitializeEventListeners(BossBlackboard blackboard)
    {
        blackboard.onHealthChanged.AddListener((current) =>
        {
            float halfHealth = runner.blackboard.GetMaxHealth() / 2f;
            if (current <= halfHealth)
                TryTriggerPhase2();
        });
    }

    private void ApplySpriteEffects(BossBlackboard bossBlackboard)
    {
        bossBlackboard.GFXObject.transform.Find("GFX").TryGetComponent<SpriteRenderer>(out var sr);
        var color = sr.color;
        color.b = 1f;
        sr.color = color;

        var dummies = bossBlackboard.GFXObject.transform.Find("GFX_Rotation").GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var dummy in dummies)
        {
            var dummyColor = dummy.color;
            dummyColor.b = 1f;
            dummy.color = dummyColor;
        }
    }

    private void SetInfiniteStamina(BossBlackboard blackboard)
    {
        var runtimeData = Traverse.Create(blackboard).Field<BossRunTimeData>("bossRunTimeData").Value;
        runtimeData.MaxStamina = float.MaxValue;
        runtimeData.CurrentStamina = float.MaxValue;
    }

    private void ChangeHeavyModifiers(BossBlackboard blackboard)
    {
        blackboard.Profile.WeakPointRangeScale = WEAK_POINT_RANGES;
    }

    private void TryTriggerPhase2()
    {
        if (IsP2) return;

        IsP2 = true;
        AttackManager.SetPhase2();
        
        if (!disableDarknessP2.Value)
            lights?.SetActive(false);
    }

    private void ApplyLightningEffects(float brightnessVolume, Color lightningColor)
    {
        VolumeManager.Instance.SetWeight(VolumeManager.Instance.BrightVolume, brightnessVolume, 0f);
        StartCoroutine(SetLightning(lightningColor));
    }

    private IEnumerator SetLightning(Color lightningColor)
    {
        GameObject lightsHolder = null;

        do
        {
            lightsHolder = GameObject.Find("SceneRoot/NonEntities/BackGround/Props/Lighting");
            yield return null;
        }
        while (lightsHolder == null);

        var light = lightsHolder.transform.Find("Shdow (2)").GetComponent<SpriteRenderer>();
        light.color = lightningColor;
    }

    private void TryCleanUp()
    {
        if (lights && !lights.activeSelf)
            lights.SetActive(true);
    }

    public void UnlockAbilities()
    {
        StartCoroutine(PlayerInitializationRoutine());
    }

    private IEnumerator PlayerInitializationRoutine()
    {
        yield return new WaitUntil(() => GameManager.Instance.PlayerStateMachine);

        GameManager.Instance.DataManager.OutGameData.IsStoryMode = false;

        var player = GameManager.Instance.PlayerStateMachine;        
        
        player.PlayerRunTimeData.CanStrongAttack = true;
        player.PlayerRunTimeData.CanThrowShuriken = true;
        player.PlayerRunTimeData.AddStamina(1000f);
    }

    private IEnumerator StoreLights()
    {
        yield return new WaitUntil(() => GameManager.Instance);
        lights = GameObject.Find("Lights");
    }

    public void Log(object message, LogLevel level = LogLevel.Info)
    {
        if (!IsDebug) return;

        Logger.Log(level, message);
    }
}