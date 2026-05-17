---
title: "バーチャルライブ制作メモ総目次: Unity照明/カメラ/モキャプ記事一覧"
emoji: "🌾"
type: "idea"
topics: ["unity", "vlive", "lighting", "camera", "mocap"]
published: true
---

バーチャルライブまわりで調べたこと、詰まったこと、あとで記事にしたいことを、目次だけでも見返せるように整理しています。

自分用にメモしていた備忘録がベースなので、体系的な教科書というよりは「このへんで一回詰まった」「ここはあとで困りそう」「あとで見返したい」を分野ごとに圧縮した置き場です。

内容は、手元で試した範囲に寄っています。
自分の環境や観測範囲ベースのメモとして読んでもらえると嬉しいです。

ここしばらく色々作っていたので、自分でも見返せるように、いったん分類して整理しています。
一つ一つがライブのクオリティ向上のために研究した内容のつもりです。

まだ記事になっていない項目も多いので、書いたら順次リンクを貼る予定。

この記事が制作メモ全体の総目次です。各記事の先頭と末尾から戻れるようにしています。

実践リポジトリはVLiveKit系として整理中です。公開リンクがあるコードやサンプルは、関連する記事の本文中に挟んでいきます。

## この目次の持ち帰り: 技術名だけでなく制作の分野ごとに迷子を減らす

- 外部制御、カメラ、LED/VJ、ポスプロ、キャラルック、モキャプ、ステージ小物を分けて見る。
- 目次の見出しだけで、どの詰まりがどの分野に入るかを思い出せるようにする。
- 記事化したものは、検証リポジトリや本文中の関連リンクまで辿れる入口として整理する。

## GitHubは検証リポジトリと記事導線として見る

