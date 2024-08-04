using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public static class GameConst
    {
        /// <summary>
        /// 场地高度
        /// </summary>
        public static int BackgroundHeight => Config.backgroundHeight;
        /// <summary>
        /// 场地宽度
        /// </summary>
        public static int BackgroundWidth => Config.backgroundWidth;
        /// <summary>
        /// 方块场地高度
        /// </summary>
        public static int TetrisGroundHeight => Config.tetrisGroundHeight;
        /// <summary>
        /// 方块场地宽度
        /// </summary>
        public static int TetrisGroundWidth => Config.tetrisGroundWidth;

        public static float UpdateSeconds = 1;

        public static int BlockSize => Config.blockSize;

        public static float KeyCodeUpdateTime => Config.keyCodeUpdateTime;

        public static GameConstConfig Config;
    }

    [Serializable]
    public class GameConstConfig
    {
        public int backgroundHeight = 30;
        public int backgroundWidth = 40;
        public int tetrisGroundHeight = 10;
        public int tetrisGroundWidth = 20;
        public int blockSize = 20;
        public float keyCodeUpdateTime = 0.3f;
    }

    public enum TetrisState
    {
        Rotate0 = 0,
        Rotate90 = 1,
        Rotate180 = 2,
        Rotate270 = 3
    }

    public static class BlockState
    {
        public const int Null = 0;
        public const int SoftBlock = 1;
        public const int Block = 2;
    }

    public enum MoveBlockType
    {
        T = 1,
        L = 2,
        LR = 3,
        I = 4,
        S = 5,
        Z = 6,
        ZR = 7
    }
}
