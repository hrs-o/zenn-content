WebGLでPC、スマホ問わずプレイ出来るところを目指す。

## 環境

- Windows 10
- Unity 2021.2.0
  - WebGLモジュール追加済み
- Rider 2021.2.2

## プロジェクトの作成

- 2Dプロジェクトで作成

## プロジェクトの設定変更

### Build Settings

- WebGLにSwitch Platform

### PlayerSettings

#### Resolution and Presentation > Resolution

- 今回は縦画面で、お好みで調整。

項目 | 設定値
--- | --- 
Default Canvas Width | 504
Default Canvas Height | 896


#### Other Settings > Rendering

- スマホでWebGL表示させないなら不要

項目 | 設定値
--- | --- 
Auto Graphics API | チェック外す
Graphics APIs | WebGL 1.0のみ残す(WebGL 2.0は削除)
Lightmap Encoding | Normal Quality

#### Other Settings > Configuration

項目 | 設定値 | 備考
--- | --- | ---
Api Compatibility level | .NET Framework | 
Active Input Handling | Both | アセットによって新しい入力求められたり古い入力求められるのでそれが無いなら好みで


## パッケージの追加

- UniTask
- UniRX
- MessagePipe
- MessagePipe.VContainer
- VContainer


## メインゲーム部分の作成

### シーンの作成

- Assets/Scenes/SampleScene.unityをAssets/BlockTower/Scenes/Main.unityにリネーム

### 背景色を変える

- Main CameraのCamera > Backgroundから好みの色に変更

### 地面を作る

- 2D Object > Sprites Squareでスプライトを作成
- 名前をGroundに変更
- Staticにチェック
- Box Cllider 2Dコンポーネントを追加する
- Transformの値を変更して地面っぽくする
- SpriteRendererのColorで好みの色に変更

#### Transform

項目 | X | Y | Z 
--- | --- | --- | ---
Position | 0 | -1.25 | 0
Rotation | 0 | 0 | 0
Scale | 4 | 0.25 | 1


### Unity Assembly Definitionの設定

#### Models

Assets/BlockTower/Scripts/Models/Models.asmdef を作成

- Use GUIDsに下記を設定
  - UniRX
  - UniTask

#### Presenters

Assets/BlockTower/Scripts/Presenters/Presenters.asmdef を作成

- Use GUIDsに下記を設定
  - UniRX
  - UniTask
  - MessagePipe
  - VContainer
  - Models

#### Views

Assets/BlockTower/Scripts/Views/Views.asmdef を作成

- Use GUIDsに下記を設定
  - Unity.TextMeshPro
  - UniRX
  - UniTask
  - Unity.InputSystem
  - VContainer
  - Presenters
  - Models


### 開始時のカウントダウン実装

#### スクリプト作成

- Assets/BlockTower/Scripts/ 配下に以下のスクリプトを作成

```cs:Assets/BlockTower/Scripts/LifetimeScopes/MainLifetimeScope.cs
using BlockTower.Models;
using BlockTower.Models.Interfaces;
using BlockTower.Presenters.Main;
using BlockTower.Presenters.Main.Managers;
using BlockTower.Views.Main;
using VContainer;
using VContainer.Unity;

namespace BlockTower.Scripts.LifetimeScopes
{
    public class MainLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            Register(builder);
            RegisterComponent(builder);
            RegisterEntryPoint(builder);
        }

        private void Register(IContainerBuilder builder)
        {
            builder.Register<ICountdownText, CountdownTextModel>(Lifetime.Scoped);
        }

        private void RegisterComponent(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<CountdownTextView>();
        }

        private void RegisterEntryPoint(IContainerBuilder builder)
        {
            builder.UseEntryPoints(Lifetime.Singleton, entryPoints =>
            {
                entryPoints.Add<GameStateManager>().AsSelf();
                entryPoints.Add<CountdownTextPresenter>().AsSelf();
            });
        }
    }
}
```

```cs:Assets/BlockTower/Scripts/Models/Interfaces/ICountdownText.cs
using UniRx;

namespace BlockTower.Models.Interfaces
{
    public interface ICountdownText
    {
        public IReadOnlyReactiveProperty<int> CountdownSec { get; }
        public void CountDown();
    }
}
```

```cs:Assets/BlockTower/Scripts/Models/CountdownTextModel.cs
using System.Threading;
using BlockTower.Models.Interfaces;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UniRx;

namespace BlockTower.Models
{
    [UsedImplicitly]
    public class CountdownTextModel : ICountdownText
    {
        private const int DefaultCountdownSec = 3;
        public IReadOnlyReactiveProperty<int> CountdownSec => _countdownSec;

        private readonly IntReactiveProperty _countdownSec = new(DefaultCountdownSec);

        public void CountDown()
        {
            CountDownAsync(new CancellationToken()).Forget();
        }

        private async UniTaskVoid CountDownAsync(CancellationToken token)
        {
            _countdownSec.Value = DefaultCountdownSec;
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(1000, cancellationToken: token);
                _countdownSec.Value--;

                if (_countdownSec.Value != 0) continue;
                await UniTask.Delay(1000, cancellationToken: token);
                return;
            }
        }
    }
}

```

