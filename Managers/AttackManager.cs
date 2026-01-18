using System;
using System.Collections.Generic;
using UnityEngine;
using KamuraPrime.States;

using static KamuraPrime.Misc.BehaviorTreeUtils;

namespace KamuraPrime
{
    public static class AttackManager
    {
        public static Dictionary<string, NodeBT> Registry = new();

        private static Dictionary<EliteAirShootState.ShootType, EliteAirAttackInfo> spreads = new();
        private static HashSet<string> nonCancellableStates = new()
        {
            "5 Step Tele Combo",
            "Arena Bounds Combo",
            "Aim Combo",
            "Triple Spread Combo",
            "Triple Shotgun Combo",
        };

        private static BehaviourTree bossTree;
        private static BehaviourTreeRunner bossTreeRunner;
        private static BossBlackboard bossBlackboard;
        private static Animator bossAnimator;
        private static bool initializedCustomFsm = false;

        private static PhaseNode phaseNode;
        private static OneStepBranchNode branchNode; 

        private static RandomSelectorNode p1Selector;
        private static RandomSelectorNode p2Selector;

        private static ParticleSystem waveEffect;
        private static ParticleSystem.MinMaxGradient originalWaveColor;

        public static void Initialize(BehaviourTree tree, BehaviourTreeRunner runner, BossBlackboard blackboard, Animator animator)
        {
            bossTree = tree;
            bossTreeRunner = runner;
            bossAnimator = animator;
            bossBlackboard = blackboard;

            Registry.Clear();
            IndexTree(bossBlackboard, Registry, bossTree.rootNodeNode);

            if (!initializedCustomFsm)
                ModifyFsm();

            BindNodes(bossBlackboard);
            
            waveEffect = bossBlackboard.GFXObject.transform.Find("GFX/wave").GetComponent<ParticleSystem>();
            originalWaveColor = waveEffect.main.startColor;
        }

        private static void BindNodes(BossBlackboard blackBoard)
        {
            foreach (var node in Registry.Values)
            {
                BindNodeRecursive(blackBoard, node);
            }
        }

        private static void GetBranchNode()
        {
            if (bossTree.rootNodeNode is RootNode root && root.child is OneStepBranchNode osbn)
            {
                branchNode = osbn;
            }
            else
            {
                foreach (var node in Registry.Values)
                {
                    if (node is OneStepBranchNode foundBranch)
                    {
                        branchNode = foundBranch;
                        break;
                    }
                }
            }

            if (branchNode == null)
            {
                Plugin.Instance.Log("CRITICAL: OneStepBranchNode not found!", BepInEx.Logging.LogLevel.Error);
            }
        }

        private static void GetPhaseNode()
        {
            if (Registry.TryGetValue("PhaseNode", out var pNode))
            {
                phaseNode = pNode as PhaseNode;
            }
            else
            {
                phaseNode = branchNode.defaultBranch as PhaseNode;
            }
        }

        public static void ModifyFsm()
        {
            if (initializedCustomFsm) return;

            initializedCustomFsm = true;

            GetBranchNode();
            GetPhaseNode();

            CacheSpreads();
            InitializeCustomAttacks();
            RemoveGreggy();
        }

        public static bool CanCancelCurrentState(string trigger)
        {
            if (trigger != "IsHit" && trigger != "IsGroggy")
                return true;

            if (!bossBlackboard.CurrentNode)
                return false;

            var currentNode = bossBlackboard.CurrentNode;
            return !nonCancellableStates.Contains(currentNode.Label);
        }