検証リポジトリやサンプルは [GitHubのVLiveKit系リポジトリ](https://github.com/toshi-kundesu?tab=repositories&q=VLiveKit) に整理中です。
READMEを見て、もう記事にできそうなものだけ各カテゴリに混ぜています。

## 外部制御はUnityを全部入りにせず信号を受ける設計で見る

ArtNet、DMX、OSC、Timecodeなど、外部信号でUnityを動かす系。
Unityを全部入りの制御アプリにするより、外から信号を受けるレンダリングマシンとして扱う方向が何かと便利な気がしています。

- [個人でバーチャルライブを作るときの技術メモ: ArtNet/NDI/Timecode/SSS](./vlive-technical-input-map-memo)
- [Unityライブ照明をArtNet/DMXで外部制御するメモ](./vlive-lighting-main-subject-memo)
- [Unity ArtNet受信はPrefabに寄せる: DMX RECとTimeline再現メモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [UnityはLTCを直接読まずOSCで受ける: TouchDesigner/Timecode同期メモ](./unity-timecode-touchdesigner-osc-memo)
- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)

## カメラは見せるカットと書き出し安定化を分けて考える

カメラ、Cinemachine、Recorder、DOF、スイッチング、素材書き出し系。
ライブっぽい画を作るためのカメラ挙動と、書き出し後に確認しやすくするためのレンダリング安定化をここに入れたい。

- [バーチャルライブのカメラワークを決めカットと音で考えるメモ](./live-camera-direction-memo)
- [Unity Cinemachineでライブカメラを整理する: Timeline/Recorder技術メモ](./unity-cinemachine-live-camera-memo)
- [UnityRecorderの仕様についてのメモ](./unity-recorder-audio-frame-sync-memo)
- [バーチャルライブのカメラ反応メモ: 手元/足元/バックショット/実在感](./vlive-camera-audience-reaction-memo)
- [Unityライブカメラ検証メモ: CameraCrossFade/ViewFinder/Composite](./unity-photography-camera-tools-memo)
- Cinemachineでクロスフェードするメモ
- ライブの前ツラレールカメラと広角レンズのメモ
- カメラの注視点、慣性、フォーカス挙動のメモ
- 限られたカメラ素材から画のバリエーションを作るメモ

## LED/VJは背景映像をUnityでどこまでプレビューするかを決める

LED、VJ、映像投影、NDI、UV転写、スクリーン系。
ライブで背景映像やLEDをどう扱うか、Unity内でどこまでプレビューするかを整理する場所です。

- [Unity LED/VJ検証: Spout/NDI/KodeLife/DMXで背景映像を扱うメモ](./vlive-led-vj-information-memo)
- [Unity LEDVisionシェーダ検証: 板ポリ/ShaderGraph/VJ/DMXメモ](./unity-ledvision-shadergraph-vj-memo)
- NDIで外部映像をUnityに持ち込む検証
- PlaneからUV転写してLED投影するシェーダーメモ
- VJと照明を連動させたいメモ
- LEDスクリーン用シェーダーとLTCGI連携のメモ

## ポスプロはUnity内とAE後処理の分担を早めに見る

ポスプロ、カラグレ、Bloom、Diffusion、VHS、レンズっぽい処理。
Unity内でやるもの、AEなど外に出した方がよさそうなもの、両方の検証を入れる場所。

- [Unity/After Effectsポスプロ検証: ライトラップ/グレイン/色収差メモ](./vlive-postprocess-purpose-memo)
- Diffusion、キャラBloom、Custom Post Processのメモ
- VHS処理をHDRPに移植したいメモ
- 色収差、グレイン、ライトラップのメモ
- 参考映像の色をそのまま持ち込むと弱く見えがちな話
- Unity素材をAEで合成してみるメモ

## キャラルックはライブ照明の中でデザインの見え方を調整する

VRM、MMD、MToon、HDRP、Toonシェーダ、キャラルック系。
ライブ照明の中でキャラのデザインの見え方を調整する検証が多いです。

- [HDRPでVRM/MToonルックを調整する: ライブ照明向けキャラシェーダメモ](./vlive-character-shader-design-memo)
- [UnityでVRM/トゥーン系マテリアルを触るときの深度・Cull・透明のメモ](./unity-vrm-toon-depth-cull-transparent-memo)
- [Unity Toonシェーダーギミックメモ: 法線球面化/ライト方向制限](./unity-toon-normal-light-direction-memo)
- [HDRP Toonで顔の影と髪影を調整する: Custom Light/HairShadow/Stencilメモ](./hdrp-toon-custom-light-hairshadow-memo)
- [VRM/MMDをBlenderとUnityで往復する: Humanoid/AutoRigPro/揺れものメモ](./vrm-mmd-blender-unity-animation-workflow-memo)
- [MMDMechanimからMToonへシェーダを変換する: ShadowColor/Outline/SphereMapメモ](./unity-mmdmechanim-mtoon-converter-memo)
- [Unityキャラルック検証: 軽量SSS/髪の異方性反射/肌Bloomメモ](./unity-lightweight-sss-hair-anisotropy-memo)
- LiveToonの実装済み機能を整理するメモ
- HairShadowで顔ののっぺり感を減らす
- フィギュアっぽく見える定番ミスのメモ
- 視野角打ち消しと接地ズレ回避のメモ

## モキャプ/フェイシャルは照明より先に主役の見え方を決める

モキャプ、フェイシャル、ポージング、リグ、視線、アクト系。
照明や背景が良くても、ここが弱いと主役が寂しく見える、というメモがかなり多いです。

- [キャラクターライブの顔まわりを見る: フェイシャル/視線/決めポーズメモ](./vlive-character-expression-memo)
- [キャラクターアニメーションの短尺検証: 決めカット/余韻/フェイシャルメモ](./vlive-character-animation-keycut-memo)
- [Unityフェイシャルセレクター検証: 下瞼/口移動/左右非対称メモ](./vlive-facial-selector-shape-design-memo)
- [キャラライブのアクトをダンス量だけで考えない: 客席煽り/手拍子/移動メモ](./vlive-act-is-not-only-dance-memo)
- [Unityキャラ自動アニメーション: 呼吸/目パチ/EyeDirtの実装メモ](./unity-character-auto-animation-scripts-memo)
- [Unity uLipSync/OSCでリップシンクを送受信する: FacialReceiver/BlendShapeメモ](./unity-ulipsync-osc-facial-control-memo)
- ライブ用キャラモーションで「決めカット」が大事だと思ったメモ
- フェイシャルがないと寄りカメラが映えない問題
- 下瞼、口の横幅、まつ毛、視線が効く話
- 肩まわりの動きがキャラ表現に効くメモ
- 慣性式モキャプの小型機材検証メモ
- 呼吸、まばたき、視線、BlendShapeゆらぎ制御のメモ

## ステージ小物はキャラと背景だけの画にライブの場所を足す

ステージ、特効、小物、Video Rack、テストシーン系。
ライブっぽい空間や、検証用の最小シーンを作るための素材置き場に近いです。

- [Unityステージ制作メモ: トラス/床/バミリ/MVR/Vectorworks](./vlive-stage-composition-memo)
- [バーチャルライブ小物メモ: マイク/ケーブル/黒い備品の場所感](./vlive-live-props-scale-memo)
- [MVR/VectorWorksからUnityへ配灯を持ち込む: 灯体Prefab置換とMagicQ CSVメモ](./unity-mvr-vectorworks-fixture-prefab-memo)
- [Unity VFX Graphでライブ特効を作る: CO2/スモーク/スパークラー/Six Way Smokeメモ](./unity-vfxgraph-live-special-effects-memo)
- ライブ小物を作っておくと画の場所感が出るメモ
- ステージの幕、トラス、バミリ、大黒幕まわり
- テスト用ライブシーンを作るメモ

## 検証環境は小さいsandboxと再利用アセットに分ける

実験場、テストアセット、外部ライブラリ置き場。
まだ記事にするほどではないけど、後から効いてきそうな小さい検証はここに溜まりそうです。

- 小さい検証をsandboxに分ける扱い方メモ
- FrameRate、AudioListener、HDRI、UVチェック用アセットのメモ
- 外部ライブラリや配布物の扱いメモ
- GitHub ActionsやScoped Registryでパッケージ化するメモ

## ゲーム研究は色/キャラデザイン/AE処理をライブに戻す

ゼンレスゾーンゼロなどを入口に、色、キャラデザイン、AE処理、視線誘導をライブ画面に戻して見る場所です。

- [ゼンゼロ風ルック研究メモ: 色/キャラ/AE合成の分解](./vlive-game-look-research-memo)
- ゼンゼロ的な彩度/コントラスト/差し色を見るメモ
- 参考ルックをそのまま移植すると弱くなる話

## 詰まりは技術より段取りや基準の不足で起きることがある

ジャンルに分けにくい、検証の進め方や考え方。
複雑な技術問題に見えて、実際は段取り、基準、検証不足、素材管理で詰まっているだけ、みたいな話もここ。

- [バーチャルライブ制作のUnity/照明/カメラ/モキャプ入口メモ](./vlive-beginner-input-memo)
- [バーチャルライブ個人制作の修正順メモ: 加点より減点回避を見る](./vlive-production-risk-minus-fix-memo)
- [キャラライブの修正ポイント: 揺れもの/めり込み/雑に見える処理メモ](./vlive-character-care-minus-points-memo)
- 技術的にすごいことと、ライブとして良いことは別というメモ
- YouTube配信ライブがライブ映像の入口になりうる話

## おわり

いったん、記事化できる単位と、これから掘る候補を並べました。
この目次自体も、記事やGitHub側の整理が進んだら更新していきます。

## 総目次

この記事が制作メモ全体の総目次です。各記事から戻れる入口として更新していきます。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