```cs:Assets/BlockTower/Scripts/Presenters/Main/Enums/GameState.cs
namespace BlockTower.Presenters.Main.Enums
{
    public enum GameState
    {
        Init,
        Countdown,
        GeneratingBlock
    }
}
```

```cs:Assets/BlockTower/Scripts/Presenters/Main/Managers/GameStateManager.cs
using System;
using BlockTower.Presenters.Main.Enums;
using JetBrains.Annotations;
using UniRx;
using VContainer.Unity;

namespace BlockTower.Presenters.Main.Managers
{
    [UsedImplicitly]
    public class GameStateManager : IPostStartable, IDisposable
    {
        public IReadOnlyReactiveProperty<GameState> CurrentState => _currentState;
        private readonly ReactiveProperty<GameState> _currentState = new(GameState.Init);

        private readonly CompositeDisposable _disposable = new();

        public void PostStart()
        {
            CurrentState
                .Where(state => state == GameState.Init)
                .Subscribe(_ => _currentState.Value = GameState.Countdown)
                .AddTo(_disposable);
        }

        public void ChangeStateGeneratingBlock()
        {
            _currentState.Value = GameState.GeneratingBlock;
        }

        public void Dispose() => _disposable?.Dispose();
    }
}


```

```cs:Assets/BlockTower/Scripts/Presenters/Main/CountdownTextPresenter.cs
using System;
using System.Threading;
using BlockTower.Models.Interfaces;
using BlockTower.Presenters.Main.Enums;
using BlockTower.Presenters.Main.Managers;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UniRx;
using VContainer;
using VContainer.Unity;

namespace BlockTower.Presenters.Main
{
    [UsedImplicitly]
    public class CountdownTextPresenter : IPostStartable, IDisposable
    {
        public IReadOnlyReactiveProperty<bool> IsEnabled => _isEnabled;
        private readonly BoolReactiveProperty _isEnabled = new();

        public IReadOnlyReactiveProperty<int> CountdownSec => _countdownSec;
        private readonly IntReactiveProperty _countdownSec = new();

        private readonly CancellationTokenSource _cancelTokenSrc = new();
        private readonly CompositeDisposable _disposable = new();

        [Inject] private GameStateManager _gameStateManager;
        [Inject] private ICountdownText _model;

        public void PostStart()
        {
            _gameStateManager.CurrentState
                .Where(state => state == GameState.Countdown)
                .Subscribe(_ =>
                {
                    _isEnabled.Value = true;
                    _model.CountDown();
                })
                .AddTo(_disposable);

            _gameStateManager.CurrentState
                .Where(state => state == GameState.GeneratingBlock)
                .Subscribe(_ => _isEnabled.Value = false)
                .AddTo(_disposable);

            _model.CountdownSec
                .Subscribe(sec => UpdateCountdownSec(sec, _cancelTokenSrc.Token).Forget())
                .AddTo(_disposable);
        }

        private async UniTaskVoid UpdateCountdownSec(int sec, CancellationToken token)
        {
            _countdownSec.Value = sec;
            if (sec != 0) return;
            await UniTask.Delay(1000, cancellationToken: token);
            _gameStateManager.ChangeStateGeneratingBlock();
        }

        public void Dispose()
        {
            _disposable?.Dispose();
            _cancelTokenSrc?.Cancel();
        }
    }
}


```

```cs:Assets/BlockTower/Scripts/Views/Main/CountdownTextView.cs
using BlockTower.Presenters.Main;
using TMPro;
using UniRx;
using UnityEngine;
using VContainer;

namespace BlockTower.Views.Main
{
    public class CountdownTextView : MonoBehaviour
    {
        [SerializeField] private string gameStartMessage = "START!";

        [Inject] private CountdownTextPresenter _presenter;

        private TextMeshProUGUI _countdownText;

        private void Start()
        {
            _countdownText = GetComponent<TextMeshProUGUI>();
            Enabled(false);

            _presenter.CountdownSec
                .Subscribe(UpdateCountDownText)
                .AddTo(this);

            _presenter.IsEnabled
                .Subscribe(Enabled)
                .AddTo(this);
        }

        private void UpdateCountDownText(int sec)
        {
            if (sec <= 0)
            {
                _countdownText.text = gameStartMessage;
                return;
            }

            _countdownText.text = sec.ToString();
        }

        private void Enabled(bool isEnabled)
        {
            gameObject.SetActive(isEnabled);
        }
    }
}
```


#### カウントダウン用のUI Textを追加

- UI > Canvasでキャンバスを作成
  - 名前をHudCanvasに変更