        private static void InitializeCustomAttacks()
        {
            NodeBT originalP1Node = null;
            NodeBT originalP2Node = null;

            if (phaseNode.children.Count > 0)
            {
                originalP1Node = phaseNode.children[0];
            }

            if (phaseNode.children.Count > 1)
            {
                originalP2Node = phaseNode.children[1];
            }

            InitializeP1Selector(originalP1Node, originalP2Node);
            InitializeP2Selector(originalP2Node, originalP1Node);

            phaseNode.children.Clear();
            phaseNode.children.Add(p1Selector);
            phaseNode.children.Add(p2Selector);

            Plugin.Instance.Log($"P1 Selector has {p1Selector.children.Count} total attacks");
            Plugin.Instance.Log($"P2 Selector has {p2Selector.children.Count} total attacks");
        }

        private static void InitializeP1Selector(NodeBT originalP1Node, NodeBT originalP2Node)
        {
            p1Selector = ScriptableObject.CreateInstance<RandomSelectorNode>();
            p1Selector.Label = "P1";
            p1Selector.name = "P1";

            if (originalP1Node != null)
            {
                if (originalP1Node is CompositeNodeBt composite)
                {
                    foreach (var child in composite.children)
                    {
                        p1Selector.children.Add(child);
                    }
                }
                else
                {
                    p1Selector.children.Add(originalP1Node);
                }
            }
            if (originalP2Node != null)
            {
                if (originalP2Node is CompositeNodeBt composite)
                {
                    foreach (var child in composite.children)
                    {
                        p1Selector.children.Add(child);
                    }
                }
                else
                {
                    p1Selector.children.Add(originalP2Node);
                }
            }

            p1Selector.children.Add(CreateTeleportCombo());
            p1Selector.children.Add(CreateAimingCombo());
            p1Selector.children.Add(AddArenaBoundsCombo());
            BindNodeRecursive(bossBlackboard, p1Selector);
        }

        private static void InitializeP2Selector(NodeBT originalP2Node, NodeBT originalP1Node)
        {
            p2Selector = ScriptableObject.CreateInstance<RandomSelectorNode>();
            p2Selector.Label = "P2";
            p2Selector.name = "P2";

            if (originalP1Node != null)
            {
                if (originalP1Node is CompositeNodeBt composite)
                {
                    foreach (var child in composite.children)
                    {
                        p2Selector.children.Add(child);
                    }
                }
                else
                {
                    p2Selector.children.Add(originalP1Node);
                }
            }

            if (originalP2Node != null)
            {
                if (originalP2Node is CompositeNodeBt composite)
                {
                    foreach (var child in composite.children)
                    {
                        p2Selector.children.Add(child);
                    }
                }
                else
                {
                    p2Selector.children.Add(originalP2Node);
                }
            }

            p2Selector.children.Add(CreateTripleSpreadCombo());
            p2Selector.children.Add(CreateTripleShotgunCombo());
            p2Selector.children.Add(CreateTeleportCombo());
            p2Selector.children.Add(CreateAimingCombo());
            p2Selector.children.Add(AddArenaBoundsCombo());
            BindNodeRecursive(bossBlackboard, p2Selector);
        }
        
        public static void SetPhase2()
        {
            ForceNode(branchNode, bossBlackboard, Registry[Constants.P2_ENTER_STATE]);
        }

        private static void RemoveGreggy()
        {
            branchNode.branches.Remove("IsGroggy");
        }

