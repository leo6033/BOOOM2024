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

        private BTetrisMoveBlock _currentMoveBlock;

        public BTetrisMoveBlock CurrentMoveBlock => _currentMoveBlock;
        
        public Dictionary<MoveBlockType, BTetrisMoveBlock> blockDict = new Dictionary<MoveBlockType, BTetrisMoveBlock>()
        {
            { MoveBlockType.T , new TTetrisMoveBlock()},
            { MoveBlockType.L , new LTetrisMoveBlock()},
            { MoveBlockType.LR , new LRTetrisMoveBlock()},
            { MoveBlockType.S , new STetrisMoveBlock()},
            { MoveBlockType.I , new ITetrisMoveBlock()},
            { MoveBlockType.Z , new ZTetrisMoveBlock()},
            { MoveBlockType.ZR , new ZRTetrisMoveBlock()}
        };

        public Dictionary<TetrisState, Func<int, int, Vector2Int>> tetrisToAreaPosFuncs = new Dictionary<TetrisState, Func<int, int, Vector2Int>>()
        {
            { TetrisState.Rotate0 , (x, y) => new Vector2Int(x - GameConst.TetrisGroundWidth / 2, y - GameConst.TetrisGroundHeight / 2)},
            { TetrisState.Rotate90 , (x, y) => new Vector2Int(GameConst.TetrisGroundHeight / 2 - y - 1,  x - GameConst.TetrisGroundWidth / 2)},
            { TetrisState.Rotate180 , (x, y) => new Vector2Int(GameConst.TetrisGroundWidth / 2 - x - 1, GameConst.TetrisGroundHeight / 2 - y - 1)},
            { TetrisState.Rotate270 , (x, y) => new Vector2Int(y - GameConst.TetrisGroundHeight / 2, GameConst.TetrisGroundWidth / 2 - x - 1)}
        };

        public Dictionary<TetrisState, Func<int, int, Vector2Int, Vector2Int>> areaPosToTetrisPosFuncs =
            new Dictionary<TetrisState, Func<int, int, Vector2Int, Vector2Int>>()
            {
                {
                    TetrisState.Rotate0,
                    (x, y, center) => new Vector2Int(x - center.x + GameConst.TetrisGroundWidth / 2,
                        y - center.y + GameConst.TetrisGroundHeight / 2)
                },
                {
                    TetrisState.Rotate90,
                    (x, y, center) => new Vector2Int(y - center.y + GameConst.TetrisGroundWidth / 2,
                        GameConst.TetrisGroundHeight / 2 - 1 - x + center.x)
                },
                {
                    TetrisState.Rotate180,
                    (x, y, center) => new Vector2Int(GameConst.TetrisGroundWidth / 2 - x - 1 + center.x,
                        GameConst.TetrisGroundHeight / 2 - 1 - y + center.y)
                },
                {
                    TetrisState.Rotate270,
                    (x, y, center) => new Vector2Int(GameConst.TetrisGroundWidth / 2 - 1 - y + center.y,
                        x + GameConst.TetrisGroundHeight / 2 - center.x)
                }
            };

        public Tetris()
        {
            _area = new int[GameConst.BackgroundWidth, GameConst.BackgroundHeight];
            _tetrisArea = new int[GameConst.TetrisGroundWidth, GameConst.TetrisGroundHeight];
            _tetrisCenter = new Vector2Int(GameConst.BackgroundWidth / 2, GameConst.BackgroundHeight / 2);
            _rotateState = TetrisState.Rotate0;

            UpdateAllBlockValue();
        }

        /// <summary>
        /// 更新场地数据
        /// </summary>
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
                    _area[pos.x, pos.y] = _tetrisArea[i, j];
                }
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Tick()
        {
            TickMoveBlock();
            UpdateAllBlockValue();
        }

        public void TickMoveBlock()
        {
            if (_currentMoveBlock == null)
            {
                CreateNewMoveBlock();
                return;
            }
            
            var targetPos = _currentMoveBlock.pos + Vector2Int.down;

            var board = GetTetrisAreaBoard();
            
            for (int i = 0; i < _currentMoveBlock.Width; i++)
            {
                for (int j = 0; j < _currentMoveBlock.Height; j++)
                {
                    var posX = targetPos.x + i - _currentMoveBlock.Width / 2;
                    var posY = targetPos.y + j;

                    if (_area[posX, posY] == BlockState.SoftBlock || posY < board.y1)
                    {
                        MoveBlockFinish(BlockState.SoftBlock);
                        return;
                    }
                    else if (_area[posX, posY] == BlockState.Block || posY < 0)
                    {
                        MoveBlockFinish(BlockState.Block);
                        return;
                    }
                    else if (posY == board.y2)
                    {
                        return;
                    }
                }
            }

            _currentMoveBlock.pos = targetPos;
        }

        private void MoveBlockFinish(int value)
        {
            // 落在移动场地内
            if (value == BlockState.SoftBlock)
            {
                var func = areaPosToTetrisPosFuncs[_rotateState];
                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    for (int j = 0; j < _currentMoveBlock.Height; j++)
                    {
                        var posX = _currentMoveBlock.pos.x + i - _currentMoveBlock.Width / 2;
                        var posY = _currentMoveBlock.pos.y + j;

                        var pos = func(posX, posY, _tetrisCenter);
                        
                        Debug.Log($"[MoveBlockFinish] {pos}");
                        _tetrisArea[pos.x, pos.y] = value;
                    }
                }
                
            }
            // 落在总场地
            else
            {
                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    for (int j = 0; j < _currentMoveBlock.Height; j++)
                    {
                        var posX = _currentMoveBlock.pos.x + i - _currentMoveBlock.Width / 2 - _tetrisCenter.x + GameConst.TetrisGroundWidth / 2;
                        var posY = _currentMoveBlock.pos.y + j - _tetrisCenter.y + GameConst.TetrisGroundHeight / 2;

                        _area[posX, posY] = value;
                    }
                }
            }

            _currentMoveBlock = null;
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

        public void CreateNewMoveBlock()
        {
            _currentMoveBlock = new ITetrisMoveBlock();

            if (_rotateState == TetrisState.Rotate0)
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(0, GameConst.TetrisGroundHeight / 2 - 1);
            }
            else if (_rotateState == TetrisState.Rotate90)
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(-GameConst.TetrisGroundHeight / 2, 0);
            }
            else if (_rotateState == TetrisState.Rotate180)
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(0, -GameConst.TetrisGroundHeight / 2);
            }
            else
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(GameConst.TetrisGroundHeight / 2 - 1, 0);
            }

            AdjustMoveBlockPosition();
        }

        public void AdjustMoveBlockPosition()
        {
            

            var moveDis = 0;
            if (_rotateState == TetrisState.Rotate0 || _rotateState == TetrisState.Rotate180)
            {
                for (int i = 0; i < _currentMoveBlock.Width ; i++)
                {
                    var tmp = 0;
                    for (int j = 0; j < _currentMoveBlock.Height; j++)
                    {
                        var posX = _currentMoveBlock.pos.x + i - _currentMoveBlock.Width / 2;
                        var posY = _currentMoveBlock.pos.y + j;

                        if (_area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }

                    moveDis = math.max(moveDis, tmp);
                }
                
            }
            else
            {
                for (int i = 0; i < _currentMoveBlock.Height; i++)
                {
                    var tmp = 0;
                    for (int j = 0; j < _currentMoveBlock.Width; j++)
                    {
                        var posX = _currentMoveBlock.pos.x + j - _currentMoveBlock.Width / 2;
                        var posY = _currentMoveBlock.pos.y + i;
                        
                        if (_area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }
                    moveDis = math.max(moveDis, tmp);
                }
            }
        }

        public void DoAdjustMoveBlock(int move)
        {
            
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