- UI > Text - Text Mesh ProをHudCanvasの子に追加
  - 名前をCountdownTextに変更
  - Inspector -> TextMeshPro - Text (UI) -> Extra Settings -> Raycast Targetのチェックを外す
  - サイズや位置を調整
  - CountdownTextView.csをAdd Component
- 生成されたEvenSystemのInspectorで Replace with InputSystemUIInputModuleをクリック

#### LifetimeScopeをシーンに追加

- Create Emptyでからのゲームオブジェクトを追加
- 名前を _LifetimeScopeに変更
- MainLifetimeScope.csを追加


### カウントダウン後のブロックの生成実装

DTBの動物をここではブロックと呼んでいく

#### スクリプト作成・変更

```diff:Assets\BlockTower\Scripts\LifetimeScopes\MainLifetimeScope.cs
 using BlockTower.Models.Interfaces;
 using BlockTower.Presenters.Main;
 using BlockTower.Presenters.Main.Managers;
 using BlockTower.Views.Main;
 using VContainer;
 using VContainer.Unity;
 
         protected override void Configure(IContainerBuilder builder)
         {
             Register(builder);
             RegisterComponent(builder);
             RegisterEntryPoint(builder);
         }

         private void RegisterComponent(IContainerBuilder builder)
         {
             builder.RegisterComponentInHierarchy<CountdownTextView>();
+            builder.RegisterComponentInHierarchy<StageView>();
         }

         private void RegisterEntryPoint(IContainerBuilder builder)
         {
             builder.UseEntryPoints(Lifetime.Singleton, entryPoints =>
             {
                 entryPoints.Add<GameStateManager>().AsSelf();
                 entryPoints.Add<CountdownTextPresenter>().AsSelf();
+                entryPoints.Add<StagePresenter>().AsSelf();
             });
         }
     }

```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\Enums\GameState.cs
@@ -4,6 +4,7 @@
     {
         Init,
+        Ready,
         Countdown,
-        GeneratingBlock
+        GeneratingBlock,
+        GeneratedBlock,
     }
 }

```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\Managers\GameStateManager.cs
@@ -17,16 +17,26 @@ namespace BlockTower.Presenters.Main.Managers
         public void PostStart()
         {
             CurrentState
-                .Where(state => state == GameState.Init)
+                .Where(state => state == GameState.Ready)
                 .Subscribe(_ => _currentState.Value = GameState.Countdown)
                 .AddTo(_disposable);
         }
 
+        public void ChangeStateInitializedBlock()
+        {
+            _currentState.Value = GameState.Ready;
+        }
+
         public void ChangeStateGeneratingBlock()
         {
             _currentState.Value = GameState.GeneratingBlock;
         }
 
+        public void ChangeStateGeneratedBlock()
+        {
+            _currentState.Value = GameState.GeneratedBlock;
+        }
+
         public void Dispose() => _disposable?.Dispose();
     }
 }
```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\StagePresenter.cs
using System;
using BlockTower.Presenters.Main.Enums;
using BlockTower.Presenters.Main.Managers;
using JetBrains.Annotations;
using UniRx;
using VContainer;
using VContainer.Unity;

namespace BlockTower.Presenters.Main
{
    [UsedImplicitly]
    public class StagePresenter : IPostStartable, IDisposable
    {
        private const float DefaultGenerateHeight = 3f;

        public IObservable<float> GenerateBlockAsObservable => _generateBlockSubject;
        private readonly Subject<float> _generateBlockSubject = new();
        public IObservable<Unit> InitBlockAsObservable => _initBlockSubject;
        private readonly Subject<Unit> _initBlockSubject = new();

        private readonly CompositeDisposable _disposable = new();

        [Inject] private GameStateManager _gameStateManager;

        public void PostStart()
        {
            _generateBlockSubject.AddTo(_disposable);
            _initBlockSubject.AddTo(_disposable);

            _gameStateManager.CurrentState
                .Where(state => state == GameState.Init)
                .Subscribe(_ => _initBlockSubject.OnNext(Unit.Default))
                .AddTo(_disposable);

            _gameStateManager.CurrentState
                .Where(state => state == GameState.GeneratingBlock)
                .Subscribe(_ => _generateBlockSubject.OnNext(DefaultGenerateHeight))
                .AddTo(_disposable);
        }

