using System;
using System.Collections;
using System.Collections.Generic;
using Disc0ver.Engine;
using Engine.Runtime;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace Gameplay.UI
{
    public class TetrisWindow: MonoBehaviour
    {
        public Tetris tetris;
        public GameObject blockPrefab;
        public GameConstConfig config;
        public RectTransform tetrisArea;

        private Random _random = new Random();

        public GameObject destroyPrefab;
        
        private Image[,] _tetrisBlocks;
        public RectTransform areaRectTransform;

        public Text scoreText;
        public Text bombNumText;
        public Text bombNumMaxText;
        public Button btnSetting;
        private UISwitchImage _btnSettingImg;
        public Button btnIntroduce;
        private UISwitchImage _btnIntroduceImg;
        public UISwitchImage bombSwitchImg;
        public UISwitchImage speedImg;

        public MenuWindow menuWindow;
        public DescriptionWindow descriptionWindow;
        public SettlementWindow settlementWindow;

        public List<GameObject> blockPreViewLists = new List<GameObject>();

        private bool _dirKeyDown = false;
        private float _lastDirKeyDownTime = 0f;
        private Vector2Int _lastDir;
        
        private float _nextTickTime = 0;

        private RectTransform _rectTransform;
        private bool _playShake;

        public void Awake()
        {
            GameConst.Config = config;
            GameEvent.AddEventListener<int, int>(EventType.BlockDestroy, PlayDestroy);
            GameEvent.AddEventListener(EventType.BombBoom, ShakeWindow);
        }

        public void Initialization()
        {
            tetris = new Tetris();
            _playShake = false;
            
            _rectTransform = gameObject.GetComponent<RectTransform>();
            _rectTransform.anchoredPosition = Vector2.zero;

            if (_tetrisBlocks != null)
            {
                foreach (var image in _tetrisBlocks)
                {
                    if(image!= null)
                        Destroy(image.gameObject);
                }
            }

            _tetrisBlocks = new Image[GameConst.BackgroundWidth, GameConst.BackgroundHeight];
            var size = new Vector2(GameConst.BlockSize, GameConst.BlockSize);
            var scale = new Vector3(GameConst.BlockSize / 100f, GameConst.BlockSize / 100f, GameConst.BlockSize / 100f);

            for (int i = 0; i < GameConst.BackgroundWidth; i++)
            {
                for (int j = 0; j < GameConst.BackgroundHeight; j++)
                {
                    _tetrisBlocks[i, j] = GameObject.Instantiate(blockPrefab, areaRectTransform).GetComponentInChildren<Image>();
                    var rectTransform = _tetrisBlocks[i, j].GetComponent<RectTransform>();
                    
                    rectTransform.anchoredPosition =
                        new Vector2((i - GameConst.BackgroundWidth / 2 + 0.5f) * GameConst.BlockSize ,
                            (j - GameConst.BackgroundHeight / 2 + 0.5f) * GameConst.BlockSize);
                    // rectTransform.sizeDelta = size;
                    rectTransform.localScale = scale;
                }
            }

            UpdateAllBlocks();
        }

        public void StartGame()
        {
            Initialization();

            bombNumMaxText.text = $"{GameConst.BombLimit}";
            scoreText.text = $"{tetris.Score}";
            bombNumText.text = $"{tetris.BombInfo.remainBlockNum}";
            speedImg.SetIndex(tetris.SpeedLevel - 1);
            
            StartCoroutine(UpdateCo());
        }

        public IEnumerator UpdateCo()
        {
            while (!tetris.GameFinish)
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
                else if (Input.GetKeyDown(KeyCode.V))
                {
                    tetris.DecreaseSpeedLevel();
                }
                else if (Input.GetKeyDown(KeyCode.B))
                {
                    tetris.SetNextBomb();
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
                    
                    _nextTickTime += 1f / tetris.LevelInfo.speedTime;
                }
                
                UpdateAllBlocks();
                ShowNextBlock();

                scoreText.text = $"{tetris.Score}";
                bombNumText.text = $"{tetris.BombNum}";
                speedImg.SetIndex(tetris.SpeedLevel - 1);
                bombSwitchImg.SetIndex(tetris.NextIsBomb ? 1 : 0);

                if (_playShake)
                {
                    ShakeWindow();
                    _playShake = false;
                }

                yield return null;
            }

            UpdateAllBlocks();
            settlementWindow.Open(tetris.Score);

            foreach (var image in _tetrisBlocks)
            {
                Destroy(image.gameObject);
            }

            _tetrisBlocks = null;
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
                    var color = Color.white;

                    var anim = _tetrisBlocks[i, j].transform.parent.GetComponent<Animator>();
                    var switchImage = _tetrisBlocks[i, j].GetComponent<UISwitchImage>();
                    switchImage.SetIndex(0);
                    if (tetris[i, j] == BlockState.SoftBlock || tetris[i, j] == BlockState.Block)
                    {
                        // color = Color.blue;
                        anim.enabled = true;
                        if(_random.NextDouble() < 0.3f && !anim.GetCurrentAnimatorStateInfo(0).IsName("Spark"))
                            anim.Play("Spark");
                    }
                    else
                    {
                        anim.enabled = false;
                    }

                    if (tetris[i, j] == BlockState.Bomb)
                    {
                        switchImage.SetIndex(3);
                    }
                    

                    if (tetris[i, j] == BlockState.Null)
                    {
                        color.a = 0;
                    }

                    _tetrisBlocks[i, j].color = color;
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

                        if (tetris.CurrentMoveBlock.Block[i, j] != 0)
                        {
                            _tetrisBlocks[posX, posY].color = Color.white;
                            var switchImage = _tetrisBlocks[posX, posY].GetComponent<UISwitchImage>();
                            
                            if (tetris.CurrentMoveBlock.BlockType != MoveBlockType.Bomb)
                            {
                                switchImage.SetIndex(tetris.MoveBlockIsBlocked ? 2 : 1);
                            }
                            else
                            {
                                switchImage.SetIndex(3);
                            }
                        }
                    }
                }
            }

            tetrisArea.anchoredPosition = new Vector2((tetris.Center.x - GameConst.BackgroundWidth / 2) * GameConst.BlockSize ,
                (tetris.Center.y - GameConst.BackgroundHeight / 2) * GameConst.BlockSize);

            var rotation = tetrisArea.rotation;
            if (tetris.RotateState == TetrisState.Rotate0)
            {
                rotation.eulerAngles = new Vector3(0, 0, 0);
            }
            else if (tetris.RotateState == TetrisState.Rotate90)
            {
                rotation.eulerAngles = new Vector3(0, 0, 90);
            }
            else if(tetris.RotateState == TetrisState.Rotate180)
            {
                rotation.eulerAngles = new Vector3(0, 0, 180);
            }
            else
            {
                rotation.eulerAngles = new Vector3(0, 0, 270);
            }

            tetrisArea.rotation = rotation;
        }

        public void OnMenuClick()
        {
            menuWindow.Open();
        }

        public void OnDescriptionClick()
        {
            descriptionWindow.Open();
        }

        public void PlayDestroy(int i, int j)
        {
            var go = GameObject.Instantiate(destroyPrefab, areaRectTransform);
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchoredPosition =
                new Vector2((i - GameConst.BackgroundWidth / 2  + 0.5f) * GameConst.BlockSize ,
                    (j - GameConst.BackgroundHeight / 2 + 0.5f) * GameConst.BlockSize);
            rectTransform.localScale = new Vector3(GameConst.BlockSize / 100f, GameConst.BlockSize / 100f, GameConst.BlockSize / 100f);;
            _playShake = true;
        }

        private void ShowNextBlock()
        {
            foreach (var preView in blockPreViewLists)
            {
                preView.SetActive(false);
            }

            if (tetris.NextIsBomb)
            {
                blockPreViewLists[7].SetActive(true);
            }
            else
            {
                blockPreViewLists[tetris.NextBlockId].SetActive(true);
            }
        }

        private void ShakeWindow()
        {
            StartCoroutine(CoShakeWindow());
        }

        private IEnumerator CoShakeWindow()
        {
            var shakeTime = 0.5f;
            while (shakeTime > 0)
            {
                shakeTime -= Time.deltaTime;

                var position = new Vector2((float)_random.NextDouble(), (float)_random.NextDouble()) * 3;

                _rectTransform.anchoredPosition = position;
                yield return null;
            }
            
            _rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}