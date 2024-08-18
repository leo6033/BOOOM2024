using System;
using System.Collections.Generic;
using Engine.Runtime;
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

    public struct TetrisLevelInfo
    {
        public int level;
        public int teleportValue;
        public int teleportValueAdd;
        public int teleportValueDecrease;
        public float speedTime;
        public int score;
        public int scoreCost;
        public int bombSolidTime;

        public TetrisLevelInfo(int level)
        {
            var table = TableModule.Get("SpeedLevel");
            this.level = level;
            teleportValue = (int)table.GetData((uint)level, "TeleportValue");
            teleportValueAdd = (int)table.GetData((uint)level, "TeleportValueAdd");
            teleportValueDecrease = (int)table.GetData((uint)level, "TeleportValueDecrease");
            speedTime = (float)table.GetData((uint)level, "SpeedTime");
            score = (int)table.GetData((uint)level, "Score");
            scoreCost = (int)table.GetData((uint)level, "ScoreCost");
            bombSolidTime = (int)table.GetData((uint)level, "BombSolidTime");
        }
    }

    public struct BombInfo
    {
        public Vector2Int pos;
        public bool hasBomb;
        public int remainBlockNum;
    }
    
    public class Tetris
    {
        public int this[int x, int y] => _area[x, y]; 
        public bool GameFinish { get; private set; }
        public float Speed { get; private set; }
        public TetrisLevelInfo LevelInfo { get; private set; }

        private BombInfo _bombInfo;
        public BombInfo BombInfo => _bombInfo;

        public int SpeedLevel
        {
            get => _speedLevel;
            private set
            {
                _speedLevel = value;
                if (_speedLevel > GameConst.MaxSpeedLevel)
                    _speedLevel = GameConst.MaxSpeedLevel;
                else if(_speedLevel == 0)
                    _speedLevel = 1;

                LevelInfo = new TetrisLevelInfo(_speedLevel);
                Speed = 1f / LevelInfo.speedTime;
            }
        }

        public int Score { get; private set; }

        private int _teleportValue;

        public int TeleportValue
        {
            get => _teleportValue;
            private set
            {
                if (_teleportValue > value)
                {
                    if (SpeedLevel < GameConst.MaxSpeedLevel)
                    {
                        var levelInfo = new TetrisLevelInfo(SpeedLevel + 1);
                        if (_teleportValue >= levelInfo.teleportValue)
                        {
                            SpeedLevel++;
                        }
                    }
                }
                else if (_teleportValue < value)
                {
                    if (_teleportValue < LevelInfo.teleportValue)
                    {
                        SpeedLevel--;
                    }
                }
                _teleportValue = math.max(0, value);
            }
        }
        

        private int _speedLevel;
        public TetrisState RotateState => _rotateState;

        private int[,] _area;
        private int[,] _tetrisArea;
        private TetrisState _rotateState;

        public Vector2Int Center => _tetrisCenter;
        private Vector2Int _tetrisCenter;

        private BTetrisMoveBlock _currentMoveBlock;

        private List<float> _weight;

        public int BombNum => _bombNum;
        private int _bombNum;
        private bool _nextIsBomb;

        public bool NextIsBomb => _nextIsBomb;
        public int NextBlockId { get; private set; }

        public bool MoveBlockIsBlocked => _blockedTimes > 0;

        private float _blockedTimes = 0;
        
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
            GameFinish = false;
            
            _nextIsBomb = false;
            _bombNum = 0;
            _teleportValue = 0;
            _blockedTimes = 0;
            SpeedLevel = 1;
            Score = 0;
            _bombInfo = new BombInfo() { hasBomb = false, pos = Vector2Int.zero, remainBlockNum=LevelInfo.bombSolidTime };

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
            
            GenNextBlockId();
            UpdateAllBlockValue();
        }

        /// <summary>
        /// 更新场地数据
        /// </summary>
        public void UpdateAllBlockValue()
        {
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

            if (_bombInfo.hasBomb)
                _area[_bombInfo.pos.x, _bombInfo.pos.y] = BlockState.Bomb;
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Tick()
        {
            inTick = true;
            TickMoveBlock();

            // 创建炸弹
            if (_bombInfo.remainBlockNum <= 0 && _bombInfo.hasBomb == false && _bombNum < GameConst.BombLimit)
            {
                var tetrisBoard = GetTetrisAreaBoard();

                while (!_bombInfo.hasBomb)
                {
                    var x = Random.Range(tetrisBoard.x2 + GameConst.MinBombDistance,
                        tetrisBoard.x2 + GameConst.BackgroundWidth - GameConst.MinBombDistance) % GameConst.BackgroundWidth;
                    var y = Random.Range(tetrisBoard.y2 + GameConst.MinBombDistance,
                                tetrisBoard.y2 + GameConst.BackgroundHeight - GameConst.MinBombDistance) %
                            GameConst.BackgroundHeight;
                    if (_area[x, y] == BlockState.Null)
                    {
                        _bombInfo.pos = new Vector2Int(x, y);
                        _bombInfo.hasBomb = true;
                    }
                }
                
            }
            
            UpdateAllBlockValue();

            TeleportValue -= LevelInfo.teleportValueDecrease;
            inTick = false;
        }

        public void SolidMoveBlock()
        {
            while (_currentMoveBlock != null && !GameFinish)
            {
                TickMoveBlock();
            }
            UpdateAllBlockValue();

            TeleportValue += LevelInfo.teleportValueAdd;
        }

        private void TickMoveBlock()
        {
            if (_blockedTimes > GameConst.DestroyTimeCount)
            {
                _currentMoveBlock = null;
            }
            
            if (_currentMoveBlock == null)
            {
                _blockedTimes = 0;
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
                        if (_currentMoveBlock.pos.y == 0 || _area[posX, posY] == BlockState.Block)
                        {
                            MoveBlockFinish(BlockState.Block);
                            return;
                        }
                        
                        if (posY == board.y2 - 1 && _rotateState != TetrisState.Rotate0)
                        {
                            _blockedTimes += (1f  /LevelInfo.speedTime);
                            return;
                        }
                        
                        if ((_currentMoveBlock.pos.y == board.y1 && _rotateState != TetrisState.Rotate180 && posX >= board.x1 && posX < board.x2) || _area[posX, posY] == BlockState.SoftBlock)
                        {
                            MoveBlockFinish(BlockState.SoftBlock);
                            return;
                        }
                        

                    }
                }
            }

            _blockedTimes = 0;
            _currentMoveBlock.pos = targetPos;
        }

        private void MoveBlockFinish(int value)
        {
            var startY = GameConst.BackgroundHeight;
            var endY = 0;
            var startScore = Score;
            
            // 炸弹块
            if (_currentMoveBlock.BlockType == MoveBlockType.Bomb)
            {

                var func = areaPosToTetrisPosFuncs[_rotateState];
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        var posX = _currentMoveBlock.pos.x + i;
                        var posY = _currentMoveBlock.pos.y + j;
                    
                        var pos = func(posX, posY, _tetrisCenter);
                        if (pos.x >= 0 && pos.x < GameConst.TetrisGroundWidth && pos.y >= 0 &&
                            pos.y < GameConst.TetrisGroundHeight)
                        {
                            _tetrisArea[pos.x, pos.y] = 0;
                        }
                        else if (posX >= 0 && posX < GameConst.BackgroundWidth && posY >= 0 &&
                                 posY < GameConst.BackgroundHeight)
                        {
                            _area[posX, posY] = 0;
                        }
                    }
                }

                GameEvent.Send(EventType.BombBoom);
                SoundModule.Instance.PlayAudio(SoundId.BombEffect);
            }
            // 落在移动场地内
            else if (value == BlockState.SoftBlock)
            {
                var func = areaPosToTetrisPosFuncs[_rotateState];
                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    for (int j = 0; j < _currentMoveBlock.Height; j++)
                    {
                        var posX = _currentMoveBlock.pos.x + i - _currentMoveBlock.Width / 2;
                        var posY = _currentMoveBlock.pos.y + j;

                        var pos = func(posX, posY, _tetrisCenter);
                        
                        if (pos.x < 0 || pos.x >= GameConst.TetrisGroundWidth || pos.y < 0 ||
                            pos.y >= GameConst.TetrisGroundHeight)
                        {
                            GameFinish = true;
                            Debug.Log("Game Finish!!!");
                            return;
                        }

                        if (_currentMoveBlock.Block[i, j] != 0)
                        {
                            _tetrisArea[pos.x, pos.y] = value;
                            _bombInfo.remainBlockNum--;
                        }
                            

                        startY = math.min(pos.y, startY);
                        endY = math.max(pos.y, endY);
                    }
                }

                var clearCount = 0;
                for (int j = endY; j >= startY; j--)
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
                            var tmp = tetrisToAreaPosFuncs[_rotateState](i, j + clearCount) + _tetrisCenter;
                            GameEvent.Send(EventType.BlockDestroy, tmp.x, tmp.y);
                        }

                        for (int j1 = j + 1; j1 < GameConst.TetrisGroundHeight; j1++)
                        {
                            for (int i = 0; i < GameConst.TetrisGroundWidth; i++)
                            {
                                _tetrisArea[i, j1 - 1] = _tetrisArea[i, j1];
                                _tetrisArea[i, j1] = BlockState.Null;
                            }
                        }

                        Score += LevelInfo.score;
                        clearCount++;
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
                        var posX = _currentMoveBlock.pos.x + i - _currentMoveBlock.Width / 2;
                        var posY = _currentMoveBlock.pos.y + j;

                        if (_currentMoveBlock.Block[i, j] != 0)
                        {
                            _area[posX, posY] = value;
                            _bombInfo.remainBlockNum--;
                        }
                        
                        startY = math.min(posY, startY);
                        endY = math.max(posY, endY);
                    }
                }
                
                var clearCount = 0;
                for (int j = endY; j >= startY; j--)
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
                            GameEvent.Send(EventType.BlockDestroy, i, j);
                        }
                        
                        for (int j1 = j + 1; j1 < GameConst.TetrisGroundHeight; j1++)
                        {
                            for (int i = 0; i < GameConst.TetrisGroundWidth; i++)
                            {
                                _area[i, j1 - 1] = _area[i, j1];
                                _area[i, j1] = BlockState.Null;
                            }
                        }
                        
                        Score += LevelInfo.score;
                        clearCount++;
                    }
                }
            }

            if (Score > startScore)
            {
                SoundModule.Instance.PlayElimate();
            }
            else if (_currentMoveBlock.BlockType != MoveBlockType.Bomb)
            {
                SoundModule.Instance.PlayAudio(SoundId.BlockSolid);
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
            Debug.Log("CreateNewMoveBlock");
            
            if (_nextIsBomb)
            {
                _currentMoveBlock = Activator.CreateInstance<BombTetrisMoveBlock>();
                _nextIsBomb = false;
                _bombNum -= 1;
            }
            else
            {
                _currentMoveBlock = Activator.CreateInstance(_moveBlocks[NextBlockId + 1]) as BTetrisMoveBlock;
            }

            GenNextBlockId();
            
            if (_rotateState == TetrisState.Rotate0)
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(0, GameConst.TetrisGroundHeight / 2 - _currentMoveBlock.Height);
            }
            else if (_rotateState == TetrisState.Rotate90)
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(-GameConst.TetrisGroundHeight / 2 + _currentMoveBlock.Width / 2, GameConst.TetrisGroundWidth / 2 - _currentMoveBlock.Height);
            }
            else if (_rotateState == TetrisState.Rotate180)
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(0, -GameConst.TetrisGroundHeight / 2);
            }
            else
            {
                _currentMoveBlock.pos = _tetrisCenter + new Vector2Int(GameConst.TetrisGroundHeight / 2 - _currentMoveBlock.Width / 2 - 1, GameConst.TetrisGroundWidth / 2 - _currentMoveBlock.Height);
            }

            if (!AdjustMoveBlockPosition())
            {
                _currentMoveBlock = null;
            }
            
            UpdateAllBlockValue();
        }

        private bool AdjustMoveBlockPosition()
        {
            if (_currentMoveBlock == null)
                return true;

            var board = GetTetrisAreaBoard();
            var blockBoard = GetMoveBlockBoard();
            
            var moveDis = 0;
            var oriPos = _currentMoveBlock.pos;
            // 方块卡在场地边缘
            if (_rotateState == TetrisState.Rotate0)
            {
                if (blockBoard.y1 < board.y1 && blockBoard.y2 >= board.y1)
                {
                    _currentMoveBlock.pos.y += (board.y1 - blockBoard.y1 + 1);
                }

                if (blockBoard.x1 < board.x1 && blockBoard.x2 >= board.x1)
                {
                    _currentMoveBlock.pos.x += (board.x1 - blockBoard.x1 + 1);
                }
                else if (blockBoard.x1 < board.x2 && blockBoard.x2 >= board.x2)
                {
                    _currentMoveBlock.pos.x -= (blockBoard.x2 - board.x2 + 1);
                }
            }
            else if(_rotateState == TetrisState.Rotate90)
            {
                if (blockBoard.y1 < board.y1 && blockBoard.y2 >= board.y1)
                {
                    _currentMoveBlock.pos.y += (board.y1 - blockBoard.y1 + 1);
                }
                else if (blockBoard.y1 < board.y2 && blockBoard.y2 >= board.y2)
                {
                    _currentMoveBlock.pos.y -= (blockBoard.y2 - board.y2 + 1);
                }

                if (blockBoard.x1 < board.x2 && blockBoard.x2 >= board.x2 + 1)
                {
                    _currentMoveBlock.pos.x -= (blockBoard.x2 - board.x2 + 1);
                } 
            }
            else if (_rotateState == TetrisState.Rotate180)
            {
                if (blockBoard.y1 < board.y2 && blockBoard.y2 >= board.y2)
                {
                    _currentMoveBlock.pos.y -= (blockBoard.y2 - board.y2 + 1);
                }

                if (blockBoard.x1 < board.x1 && blockBoard.x2 >= board.x1)
                {
                    _currentMoveBlock.pos.x += (board.x1 - blockBoard.x1 + 1);
                }
                else if (blockBoard.x1 < board.x2 && blockBoard.x2 >= board.x2)
                {
                    _currentMoveBlock.pos.x -= (blockBoard.x2 - board.x2 + 1);
                }
            }
            else
            {
                if (blockBoard.y1 < board.y1 && blockBoard.y2 >= board.y1)
                {
                    _currentMoveBlock.pos.y += (board.y1 - blockBoard.y1 + 1);
                }
                else if (blockBoard.y1 < board.y2 && blockBoard.y2 >= board.y2)
                {
                    _currentMoveBlock.pos.y -= (blockBoard.y2 - board.y2 + 1);
                }

                if (blockBoard.x1 < board.x1 && blockBoard.x2 >= board.x1)
                {
                    _currentMoveBlock.pos.x += (board.x1 - blockBoard.x1 + 1);
                }
            }

            var totalMove = 0;
            do
            {
                moveDis = 0;
                
                if (_rotateState == TetrisState.Rotate0 || _rotateState == TetrisState.Rotate180)
                {
                    for (int i = 0; i < _currentMoveBlock.Width; i++)
                    {
                        var tmp1 = 0;
                        for (int j = 0; j < _currentMoveBlock.Height; j++)
                        {
                            var posX = _currentMoveBlock.pos.x + i - _currentMoveBlock.Width / 2;
                            var posY = _currentMoveBlock.pos.y + j;

                            if (posY >= GameConst.BackgroundHeight || posY < 0)
                            {
                                
                            }
                            else if (_currentMoveBlock.Block[i, j] != 0 && _area[posX, posY] != BlockState.Null)
                            {
                                tmp1 = math.max(tmp1, i + 1);
                            }
                        }

                        moveDis = math.max(moveDis, tmp1);
                    }
                    
                    if (_rotateState == TetrisState.Rotate0)
                        _currentMoveBlock.pos += new Vector2Int(0, moveDis);
                    else
                        _currentMoveBlock.pos -= new Vector2Int(0, moveDis);
                }
                else
                {
                    for (int i = 0; i < _currentMoveBlock.Height; i++)
                    {
                        var tmp1 = 0;
                        for (int j = 0; j < _currentMoveBlock.Width; j++)
                        {
                            var posX = _currentMoveBlock.pos.x + j - _currentMoveBlock.Width / 2;
                            var posY = _currentMoveBlock.pos.y + i;

                            if (posX >= GameConst.BackgroundWidth || posX < 0)
                            {
                                
                            }
                            else if (_currentMoveBlock.Block[j, i] != 0 && _area[posX, posY] != BlockState.Null)
                            {
                                tmp1 = math.max(tmp1, j + 1);
                            }
                        }

                        moveDis = math.max(moveDis, tmp1);
                    }

                    if (_rotateState == TetrisState.Rotate90)
                        _currentMoveBlock.pos -= new Vector2Int(moveDis, 0);
                    else
                        _currentMoveBlock.pos += new Vector2Int(moveDis, 0);
                    
                }

                totalMove += moveDis;
                
            } while (moveDis != 0);
            
            Debug.Log($"AdjustMoveBlockPosition, totalMove {totalMove}");
            
            if (!DoAdjustMoveBlock(0))
            {
                Debug.Log("DoAdjustMoveBlock fail");
                _currentMoveBlock.pos = oriPos;
                return false;
            }
            
            return true;
        }

        private bool DoAdjustMoveBlock(int move)
        {
            var backDis = 0;
            
            if (_rotateState == TetrisState.Rotate0)
            {
                var targetPos = _currentMoveBlock.pos + new Vector2Int(0, move);

                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    var tmp = 0;
                    var tmp1 = 0;
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
                            tmp1 = math.max(tmp1, j + 1);
                        }
                    }

                    backDis = math.max(backDis, tmp + tmp1);
                }

                if (MoveTetrisArea(0, -backDis, false))
                {
                    Debug.Log($"DoAdjustMoveBlock success, move back {backDis}");
                    _currentMoveBlock.pos = targetPos - new Vector2Int(0, backDis);
                    return true;
                }
            }
            else if(_rotateState == TetrisState.Rotate90)
            {
                var targetPos = _currentMoveBlock.pos - new Vector2Int(move, 0);

                for (int j = 0; j < _currentMoveBlock.Height; j++)
                {
                    var tmp = 0;
                    var tmp1 = 0;
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
                            // tmp++;
                            tmp1 = math.max(tmp1, i + 1);
                        }
                    }

                    backDis = math.max(backDis, tmp + tmp1);
                }

                if (MoveTetrisArea(backDis, 0, false))
                {
                    Debug.Log($"DoAdjustMoveBlock success, move back {backDis}");
                    _currentMoveBlock.pos = targetPos + new Vector2Int(backDis, 0);
                    return true;
                }
            }
            else if (_rotateState == TetrisState.Rotate180)
            {
                var targetPos = _currentMoveBlock.pos - new Vector2Int(0, move);

                for (int i = 0; i < _currentMoveBlock.Width; i++)
                {
                    var tmp = 0;
                    var tmp1 = 0;
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
                            tmp1 = math.max(tmp1, j + 1);
                        }
                    }

                    backDis = math.max(backDis, tmp + tmp1);
                }

                if (MoveTetrisArea(0, backDis, false))
                {
                    Debug.Log($"DoAdjustMoveBlock success, move back {backDis}");
                    _currentMoveBlock.pos = targetPos  + new Vector2Int(0, backDis);
                    return true;
                }
            }
            else
            {
                var targetPos = _currentMoveBlock.pos + new Vector2Int(move, 0);

                for (int j = 0; j < _currentMoveBlock.Height; j++)
                {
                    var tmp = 0;
                    var tmp1 = 0;
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
                            tmp1 = math.max(tmp1, i + 1);
                        }
                    }

                    backDis = math.max(backDis, tmp + tmp1);
                }

                if (MoveTetrisArea(-backDis, 0, false))
                {
                    Debug.Log($"DoAdjustMoveBlock success, move back {backDis}");
                    _currentMoveBlock.pos = targetPos - new Vector2Int(backDis, 0);
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

                                    CheckIsBombInTetrisArea();
                                    _currentMoveBlock.pos = targetPos;
                                    return true;
                                }
                            }
                        }
                    }
                }

                CheckIsBombInTetrisArea();
                    
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
            var oriTetrisCenter = _tetrisCenter;
            
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

            targetAreaBoard = GetTargetRotateTetrisAreaBoard(_rotateState);
            var move = 0;
            
            for (int j = targetAreaBoard.y1; j < targetAreaBoard.y2; j++)
            {
                var tmp = 0;
                for (int i = targetAreaBoard.x1; i < targetAreaBoard.x2; i++)
                {
                    if (_area[i, j] == BlockState.Block)
                    {
                        tmp++;
                    }
                }

                move = math.max(move, tmp);
            }

            if (!MoveTetrisArea(0, move))
            {
                _rotateState = oriRotate;
                _tetrisCenter = oriTetrisCenter;
                return false;
            }

            CheckIsBombInTetrisArea();
            
            UpdateAllBlockValue();

            // check is valid
            var valid = AdjustMoveBlockPosition();
            if (!valid)
            {
                _rotateState = oriRotate;
                _tetrisCenter = oriTetrisCenter;
            }
            
            UpdateAllBlockValue();
            
            return valid;
        }
        
        public void CheckIsBombInTetrisArea()
        {
            var board = GetTetrisAreaBoard();
            if (!_bombInfo.hasBomb)
                return;

            if (_bombInfo.pos.x < board.x2 && _bombInfo.pos.x >= board.x1 && _bombInfo.pos.y < board.y2 &&
                _bombInfo.pos.y >= board.y1)
            {
                _bombInfo.hasBomb = false;
                _bombInfo.remainBlockNum = LevelInfo.bombSolidTime;
                _bombNum += 1;
                SoundModule.Instance.PlayAudio(SoundId.PickBomb);
            }
        }

        public void DecreaseSpeedLevel()
        {
            if(SpeedLevel <= 1)
                return;
            Score -= LevelInfo.scoreCost;
            var levelInfo = new TetrisLevelInfo(SpeedLevel - 1);
            TeleportValue -= (LevelInfo.teleportValue - levelInfo.teleportValue);
        }

        public bool SetNextBomb()
        {
            if (_bombNum > 0 && !_nextIsBomb)
            {
                _nextIsBomb = true;
                SoundModule.Instance.PlayAudio(SoundId.UseBomb);
                return true;
            }

            return false;
        }

        private void GenNextBlockId()
        {
            var ran = Random.value;

            for (int i = 0; i < _weight.Count; i++)
            {
                if (ran <= _weight[i])
                {
                    NextBlockId = i;
                    return;
                }
            }
        }
    }
}