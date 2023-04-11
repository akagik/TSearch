# TSearch - Unity Quick Search Package

TSearchは、Unityエディタ上でCmd (またはCtrl) + T ショートカットキーを使用して、
Assets, メニューコマンド, GameObjects, 検索履歴を高速に検索できるコマンドパレット機能を追加するパッケージです。

![TSearch Screenshot](./images/Animation.gif)
![TSearch Screenshot](./images/screenshot.png)

## 主な機能

- Assetsの高速検索
- Editor Commandsの高速検索
- Hierarchy内のGameObjectsの高速検索
- 検索履歴の管理

## インストール方法

### PackageManagerを使用したインストール

1. Unityプロジェクトの`Packages/manifest.json`ファイルを開きます。
2. 以下のように依存関係にTSearchパッケージとUniTaskパッケージを追加してください。

```json
{
  "dependencies": {
    "net.room6.tsearch": "https://github.com/akagik/TSearch.git#x.y.z",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

注意: `x.y.z` はTSearchパッケージのバージョン番号です。適切なバージョン番号に置き換えてください。

### UniTask依存なしのインストール

TSearchはデフォルトでUniTaskに依存していますが、UniTaskを使用しない場合はno-unitaskブランチをインストールすることができます。

```json
{
  "dependencies": {
    "net.room6.tsearch": "https://github.com/akagik/TSearch.git#no-unitask"
  }
}
```

## 使用方法

1. Unityエディタ上で、Cmd (またはCtrl) + T ショートカットキーを押して、TSearchコマンドパレットを開きます。
2. TabまたはShift + Tabキーで検索タブを切り替えることができます。
3. 検索バーに検索したいキーワードを入力します。
4. 検索結果から目的のAsset, Command, GameObjectを上下キーで選択します。
5. Enterキーを押すか、マウスでクリックして、目的のアセットを開きます。(コマンドの場合は実行します。)
6. Escキーで検索操作はキャンセルできます。
7. フォルダアイコンをクリックすることで、現在選択中のアセットをそのフォルダに移動することもできます。

## ライセンス

TSearchは[MITライセンス](LICENSE.md)のもとで公開されています。
