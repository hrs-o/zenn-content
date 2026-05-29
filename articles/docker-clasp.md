---
title: "Windows10+Dockerでの開発環境をWSL2化"
emoji: "🖥"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["wsl2","docker"]
published: true
---

## まえがき

- WSL2を使う人との作業が増えていく中で自分の環境も変えたいな、ということで対応していったログです
- WSLに関しては公式ドキュメントが充実しているので詳しく見るなら公式行くのが良い感じ
    - https://docs.microsoft.com/ja-jp/windows/wsl/

## 前提

- 作業は主に[Windows Terminal](https://www.microsoft.com/ja-jp/p/windows-terminal/9n0dx20hk701)で行っている
- [Docker for Windows](https://docs.docker.jp/docker-for-windows/install.html)導入済み
- [WSL2実行環境](https://docs.microsoft.com/ja-jp/windows/wsl/install-win10)構築済み

## Windows上での設定

- `PS >` から始まっているコマンドはPowerShellで実行

### Docker for Windowsの設定

- 基本的に公式の通り
    - https://docs.docker.com/docker-for-windows/wsl/

#### General

項目 | 設定
 --- | ---
Use the WSL2 based engine | チェック入れる

### Resources > WSL INTEGRATION

項目 | 設定
 --- | ---
Enable integration with ~~ | チェック入れる

### .wslconfig を使用してグローバル オプションを構成する

- [.wslconfig を使用してグローバル オプションを構成する](https://docs.microsoft.com/ja-jp/windows/wsl/wsl-config#configure-global-options-with-wslconfig)

```powershell
PS > code -n "$env:USERPROFILE\.wslconfig"
```

```ini:.wslconfig
[wsl2]
memory=4GB
```

### ネットワークドライブとして追加する

- Windowsアプリ（IDEなど）から参照する際に、パスの解決がうまくいかなかったりしたので追加
    - net use `ドライブレター` \\\\wsl$\\`ディストリビューション名`
```powershell
PS > net use U: \\wsl$\Ubuntu
```


## WSLコンテナ内

- 今回はUbuntu 20.04.2 LTS

### とりあえず最新化

```bash
$ sudo apt update && sudo apt upgrade
```

### ディストリビューションごとの起動設定を wslconf で構成する

- [ディストリビューションごとの起動設定を wslconf で構成する](https://docs.microsoft.com/ja-jp/windows/wsl/wsl-config#configure-per-distro-launch-settings-with-wslconf)
    - 全オプション書いてあるわけじゃなさそう
- [WSL のファイルのアクセス許可](https://docs.microsoft.com/ja-jp/windows/wsl/file-permissions)

```bash
$ sudo vim /etc/wsl.conf
```

```ini:/etc/wsl.conf
[automount]
options = "metadata"  # DrvFsマウントオプションPermission情報をメタデータに保存出来るようにする
crossDistro = true  # 正直良くわかっていない
```

### bashの表示を変える

```bash
$ vim ~/.bashrc
```

- 一番下に追記

```bash:~/.bashrc
PS1="\[\033[1;30m\]\D{%m.%d} \t \[\033[1;32m\]\u@\h \[\033[1;35m\]\s \[\033[1;33m\]\w\[\033[36m\]\$(__git_ps1)\[\033[00m\]\n\$ "
if [ -f /path/to/git-completion.bash ]; then
    source /path/to/git-completion.bash
fi
if [ -f /path/to/git-prompt.sh ]; then
    source /path/to/git-prompt.sh
fi
GIT_PS1_SHOWDIRTYSTATE=true
GIT_PS1_SHOWUNTRACKEDFILES=true
GIT_PS1_SHOWSTASHSTATE=true
GIT_PS1_SHOWUPSTREAM=auto
```

### dotfilesにシンボリックリンク

```bash
$ cd ~/
$ ln -s /mnt/c/Users/user/.gitconfig .gitconfig
$ ln -s /mnt/c/Users/user/.ssh/ ./.ssh
```

### 日本語化

- [WSL2を日本語化するときにやったこと](https://qiita.com/hacryu_4616/items/2ef603428993115a2a2d)を参考に作業


```bash
$ sudo apt install -y language-pack-ja
$ sudo update-locale LANG=ja_JP.UTF8
$ sudo apt -y install manpages-ja manpages-ja-dev
```

#### タイムゾーン変更

- アジア > 東京 を選択

```bash
$ sudo dpkg-reconfigure tzdata
```

#### 確認

- JSTで表示されていればOK

```bash
$ date
```
