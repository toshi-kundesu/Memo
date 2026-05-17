---
title: "UnityでVRM/トゥーン系マテリアルを触るときの深度・Cull・透明のメモ"
emoji: "🧭"
type: "tech"
topics: ["unity", "vrm", "shader", "mtoon", "hdrp"]
published: false
---

[バーチャルライブ制作メモ目次](./vlive-production-memo-index) に戻る。

VRM やトゥーン系 shader の material を変換していると、見た目の崩れが全部同じに見えます。

目が抜ける、髪が前に出る、服が透ける、裏面だけ見える、遠くに行くと急に前面に出る、近距離でだけピンクになる。

症状だけ見ると「Cull が逆かも」「ZTest かな」「VRM の material 値が落ちたかも」と一気に疑いたくなります。けれど、ここは順番を決めて見たほうがかなり楽でした。

この記事は特定の shader 実装ではなく、Unity で VRM / MToon / トゥーン系 material を触るときの一般メモです。

## このメモの持ち帰り

- `Cull Back` は背面を捨てる。普通の外側から見る球なら、基本はこれで外側が見える。
- `Cull Front` と `Cull Back` を入れ替える前に、元 material の値と shader pass の `Cull` 指定を確認する。
- `Transparent` は「半透明になる設定」ではなく、Blend、ZWrite、ZTest、RenderQueue、sort、depth pass の組み合わせで決まる。
- VRM の material 値は見た目の仕様の一部なので、変換時はまず値を保つ。
- 近距離や遠距離でだけ壊れる場合は、clip space の `z` 操作、depth prepass、transparent depth、mipmap alpha、renderer sorting を疑う。

## まず描画をパスの列として見る

画面に出る絵は、material の inspector に見える値だけで決まっているわけではありません。

ざっくり見ると、次のような列で決まります。

1. mesh の頂点が vertex shader で clip space に変換される
2. `Cull` で描かない三角形が捨てられる
3. depth test で、すでにある深度に対して前か後ろかを判定する
4. 必要なら depth buffer に書く
5. fragment shader の色を blend して color buffer に書く
6. outline や transparent depth など、別 pass があれば同じ object がもう一度描かれる

なので「material の値は合っているのに見た目が変」というときは、値の移植ミスだけでなく、shader pass のどこかが別の規則で描いている可能性があります。

特にトゥーン系 shader は、本体、outline、shadow caster、depth only、depth normals、transparent depth prepass などで pass が増えがちです。1 箇所だけ正しくても、別 pass が違う `Cull` や `ZTest` で動くと、見た目は普通に崩れます。

## Cull は「見える面」ではなく「捨てる面」

`Cull` は、どちら側の面を描くかというより、どちら側の面を捨てるかの指定です。

ShaderLab では基本的にこう考えます。

- `Cull Back`: 背面を捨てる
- `Cull Front`: 表面を捨てる
- `Cull Off`: 表面も背面も捨てない

普通の球やキャラクター mesh を外側から見るなら、表面が見えてほしいので `Cull Back` が自然です。`Cull Front` にすると、外側の表面を捨てるので、内側だけ見えるような状態になります。

ただし、キャラクター asset では次の要因で「逆に見える」ことがあります。

- mesh の winding order が想定と違う
- parent transform に負の scale が入っている
- 両面前提の薄い板 mesh がある
- shader の outline pass だけ別の `Cull` を使っている
- depth normals や GBuffer など、想定外の pass が使われている

ここで converter 側で `Front` と `Back` を雑に入れ替えると、1 つの model では直っても別の model で壊れます。

まずやることは、元 material の `_CullMode` や double-sided 相当の値が、変換後の material に同じ意味で届いているかを見ることです。値が同じで見え方だけ違うなら、shader pass 側の `Cull [_CullMode]` 指定や、pass の選ばれ方を見ます。

## VRM では material 値も仕様の一部として扱う

VRM は「mesh と texture があるだけ」ではなく、キャラクターの見た目を再現するための material 情報を持っています。

VRM 0.x の MToon では、Unity material の property として見える値がかなり重要です。

- `_BlendMode`
- `_CullMode`
- `_Color`
- `_MainTex`
- `_ShadeTexture`
- `_Cutoff`
- `_OutlineWidthMode`
- `_OutlineWidth`
- `_OutlineColorMode`
- `_OutlineColor`
- `_EmissionColor`
- `_RimColor`
- `_RimLightingMix`
- `_RimFresnelPower`
- `_RimLift`

