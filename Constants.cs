using UnityEngine;

namespace KamuraPrime
{
    public static class Constants
    {
        public const string BOSS_NAME = "Marshall Kamura";

        public const string BOSS_KEY = "KamuraPrime";
        public const string BOSS_CHALLENGE_TEXT = "Marshall Challenge";

        public const string P2_ENTER_STATE = "Triple Spread Combo";
        public const string DEBUG_NODE_NAME = "Triple Shotgun Combo";

        public const float MAX_HP = 5500f;
        public static readonly float[] PHASE_THRESHOLDS = [.51f, 0f];
        public static readonly float[] WEAK_POINT_RANGES = [1f, .85f, .7f];

        public static readonly float[] ORIGINAL_DAMAGES = [0f, 125f, 250f, 500f, 0f];
        public static readonly float[] KAMURA_DAMAGES = [0f, 65f, 115f, 215f, 0f];

        public static readonly Vector2 ARENA_TOP_POS = new Vector2(2420f, -268f);

        public const float ARENA_TOP_POS_Y = -268f;
        public const float ARENA_BOTTOM_POS_Y = -286f;
        
        public const float ARENA_X_MAX_POS = 2438f;
        public const float ARENA_X_MIN_POS = 2401f;

        public const int BULLET_PRELOAD_COUNT = 200;
        public const float BULLET_HOME_STRENGTH = 500f;
        public const string STICKY_BOMB_PROJECTILE_NAME = "StickyBomb";
        public const string BULLET_PROJECTILE_NAME = "Bullet";
    }
}