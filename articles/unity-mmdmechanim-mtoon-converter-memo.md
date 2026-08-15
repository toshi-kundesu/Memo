---
title: "MMD4MecanimからMToonへ変換するときの材質・透明・描画順メモ"
emoji: "🧰"
type: "tech"
topics: ["unity", "mmd", "vrm", "mtoon", "shader"]
published: false
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

MMDモデルをUnityやVRM側へ持っていくとき、シェーダーを差し替えるだけでは元の見た目を保てないことがあります。
PMXやMMD4Mecanimのマテリアルには、色だけでなく、影、トゥーン、スフィア、輪郭、両面、透明、描画順といった「その材質をどう見せたいか」がまとまっているためです。

このメモでは、MMD4MecanimのMMDLit系マテリアルをMToonへ寄せるときに、何を読み取り、何を移し、何を変換できないものとして残すかを整理します。

## 先に結論

- MMDのマテリアルは、単なるテクスチャと色ではなく、描画意図のセットとして読む。
- `Transparent` という名前だけでBlendと決めず、テクスチャのalpha、Diffuseのalpha、RenderQueue、ZWriteを合わせて分類する。
- `Edge`、`BothFaces`、`NoShadowCasting`、`Tess` など、MMD4Mecanimのシェーダー名に含まれる機能も変換元の仕様として扱う。
- MToonでは、まず `alphaMode`、`transparentWithZWrite`、`doubleSided`、Outlineを確定し、その後に色や影を移す。
- `renderQueueOffsetNumber` は描画カテゴリを変える値ではなく、同じカテゴリ内の前後関係を調整する値として使う。
- ToonTex、SphereMap、Specularは一対一変換できない。無視せず、近い表現へ寄せたことをログに残す。
- 変換前のマテリアルとプロパティを保存し、何度でもやり直せる手順にする。

## MMDマテリアルは「描画意図の束」

MMD側で確認したい代表的な要素は次のとおりです。

| MMD / MMD4Mecanim側 | 主な役割 | MToon側で検討する場所 |
| --- | --- | --- |
| Diffuse / MainTex | 基本色とalpha | Base Color / Base Map |
| Ambient / ToonTex | 影側の明るさ、段階、色味 | Shade Color / Shade Texture / Shading Shift |
| Specular / Shininess | ハイライトの色と鋭さ | Rim、MatCap、必要なら独自拡張 |
| SphereMap | 加算または乗算の質感 | MatCap、Rim、必要なら独自合成 |
| Edge | 輪郭の有無、色、幅 | Outline |
| BothFaces | 両面表示 | `doubleSided` / Cull Off |
| マテリアル名と並び順 | 部位や重なりの意図 | 名前を維持し、Queue調整の手掛かりにする |

特にマテリアル名は、人間向けのメタデータとして役立ちます。`hairshadow`、`eye_hi`、袖、まつ毛、前髪などの名前があれば、透明や重なりの意図を推測しやすくなります。変換時に機械的な連番だけへ置き換えないほうが、後の調整が楽です。

## 「Transparent」は見た目ではなく描画分類

MMD4Mecanimのシェーダー名に `Transparent` が含まれていても、画面上で半透明に見えるとは限りません。

少なくとも次を分けて考えます。

1. alphaの閾値で抜くCutout / Mask
2. 半透明を重ねるBlend
3. 非表示に近い透明材質
4. alphaはほぼ1だが、描画順の都合でTransparentになっている材質

判定するときは、シェーダー名だけでなく次を見ます。

- MainTexのalphaチャンネル
- Diffuseまたは `_Color` のalpha
- RenderQueue
- RenderTypeタグ
- Blendの組み合わせ
- ZWriteとZTest
- alpha test / alpha blendのキーワード

見た目が不透明でもTransparentパスにいる材質は、ソート、Fog、Depth Prepass、被写界深度などではTransparentとして動きます。

## MMD4Mecanimのシェーダー名から読むこと

MMD4Mecanimでは、シェーダー名の組み合わせに機能が表れることがあります。

- `MMDLit`: 基本のライティング付き材質
- `Transparent`: 透明系の描画設定
- `Edge`: 輪郭パスあり
- `BothFaces`: 両面描画
- `NoShadowCasting`: 影を落とさない
- `Tess`: テッセレーションを使う派生

この名前は、移行後に同名の機能をオンにするためだけのものではありません。元の材質が何を期待していたかを知るための入力です。

### RenderQueueは局所的な前後関係も持つ

透明系では、同じTransparentカテゴリの中でもQueueの差が髪、瞳、ハイライト、服の重なりを支えていることがあります。全部を一律の値へ揃えると、元モデルでは成立していた順番が崩れます。

