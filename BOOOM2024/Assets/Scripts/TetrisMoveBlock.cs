using UnityEngine;

namespace Gameplay
{
    public abstract class BTetrisMoveBlock
    {
        public abstract int Height { get; }
        public abstract int Width { get; }
        public abstract MoveBlockType BlockType { get; }

        public Vector2Int pos = Vector2Int.zero;

        public abstract int[,] Block { get; }
    }

    public class TTetrisMoveBlock: BTetrisMoveBlock
    {

        public override int Height => 2;
        public override int Width => 3;
        public override MoveBlockType BlockType => MoveBlockType.T;
        public override int[,] Block => _block;

        private static readonly int[,] _block = new int[,] { { 1, 0 }, { 1, 1 }, { 1, 0 } };
    }

    public class LTetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 2;
        public override int Width => 3;
        public override MoveBlockType BlockType => MoveBlockType.L;
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { { 1, 0 }, { 1, 0 }, { 1, 1 } };
    }
    
    public class LRTetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 2;
        public override int Width => 3;
        public override MoveBlockType BlockType => MoveBlockType.LR;
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { { 0, 1 }, { 0, 1 }, { 1, 1 } };
    }
    
    public class STetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 2;
        public override int Width => 2;
        public override MoveBlockType BlockType => MoveBlockType.S;
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { { 1, 1 }, { 1, 1 } };
    }
    
    public class ITetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 1;
        public override int Width => 4;
        public override MoveBlockType BlockType => MoveBlockType.I;
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { {1}, {1}, {1}, {1} };
    }
    
    public class ZTetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 2;
        public override int Width => 3;
        public override MoveBlockType BlockType => MoveBlockType.Z;
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { {0, 1}, {1, 1}, {1, 0}};
    }
    
    public class ZRTetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 2;
        public override int Width => 3;
        public override MoveBlockType BlockType => MoveBlockType.ZR;
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { {1, 0}, {1, 1}, {0, 1}};
    }

    public class BombTetrisMoveBlock : BTetrisMoveBlock
    {
        public override int Height => 1;
        public override int Width => 1;
        public override MoveBlockType BlockType => MoveBlockType.Bomb;
        
        public override int[,] Block => _block;
        
        private static readonly int[,] _block = new int[,] { {1}};
    }
}