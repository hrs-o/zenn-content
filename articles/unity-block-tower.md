## 概要

- Unityをキチンと触ったことがなかったのでゲームを作る練習がてらWebGLでPC、スマホ問わずプレイ出来るところを目指す
- UniRX、MessagePipe、新しいInputSystemなど触ってみたいっていうのもあったのでこれらも導入してみる

## 環境

- Windows 10
- Unity 2022.1.0a15
  - WebGLモジュール追加済み
- Rider 2021.2.2

## プロジェクトの作成

- 2Dプロジェクトで作成

## プロジェクトの設定変更

### Build Settings

- WebGLにSwitch Platform

### PlayerSettings

#### Resolution and Presentation > Resolution

項目 | 設定値
--- | --- 
Default Canvas Width | 504
Default Canvas Height | 896


#### Resolution and Presentation > WebGL Template

- PWA を選択

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

- Input System
- VContainer
- UniTask
- UniRX
- MessagePipe
- MessagePipe.VContainer

## Unity Assembly Definitionの設定

### Models

Assets/BlockTower/Scripts/Models/Models.asmdef を作成

- Use GUIDsに下記を設定
  - UniRX
  - UniTask

### Presenters

Assets/BlockTower/Scripts/Presenters/Presenters.asmdef を作成

- Use GUIDsに下記を設定
  - UniRX
  - UniTask
  - MessagePipe
  - VContainer
  - Models

### Views

Assets/BlockTower/Scripts/Views/Views.asmdef を作成

- Use GUIDsに下記を設定
  - Unity.TextMeshPro
  - Unity.InputSystem
  - UniRX
  - UniTask
  - MessagePipe
  - VContainer
  - Presenters

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



### 開始時のカウントダウン実装

#### カウントダウン用のUI Textを追加

- UI > Canvasでキャンバスを作成
  - 名前をHudCanvasに変更
  - Canvas Scaler > UI Scale ModeをScale With Screen Sizeに変更
  - Canvas Scaler > Reference Resolution をX 504, Y 896に変更
  - Graphic Raycasterのチェックを外す
- UI > Text - Text Mesh ProをHudCanvasの子に追加
  - 名前をCountdownTextに変更
  - Inspector -> TextMeshPro - Text (UI) -> Extra Settings -> Raycast Targetのチェックを外す
  - サイズや位置を調整
  - CountdownTextView.csをAdd Component
- 生成されたEvenSystemのInspectorで Replace with InputSystemUIInputModuleをクリック

#### LifetimeScopeをシーンに追加

- Create Emptyで空のゲームオブジェクトを追加
- 名前を _LifetimeScopeに変更
- MainLifetimeScope.csを追加

#### Stageをシーンに追加

- Create Emptyで空のゲームオブジェクトを追加
- 名前を _Stageに変更
- StageView.csを追加

### カウントダウン後のブロックの生成実装

DTBの動物をここではブロックと呼んでいく

#### ブロックを作る

- 空のGame Objectを追加しSpawnBlockに名前を変更
  - SpawnBlockを非アクティブにする
- SpawnBlockの中に2D Object > Sprites Squareでスプライトを作成
  - 名前をBlockに変更
  - RigidBody 2Dコンポーネントを追加する
  - RigidBodyのBody TypeをKinematicに変更
  - BlockView.csをAdd Component
  - Block Spritesにスプライトをいくつか追加する

#### ステージにブロックを紐付ける

- _StageのBlock TemplateにBlockViewを紐付ける

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

#### ブロックにスクリプトを紐付ける

- Blockオブジェクトに`BlockFaller.cs`、`BlockMover.cs`、`Player Input`をAdd Component
- Inspector内にあるPlayer InputeのActionsにControllerを選択

### ブロック落下後にブロックを製造

### キーボードによるブロック回転処理の実装

#### Input Systemの設定

- Controller.inputactionsのMainのActionsに回転用の定義を追加する
- ActionsにRotateLeftを追加
  - 子にZ [Keyboard] を追加
- ActionsにRotateRightを追加
  - 子にX [Keyboard] を追加
- Save Assetを押して保存

#### シーンの変更

- Blockオブジェクトに`BlockRotator.cs`をAdd Component
  - Rotate Angleを変更する(30など)


### シーンの回転ボタンでブロックの回転を実装

#### シーンの変更

- シーンにRotatorCanvasを追加
  - RotatorCanvas内にUI > Button - TextMeshProを２つ追加
  - それぞれRotateLeft、RotateRightとテキストを変更する
  - サイズや位置はよしなに

### ブロックの積み上がりに応じてカメラを移動

#### 判定エリアの作成

- 空のGameObjectを作成し名前をMoveCameraAreaに変更
- Positionを0, 3, 0に変更
- Scaleを 10, 4, 1に変更
- BoxColider2dをAdd Component
  - Is Triggerにチェックを入れる
- `MoveCameraArea.cs`、`MoveCameraAreaMover.cs` をAdd Component

#### カメラにスクリプトを紐付ける

- Main Cameraに `CameraMover.cs` をAdd Component


### ブロックが範囲外にいったらゲーム終了にする

#### ブロックにスクリプトを紐付ける

- Blockに `BlockDestroyer.cs` をAdd Component

#### リザルト画面の作成

- シーンにResultCanvasを追加
- `ResultView.cs` をAdd Component
- UI > ImageをResultCanvasの子に追加
  - Backgroundに名前を変更
  - Anchorsを両方ともstreatchに変更
  - Left, Right, Top, Bottomを0に変更
  - 色はお好みで
  - Raycast Targetのチェックを外す
- UI > Text - Text Mesh ProをResultCanvasの子に追加
  - ResultTitleTextに名前を変更
  - TextをResultに変更
  - Raycast Targetのチェックを外す

### リザルト画面からリスタートしてゲームを開始

#### リスタートボタンを作成

- UI > Button - Text Mesh ProをResultCanvasの子に追加
  - RestartButtonに名前を変更
  - TextをRestartに変更
  - `RestartButtonView.cs` をAdd Component

