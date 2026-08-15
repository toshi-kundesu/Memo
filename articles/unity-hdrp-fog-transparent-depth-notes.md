---
title: "Unity HDRPのFogとTransparent Depthで詰まったときのメモ"
emoji: "🌫️"
type: "tech"
topics: ["unity", "hdrp", "fog", "shader", "transparent"]
published: true
---

HDRPでキャラクターを描いていると、Fogを入れた瞬間に「透明の面が浮く」「シルエットだけがFogに出る」「背景があると見え方が変わる」といったことがあります。

原因はalphaだけではありません。HDRPのFogがどの深度を参照するか、Transparentがどのパスで描かれるか、Depth Write、Depth Prepass、ZTest、RenderQueue、Receive Fogがどう組み合わさっているかを見る必要があります。

MMD4MecanimやMToonから変換した材質で起きやすい例も含めて、調査の順序をまとめます。

## 先に結論

- HDRPのFogは、Opaque向けの深度ベース処理と、Transparent材質自身が受ける処理を分けて考える。
- Transparent材質が深度を書くと、後段のFogから「そこに面がある」と見え、透明面のシルエットが出ることがある。
- alphaが1でも、Transparentパスで描かれている限りOpaqueと同じ挙動にはならない。
- Receive Fogを切ることと、Depth WriteやTransparent Depth Prepassを切ることは別の操作。
- ZTestが `Equal` のTransparentは、対応する深度が先に書かれていないと消える。通常のTransparentやCutoutでは `LEqual` を基準に確認する。
- RenderQueue、Blend、ZWrite、ZTest、Cull、RenderType、キーワードを一つの描画状態として確認する。
- 変換処理では元の状態を保存し、Queueや独自alpha式を機械的に潰さない。

## HDRPのFogは一枚岩ではない

HDRPのFogはVolumeで設定しますが、描画の観点では少なくとも次の2経路を分けて考えます。

1. カメラから見たOpaqueの深度を使い、後段で合成されるFog
2. Transparent材質が自身のForward Pass内で受けるFog

Opaque向けFogはDepth BufferやDepth Pyramidにある距離を見ます。Transparentは、材質設定やシェーダーの実装に応じてFogを受けるか、深度を書くかが変わります。

この違いをまとめて「Fogが変」と考えると、Receive Fogだけを切って直らない理由が分かりにくくなります。

## Transparentが深度を書くとFogに面が見える

Transparent材質でも、Depth WriteやTransparent Depth Prepassを使えば深度バッファへ値を書けます。これは透明同士の前後関係や後段処理を安定させるために便利です。

一方で、Fogや後段処理からは「その深度に面がある」と読まれます。色がほぼ透明でも、深度だけが残れば、次のような症状になります。

- 髪、瞳、ハイライトなどの透明面がFogのシルエットとして浮く。
- 透明面の奥にあるキャラクターが不自然にFogで区切られる。
- 手前の透明面だけが残り、奥の透明面が消える。
- alphaを変えてもシルエットが消えない。
- 見た目はOpaqueに近いのに、FogだけTransparentらしい崩れ方をする。

この場合、原因は「透明度」ではなく「透明面が深度を書いていること」です。

## Receive FogとDepth Writeは別物

HDRPのTransparent材質でReceive Fogを無効にしても、その材質がDepth Prepassで深度を書いていれば、Opaque向けFogや他の後段処理へ影響することがあります。

確認する項目は次のとおりです。

- Surface Type
- Blend Mode
- Receive Fog
- Depth Write
- Transparent Depth Prepass
- Transparent Depth Postpass
- RenderQueue
- ZTest
- Cull

Fogの問題では、Receive Fogだけでなく「どのパスが、いつ、どんな深度を書いているか」まで見ます。

## alphaが1でもTransparentはTransparent

Transparent Surface Typeで描かれている材質は、alphaが1でも描画分類としてはTransparentです。見た目が不透明に近いことと、Opaqueパスで描かれることは別です。

この違いは次に影響します。

- Fog
- Depth Pyramid
- Transparent sorting
- Transparent Depth Prepass / Postpass
- 被写界深度
- SSRや反射
- Motion Vector

「alphaを255にしたからOpaqueと同じ」とは考えず、Surface Typeとパスを確認します。

## 背景があるとFogの見え方が変わる理由

