using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class TetrisWindow: MonoBehaviour
    {
        public Tetris tetris;
        public GameObject blockPrefab;
        public GameConstConfig config;
        
        private Image[,] _tetrisBlocks;
        public RectTransform areaRectTransform;

        private bool _dirKeyDown = false;
        private float _lastDirKeyDownTime = 0f;
        private Vector2Int _lastDir;

        private float _nextTickTime = 0;

        public void Awake()
        {
            GameConst.Config = config;
            Initialization();
        }

        public void Initialization()
        {
            tetris = new Tetris();

            _tetrisBlocks = new Image[GameConst.BackgroundWidth, GameConst.BackgroundHeight];
            var size = new Vector2(GameConst.BlockSize, GameConst.BlockSize);

            for (int i = 0; i < GameConst.BackgroundWidth; i++)
            {
                for (int j = 0; j < GameConst.BackgroundHeight; j++)
                {
                    _tetrisBlocks[i, j] = GameObject.Instantiate(blockPrefab, areaRectTransform).GetComponent<Image>();
                    var rectTransform = _tetrisBlocks[i, j].GetComponent<RectTransform>();
                    
                    rectTransform.anchoredPosition =
                        new Vector2((i - GameConst.BackgroundWidth / 2) * GameConst.BlockSize,
                            (j - GameConst.BackgroundHeight / 2) * GameConst.BlockSize);
                    rectTransform.sizeDelta = size;
                }
            }

            UpdateAllBlocks();
        }

        private void Start()
        {
            StartCoroutine(UpdateCo());
        }

        public IEnumerator UpdateCo()
        {
            while (!tetris.gameFinish)
            {

                var dir = Vector2Int.zero;
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    dir = Vector2Int.left;
                }
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    dir = Vector2Int.down;
                }
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    dir = Vector2Int.right;
                }
                else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    dir = Vector2Int.up;
                }
                else if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (tetris.RotateTetrisArea(1))
                        UpdateAllBlocks();
                }
                else if (Input.GetKeyDown(KeyCode.X))
                {
                    if (tetris.RotateTetrisArea(-1))
                        UpdateAllBlocks();
                }
                else if (Input.GetKeyDown(KeyCode.C))
                {
                    tetris.SolidMoveBlock();
                    UpdateAllBlocks();
                }


                if (dir != Vector2Int.zero && tetris.MoveTetrisArea(dir.x, dir.y))
                {
                    UpdateAllBlocks();
                    _lastDir = dir;
                    _dirKeyDown = true;
                    _lastDirKeyDownTime = Time.time;
                    if(GameConst.UseReset)
                        _nextTickTime = _lastDirKeyDownTime + 1;
                }
                else if (_dirKeyDown && _lastDirKeyDownTime + GameConst.KeyCodeUpdateTime < Time.time)
                {
                    if (tetris.MoveTetrisArea(_lastDir.x, _lastDir.y))
                    {
                        UpdateAllBlocks();
                    }

                    _lastDirKeyDownTime = Time.time;
                    if(GameConst.UseReset)
                        _nextTickTime = _lastDirKeyDownTime + 1;
                }

                if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D) ||
                    Input.GetKeyUp(KeyCode.W)
                    || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow) ||
                    Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow))
                {
                    _dirKeyDown = false;
                }

                if (Time.time > _nextTickTime)
                {
                    tetris.Tick();
                    UpdateAllBlocks();
                    _nextTickTime += 1;
                }

                yield return null;
            }

            UpdateAllBlocks();
        }
        
        // public void Update()
        // {
        //     if (tetris.inTick)
        //         return;
        //     
        //     var dir = Vector2Int.zero;
        //     if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        //     {
        //         dir = Vector2Int.left;
        //     }
        //     else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        //     {
        //         dir = Vector2Int.down;
        //     }
        //     else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        //     {
        //         dir = Vector2Int.right;
        //     }
        //     else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        //     {
        //         dir = Vector2Int.up;
        //     }
        //     else if (Input.GetKeyDown(KeyCode.Z))
        //     {
        //         if(tetris.RotateTetrisArea(1))
        //             UpdateAllBlocks();
        //     }
        //     else if(Input.GetKeyDown(KeyCode.X))
        //     {
        //         if(tetris.RotateTetrisArea(-1))
        //             UpdateAllBlocks();
        //     }
        //     else if (Input.GetKeyDown(KeyCode.C))
        //     {
        //         tetris.SolidMoveBlock();
        //         UpdateAllBlocks();
        //     }
        //     
        //     
        //     if (dir != Vector2Int.zero && tetris.MoveTetrisArea(dir.x, dir.y))
        //     {
        //         _nextTickTime = _lastDirKeyDownTime + 1;
        //
        //         UpdateAllBlocks();
        //         _lastDir = dir;
        //         _dirKeyDown = true;
        //         _lastDirKeyDownTime = Time.time;
        //     }
        //     else if(_dirKeyDown && _lastDirKeyDownTime + GameConst.KeyCodeUpdateTime < Time.time)
        //     {
        //         _nextTickTime = _lastDirKeyDownTime + 1;
        //
        //         if (tetris.MoveTetrisArea(_lastDir.x, _lastDir.y))
        //         {
        //             UpdateAllBlocks();
        //         }
        //         _lastDirKeyDownTime = Time.time;
        //     }
        //
        //     if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.W) 
        //         || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow) )
        //     {
        //         _dirKeyDown = false;
        //     }
        // }

        public void UpdateAllBlocks()
        {
            // tetris.UpdateAllBlockValue();

            var board = tetris.GetTetrisAreaBoard();
            
            for (int i = 0; i < GameConst.BackgroundWidth; i++)
            {
                for (int j = 0; j < GameConst.BackgroundHeight; j++)
                {
                    if (tetris[i, j] == BlockState.Null)
                    {
                        if (i >= board.x1 && i < board.x2 && j >= board.y1 && j < board.y2)
                        {
                            _tetrisBlocks[i, j].color = Color.gray;
                            if ((tetris.RotateState == TetrisState.Rotate0 && j == board.y2 - 1) || 
                                (tetris.RotateState == TetrisState.Rotate90 && i == board.x1) ||
                                (tetris.RotateState == TetrisState.Rotate180 && j == board.y1) ||
                                (tetris.RotateState == TetrisState.Rotate270 && i == board.x2 - 1))
                            {
                                _tetrisBlocks[i, j].color = Color.cyan;
                            }
                                
                        }
                        else
                        {
                            _tetrisBlocks[i, j].color = Color.white;
                        }
                    }
                    else if (tetris[i, j] == BlockState.SoftBlock)
                    {
                        _tetrisBlocks[i, j].color = Color.blue;
                    }
                    else
                    {
                        _tetrisBlocks[i, j].color = Color.red;
                    }
                }
            }

            if (tetris.CurrentMoveBlock != null)
            {
                for (int i = 0; i < tetris.CurrentMoveBlock.Width; i++)
                {
                    for (int j = 0; j < tetris.CurrentMoveBlock.Height; j++)
                    {
                        var posX = tetris.CurrentMoveBlock.pos.x + i - tetris.CurrentMoveBlock.Width / 2;
                        var posY = tetris.CurrentMoveBlock.pos.y + j;
                        
                        if(tetris.CurrentMoveBlock.Block[i, j] != 0)
                            _tetrisBlocks[posX, posY].color = Color.black;
                    }
                }
            }
        }
    }
}