        private static EliteChainShootState CreateTeleportCombo()
        {
            var node = ScriptableObject.CreateInstance<EliteChainShootState>();
            node.Label = "5 Step Tele Combo";
            node.name = "5 Step Tele Combo";
            node.DisableGravity = true;
            node.ReloadAtEnd = false;

            node.Steps.Add(CreateStep(
                false,
                new Vector2(-12f, -100f),
                EliteChainShootState.StepType.Ground,
                EliteChainShootState.TeleportPositionType.FromPlayer, 
                EliteAirShootState.ShootType.SameTime,
                false,
                6,
                () =>
                {
                    var profile = FmodSfxPool.Instance.SoundProfile.SwordMan;
                    var listener = GameManager.Instance.PlayerStateMachine.transform;
                    FmodSfxPool.Instance.PlaySound("Attack", listener, profile);

                    var main = waveEffect.main;
                    main.startColor = Color.red;

                    waveEffect.gameObject.SetActive(false);
                    waveEffect.gameObject.SetActive(true);
                }));
            node.Steps.Add(CreateStep(
                false,
                new Vector2(10f, 15f),
                EliteChainShootState.StepType.Aim,
                EliteChainShootState.TeleportPositionType.FromPlayer));
            node.Steps.Add(CreateStep(
                true,
                new Vector2(15f, -100f),
                EliteChainShootState.StepType.Ground,
                EliteChainShootState.TeleportPositionType.FromPlayer,
                EliteAirShootState.ShootType.SameTime,
                false,
                6,
                () =>   
                {
                    waveEffect.gameObject.SetActive(false);
                    var main = waveEffect.main;
                    main.startColor = originalWaveColor;
                }));
            node.Steps.Add(CreateStep(
                true,
                new Vector2(-10f, 15f),
                EliteChainShootState.StepType.Aim,
                EliteChainShootState.TeleportPositionType.FromPlayer,
                EliteAirShootState.ShootType.SameTime,
                false));
            node.Steps.Add(CreateStep(
                true,
                Constants.ARENA_TOP_POS,
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Upper,
                false));

            BindNodeRecursive(bossBlackboard, node);
            Registry.TryAdd(node.Label, node);
            return node;
        }

        private static EliteChainShootState CreateAimingCombo()
        {
            var node = ScriptableObject.CreateInstance<EliteChainShootState>();
            node.Label = "Aim Combo";
            node.name = "Aim Combo";
            node.DisableGravity = true;
            node.ReloadAtEnd = false;
            
            node.Steps.Add(CreateStep(
                true,
                new Vector2(-7f, 15f),
                EliteChainShootState.StepType.Aim,
                EliteChainShootState.TeleportPositionType.FromPlayer,
                EliteAirShootState.ShootType.SameTime,
                true,
                12,
                () =>
                {
                    bossAnimator.speed = 1.25f;
                }
            ));

            node.Steps.Add(CreateStep(
                true,
                new Vector2(7f, 15f),
                EliteChainShootState.StepType.Aim,
                EliteChainShootState.TeleportPositionType.FromPlayer,
                EliteAirShootState.ShootType.SameTime,
                true,
                12,
                () =>
                {
                    bossAnimator.speed = 1.5f;
                }
            ));

            BindNodeRecursive(bossBlackboard, node);
            Registry.TryAdd(node.Label, node);
            return node;
        }
        
        private static EliteChainShootState AddArenaBoundsCombo()
        {
            var node = ScriptableObject.CreateInstance<EliteChainShootState>();
            node.Label = "Arena Bounds Combo";
            node.name = "Arena Bounds Combo";
            node.DisableGravity = true;
            node.ReloadAtEnd = false;

            node.Steps.Add(CreateStep(
                false,
                new Vector2(Constants.ARENA_X_MIN_POS + 2f, Constants.ARENA_BOTTOM_POS_Y),
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Side,
                false, 12, () =>
                {
                    var main = waveEffect.main;
                    main.startColor = Color.green;
                    waveEffect.gameObject.SetActive(false);
                    waveEffect.gameObject.SetActive(true);
                    bossAnimator.speed = 1.75f;
                }
            ));
            node.Steps.Add(CreateStep(
                false,
                new Vector2(Constants.ARENA_X_MIN_POS + 2f, Constants.ARENA_TOP_POS_Y - 2f),
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Side,
                false, 12, null, -50f
            ));
            node.Steps.Add(CreateStep(
                true,
                new Vector2(Constants.ARENA_X_MAX_POS - 2f, Constants.ARENA_TOP_POS_Y - 2f),
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Side,
                false, 12, null, 50f
            ));
            node.Steps.Add(CreateStep(
                true,
                new Vector2(Constants.ARENA_X_MAX_POS - 2f, Constants.ARENA_BOTTOM_POS_Y - 2f),
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Side,
                false, 12, () =>
                {
                    waveEffect.gameObject.SetActive(false);
                    var main = waveEffect.main;
                    main.startColor = originalWaveColor;
                    bossAnimator.speed = 1.5f;
                }
            ));

            BindNodeRecursive(bossBlackboard, node);
            Registry.TryAdd(node.Label, node);
            return node;
        }