Fogのシルエット問題では、背景にOpaqueオブジェクトがあるかどうかで症状が変わることがあります。

背景がなければ、空や遠方の深度に対してFogが強く乗ります。背景があれば、そのOpaque面の深度がFogの基準になります。そこへTransparentの深度が部分的に入ると、透明面の形がFogの境界として見えます。

そのため、「背景があると直る」場合でも、Transparent材質が正しくなったとは限りません。深度バッファの内容が変わった結果、症状が目立たなくなっているだけかもしれません。

## Transparent Depth Prepassは万能ではない

Transparent Depth Prepassは、Transparentの色を描く前に深度を書く機能です。ソートや後段処理を助けますが、キャラクター材質では副作用もあります。

- Transparent面がFogのシルエットになる。
- 奥のTransparentが消える。
- 前髪だけが残り、重なりが壊れる。
- 髪や瞳の内部が不自然に隠れる。
- Depth of FieldやSSRに意図しない面が読まれる。

髪、瞳、まつ毛、ハイライト、MMD由来の半透明パーツのように、薄い面が見た目の一部になっている材質では特に注意します。

## ZTestのEqualとLEqual

シェーダー変換時に見落としやすいのがZTestです。

`Equal` は、すでに深度バッファに書かれた値と一致する場所だけを描きます。Depth Prepassと組み合わせるPassでは有効ですが、対応する深度が先に書かれていないTransparentへそのまま適用すると、瞳やハイライトが消えることがあります。

Unityの比較関数の数値では、一般に `Equal` が3、`LessEqual` が4です。元シェーダーの `_ZTeForLiOpa` などをコピーする場合、値だけでなく、どのPass向けの設定かを確認します。

確認の基準は次のようにします。

- 通常のTransparent: まず `LEqual` で確認する。
- Cutout / Mask: まず `LEqual` で確認する。
- OpaqueのLighting Pass: Depth Prepassが確実にある場合だけ `Equal` を検討する。
- Outline: Baseとは別にZTest、ZWrite、Cullを確認する。

Opaque向け最適化の `Equal` を、TransparentやCutoutへ一律にコピーしないようにします。

## MToonとMMD4Mecanimから変換するときの注意

MToonでは `alphaMode`、`transparentWithZWrite`、`renderQueueOffsetNumber`、`doubleSided`、Outlineなどが描画状態を作ります。MMD4Mecanim側では、シェーダー名の `Transparent`、`Edge`、`BothFaces` と、RenderQueue、ZWrite、Offset、Cullなどに同じ意図が分散しています。

変換時は、次の順序を基準に分類します。

1. OPAQUE
2. MASK / Cutout
3. BLEND + ZWrite
4. BLEND + ZWriteなし

`renderQueueOffsetNumber` は同じカテゴリ内の前後調整に使います。ハイライトや瞳のQueueが独自に3500へ置かれているなど、元の相対順に意味がある場合は保存します。

材質の意味とプロパティ対応は [MMD4MecanimからMToonへ変換するときの材質・透明・描画順メモ](./unity-mmdmechanim-mtoon-converter-memo) にまとめています。

## シェーダー差し替え前に保存する

変換前に、元マテリアルまたは値のスナップショットを残します。

- `_BlendMode`
- `_CullMode`
- `_SrcBlend` / `_DstBlend`
- `_ZWrite`
- `_ZTeForLiOpa` などのZTest
- `_Color`
- `_MainTex`
- `_ShadeTexture`
- RenderQueue
- RenderTypeタグ
- `_ALPHATEST_ON`
- `_ALPHABLEND_ON`
- `_ENABLE_FOG_ON_TRANSPARENT`
- その他のシェーダーキーワード

簡単な変換例として Opaqueを2225、Cutoutを2450、Transparentを3000へ置くことはできます。ただし、値そのものより分類と相対順を守ることが大切です。元が3500などの意図的なQueueなら、3000へ丸めずに理由を確認します。

## alpha式を単純化しすぎない

元シェーダーのalphaが常に `_Color.a * _MainTex.a` とは限りません。Cutoff、材質係数、頂点色、独自マスクなどを組み合わせていることがあります。

変換後に輪郭が欠ける、Cutoutが太る、透明部が残る場合は、元のalpha式と閾値を確認します。`_Color` のalphaも保存します。

