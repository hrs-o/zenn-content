using JetBrains.Annotations;
using MessagePipe;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

namespace UnityTower.Main
{
    [UsedImplicitly]
    public class BlockGenerateHandler : IRequestHandler<BlockGenerateRequest, Block>
    {
        [Inject] private Block _blockTemplate;

        /// <summary>
        /// ブロックの生成処理.
        /// </summary>
        /// <param name="request"></param>
        public Block Invoke(BlockGenerateRequest request)
        {
            var block = Object.Instantiate(_blockTemplate, new Vector3(0, request.PivotHeight, 0), Quaternion.identity);
            block.AddComponent<PolygonCollider2D>();

            return block;
        }
    }
}
