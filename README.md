# TSearch - Unity Quick Search Package

TSearchは、Unityエディタ上でCmd (またはCtrl) + T ショートカットキーを使用して、
Assets, メニューコマンド, GameObjects, 検索履歴を高速に検索できるコマンドパレット機能を追加するパッケージです。

![TSearch Screenshot](./images/screenshot.png)

## 主な機能

- Assetsの高速検索
- Editor Commandsの高速検索
- Hierarchy内のGameObjectsの高速検索
- 検索履歴の管理

## インストール方法

### PackageManagerを使用したインストール

1. Unityプロジェクトの`Packages/manifest.json`ファイルを開きます。
2. 以下のように依存関係にTSearchパッケージを追加してください。

```json
{
  "dependencies": {
    "net.room6.tsearch": "https://github.com/akagik/TSearch.git#x.y.z"
  }
}
```

注意: `x.y.z` はTSearchパッケージのバージョン番号です。適切なバージョン番号に置き換えてください。

## 使用方法

1. Unityエディタ上で、Cmd (またはCtrl) + T ショートカットキーを押して、TSearchコマンドパレットを開きます。
2. 検索バーに検索したいキーワードを入力します。
3. 検索結果から目的のAsset, Command, GameObjectを選択し、Enterキーを押すか、マウスでクリックします。

## ライセンス

TSearchは[MITライセンス](LICENSE.md)のもとで公開されています。