また、Forwardのalpha blendへディザclipをそのまま入れると、半透明の代わりに粒状の欠けが出ます。ディザ透明が必要なPassと、通常のBlendを分けます。

## Transparent Fogのキーワード

カスタムシェーダーでは、TransparentがFogを受ける処理を `_ENABLE_FOG_ON_TRANSPARENT` のようなキーワードで分岐している場合があります。

コンバーターがキーワードを有効にした結果、変換前にはなかったFogが乗ることもあります。Receive FogのUIだけでなく、実際に有効なキーワードとPass内のFog処理を確認します。

## Cull、負スケール、Passの違い

Cullの不具合に見えても、原因が頂点の巻き順や負スケールである場合があります。

- `Cull Back`: 通常の表面を描く。
- `Cull Front`: 反対側を描く。Outlineの押し出しPassなどで使う。
- `Cull Off`: 両面を描く。

Baseの面が消えるからといって、すぐCullを反転すると、Outlineや影Passが逆になることがあります。まずRendererのTransform、メッシュの巻き順、`BothFaces` / `doubleSided`、各PassのCullを分けて見ます。

特にOutlineは別Passなので、次を独立して確認します。

- Cull
- ZTest
- ZWrite
- 幅
- 色とalpha
- RenderQueueまたは描画タイミング

## 距離で崩れる場合はclip-spaceも見る

カメラ距離やNear Clipを変えたときだけ面が欠けるなら、材質値だけでなく頂点シェーダー内のclip-space z操作も疑います。

Outlineや前後オフセットのために `positionCS.z` を直接動かす実装は、Near Clip付近、反転Z、投影方式によって意図しない判定を起こすことがあります。

次を変えて症状を比較します。

- カメラ距離
- Near Clip Plane
- FOV
- Outline幅
- Polygon Offset
- ZTest

値、Pass、距離依存の順に切り分けると原因を見つけやすくなります。

## 調査の順序

Fogや透明が崩れたときは、次の順で確認します。

1. 問題のRenderer、Material、SubMeshを特定する。
2. alphaではなくSurface Type / alphaModeを見る。
3. RenderQueueと相対順を見る。
4. Blend、RenderType、キーワードを見る。
5. Depth Writeを見る。
6. Transparent Depth Prepass / Postpassを見る。
7. ZTestが `Equal` か `LEqual` かを見る。
8. Receive FogとTransparent Fogキーワードを見る。
9. 背景の有無で症状が変わるかを見る。
10. 深度を書かない設定で症状が変わるかを見る。
11. Cull、負スケール、頂点の巻き順を見る。
12. Outlineなど別Passを一つずつ止める。
13. カメラ距離、Near Clip、FOVを変える。

デバッグ用シェーダーやMaterial Inspectorで、現在のQueue、ZWrite、ZTest、Cull、キーワードを表示できるようにしておくと調査が速くなります。

## 実装・コンバーター側のチェックリスト

- 元マテリアルを上書きせず、変換前の値を保存する。
- Opaque、Mask、Blend + ZWrite、Blendを明示的に分類する。
- 見た目が不透明でもTransparentパスなら別物として扱う。
- Transparentを無条件にDepth Prepassへ入れない。
- Fogを受ける処理と、深度を書く処理を分ける。
- Opaque向けの `Equal` をTransparentへ流用しない。
- Custom Queueと相対順を保存する。
- RenderTypeとalpha/Fogキーワードを状態に合わせる。
- BaseとOutlineのCull、ZTest、ZWriteを別々に設定する。
- 変換ログへ元と変換後の描画状態を出す。

## まとめ

HDRPのFogでTransparentのシルエットが出る問題は、alphaだけでは説明できません。TransparentがどのPassで描かれ、いつ深度を書き、その深度をFogや後段処理がどう読むかが重要です。

まずSurface Type、RenderQueue、Depth Write、Transparent Depth Prepass、ZTest、Receive Fogを並べて確認します。MMD4MecanimやMToonから変換した材質なら、元のBlend、Cull、Queue、キーワード、alpha式を保存できているかも確認します。

## 参考

- [Unity HDRP Surface Type](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4015.0/manual/Surface-Type.html)
- [Unity HDRP Fog Volume Override reference](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4016.0/manual/fog-volume-override-reference.html)

---

最終更新: 2026-08-15