変換後の基本順序は、次のように考えると整理しやすいです。

1. OPAQUE
2. MASK / Cutout
3. BLEND + ZWrite
4. BLEND + ZWriteなし

たとえばプロジェクト都合で Opaqueを2225、Cutoutを2450、Transparentを3000へ置く実装はできますが、これは固定の正解ではありません。ハイライト用に3500などのカスタムQueueが使われていたなら、その意図と相対順を保存します。

MToonの `renderQueueOffsetNumber` は、このカテゴリ内の微調整に使います。OffsetだけでOPAQUEをTransparentへ移すような設計にはしません。

## MMD4Mecanim側で拾いたいプロパティ

シェーダーの実装差はありますが、調査時には次の項目を探します。

### ToonTexと影

- `_ToonTex`
- `_ToonTone`
- `_ShadowLum`
- セルフシャドウ関連のキーワード
- Ambient、Diffuse、ShadowColor相当の値

ToonTexは、MToonに同じ意味のスロットがあるわけではありません。Shade Color、Shade Texture、Shading Shiftへ近似するか、独自ランプとして残すかを決めます。

### SphereMap

- `_SphereCube`
- `SPHEREMAP_ADD`
- `SPHEREMAP_MUL`

加算スフィアはMatCapやRimへ寄せやすい一方、乗算スフィアは単純なMatCap置換では暗部の意味が変わることがあります。加算か乗算かを失わないようにします。

### Specular

- `_Specular`
- `_Shininess`
- `SPECULAR_ON`

MToonの標準表現には、MMDのSpecularと完全に同じ入力がありません。RimやMatCapへ近似する場合も、変換したことが分かるようにします。

### 描画状態

- RenderQueue
- RenderType
- SrcBlend / DstBlend
- ZWrite / ZTest
- Cull
- Polygon Offset
- alpha test / alpha blendキーワード

`Offset`、ZWrite、Cullは、色の移植より地味ですが、ちらつき、裏面消失、透明の破綻に直結します。

MMDモデルでは、袖の重なりや肌の上に置く薄いパーツを安定させるため、意図的にPolygon Offsetを使っている場合があります。変換時に一律で0へ戻さず、どの部位の重なりを解決していた値かを確認します。

## MToon側で先に決める項目

色を移す前に、材質の描画分類を確定します。

### alphaMode

- `OPAQUE`: 通常の不透明
- `MASK`: alpha閾値で抜く
- `BLEND`: 半透明合成

CutoutをBLENDへ変えると、輪郭が柔らかくなるだけでなく、ソートや深度の性質まで変わります。元が髪やまつ毛の抜き材質なら、まずMASKとして再現できるかを確認します。

### transparentWithZWrite

Transparentが深度を書くかどうかです。髪の重なりを安定させることがありますが、HDRPのFogや後段処理ではシルエットの原因にもなります。

詳細は [Unity HDRPのFogとTransparent Depthで詰まったときのメモ](./unity-hdrp-fog-transparent-depth-notes) に分けています。

### doubleSidedとCull

`BothFaces` は `doubleSided` やCull Offへ対応させます。ただし、負スケール、頂点の巻き順、薄い布、輪郭パスのCull反転が絡むため、変換直後に表裏を確認します。

### Outline

Edgeは、材質ごとの有無、色、幅を移します。輪郭は別パスで描かれるため、BaseのCullやZTestだけを合わせても同じ見た目にならないことがあります。

### Shade、Rim、MatCap、Emission

- Ambient / ToonTex / ShadowColorは、Shade ColorやShade Textureへ寄せる。
- SphereMapは、加算・乗算の意味を確認してMatCapまたはRimへ寄せる。
- Specularは、必要に応じてRim、MatCap、独自拡張へ寄せる。
- Emissionを使う場合、テクスチャ未設定時の既定色が白だと全体が発光する実装があるため、色と強度を明示する。

完全な一対一変換より、まず元に近い状態へ着地させ、差分を人が調整できることを優先します。

## コンバーターで保存したい値

シェーダーを差し替える前に、最低でも次を退避します。

- シェーダー名
- マテリアル名と並び順
- `_BlendMode`
- `_CullMode`
- `_SrcBlend` / `_DstBlend`
- `_ZWrite`
- `_ZTeForLiOpa` などのZTest値
- `_Color`
- `_MainTex`
- `_ShadeTexture`
- ToonTex、SphereMap、Specular、Edge関連の値
- RenderQueue
- RenderTypeタグ
- alpha test / alpha blend / Fogなどのキーワード

