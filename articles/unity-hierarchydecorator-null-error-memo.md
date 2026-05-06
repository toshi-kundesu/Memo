---
title: "Unity の HierarchyDecorator NullReferenceException を直したメモ"
emoji: "🛠️"
type: "tech"
topics: ["unity", "editor", "hierarchy", "troubleshooting"]
published: true
---

Unity の Console に `HierarchyDecorator.ComponentGroup.TryGetComponent` 由来の `NullReferenceException` が出た。
Asset source: [WooshiiDev/HierarchyDecorator](https://github.com/WooshiiDev/HierarchyDecorator)


![HierarchyDecorator の NullReferenceException が Console に出ている状態](/images/unity-hierarchydecorator-null-error-memo/hierarchydecorator-null-reference.png)

スタックを見ると、落ちている場所はここだった。

```text
HierarchyDecorator.ComponentGroup.TryGetComponent(...)
Packages/VLiveKit_ThirdPartyUtilities/Assets/toshi.VLiveKit/ThirdPartyUtilities/HierarchyDecorator/Scripts/Editor/Data/Types/ComponentGroup.cs
```

呼び出し元は `ComponentData.UpdateData()` や `Settings.OnAfterDeserialize()`。つまり Unity の import / domain reload / deserialize の流れで、HierarchyDecorator の設定データが読み直されたタイミングで落ちていた。

## 原因っぽいもの

HierarchyDecorator の元実装は、serialized field がだいたい正常に入っている前提で書かれていた。

ただ、package を移動したり、submodule / package 化したり、Unity のバージョンや package 構成が変わったりすると、古い serialized data の中に `null` な group や component が混ざることがある。

その状態で `TryGetComponent()` が走ると、こういう参照で落ちる。

```csharp
if (componentType.Type == type && componentType.IsValid())
```

`componentType` 自体が `null` だと、ここで `NullReferenceException` になる。

## 入れた修正

今回は `VLiveKit_ThirdPartyUtilities` 側に、ローカル修正だと後から分かるコメントを入れたうえで、null 耐性を追加した。

触ったファイルはこの2つ。

```text
Packages/VLiveKit_ThirdPartyUtilities/Assets/toshi.VLiveKit/ThirdPartyUtilities/HierarchyDecorator/Scripts/Editor/Data/Types/ComponentGroup.cs
Packages/VLiveKit_ThirdPartyUtilities/Assets/toshi.VLiveKit/ThirdPartyUtilities/HierarchyDecorator/Scripts/Editor/Data/ComponentData.cs
```

`ComponentGroup.cs` では、serialized list や cache が null でも落ちないようにした。

```csharp
// VLiveKit local patch:
// Serialized HierarchyDecorator data can contain null component lists after package moves or Unity upgrades.
// Recreate the list here so default cache/query behavior can continue without editor-console errors.
private void ValidateCache()
{
    if (components == null)
    {
        components = new List<ComponentType>();
    }

    ...
}
```

`TryGetComponent()` でも、`type` や `componentType` が null の場合は単に見つからなかった扱いにした。

```csharp
if (type == null)
{
    component = null;
    return false;
}

...

if (componentType != null && componentType.Type == type && componentType.IsValid())
{
    component = componentType;
    return true;
}
```

`ComponentData.cs` 側では、Unity の deserialize 後に `unityGroups`、`customGroups`、`allCustomComponents` が null になっていても戻せるようにした。

```csharp
// VLiveKit local patch:
// Old HierarchyDecorator settings can deserialize with null groups after package moves or Unity upgrades.
// Keep the original behavior, but normalize those fields before querying or rebuilding the cache.
private void EnsureSerializedState()
{
    if (unityGroups == null)
    {
        unityGroups = new ComponentGroup[0];
    }

    if (customGroups == null)
    {
        customGroups = new List<ComponentGroup>();
    }

    if (allCustomComponents == null)
    {
        allCustomComponents = new ComponentGroup("All");
    }
}
```

## 修正後

赤エラーは消えて、`HierarchyDecorator components updated due to changes detected.` という警告だけになった。

![HierarchyDecorator の更新通知だけが残っている状態](/images/unity-hierarchydecorator-null-error-memo/hierarchydecorator-warning-only.png)

これは「Unity のコンポーネント一覧が変わったので、HierarchyDecorator の内部データを更新した」という通知なので、エラーではない。

毎回出てうるさい場合は `Debug.LogWarning(...)` を `Debug.Log(...)` に下げるか、ログ自体を消してもよさそう。ただ、今回は「更新が走ったこと」が見えるほうが安心だったので、そのまま残した。

## メモ

今回の修正は、HierarchyDecorator の見た目や機能を変えるものではなく、古い serialized data や package 移動後の null に耐えるための防御だけ。

あとで元実装との差分を見るときは、`VLiveKit local patch` で検索すれば、こちらで入れた意図つきの変更箇所が分かるようにした。
