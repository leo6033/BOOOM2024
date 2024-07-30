using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Gameplay
{
    public struct TetrisBoard
    {
        public int x1;
        public int x2;
        public int y1;
        public int y2;

        public bool Valid()
        {
            return x1 >= 0 && x2 < GameConst.BackgroundWidth && y1 >= 0 && y2 < GameConst.BackgroundHeight;
        }
    }
    
    public class Tetris
    {
        public int this[int x, int y] => _area[x, y]; 

        private int[,] _area;
        private int[,] _tetrisArea;
        private TetrisState _rotateState;

        private Vector2Int _tetrisCenter;

        public Dictionary<TetrisState, Func<int, int, Vector2Int>> tetrisToAreaPosFuncs = new Dictionary<TetrisState, Func<int, int, Vector2Int>>()
        {
            { TetrisState.Rotate0 , (x, y) => new Vector2Int(x - GameConst.TetrisGroundWidth / 2, y - GameConst.TetrisGroundHeight / 2)},
            { TetrisState.Rotate90 , (x, y) => new Vector2Int(GameConst.TetrisGroundHeight / 2 - y - 1,  x - GameConst.TetrisGroundWidth / 2)},
            { TetrisState.Rotate180 , (x, y) => new Vector2Int(GameConst.TetrisGroundWidth / 2 - x - 1, GameConst.TetrisGroundHeight / 2 - y - 1)},
            { TetrisState.Rotate270 , (x, y) => new Vector2Int(y - GameConst.TetrisGroundHeight / 2, GameConst.TetrisGroundWidth / 2 - x - 1)}
        };

        public Tetris()
        {
            _area = new int[GameConst.BackgroundWidth, GameConst.BackgroundHeight];
            _tetrisArea = new int[GameConst.TetrisGroundWidth, GameConst.TetrisGroundHeight];
            _tetrisCenter = new Vector2Int(GameConst.BackgroundWidth / 2, GameConst.BackgroundHeight / 2);
            _rotateState = TetrisState.Rotate0;

            UpdateAllBlockValue();
        }

        public void UpdateAllBlockValue()
        {
            Debug.Log($"current rotate {_rotateState}");
            for (int i = 0; i < GameConst.BackgroundWidth; i++)
            {
                for (int j = 0; j < GameConst.BackgroundHeight; j++)
                {
                    if (_area[i, j] != BlockState.Block)
                        _area[i, j] = BlockState.Null;
                }
            }

            var action = tetrisToAreaPosFuncs[_rotateState];

            for (int i = 0; i < GameConst.TetrisGroundWidth; i++)
            {
                for (int j = 0; j < GameConst.TetrisGroundHeight; j++)
                {
                    var pos = action(i, j) + _tetrisCenter;
                    Debug.Log(pos);
                    _area[pos.x, pos.y] = _tetrisArea[i, j];
                }
            }
        }

        public TetrisBoard GetTetrisAreaBoard()
        {
            return GetTargetRotateTetrisAreaBoard(_rotateState);
        }

        private TetrisBoard GetTargetRotateTetrisAreaBoard(TetrisState state)
        {
            if (state == TetrisState.Rotate0 || state == TetrisState.Rotate180)
            {
                return new TetrisBoard()
                {
                    x1 = _tetrisCenter.x - GameConst.TetrisGroundWidth / 2,
                    x2 = _tetrisCenter.x + GameConst.TetrisGroundWidth / 2,
                    y1 = _tetrisCenter.y - GameConst.TetrisGroundHeight / 2,
                    y2 = _tetrisCenter.y + GameConst.TetrisGroundHeight / 2
                };
            }
            else
            {
                return new TetrisBoard()
                {
                    x1 = _tetrisCenter.x - GameConst.TetrisGroundHeight / 2,
                    x2 = _tetrisCenter.x + GameConst.TetrisGroundHeight / 2,
                    y1 = _tetrisCenter.y - GameConst.TetrisGroundWidth / 2,
                    y2 = _tetrisCenter.y + GameConst.TetrisGroundWidth / 2
                };
            }
        }

        /// <summary>
        /// 方块场地移动
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool MoveTetrisArea(int x, int y)
        {
            var board = GetTetrisAreaBoard();

            board.x1 += x;
            board.x2 += x;
            board.y1 += y;
            board.y2 += y;

            if (board.x1 >= 0 && board.x2 <= GameConst.BackgroundWidth && board.y1 >= 0 && board.y2 <= GameConst.BackgroundHeight)
            {

                _tetrisCenter = _tetrisCenter + new Vector2Int(x, y);
                UpdateAllBlockValue();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 方块场地旋转
        /// </summary>
        /// <returns></returns>
        public bool RotateTetrisArea(int value)
        {
            var oriRotate = _rotateState;
            
            var targetRotateState = (TetrisState)(((int)_rotateState + 4 + value) % 4);

            var targetAreaBoard = GetTargetRotateTetrisAreaBoard(targetRotateState);

            if (!targetAreaBoard.Valid())
            {
                if (targetAreaBoard.y1 < 0)
                {
                    _tetrisCenter.y -= targetAreaBoard.y1;
                }
                else if (targetAreaBoard.y2 >= GameConst.BackgroundHeight)
                {
                    _tetrisCenter.y -= (targetAreaBoard.y2 - GameConst.BackgroundHeight + 1);
                }

                if (targetAreaBoard.x1 < 0)
                {
                    _tetrisCenter.x -= targetAreaBoard.x1;
                }
                else if (targetAreaBoard.x2 >= GameConst.BackgroundWidth)
                {
                    _tetrisCenter.x -= (targetAreaBoard.x2 - GameConst.BackgroundWidth + 1);
                }
            }

            _rotateState = targetRotateState;
            
            UpdateAllBlockValue();
            
            // check is valid
            var valid = true;
            if (!valid)
            {
                _rotateState = oriRotate;
                UpdateAllBlockValue();
            }
            
            return valid;
        }
    }
}