実装上は、元マテリアルの複製またはシリアライズしたスナップショットを残してから変換します。シェーダー差し替え後に元シェーダー固有のプロパティを読もうとしても、値を取り出せないことがあります。

## 変換手順

### 1. 元マテリアルを退避する

元アセットを上書きせず、変換前のマテリアル、シェーダー名、プロパティ、キーワード、Queueを保存します。

### 2. 描画カテゴリを分類する

Opaque、Mask、Blend + ZWrite、Blendの4群を基本にします。シェーダー名だけでなく、alpha、Blend、ZWrite、Queueを見て決めます。

### 3. MToonの描画状態を作る

`alphaMode`、`transparentWithZWrite`、`doubleSided`、Outline、QueueとOffsetを設定します。色より先にここを決めます。

### 4. 基本色と影を移す

DiffuseとMainTexをBaseへ、Ambient、ToonTex、ShadowColorをShade側へ寄せます。ShadowColorが既定のピンク寄りになるなど、変換後の初期値が混ざっていないか確認します。

### 5. Edge、SphereMap、Specularを近似する

EdgeをOutlineへ移し、SphereMapとSpecularはMatCap、Rim、独自拡張のどれで扱ったかを記録します。変換できないToonTexを黙って捨てないようにします。

### 6. 名前と相対順を維持する

部位名とマテリアル順を残し、Transparent内の相対Queueを復元します。

### 7. 差分をログに出す

次のような情報があると、後から原因を追いやすくなります。

- 元と変換後のシェーダー名
- 判定したalphaMode
- Queue、ZWrite、ZTest、Cullの変更
- ToonTex、SphereMap、Specularの変換先
- 変換できず既定値を使った項目

## よくある崩れ方

### 影色がピンクや灰色へ寄る

MToon側の既定Shade Colorが残っている可能性があります。元のAmbient、ToonTex、ShadowColorを確認します。

### 全体が明るすぎる

ToonTexやAmbientの影表現を捨てた、Emissionが有効になった、SphereMapの乗算を加算として移した、といった可能性があります。

### 髪や瞳の前後関係が壊れる

TransparentのQueueを一律にした、ZWriteを変えた、CutoutをBlendへ変えた可能性があります。マテリアル名と元の相対Queueを見直します。

### 裏面が消える、または輪郭だけ変になる

BothFacesとCull、負スケール、OutlineパスのCullを別々に確認します。

### alphaを変えても透明にならない

`_Color.a` だけでなくMainTexのalpha、alphaMode、キーワード、Blend、Cutoffを確認します。元シェーダーが `_Color.a * _MainTex.a` 以外の式を使っていた場合は、その閾値処理も保存します。

## 調査チェックリスト

1. 元マテリアルと変換前プロパティを保存したか。
2. マテリアル名と並び順を維持したか。
3. MMD4Mecanimのシェーダー名にある `Transparent`、`Edge`、`BothFaces`、`NoShadowCasting`、`Tess` を読んだか。
4. MainTexとDiffuseのalphaを確認したか。
5. RenderQueue、RenderType、Blend、ZWrite、ZTest、Cull、Offsetを確認したか。
6. CutoutとBlendを区別したか。
7. ToonTex、Ambient、ShadowColorをShade側へどう移したか。
8. SphereMapが加算か乗算かを確認したか。
9. SpecularとShininessをどう扱ったか。
10. Edgeの有無、色、幅とOutlineパスを確認したか。
11. BothFacesとdoubleSidedの対応を確認したか。
12. Transparent内の相対Queueを維持したか。
13. 変換できない項目をログへ残したか。
14. Fog、背景、カメラ距離を変えて確認したか。

## 変換処理はやり直せる形にする

コンバーターは、一度で完璧な見た目を作る道具というより、手作業で直せる初期状態を作る道具として考えると扱いやすいです。

- 元マテリアルを上書きしない。
- 変換先を別フォルダへ作る。
- Dry Runまたは変換内容の一覧を出す。
- 変換バージョンを記録する。
- 再変換時に手調整を上書きするか選べるようにする。
- 元のQueueとシェーダー状態へ戻せるようにする。

この形なら、モデルごとの例外が増えても検証と修正を繰り返せます。

## 関連記事

- [Unity HDRPのFogとTransparent Depthで詰まったときのメモ](./unity-hdrp-fog-transparent-depth-notes)
- [VRM/MMDをBlenderとUnityで往復するメモ](./vrm-mmd-blender-unity-animation-workflow-memo)
- [HDRP Toonで顔の影と髪影を調整するメモ](./hdrp-toon-custom-light-hairshadow-memo)
- [HDRPでVRM/MToonルックを調整するメモ](./vlive-character-shader-design-memo)

---

最終更新: 2026-08-15