        public void Dispose() => _disposable?.Dispose();
    }
}
```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockView.cs
using UnityEngine;

namespace BlockTower.Views.Main.Block
{
    public class BlockView : MonoBehaviour
    {
        [SerializeField] private Sprite[] sprites;
        public void Init(float generateHeight)
        {
            var position = new Vector3(0, generateHeight, 0);
            transform.SetPositionAndRotation(
                position,
                Quaternion.identity
            );
            gameObject.GetComponent<Rigidbody2D>().position = position;
            gameObject.GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
            gameObject.AddComponent<PolygonCollider2D>();
        }
    }
}

```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\StageView.cs
using System.Collections.Generic;
using System.Threading;
using BlockTower.Presenters.Main;
using BlockTower.Presenters.Main.Managers;
using BlockTower.Views.Main.Block;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BlockTower.Views.Main
{
    public class StageView : MonoBehaviour
    {
        [SerializeField] private BlockView blockTemplate;
        [Inject] private IObjectResolver _resolver;
        [Inject] private StagePresenter _presenter;
        [Inject] private GameStateManager _gameStateManager;

        private readonly CancellationTokenSource _cancelTokenSrc = new();
        private readonly List<BlockView> _blocks = new();

        private void Start()
        {
            _presenter.InitBlockAsObservable
                .Subscribe(_ => Init(_cancelTokenSrc.Token).Forget())
                .AddTo(this);

            _presenter.GenerateBlockAsObservable
                .Subscribe(generateHeight =>
                {
                    var block = _resolver.Instantiate(blockTemplate);
                    block.Init(generateHeight);
                    _blocks.Add(block);
                    _gameStateManager.ChangeStateGeneratedBlock();
                })
                .AddTo(this);
        }

        private async UniTaskVoid Init(CancellationToken token)
        {
            for (var idx = 0; idx < _blocks.Count; idx++)
            {
                if (_blocks[idx] == null) continue;
                Destroy(_blocks[idx].gameObject);
            }

            _blocks.Clear();

            await UniTask.Delay(1000, cancellationToken: token);
            _gameStateManager.ChangeStateInitializedBlock();
        }

        private void OnDestroy()
        {
            _cancelTokenSrc?.Cancel();
        }
    }
}

```

#### ブロックを作る

- 空のGame Objectを追加しSpawnBlockに名前を変更
  - SpawnBlockを非アクティブにする
- SpawnBlockの中に2D Object > Sprites Squareでスプライトを作成
  - 名前をBlockに変更
  - RigidBody 2Dコンポーネントを追加する
  - RigidBodyのBody TypeをKinematicに変更
  - BlockView.csをAdd Component
  - Block Spritesにスプライトをいくつか追加する

#### ステージを作る

- 空のGame Objectを追加し_Stageに名前を変更
- StageView.csをAdd Component
- Block TemplateにBlockViewを紐付ける

### ブロック移動・落下処理の実装

#### Input Systemの設定

- Assets/BlockTowerディレクトリ内で右クリックし、Create > Input Actions
- Controller.inputactionsを作成
- Action MapsにMainを追加
- ActionsにPressを追加
  - Pressの子にLeft Button [Mouse] と Press [Touchscreen] を追加
- ActionsにMoveを追加
  - MoveのAction typeを `Value`、 Control Typeを `Vector2` に設定
  - Moveの子に Point [Any] を追加
- Save Assetを押して保存


#### スクリプト作成

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockFaller.cs
using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockTower.Views.Main.Block
{
    public class BlockFaller : MonoBehaviour
    {
        private const float StopWaitSec = 1f;

        private InputAction _inputPress;

        private Rigidbody2D _rigidbody;

        private float _startY;
        private float _stopElapsedSec;

        private IDisposable _stoppedDisposable;

        private void Start()
        {
            var playerInput = GetComponent<PlayerInput>();
            var actionMap = playerInput.currentActionMap;
            _inputPress = actionMap["Press"];

            _rigidbody = GetComponent<Rigidbody2D>();
            _startY = _rigidbody.position.y;

            this.UpdateAsObservable()
                .FirstOrDefault(_ => _inputPress.WasReleasedThisFrame())
                .Subscribe(_ => FallBlock())
                .AddTo(this);

            _stoppedDisposable = this.UpdateAsObservable()
                .Where(_ => _rigidbody.position.y < _startY)
                .Subscribe(_ => Falling())
                .AddTo(this);
        }

        private void Falling()
        {
            if (!_rigidbody.IsSleeping())
            {
                _stopElapsedSec = 0;
                return;
            }

            _stopElapsedSec += Time.deltaTime;
            if (!(_stopElapsedSec >= StopWaitSec)) return;
            _stoppedDisposable?.Dispose();
            _stopElapsedSec = 0;
        }

        private void FallBlock()
        {
            _rigidbody.isKinematic = false;
        }
    }
}

```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockMover.cs
using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockTower.Views.Main.Block
{
    public class BlockMover : MonoBehaviour
    {
        private Camera _camera;
        private Rigidbody2D _rigidbody;

        private InputAction _inputPress;
        private InputAction _inputMove;

        private IDisposable _moveDisposable;

        private void Start()
        {
            Debug.Assert(Camera.main != null, "Camera.main != null");
            _camera = Camera.main;

            var playerInput = GetComponent<PlayerInput>();
            var actionMap = playerInput.currentActionMap;
            _inputPress = actionMap["Press"];
            _inputMove = actionMap["Move"];

            _rigidbody = GetComponent<Rigidbody2D>();

            _moveDisposable = this.UpdateAsObservable()
                .Where(_ => _inputPress.IsPressed())
                .Select(_ => _inputMove.ReadValue<Vector2>())
                .Subscribe(MoveBlock)
                .AddTo(this);

            this.ObserveEveryValueChanged(_ => _rigidbody.isKinematic)
                .FirstOrDefault(x => x == false)
                .Subscribe(_ => _moveDisposable?.Dispose())
                .AddTo(this);
        }

        private void MoveBlock(Vector2 inputPos)
        {
            var inputX = _camera.ScreenToWorldPoint(inputPos).x;
            _rigidbody.position = new Vector2(inputX, _rigidbody.position.y);
        }
    }
}

