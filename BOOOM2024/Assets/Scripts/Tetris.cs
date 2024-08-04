using System;
using System.Collections.Generic;
using Engine.SettingModule;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public bool gameFinish { get; private set; }
        public TetrisState RotateState => _rotateState;

        private int[,] _area;
        private int[,] _tetrisArea;
        private TetrisState _rotateState;

        private Vector2Int _tetrisCenter;

        private BTetrisMoveBlock _currentMoveBlock;

        private List<float> _weight;
        
        public bool inTick { get; private set; }

        private Dictionary<int, Type> _moveBlocks = new Dictionary<int, Type>()
        {
            {(int)MoveBlockType.I, typeof(ITetrisMoveBlock)},
            {(int)MoveBlockType.L, typeof(LTetrisMoveBlock)},
            {(int)MoveBlockType.LR, typeof(LRTetrisMoveBlock)},
            {(int)MoveBlockType.T, typeof(TTetrisMoveBlock)},
            {(int)MoveBlockType.S, typeof(STetrisMoveBlock)},
            {(int)MoveBlockType.Z, typeof(ZTetrisMoveBlock)},
            {(int)MoveBlockType.ZR, typeof(ZRTetrisMoveBlock)}
        };

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
            gameFinish = false;

            var table = TableModule.Get("TetrisCube");
            _weight = new List<float>();
            int count = 0;
            inTick = false;

            for (int i = 1; i <= table.csvData.Count; i++)
            {
                var weight = (int)table.GetData((uint)i, "Weight");
                count += weight;
                _weight.Add(count);
            }

            for (int i = 0; i < _weight.Count; i++)
            {
                _weight[i] /= count;
            }

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
            inTick = true;
            TickMoveBlock();
            UpdateAllBlockValue();
            inTick = false;
        }

        public void SolidMoveBlock()
        {
            while (_currentMoveBlock != null && !gameFinish)
            {
                TickMoveBlock();
            }
            UpdateAllBlockValue();
        }

        private void TickMoveBlock()
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

                    if (_currentMoveBlock.Block[i, j] != 0)
                    {
                        if (posY < 0 || _area[posX, posY] == BlockState.Block)
                        {
                            MoveBlockFinish(BlockState.Block);
                            return;
                        }
                        
                        if ((posY < board.y1 && _rotateState != TetrisState.Rotate180 && posX >= board.x1 && posX < board.x2) || _area[posX, posY] == BlockState.SoftBlock)
                        {
                            MoveBlockFinish(BlockState.SoftBlock);
                            return;
                        }
                        
                        if (posY == board.y2 - 1 && _rotateState != TetrisState.Rotate0)
                        {
                            return;
                        }
                    }
                }
            }

            _currentMoveBlock.pos = targetPos;
        }

        private void MoveBlockFinish(int value)
        {
            var startY = GameConst.BackgroundHeight;
            var endY = 0;
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
                        if (pos.x < 0 || pos.x >= GameConst.TetrisGroundWidth || pos.y < 0 ||
                            pos.y >= GameConst.TetrisGroundHeight)
                        {
                            gameFinish = true;
                            return;
                        }
                        
                        if(_currentMoveBlock.Block[i, j] != 0)
                            _tetrisArea[pos.x, pos.y] = value;

                        startY = math.min(pos.y, startY);
                        endY = math.max(pos.y, endY);
                    }
                }

                
                for (int j = startY; j <= endY; j++)
                {
                    var count = 0;
                    for (int i = 0; i < GameConst.TetrisGroundWidth; i++)
                    {
                        if (_tetrisArea[i, j] == BlockState.SoftBlock)
                        {
                            count++;
                        }
                    }

                    if (count == GameConst.TetrisGroundWidth)
                    {
                        for (int i = 0; i < GameConst.TetrisGroundWidth; i++)
                        {
                            _tetrisArea[i, j] = BlockState.Null;
                        }
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

                        if(_currentMoveBlock.Block[i, j] != 0)
                            _area[posX, posY] = value;
                        
                        startY = math.min(posY, startY);
                        endY = math.max(posY, endY);
                    }
                }
                
                for (int j = startY; j <= endY; j++)
                {
                    var count = 0;
                    for (int i = 0; i < GameConst.BackgroundWidth; i++)
                    {
                        if (_area[i, j] == BlockState.Block)
                        {
                            count++;
                        }
                    }

                    if (count == GameConst.BackgroundWidth)
                    {
                        for (int i = 0; i < GameConst.BackgroundWidth; i++)
                        {
                            _area[i, j] = BlockState.Null;
                        }
                    }
                }
            }

            _currentMoveBlock = null;
        }

        public TetrisBoard GetTetrisAreaBoard()
        {
            return GetTargetRotateTetrisAreaBoard(_rotateState);
        }

        public TetrisBoard GetMoveBlockBoard()
        {
            return new TetrisBoard()
            {
                x1 = _currentMoveBlock.pos.x - _currentMoveBlock.Width / 2,
                x2 = _currentMoveBlock.pos.x + _currentMoveBlock.Width / 2,
                y1 = _currentMoveBlock.pos.y,
                y2 = _currentMoveBlock.pos.y + _currentMoveBlock.Height
            };
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

        private void CreateNewMoveBlock()
        {
            var ran = Random.value;
            for (int i = 0; i < _weight.Count; i++)
            {
                if (ran <= _weight[i])
                {
                    _currentMoveBlock = Activator.CreateInstance(_moveBlocks[i + 1]) as BTetrisMoveBlock;
                    break;
                }
            }

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
            
            UpdateAllBlockValue();
        }

        private bool AdjustMoveBlockPosition()
        {
            if (_currentMoveBlock == null)
                return true;
            
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

                        if (posY >= GameConst.BackgroundHeight || posY < 0)
                        {
                            tmp++;
                        }
                        else if (_currentMoveBlock.Block[i, j] != 0 && _area[posX, posY] != BlockState.Null)
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
                        
                        if (posX >= GameConst.BackgroundWidth || posX < 0)
                        {
                            tmp++;
                        }
                        else if (_currentMoveBlock.Block[j, i] != 0 &&_area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }
                    moveDis = math.max(moveDis, tmp);
                }
            }

            return DoAdjustMoveBlock(moveDis);
        }

        private bool DoAdjustMoveBlock(int move)
        {
            if (move == 0)
                return true;
            
            var backDis = 0;
            
            if (_rotateState == TetrisState.Rotate0)
            {
                var targetPos = _currentMoveBlock.pos + new Vector2Int(0, move);

                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    var tmp = 0;
                    for (int j = 0; j < _currentMoveBlock.Height; j++)
                    {
                        var posX = targetPos.x + i - _currentMoveBlock.Width / 2;
                        var posY = targetPos.y + j;

                        if (posY >= GameConst.BackgroundHeight)
                        {
                            tmp++;
                        }
                        else if (_currentMoveBlock.Block[i, j] != 0 && _area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }

                    backDis = math.max(backDis, tmp);
                }

                if (MoveTetrisArea(0, -backDis, false))
                {
                    _currentMoveBlock.pos = targetPos;
                    return true;
                }
            }
            else if(_rotateState == TetrisState.Rotate90)
            {
                var targetPos = _currentMoveBlock.pos - new Vector2Int(move, 0);

                for (int j = 0; j < _currentMoveBlock.Height; j++)
                {
                    var tmp = 0;
                    for (int i = 0; i < _currentMoveBlock.Width; i++)
                    {
                        var posX = targetPos.x + i - _currentMoveBlock.Width / 2;
                        var posY = targetPos.y + j;

                        if (posX < 0)
                        {
                            tmp++;
                        }
                        else if (_currentMoveBlock.Block[i, j] != 0 && _area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }

                    backDis = math.max(backDis, tmp);
                }

                if (MoveTetrisArea(backDis, 0, false))
                {
                    _currentMoveBlock.pos = targetPos;
                    return true;
                }
            }
            else if (_rotateState == TetrisState.Rotate180)
            {
                var targetPos = _currentMoveBlock.pos - new Vector2Int(0, move);

                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    var tmp = 0;
                    for (int j = 0; j < _currentMoveBlock.Height; j++)
                    {
                        var posX = targetPos.x + i - _currentMoveBlock.Width / 2;
                        var posY = targetPos.y + j;

                        if (posY < 0)
                        {
                            tmp++;
                        }
                        else if (_currentMoveBlock.Block[i, j] != 0 && _area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }

                    backDis = math.max(backDis, tmp);
                }

                if (MoveTetrisArea(0, backDis, false))
                {
                    _currentMoveBlock.pos = targetPos;
                    return true;
                }
            }
            else
            {
                var targetPos = _currentMoveBlock.pos + new Vector2Int(move, 0);

                for (int j = 0; j < _currentMoveBlock.Height; j++)
                {
                    var tmp = 0;
                    for (int i = 0; i < _currentMoveBlock.Width; i++)
                    {
                        var posX = targetPos.x + i - _currentMoveBlock.Width / 2;
                        var posY = targetPos.y + j;

                        if (posX >= GameConst.BackgroundWidth)
                        {
                            tmp++;
                        }
                        else if (_currentMoveBlock.Block[i, j] != 0 && _area[posX, posY] != BlockState.Null)
                        {
                            tmp++;
                        }
                    }

                    backDis = math.max(backDis, tmp);
                }

                if (MoveTetrisArea(-backDis, 0, false))
                {
                    _currentMoveBlock.pos = targetPos;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 方块场地移动
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="checkMoveBlock"></param>
        /// <returns></returns>
        public bool MoveTetrisArea(int x, int y, bool checkMoveBlock = true)
        {
            var board = GetTetrisAreaBoard();

            board.x1 += x;
            board.x2 += x;
            board.y1 += y;
            board.y2 += y;

            if (board.x1 >= 0 && board.x2 <= GameConst.BackgroundWidth && board.y1 >= 0 && board.y2 <= GameConst.BackgroundHeight)
            {
                // 检查是否有垃圾方块
                for(int i = board.x1; i < board.x2; i ++)
                {
                    for (int j = board.y1; j < board.y2; j++)
                    {
                        if (_area[i, j] == BlockState.Block)
                        {
                            return false;
                        }
                    }
                }
                _tetrisCenter = _tetrisCenter + new Vector2Int(x, y);
                UpdateAllBlockValue();

                // 检查是否需要推动移动方块
                if (_currentMoveBlock != null && checkMoveBlock)
                {
                    for (int i = 0; i < _currentMoveBlock.Width; i++)
                    {
                        for (int j = 0; j < _currentMoveBlock.Height; j++)
                        {
                            var posX = i + _currentMoveBlock.pos.x - _currentMoveBlock.Width / 2;
                            var posY = j + _currentMoveBlock.pos.y;

                            if (_currentMoveBlock.Block[i, j] != 0)
                            {
                                if (_area[posX, posY] == BlockState.SoftBlock ||
                                    (y < 0 && posY == board.y1 && _rotateState != TetrisState.Rotate180 &&
                                     posX >= board.x1 && posX < board.x2) ||
                                    (y < 0 && posY == board.y2 && _rotateState != TetrisState.Rotate0 &&
                                     posX >= board.x1 && posX < board.x2) ||
                                    (y > 0 && posY == board.y1 - 1 && _rotateState != TetrisState.Rotate180 &&
                                     posX >= board.x1 && posX < board.x2) ||
                                    (y > 0 && posY == board.y2 - 1 && _rotateState != TetrisState.Rotate0 &&
                                     posX >= board.x1 && posX < board.x2) ||
                                    (x > 0 && posX == board.x1 - 1 && _rotateState != TetrisState.Rotate90 &&
                                     posY >= board.y1 && posY < board.y2) ||
                                    (x > 0 && posX == board.x2 - 1 && _rotateState != TetrisState.Rotate270 &&
                                     posY >= board.y1 && posY < board.y2) ||
                                    (x < 0 && posX == board.x1 && _rotateState != TetrisState.Rotate90 &&
                                     posY >= board.y1 && posY < board.y2) ||
                                    (x < 0 && posX == board.x2 && _rotateState != TetrisState.Rotate270 &&
                                     posY >= board.y1 && posY < board.y2))
                                {
                                    var targetPos = _currentMoveBlock.pos + new Vector2Int(x, y);

                                    var moveBlockBoard = GetMoveBlockBoard();
                                    moveBlockBoard.x1 += x;
                                    moveBlockBoard.x2 += x;
                                    moveBlockBoard.y1 += y;
                                    moveBlockBoard.y2 += y;

                                    if (!moveBlockBoard.Valid())
                                    {
                                        MoveTetrisArea(-x, -y, false);
                                        return false;
                                    }

                                    for (int i1 = 0; i1 < _currentMoveBlock.Width; i1++)
                                    {
                                        for (int j1 = 0; j1 < _currentMoveBlock.Height; j1++)
                                        {
                                            var posX1 = i1 + _currentMoveBlock.pos.x - _currentMoveBlock.Width / 2 + x;
                                            var posY1 = j1 + _currentMoveBlock.pos.y + y;

                                            if (_area[posX1, posY1] == BlockState.Block)
                                            {
                                                MoveTetrisArea(-x, -y, false);
                                                return false;
                                            }
                                        }
                                    }

                                    _currentMoveBlock.pos = targetPos;
                                    return true;
                                }
                            }
                        }
                    }
                }
                    
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
            var valid = AdjustMoveBlockPosition();
            if (!valid)
            {
                _rotateState = oriRotate;
            }
            
            UpdateAllBlockValue();
            
            return valid;
        }
        
    }
}