VRM 1.0 では `VRMC_materials_mtoon` として glTF material extension 側に整理されていますが、考え方としては同じです。base color、alpha、culling、outline、rim、shade などの値は、キャラクターの見た目を構成する情報です。

そのため shader 変換では、「変換先 shader のおすすめ値で作り直す」より先に、「元の値をそのまま通したときにどこまで再現されるか」を見たほうが切り分けしやすいです。

変換ツールで最初に守りたいのはこのあたりです。

- 元 material を直接壊さず、新しい material を作って差し替える
- 元 material への参照、または復元用 backup を残す
- `_BlendMode`, `_CullMode`, `_Color`, texture, cutoff, outline, rim を先に移す
- shader swap 後に Unity が落としがちな `RenderType` tag と keyword を戻す
- 最後に render queue や派生 property を整える

## 透明は Blend だけでは決まらない

透明 material は、`Blend` を設定しただけでは安定しません。

だいたい次の要素をセットで見ます。

- `Blend`: どう色を混ぜるか
- `ZWrite`: depth buffer に書くか
- `ZTest`: 既存 depth に対してどう判定するか
- `RenderQueue`: どのタイミングで描くか
- `RenderType`: Unity や pipeline が material をどう分類するか
- keyword: alpha test / alpha blend / transparent fog など
- renderer sorting: transparent renderer 同士の並び順

`Opaque` は depth に書けるので、前後関係が比較的安定します。

`Cutout` は alpha が閾値以下なら `clip` で捨てるため、穴あきでも depth に書きやすいです。髪の束やレースのように、透けるというより抜く material と相性がいいです。

`Transparent` は柔らかい半透明を作れますが、普通は `ZWrite Off` になります。depth に書かないので、transparent object 同士や、transparent と他 object の前後関係は renderer sorting の影響を強く受けます。

「距離が離れるほど前に出る」ように見える場合は、shader の色計算より先に、transparent sorting と depth pass を疑います。

## MToon 系の透明は Color alpha と texture alpha を見る

VRM 0.x の MToon では、透明感は texture alpha だけでなく material の color alpha にも入っていることがあります。

たとえば目の highlight や眉、顔まわりの薄い表現は、元 material では `Transparent` のまま、`_Color.a` と `_MainTex.a` の組み合わせで成立していることがあります。

このとき変換先で `_Color` を白不透明に戻してしまうと、texture は同じでも透明度が変わります。

逆に、`Transparent` がうまくいかないからといって全部 `Cutout` に寄せると、柔らかい highlight や透けた髪のニュアンスが消えます。検証では一時的に `Cutout` にして切り分けるのは便利ですが、最終的に元が `Transparent` なら、まず `Transparent` として再現する道を探したほうが安全です。

## ZTest と ZWrite は mode ごとに分けて見る

`ZTest LEqual` は、手前または同じ深度なら描く、という基本の指定です。

`ZTest Equal` は、すでに depth に同じ値が入っている場所だけ描くような使い方になります。depth prepass と組み合わせると便利ですが、透明や alpha cutout が絡むと急に扱いが難しくなります。

トゥーン系 shader では、次のような分け方が比較的わかりやすいです。

- `Opaque`: `ZWrite On`, `ZTest LEqual` または pipeline の設計に合わせる
- `Cutout`: `ZWrite On`, `ZTest LEqual`
- `Transparent`: color pass は `ZWrite Off`, `ZTest LEqual`
- transparent depth prepass: 必要な material だけ慎重に使う

透明 material に depth prepass を入れると、前後関係は安定することがあります。

ただし、柔らかい alpha texture で depth に広く書いてしまうと、見えないはずの部分が後続の描画を隠します。目の highlight、眉、髪の薄い先端などで「透明部分がピンクになる」「穴の部分が前に出る」ように見える場合は、depth pass が alpha を正しく捨てているかを確認します。

## RenderQueue は番号より意図を見る

Unity の render queue は、大まかには次のように扱われます。

- `Geometry`: 2000 付近
- `AlphaTest`: 2450 付近
- `Transparent`: 3000 付近

番号だけを丸暗記するより、「この material は depth に書いてよいのか」「他の transparent より前後どちらで描きたいのか」を見るほうがよいです。

VRM / MToon 変換では、元 material の render queue が細かく調整されていることがあります。shader swap 後に全部を同じ queue に揃えると、目、眉、髪、服の重なりが変わることがあります。