```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockView.cs
@@ -1,10 +1,14 @@
 using UnityEngine;
+using VContainer;
+using Random = UnityEngine.Random;
 
 namespace BlockTower.Views.Main.Block
 {
     public class BlockView : MonoBehaviour
     {
         [SerializeField] private Sprite[] sprites;
+        [Inject] private IObjectResolver _resolver;
+
         public void Init(float generateHeight)
         {
             var position = new Vector3(0, generateHeight, 0);
@@ -12,9 +16,24 @@ namespace BlockTower.Views.Main.Block
                 position,
                 Quaternion.identity
             );
+
+            ResolveInjections();
             gameObject.GetComponent<Rigidbody2D>().position = position;
             gameObject.GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
             gameObject.AddComponent<PolygonCollider2D>();
         }
+
+        private void ResolveInjections()
+        {
+            Resolve<BlockFaller>();
+        }
+
+        private void Resolve<T>()
+        {
+            var component = gameObject.GetComponent<T>();
+            if (component == null) return;
+            _resolver.Inject(component);
+        }
     }
 }
```

#### ブロックにスクリプトを紐付ける

- Blockオブジェクトに`BlockFaller.cs`、`BlockMover.cs`、`Player Input`をAdd Component
- Inspector内にあるPlayer InputeのActionsにControllerを選択

### ブロック落下後にブロックを製造

#### スクリプト変更

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\StagePresenter.cs
            _gameStateManager.CurrentState.Value = GameState.GeneratedBlock;
        }

+        public void StopBlock()
+        {
+            _gameStateManager.CurrentState.Value = GameState.GeneratingBlock;
+        }

        public void Dispose() => _disposable?.Dispose();
    }

```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockFaller.cs
 using System;
+using BlockTower.Presenters.Main;
+using BlockTower.Presenters.Main.Managers;
 using UniRx;
 using UniRx.Triggers;
 using UnityEngine;
 using UnityEngine.InputSystem;
+using VContainer;
 
 namespace BlockTower.Views.Main.Block
 {
@@ -10,6 +13,9 @@ namespace BlockTower.Views.Main.Block
     {
         private const float StopWaitSec = 1f;
 
+        [Inject] private StagePresenter _presenter;
+        [Inject] private GameStateManager _gameStateManager;
+
         private InputAction _inputPress;
 
         private Rigidbody2D _rigidbody;
@@ -51,6 +57,7 @@ namespace BlockTower.Views.Main.Block
             if (!(_stopElapsedSec >= StopWaitSec)) return;
             _stoppedDisposable?.Dispose();
             _stopElapsedSec = 0;
+            _gameStateManager.ChangeStateGeneratingBlock();
         }
 
         private void FallBlock()
```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockView.cs
         [SerializeField] private Sprite[] sprites;
         [Inject] private IObjectResolver _resolver;
 
+        private Rigidbody2D _rigidbody;
+
         public void Init(float generateHeight)
         {
             var position = new Vector3(0, generateHeight, 0);
@@ -18,11 +20,17 @@ namespace BlockTower.Views.Main.Block
             );
 
             ResolveInjections();
-            gameObject.GetComponent<Rigidbody2D>().position = position;
+            _rigidbody = GetComponent<Rigidbody2D>();
+            _rigidbody.position = position;
             gameObject.GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
             gameObject.AddComponent<PolygonCollider2D>();
         }
 
+        internal bool IsMoving()
+        {
+            return !_rigidbody.IsSleeping();
+        }
+
         private void ResolveInjections()
