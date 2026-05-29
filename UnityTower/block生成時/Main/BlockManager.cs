using JetBrains.Annotations;
using MessagePipe;
using UniRx;
using UnityEngine;
using VContainer;

namespace UnityTower.Main
{
    public class BlockManager : MonoBehaviour
    {
        private const float PivotHeight = 3;
        private Block[] _blocks;
        private Block _currentBlock;

        [Inject]
        private IRequestHandler<BlockGenerateRequest, Block> BlockGenerateHandler { get; [UsedImplicitly] set; }

        [Inject] private GameManager _gameManager;

        private void Start()
        {
            _gameManager.CurrentState
                .Where(_ => _gameManager.CurrentState.Value == GameState.Init)
                .Subscribe(_ =>
                {
                    _gameManager.CurrentState.Value = GameState.GeneratingBlock;
                    _currentBlock = BlockGenerateHandler.Invoke(
                        new BlockGenerateRequest { PivotHeight = PivotHeight }
                    );
                    _gameManager.CurrentState.Value = GameState.GeneratedBlock;
                })
                .AddTo(this);
        }
    }
}