最初は元の queue を尊重し、変換先 shader 側で必要な範囲だけ補正するのが無難です。

## 近距離でだけ壊れるなら clip space の z を疑う

近距離、特に near clip plane 付近でだけ描画が割れる場合は、fragment の色よりも vertex 側の depth 操作を見ます。

outline や特殊 pass で `positionCS.z` を直接ずらしていると、カメラとの距離で急に見え方が変わることがあります。

特に注意したいのはこのあたりです。

- outline の頂点押し出し後に clip space z を触っている
- depth pass と color pass で違う z 補正をしている
- `TransformObjectToHClip` の入力に `float3` / `float4` が混ざっている
- near clip plane に近いときだけ depth が反転したように見える

「近づくとピンク」「離れると前面に出る」みたいな症状は、texture や alpha の値だけでは説明できないことが多いです。

## Outline は本体とは別の小さな shader として見る

トゥーン系 shader の outline は、本体 material の延長に見えますが、実際には別 pass です。

よくある outline は、頂点を法線方向に押し出して、背面側だけ描く構造です。そのため、本体とは `Cull` の考え方が逆っぽく見えることがあります。

outline で見る項目はこのあたりです。

- outline pass の `Cull`
- outline pass の `ZTest`
- outline pass の `ZWrite`
- outline 幅が world space か screen space か
- outline color が固定色か texture / lighting mix か
- alpha material でも outline を出すべきか

本体が壊れているのか、outline だけが壊れているのかを切り分けるには、一度 outline を完全に切るのが早いです。

outline pass で本体と同じ重い lighting include を全部読み込むと、sampler 数や variant の問題も出やすくなります。outline は outline として、必要な値だけを読む設計にしたほうが安全です。

## Debug は「値」「pass」「距離」の順で見る

見た目が崩れたときは、次の順で見ると迷いにくいです。

1. 元 material と変換後 material の値を比較する
2. `Blend`, `ZWrite`, `ZTest`, `Cull`, `RenderQueue`, `RenderType`, keyword を表にする
3. 本体 pass だけで見て、outline を切る
4. `Opaque`, `Cutout`, `Transparent` を 1 material ずつ試す
5. camera を近距離、通常距離、遠距離で動かして変化を見る
6. depth prepass / transparent depth / shadow caster を 1 つずつ切る
7. 最後に lighting と fog を見る

いきなり shader 全体を直そうとすると、直ったように見えて別の material が壊れます。

特に VRM は、顔だけでも skin、eye iris、eye highlight、mouth、眉、まつ毛、髪などが別 material になっていることがあります。1 つ直ったら、同じ renderer の material slot 全部を見ます。

## 変換ツールでやっておくと助かること

shader converter を作るなら、機能より先に復元性を入れておくと安心です。

- 元 material を直接書き換えない
- 変換後 material は毎回新規作成できるようにする
- 元 material への参照を残す
- 変換ログに、元 shader 名、変換先 shader 名、mode、queue、cull を出す
- すでに変換済みなら、元 material から再変換できるようにする
- debug mode として `Cull Off` / `Front` / `Back` を強制できるようにする
- outline、transparent depth、fog を個別に切れるようにする

最終的な見た目を作る前に、「元の値を保ったまま安全に戻せる」ことを確認しておくと、レンダリングの調整がかなり落ち着きます。

## 参考リンク

- [Unity Manual: ShaderLab culling and depth testing](https://docs.unity3d.com/ja/2019.4/Manual/SL-CullAndDepth.html)
- [UniVRM: MToon](https://vrm.dev/en/univrm/shaders/shader_mtoon/)
- [VRM specification: VRMC_materials_mtoon-1.0](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_materials_mtoon-1.0/README.md)

## おわり

VRM / トゥーン系 material の描画崩れは、見た目だけだとかなり怖いです。

でも、値、pass、depth、透明、距離の順で見ていくと、だいたいどこで起きているかは分けられます。

まずは元 material の状態を保つ。次に shader pass がその値を同じ意味で使っているかを見る。最後に、透明や outline の都合で必要な補正だけを足す。

この順番を守るだけで、「なんか全部おかしい」から「この pass のこの状態だけおかしい」まで落とし込めるようになります。

## 総目次

[バーチャルライブ制作メモ目次](./vlive-production-memo-index) に戻る。

---

最終更新: 2026-05-11

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