```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\StageView.cs
         private readonly CancellationTokenSource _cancelTokenSrc = new();
         private readonly List<BlockView> _blocks = new();
+        private readonly CancellationTokenSource _cancellationTokenSrc = new();
 
         private void Start()
         {
@@ -28,16 +29,34 @@ namespace BlockTower.Views.Main
                 .AddTo(this);
 
             _presenter.GenerateBlockAsObservable
-                .Subscribe(generateHeight =>
-                {
-                    var block = _resolver.Instantiate(blockTemplate);
-                    block.Init(generateHeight);
-                    _blocks.Add(block);
-                    _gameStateManager.ChangeStateGeneratedBlock();
-                })
+                .Subscribe(generateHeight => GenerateBlock(generateHeight, _cancelTokenSrc.Token).Forget())
                 .AddTo(this);
         }
 
+        private async UniTaskVoid GenerateBlock(float generateHeight, CancellationToken token)
+        {
+            while (IsMovingBlocks())
+            {
+                if (_cancellationTokenSrc.Token.IsCancellationRequested) return;
+                await UniTask.Delay(500, cancellationToken: token);
+            }
+
+            var block = _resolver.Instantiate(blockTemplate);
+            block.Init(generateHeight);
+            _blocks.Add(block);
+            _gameStateManager.ChangeStateGeneratedBlock();
+        }
+
+        private bool IsMovingBlocks()
+        {
+            for (var i = 0; i < _blocks.Count; i++)
+            {
+                if (_blocks[i].IsMoving()) return true;
+            }
+
+            return false;
+        }
+
         private async UniTaskVoid Init(CancellationToken token)
```

### キーボードによるブロック回転処理の実装

#### Input Systemの設定

- Controller.inputactionsのMainのActionsに回転用の定義を追加する
- ActionsにRotateLeftを追加
  - 子にZ [Keyboard] を追加
- ActionsにRotateRightを追加
  - 子にX [Keyboard] を追加
- Save Assetを押して保存

#### スクリプト作成

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockRotator.cs
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockTower.Views.Main.Block
{
    public class BlockRotator : MonoBehaviour
    {
        [SerializeField] private float rotateAngle = 30f;

        private InputAction _inputRotateLeft;
        private InputAction _inputRotateRight;

        private float _startY;
        private Rigidbody2D _rigidbody;

        private readonly CompositeDisposable _disposables = new();

        private void Start()
        {
            var playerInput = GetComponent<PlayerInput>();
            var actionMap = playerInput.currentActionMap;
            _inputRotateLeft = actionMap["RotateLeft"];
            _inputRotateRight = actionMap["RotateRight"];

            _rigidbody = GetComponent<Rigidbody2D>();
            _startY = _rigidbody.position.y;

            this.ObserveEveryValueChanged(_ => _inputRotateLeft.WasPressedThisFrame())
                .Where(wasPressed => wasPressed)
                .Subscribe(_ => RotateLeft())
                .AddTo(this)
                .AddTo(_disposables);

            this.ObserveEveryValueChanged(_ => _inputRotateRight.WasPressedThisFrame())
                .Where(wasPressed => wasPressed)
                .Subscribe(_ => RotateRight())
                .AddTo(this)
                .AddTo(_disposables);

            this.ObserveEveryValueChanged(_ => _rigidbody.position.y)
                .Where(posY => posY < _startY)
                .Subscribe(_ => _disposables.Dispose())
                .AddTo(this)
                .AddTo(_disposables);
        }

        private void RotateLeft()
        {
            transform.Rotate(0, 0, rotateAngle);
        }

        private void RotateRight()
        {
            transform.Rotate(0, 0, -rotateAngle);
        }
    }
}
```

#### シーンの変更

- Blockオブジェクトに`BlockRotator.cs`をAdd Component
  - Rotate Angleを変更する(30など)



### シーンの回転ボタンでブロックの回転を実装

#### シーンの変更

- シーンにRotatorCanvasを追加
  - RotatorCanvas内にUI > Button - TextMeshProを２つ追加
  - それぞれRotateLeft、RotateRightとテキストを変更する
  - サイズや位置はよしなに

#### スクリプトの変更

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockRotator.cs
 using UnityEngine;
 using UnityEngine.InputSystem;
+using UnityEngine.UI;
 
 namespace BlockTower.Views.Main.Block
 {
     public class BlockRotator : MonoBehaviour
     {
         [SerializeField] private float rotateAngle = 30f;
+        [SerializeField] private Button rotateLeftButton;
+        [SerializeField] private Button rotateRightButton;
 
         private InputAction _inputRotateLeft;
         private InputAction _inputRotateRight;
@@ -43,6 +46,16 @@ namespace BlockTower.Views.Main.Block
                 .Subscribe(_ => _disposables.Dispose())
                 .AddTo(this)
                 .AddTo(_disposables);
+
+            rotateLeftButton.OnClickAsObservable()
+                .Subscribe(_ => RotateLeft())
+                .AddTo(this)
+                .AddTo(_disposables);
+
+            rotateRightButton.OnClickAsObservable()
+                .Subscribe(_ => RotateRight())
+                .AddTo(this)
+                .AddTo(_disposables);
         }
 
         private void RotateLeft()
```



### ブロックの積み上がりに応じてカメラを移動


