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
        public const int BackgroundHeight = 30;
        /// <summary>
        /// 场地宽度
        /// </summary>
        public const int BackgroundWidth = 40;
        /// <summary>
        /// 方块场地高度
        /// </summary>
        public const int TetrisGroundHeight = 10;
        /// <summary>
        /// 方块场地宽度
        /// </summary>
        public const int TetrisGroundWidth = 20;

        public const float UpdateSeconds = 1;

        public const int BlockSize = 20;

        public const float KeyCodeUpdateTime = 0.3f;
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
