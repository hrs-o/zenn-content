using UniRx;
using UnityEngine;

namespace UnityTower.Main
{
    public class GameManager : MonoBehaviour
    {
        public ReactiveProperty<GameState> CurrentState { get; } = new(GameState.Init);
    }
}