#### スクリプト作成

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\LifetimeScopes\MainLifetimeScope.cs
 using BlockTower.Presenters.Main;
 using BlockTower.Presenters.Main.Managers;
 using BlockTower.Views.Main;
+using BlockTower.Views.Main.MoveCameraArea;
 using VContainer;
 using VContainer.Unity;
 
@@ -26,6 +27,9 @@ namespace BlockTower.Scripts.LifetimeScopes
         {
             builder.RegisterComponentInHierarchy<CountdownTextView>();
             builder.RegisterComponentInHierarchy<StageView>();
+            builder.RegisterComponentInHierarchy<CameraMover>();
+            builder.RegisterComponentInHierarchy<MoveCameraAreaView>();
+            builder.RegisterComponentInHierarchy<MoveCameraAreaMover>();
         }
 
         private void RegisterEntryPoint(IContainerBuilder builder)
@@ -35,6 +39,7 @@ namespace BlockTower.Scripts.LifetimeScopes
                 entryPoints.Add<GameStateManager>().AsSelf();
                 entryPoints.Add<CountdownTextPresenter>().AsSelf();
                 entryPoints.Add<StagePresenter>().AsSelf();
+                entryPoints.Add<MoveCameraAreaPresenter>().AsSelf();
             });
         }
     }

```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\Enums\GameState.cs
        Ready,
        Countdown,
+       MovingCamera,
        GeneratingBlock,
        GeneratedBlock,
```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\Managers\GameStateManager.cs
        }

+       public void ChangeStateMovingCamera()
+       {
+           _currentState.Value = GameState.MovingCamera;
+       }
+
        public void ChangeStateGeneratingBlock()
        {
            _currentState.Value = GameState.GeneratingBlock;
```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\MoveCameraAreaPresenter.cs
using System;
using System.Threading;
using BlockTower.Presenters.Main.Enums;
using BlockTower.Presenters.Main.Managers;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UniRx;
using VContainer;
using VContainer.Unity;

namespace BlockTower.Presenters.Main
{
    [UsedImplicitly]
    public class MoveCameraAreaPresenter : IPostStartable, IFixedTickable, IDisposable
    {
        private const float MoveCameraHeightOffset = 0.03f;

        [Inject] private GameStateManager _gameStateManager;

        public IObservable<float> AddMoveCameraHeightOffsetAsObservable => _addMoveCameraHeightOffsetSubject;
        private readonly Subject<float> _addMoveCameraHeightOffsetSubject = new();

        private readonly CancellationTokenSource _cancelTokenSrc = new();
        private readonly CompositeDisposable _disposable = new();

        private float _currentCameraOffsetHeight;
        private bool _canMoveCamera;

        public void PostStart()
        {
            _addMoveCameraHeightOffsetSubject.AddTo(_disposable);

            _gameStateManager.CurrentState
                .Where(state => state == GameState.Init)
                .Subscribe(_ =>
                {
                    _addMoveCameraHeightOffsetSubject.OnNext(-_currentCameraOffsetHeight);
                    _currentCameraOffsetHeight = 0;
                })
                .AddTo(_disposable);
        }

        public void FixedTick()
        {
            if (_gameStateManager.CurrentState.Value != GameState.MovingCamera) return;

            MovingCameraAsync(_cancelTokenSrc.Token).Forget();
        }

        public void MovingCamera()
        {
            _canMoveCamera = true;
        }

        public void StopCamera()
        {
            _canMoveCamera = false;
        }

        private async UniTaskVoid MovingCameraAsync(CancellationToken token)
        {
            while (_canMoveCamera)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                _addMoveCameraHeightOffsetSubject.OnNext(MoveCameraHeightOffset);
                _currentCameraOffsetHeight += MoveCameraHeightOffset;
            }

            _gameStateManager.ChangeStateGeneratingBlock();
        }

        public void Dispose() => _disposable?.Dispose();
    }
}
```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Presenters\Main\StagePresenter.cs
         private readonly CompositeDisposable _disposable = new();
 
         [Inject] private GameStateManager _gameStateManager;
+        [Inject] private MoveCameraAreaPresenter _moveCameraAreaPresenter;
+
+        private float _currentGenerateHeightOffset;
 
         public void PostStart()
         {
@@ -34,7 +37,15 @@ namespace BlockTower.Presenters.Main
 
             _gameStateManager.CurrentState
                 .Where(state => state == GameState.GeneratingBlock)
-                .Subscribe(_ => _generateBlockSubject.OnNext(DefaultGenerateHeight))
+                .Subscribe(_ =>
+                    _generateBlockSubject.OnNext(
+                        DefaultGenerateHeight + _currentGenerateHeightOffset
+                    )
+                )
+                .AddTo(_disposable);
+
+            _moveCameraAreaPresenter.AddMoveCameraHeightOffsetAsObservable
+                .Subscribe(offset => _currentGenerateHeightOffset += offset)
                 .AddTo(_disposable);
         }
  
```

```diff:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\Block\BlockFaller.cs
            _stoppedDisposable?.Dispose();
            _stopElapsedSec = 0;
-           _gameStateManager.ChangeStateGeneratingBlock();
+           _gameStateManager.ChangeStateMovingCamera();
        }

        private void FallBlock()