        private static EliteChainShootState CreateTripleSpreadCombo()
        {
            var node = ScriptableObject.CreateInstance<EliteChainShootState>();
            node.Label = "Triple Spread Combo";
            node.name = "Triple Spread Combo";
            node.DisableGravity = true;
            node.ReloadAtEnd = false;

            node.Steps.Add(CreateStep(
                true,
                Constants.ARENA_TOP_POS, 
                EliteChainShootState.StepType.Air, 
                EliteChainShootState.TeleportPositionType.Absolute, 
                EliteAirShootState.ShootType.Upper));
            node.Steps.Add(CreateStep(
                false,
                Constants.ARENA_TOP_POS,
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Upper));
            node.Steps.Add(CreateStep(
                true,
                Constants.ARENA_TOP_POS,
                EliteChainShootState.StepType.Air,
                EliteChainShootState.TeleportPositionType.Absolute,
                EliteAirShootState.ShootType.Upper));

            BindNodeRecursive(bossBlackboard, node);
            Registry.TryAdd(node.Label, node);
            return node;
        }

        private static EliteChainShootState CreateTripleShotgunCombo()
        {
            var node = ScriptableObject.CreateInstance<EliteChainShootState>();
            node.Label = "Triple Shotgun Combo";
            node.name = "Triple Shotgun Combo";
            node.DisableGravity = true;
            node.ReloadAtEnd = false;

            node.Steps.Add(CreateStep(false, Constants.ARENA_TOP_POS));
            node.Steps.Add(CreateStep(false, Constants.ARENA_TOP_POS));
            node.Steps.Add(CreateStep(false, Constants.ARENA_TOP_POS));

            BindNodeRecursive(bossBlackboard, node);
            Registry.TryAdd(node.Label, node);
            return node;
        }

        private static EliteChainShootState.ChainStep CreateStep(
            bool flip,
            Vector2 teleportPos,
            EliteChainShootState.StepType stepType = EliteChainShootState.StepType.Air,
            EliteChainShootState.TeleportPositionType type = EliteChainShootState.TeleportPositionType.Absolute,
            EliteAirShootState.ShootType shootType = EliteAirShootState.ShootType.SameTime,
            bool skipTeleport = true,
            int groundAttackFrame = 12,
            Action onStepEnter = null,
            float overrideRotation = 0f) 
        {
            return new EliteChainShootState.ChainStep
            {
                Type = stepType,
                Position = teleportPos,
                TeleportPositionType = type,
                AirShootType = shootType,
                AirAttackInfo = spreads[shootType],
                Flip = flip,
                SkipTeleport = skipTeleport,
                GroundShootFrame = groundAttackFrame,
                OnStepEnter = onStepEnter,
                OverrideRotation = overrideRotation
            };
        }

        public static void ForceDebugNode()
        {
            ForceNode(branchNode, bossBlackboard, Registry[Constants.DEBUG_NODE_NAME]);
        }

        private static void CacheSpreads()
        {
            foreach (var state in Registry.Values)
            {
                if (state is EliteAirShootState airShoot)
                {
                    if (airShoot.AttackInfo == null ||
                        airShoot.AttackInfo.AttackFrameList == null ||
                        airShoot.AttackInfo.AttackFrameList.Count == 0 ||
                        spreads.ContainsKey(airShoot.shootType))
                        continue;

                    spreads[airShoot.shootType] = airShoot.AttackInfo;
                }
            }
        }
    }
}