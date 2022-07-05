using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = 0.2f;
    [SerializeField] private int _winCondition = 2048;

    [SerializeField] private GameObject winScreen, loseScreen;

    [SerializeField] private TextMeshProUGUI _scoreText;
    private int score;

    private List<Node> _nodes;
    private List<Block> _blocks;
    private GameState _state;
    private int _round;
    private BlockType getBlockTypeByValue(int value) => _types.First(t => t.value == value);

    int _counterShifts = 1;

    private void Start(){
        ChangeState(GameState.GenerateLevel);
    }

    private void Update(){
        if (_state != GameState.WaitingInput) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Shift(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Shift(Vector2.right);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shift(Vector2.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Shift(Vector2.down);
    }

    private void ChangeState(GameState newState){
        _state = newState;

        switch (newState){
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:
                SpawnBlocks(_round++ == 0 ? 2 : 1);
                break;
            case GameState.WaitingInput:
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
                winScreen.SetActive(true);
                break;
            case GameState.Lose:
                loseScreen.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    void GenerateGrid(){
        _round = 0;
        _nodes = new List<Node>();
        _blocks = new List<Block>();
        for (int x = 0; x < _width; x++){
            for (int y = 0; y < _height; y++){
                var node = Instantiate(_nodePrefab, new Vector3(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        var center = new Vector2((float) _width / 2 - 0.5f, (float) _height / 2 - 0.5f);

        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        Camera.main.transform.position = new Vector3(center.x, center.y + 1.5f, -10f);

        ChangeState(GameState.SpawningBlocks);
    }

    void SpawnBlocks(int amount){
        var freeNodes = _nodes.Where(n => n.OccupiedBlock == null).OrderBy(b => Random.value).ToList();

        if (_counterShifts > 0){
            foreach (var node in freeNodes.Take(amount)){
                SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
            }
        }

        _counterShifts = 0;

        if (freeNodes.Count() == 0){
            ChangeState(GameState.Lose);
            return;
        }


        ChangeState(_blocks.Any(b => b.Value == _winCondition) ? GameState.Win : GameState.WaitingInput);
    }

    void SpawnBlock(Node node, int value){
        var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);
        block.Init(getBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    void Shift(Vector2 dir){
        ChangeState(GameState.Moving);

        var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks){
            var next = block.Node;
            do{
                block.SetBlock(next);
                var possibleNode = GetNodeAtPosition(next.Pos + dir);
                if (possibleNode != null){
                    // we know a node is present
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value)){
                        block.MergeBlock(possibleNode.OccupiedBlock);
                        _counterShifts++;
                    }
                    else if (possibleNode.OccupiedBlock == null){
                        _counterShifts++;
                        next = possibleNode;
                    }
                }
            } while (next != block.Node);
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks){
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
        }

        sequence.OnComplete(() => {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null)){
                MergeBlocks(block.MergingBlock, block);
            }


            ChangeState(GameState.SpawningBlocks);
        });
    }

    void MergeBlocks(Block mergingBlock, Block baseBlock){
        SpawnBlock(mergingBlock.Node, mergingBlock.Value * 2);

        score += mergingBlock.Value;
        _scoreText.text = score.ToString();
        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    void RemoveBlock(Block block){
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    Node GetNodeAtPosition(Vector2 pos){
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }
}

[Serializable]
public struct BlockType{
    public int value;
    public Color color;
}

public enum GameState{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}