```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\CameraMover.cs
using BlockTower.Presenters.Main;
using UniRx;
using UnityEngine;
using VContainer;

namespace BlockTower.Views.Main
{
    public class CameraMover : MonoBehaviour
    {
        [Inject] private MoveCameraAreaPresenter _presenter;
        private Transform _transform;

        private void Start()
        {
            _transform = transform;
            _presenter.AddMoveCameraHeightOffsetAsObservable
                .Subscribe(MoveCamera)
                .AddTo(this);
        }

        private void MoveCamera(float y)
        {
            _transform.Translate(0, y, 0);
        }
    }
}
```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\MoveCameraAreaView.cs
using BlockTower.Presenters.Main;
using UnityEngine;
using VContainer;

namespace BlockTower.Views.Main.MoveCameraArea
{
    public class MoveCameraAreaView : MonoBehaviour
    {
        [Inject] private MoveCameraAreaPresenter _presenter;

        private void OnTriggerEnter2D(Collider2D other)
        {
            _presenter.MovingCamera();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            _presenter.StopCamera();
        }
    }
}

```

```cs:D:\UnityProjects\BlockTower\Assets\BlockTower\Scripts\Views\Main\MoveCameraAreaMover.cs
using BlockTower.Presenters.Main;
using UniRx;
using UnityEngine;
using VContainer;

namespace BlockTower.Views.Main.MoveCameraArea
{
    public class MoveCameraAreaMover : MonoBehaviour
    {
        [Inject] private MoveCameraAreaPresenter _presenter;
        private Transform _transform;

        private void Start()
        {
            _transform = transform;
            _presenter.AddMoveCameraHeightOffsetAsObservable
                .Subscribe(MoveArea)
                .AddTo(this);
        }

        private void MoveArea(float y)
        {
            _transform.Translate(0, y, 0);
        }
    }
}

```

#### 判定エリアの作成

- 空のGameObjectを作成し名前をMoveCameraAreaに変更
- Positionを0, 3, 0に変更
- Scaleを 10, 4, 1に変更
- BoxColider2dをAdd Component
  - Is Triggerにチェックを入れる
- `MoveCameraArea.cs`、`MoveCameraAreaMover.cs` をAdd Component

#### カメラにスクリプトを紐付ける

- Main Cameraに- `CameraMover.cs` をAdd Component


### リザルト画面を作る

- ResultCanvas作る
- ResultViewくっつける
- Backgroundいれる 全部ストレッチ れいきゃすと着る
- ResultText 上


Block周りで使えそう
https://qiita.com/mattak/items/106dfd0974653aa06fbc
UniRXを覚える
https://www.hanachiru-blog.com/entry/2020/05/14/120000#Cold-Observable



ユーザーの操作
クリック
ドラッグ
離す
回転





            this.UpdateAsObservable()
                .Where(_ => Mouse.current.leftButton.isPressed)
                .Select(_ => new Vector3(mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()).x, pivotHeight, 0))
                .Subscribe();
            if (mouseLeft.isPressed)
            {
                _currentParts.transform.position = mousePosition;
            }




### リスタート処理を作成する

- Assets/Scripts/Mainディレクトリを作成
- Mainディレクトリ内で右クリックし、Create > C# Script
- GameManager

### ゲームオーバーとリトライを作成する

何度も繰り返すので先に作ってしまう

- Assets/Scripts/Mainディレクトリを作成
- Mainディレクトリ内で右クリックし、Create > C# Script
- 名前はGameOver






## 要件定義

### 目的

- ブロックをたくさん、高く積み上げる

### ゲームシステム

- プレイヤーは一人
- ランダムでブロックが定点に出現する
- ブロックは自由落下する
- ブロックを高くつむ

### ブロック

- ドラッグで左右に移動させることが出来る
- 右、左回転させることが出来る
- ドラッグを終了（マウス、タップを離す）と自然落下する
- 画面外に行くとゲームオーバー（終了）
- 全部止まってたら次のブロックが生成される


### 全体のフロー

#### タイトルシーン

- ゲーム開始
- ヘルプ


## State

### Init

- 初期処理
- Blockの生成

### BlockGenerated

- 移動
- 回転
- 落下させる

### BlockFalling

- ブロック落下中
- 全ブロックの動作がなくなるまで待機

### BlockFallen

- 落下後、全ブロックが静止している状態
- BlockGeneratingに遷移


### BlockGenerating

- Blockの生成

### Result

- 終了


StageView
- Block
- カメラ位置関係
