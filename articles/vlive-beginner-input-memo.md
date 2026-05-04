---
title: "バーチャルライブ制作のUnity/照明/カメラ/モキャプ入口メモ"
emoji: "🎧"
type: "idea"
topics: ["vlive", "unity", "lighting", "camera", "mocap"]
published: true
---

バーチャルライブを作ろうとすると、「Unityを触ればライブっぽいものが作れる」というより、ライブっぽく見えるために見ておくものがかなり多いな、となる。

自分も最初は、シェーダ、ポスプロ、照明、カメラ、モーション、外部制御が全部ごちゃっと見えていて、何からインプットすればいいのか分かりづらかった。
なので、自分が最初に知りたかったことを棚卸しするメモとして書いておきます。

体系的な入門というより、「このへんを知っていると、あとで詰まったときに検索しやすい」くらいの温度感です。

関連する制作メモ全体は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

実践リポジトリはVLiveKit系として整理中です。公開リンクがあるコードやサンプルは、本文中の関連する項目に挟んでいます。

## この記事の持ち帰り: 最初はツール名よりライブを分解する言葉を増やす

- Unityだけを見る前に、照明、カメラ、VJ、特効、モーション、書き出しを別の棚として見る。
- `DMX`、`Art-Net`、`OSC`、`Timecode`、`Timeline`、`Cinemachine`、`Recorder` は、後から検索するための入口になる。
- GitHub、YouTube、X、雑誌のリンクは、操作手順より「どういう画や構成を見ていたか」を掴むために使う。

## 最初はソフトの操作よりライブを分解する名前がほしい

ざっくり言うと、最初に見るのはこのあたり。

- 実際のライブ映像
- 照明、LED、VJ、特効の役割
- ダンス、アクト、フェイシャル
- カメラワーク、スイッチング、編集
- DMX、Art-Net、OSC、Timecodeみたいな外部制御
- ステージ図面、トラス、幕、小物
- Unity側のTimeline、Cinemachine、Recorder

全部を深くやる必要はない。
ただ、名前だけ知っているだけでもかなり違う。
Unityだけで考え込む前に、「そういう分野や道具があるんだ」と分かるだけで、あとから調べやすくなる。

まずは全部を理解しようとするより、あとから検索できる名前を少しずつ増やしていくくらいでいいと思っている。

## ライブ映像は1フレームではなく1曲の流れで見る

最初のインプットとしては、好きなライブ映像を1曲通して何回か見るのがかなり強いと思っている。

ただし「かっこいいな」で終わらせず、見る場所を分ける。

- 主役はどのタイミングで寄られているか
- 引きの絵はどこで入るか
- 照明は歌詞やビートとどう合っているか
- LEDや背景映像は主役を食っていないか
- カットが切り替わる前に、キャラ側に決めがあるか
- スモークや特効はどの瞬間だけ強くなるか

Unity上の見た目だけ見ていると、どうしても「良い1フレーム」を作りたくなる。
でもライブは時間の中で見るものなので、1曲の流れ、カメラの切り替わり、照明の山、主役の見せ場を見た方がよさそう。
ここを飛ばすと、背景も照明も派手なのに、なぜかライブとして弱い、みたいなことが起きる。

ライブ映像は、1フレームの参考というより、時間の流れ、カメラ、照明の山を見る素材として使うとかなり見方が変わる。

照明、カメラ、配信ライブの空気感は、文章だけより動画を1曲単位で見る方が早い。

https://www.youtube.com/watch?v=J73YuUzgT6A

https://www.youtube.com/live/GTa2HxIsBPM?si=sYdvBQ3KDP5zCAGB&t=8092

https://www.youtube.com/watch?v=k-dSrQeNKzo

## DMX/Art-Net/OSC/Timecodeを知るとUnityの外側が見える

照明まわりは、最初から全部を理解しようとするとかなりつらい。
ただ、名前だけでも先に知っておくと、後で検索しやすくなる。

最低限、こういう単語を入口として置いておく。

- DMX512
- Art-Net
- sACN
- OSC
- LTC / MTC / Timecode
- Universe
- Fixture / Patch
- Cue / Playback

Unityでライブっぽいものを作ろうとすると、ついUnityの中に全部の制御UIを作りたくなる。
でも実際には、照明卓、TouchDesigner、VJソフト、音声再生、Timecodeなど、外からUnityを動かす考え方がかなり自然な場面がある。

試していると、Unityは全部入りの制御アプリというより、Art-NetやOSCを受けるレンダリングマシンとして扱う方が整理しやすかった。

このあたりは、まずDMX、Art-Net、OSC、Timecodeという入口を知っておくだけでも、あとで調べやすくなる。

MagicQやArtNetは、ポストや動画から眺めてから触る方が入りやすい。

https://x.com/orangecafe_/status/1729870376262615535

外部制御まわりは、実装寄りのメモを分けています。
まずはArtNetをUnityで受ける話、TimecodeをOSCで受ける話、MagicQで打ち込みを効率化する話を別々に見ると混ざりにくいです。

- [Unity ArtNet受信はPrefabに寄せる: DMX RECとTimeline再現メモ](./unity-artnet-prefab-receiver-dmx-rec-memo)
- [UnityはLTCを直接読まずOSCで受ける: TouchDesigner/Timecode同期メモ](./unity-timecode-touchdesigner-osc-memo)
- [MagicQで照明打ち込みを効率化する: TC REC/SpeedMaster/FXプリセットメモ](./magicq-lighting-speedmaster-workflow-memo)

## 寄りカメラに耐える顔と決めポーズを見る

キャラクターライブだと、シェーダや照明を頑張る前に、主役の動きと顔がかなり効く。

短い尺で試しても、寄りカメラになった瞬間にフェイシャルが弱いと、かなり寂しく見えることがあった。
下瞼、口の横幅、視線、まばたき、肩まわり、髪や服の揺れもの、めり込み。
細かいけど、観客側にはけっこう見える。

なので、ダンス動画やライブ映像を見るときも、足運びだけでなく、

- どこで目線を送っているか
- どこで口元や表情が変わるか
- 決めポーズの前後にどれくらい余韻があるか
- 肩、首、手先が止まりすぎていないか

を見る。

技術的にはモキャプ、手付け、フェイシャルキャプチャ、BlendShape制御の話になるけど、まずは「何が弱いとライブとして寂しく見えるか」を見ておく方が効く。

キャラライブを見るときは、顔、視線、肩、手先、決めポーズの見え方を分けて観察しておくと、あとで制作にも戻しやすい。

キャラクター表現まわりの話は、もう少し分けて [キャラクターライブの顔まわりを見る: フェイシャル/視線/決めポーズメモ](./vlive-character-expression-memo) に切り出しました。

顔、アクト、減点回避は別の記事に分けました。
寄りカメラで弱く見えるところ、表情のシェイプ設計、揺れものやめり込みの確認は、同じキャラ表現でも見る場所が違います。

- [Unityフェイシャルセレクター検証: 下瞼/口移動/左右非対称メモ](./vlive-facial-selector-shape-design-memo)
- [キャラライブのアクトをダンス量だけで考えない: 客席煽り/手拍子/移動メモ](./vlive-act-is-not-only-dance-memo)
- [キャラライブの修正ポイント: 揺れもの/めり込み/雑に見える処理メモ](./vlive-character-care-minus-points-memo)

## カメラはCinemachineの前に「何を見せるか」を決める

カメラは、UnityのCinemachineを覚える話でもあるけど、それ以前に「どこを見せるか」の話が大きい。

ライブ映像を見ていると、寄り、引き、横移動、客席越し、足元、手元、表情、ステージ全体、みたいな役割が分かれている。
全部を同じテンションで動かすと疲れるし、全部を寄りにすると空間が消える。

カメラ台数も素材も限られるので、最初から大量のカメラを作るより、まずは「この1曲で絶対に見せたいカット」を決める方が効く。

Unity側では、Timeline、Cinemachine、Recorderあたりの名前を知っておくと、後から調べやすい。
特にRecorderは、早めに短い尺で試して慣れておく。
実際に書き出してみると、フレーム、音、画質設定、ファイル形式あたりの感覚が少し掴める。

カメラはCinemachine、時間管理はTimeline、書き出しはRecorderを、まず短い尺で触ってみると感覚を掴みやすい。
カメラの方向性の話は [バーチャルライブのカメラワークを決めカットと音で考えるメモ](./live-camera-direction-memo)、CinemachineやTimeline側の技術メモは [Unity Cinemachineでライブカメラを整理する: Timeline/Recorder技術メモ](./unity-cinemachine-live-camera-memo) に切り出しました。

カメラの反応メモとRecorderの書き出しメモも分けています。
寄りの量より実在感を見る話と、フレーム/音ズレを見る話は、同じカメラ棚でも分けた方が読みやすいです。

- [バーチャルライブのカメラ反応メモ: 手元/足元/バックショット/実在感](./vlive-camera-audience-reaction-memo)
- [UnityRecorderの仕様についてのメモ](./unity-recorder-audio-frame-sync-memo)
- [Unityライブカメラ検証メモ: CameraCrossFade/ViewFinder/Composite](./unity-photography-camera-tools-memo)

照明、ステージ、小物、キャラクターアニメーション、シェーダー、ポスプロ、LED/VJ、ゲーム研究の見方は、それぞれたたき台として切り出しました。
技術寄りの実装メモも、LEDVision、uLipSync/OSC、MMD2MToon、軽量SSS、カメラ補助のように、あとでコード整理しやすい単位へ分けています。

- [個人でバーチャルライブを作るときの技術メモ: ArtNet/NDI/Timecode/SSS](./vlive-technical-input-map-memo)
- [Unityライブ照明をArtNet/DMXで外部制御するメモ](./vlive-lighting-main-subject-memo)
- [Unityステージ制作メモ: トラス/床/バミリ/MVR/Vectorworks](./vlive-stage-composition-memo)
- [バーチャルライブ小物メモ: マイク/ケーブル/黒い備品の場所感](./vlive-live-props-scale-memo)
- [キャラクターアニメーションの短尺検証: 決めカット/余韻/フェイシャルメモ](./vlive-character-animation-keycut-memo)
- [Unityキャラ自動アニメーション: 呼吸/目パチ/EyeDirtの実装メモ](./unity-character-auto-animation-scripts-memo)
- [Unity uLipSync/OSCでリップシンクを送受信する: FacialReceiver/BlendShapeメモ](./unity-ulipsync-osc-facial-control-memo)
- [HDRPでVRM/MToonルックを調整する: ライブ照明向けキャラシェーダメモ](./vlive-character-shader-design-memo)
- [MMDMechanimからMToonへシェーダを変換する: ShadowColor/Outline/SphereMapメモ](./unity-mmdmechanim-mtoon-converter-memo)
- [Unityキャラルック検証: 軽量SSS/髪の異方性反射/肌Bloomメモ](./unity-lightweight-sss-hair-anisotropy-memo)
- [Unity/After Effectsポスプロ検証: ライトラップ/グレイン/色収差メモ](./vlive-postprocess-purpose-memo)
- [Unity LED/VJ検証: Spout/NDI/KodeLife/DMXで背景映像を扱うメモ](./vlive-led-vj-information-memo)
- [Unity LEDVisionシェーダ検証: 板ポリ/ShaderGraph/VJ/DMXメモ](./unity-ledvision-shadergraph-vj-memo)
- [ゼンゼロ風ルック研究メモ: 色/キャラ/AE合成の分解](./vlive-game-look-research-memo)

## トラスやケーブルがあるとライブの場所に見える

ライブっぽさは、照明やポスプロだけではなく、ステージの物量にもかなり支えられている。

トラス、幕、ケーブル、マイク、スタンド、スピーカー、アンプ、LEDパネル、バミリ、スモーク、特効。
こういうものがあるだけで、「そこにライブの場所がある」感じが出る。

キャラと背景だけ作って終わりがちだけど、ライブ小物はかなり効く。
全部を高品質に作る必要はないので、公開されているライブ映像やステージ写真を見て、何が画面に映り込んでいるかを拾っておく。

図面や配灯の文脈も、早めに名前だけ知っておくと便利。
MVR、GDTF、Vectorworks、MagicQ CSVあたりは、Unityの関連アセットやツールを探すときの検索ワードにもなる。

ライブっぽさは、キャラと背景だけではなく、トラス、幕、ケーブル、LED、スモーク、特効みたいな周辺物でもかなり出る。

ステージや特効は、MVR/CSVで配灯を持ち込む話と、VFX Graphで煙やケーブルを見る話に分けています。

- [MVR/VectorWorksからUnityへ配灯を持ち込む: 灯体Prefab置換とMagicQ CSVメモ](./unity-mvr-vectorworks-fixture-prefab-memo)
- [Unity VFX Graphでライブ特効を作る: CO2/スモーク/スパークラー/Six Way Smokeメモ](./unity-vfxgraph-live-special-effects-memo)

## 最初から照明卓UIや物理照明を全部作らなくていい

逆に、最初から全部深掘りしなくてもよさそうなものもある。

- 独自の照明卓UIを作る
- 物理的に正しい照明モデルを全部再現する
- シェーダを先に極める
- ネットワークプロトコルを仕様書から全部読む
- ステージ、モーション、カメラ、照明、VJを最初から同時に詰める

このへんは、最初から重く考えなくていい。
まずは小さく触ってみて、気になったところだけ調べるくらいの方が入りやすい。

最初は「全部を作る」より、気になったものを1個ずつ触るくらいの方が続けやすい。

公開前や見せる前の修正順は、別で [バーチャルライブ個人制作の修正順メモ: 加点より減点回避を見る](./vlive-production-risk-minus-fix-memo) に切り出しました。

## 詰まったときに戻れる検索ワードだけ置いておく

とりあえず触ってみたり、軽く調べてみたりする入口として、このへんの名前を置いておく。

### ライブ映像はカメラ/照明/VJ/特効で見る

- `ライブ カメラワーク`
- `ライブ 照明`
- `ライブ VJ`
- `ライブ 特効`
- `バーチャルライブ`
- `XR LIVE`

### UnityはTimeline/Cinemachine/Recorderから触る

- `Unity Timeline`
- `Unity Cinemachine`
- `Unity Recorder`
- `Unity OSC`
- `Unity ArtNet`
- `Unity NDI`
- `Unity VFX Graph`
- `Unity stage lighting`

### 照明・信号はDMX/Art-Net/OSC/Timecodeが入口

- `DMX`
- `Art-Net`
- `OSC`
- `Timecode`
- `MagicQ`
- `TouchDesigner`
- `ムービングライト`
- `ピンスポット`
- `レーザー`

### キャラはフェイシャル/BlendShape/視線/まばたきが入口

- `モーションキャプチャ`
- `フェイシャル`
- `BlendShape`
- `視線`
- `まばたき`
- `VRM`
- `MToon`

### ステージはLED/トラス/スモーク/配灯で見る

- `LEDビジョン`
- `トラス`
- `スモーク`
- `ゴボ`
- `配灯`
- `MVR`
- `GDTF`

このへんは、全部を理解するためのリストではなく、詰まったときの検索ワードです。
細かい固有名詞まで追いすぎると逆に迷子になるので、最初はこのくらいで止める。

## 公式ドキュメントの前にUnityアセットやサンプルで遊んでみる

最初から公式ドキュメントを読むより、Unityの関連アセットやサンプルを調べて、軽く遊んでみる方が入りやすい気がしている。

- `Unity DMX`
- `Unity ArtNet`
- `Unity OSC`
- `Unity Timeline`
- `Unity Cinemachine`
- `Unity Recorder`
- `Unity NDI`
- `Unity VJ`
- `Unity LED screen`
- `Unity stage lighting`

いきなり正解を探すというより、どういうアセットや実装例があるのかを眺める。
その中で気になったものを触ってみる。
仕様や公式ドキュメントは、必要になったときに戻ってくる。

最初から仕様を読み切ろうとするより、Unityアセットやサンプルを触って、気になった単語を後から調べるくらいでも入りやすい。

## GitHubや雑誌は「作り方の方向」を知る入口にする

自分が最初の方に見ていたものも置いておく。
ちゃんと順番に勉強したというより、GitHubを見たり、書籍や雑誌を見たりして、「こういう作り方や見方があるのか」と入口を増やしていた感じ。

### GitHub

まずはkeijiroさんのGitHubをかなり見ていた。
Unityで映像、VFX、OSC、実験用モデル、ライブコーディングっぽいことをどう扱っているのか、リポジトリを眺めるだけでも勉強になる。

keijiroさんのGitHub

https://github.com/keijiro

keijiro/StickShow

https://github.com/keijiro/StickShow

keijiro/OscJack

https://github.com/keijiro/OscJack

keijiro/KlakHap

https://github.com/keijiro/KlakHap

keijiro/Gamma

https://github.com/keijiro/Gamma

keijiro/SixWaySmokeTest

https://github.com/keijiro/SixWaySmokeTest

あと、Takao KodaiさんのLightBeamPerformanceもかなり入口として大きかった。
UnityのTimelineでムービングライトやレーザー演出を扱う、という発想を見られる。

Kodai TakaoさんのGitHub

https://github.com/kodai100

kodai100/Unity_LightBeamPerformance

https://github.com/kodai100/Unity_LightBeamPerformance

自分の実践リポジトリは、VLiveKit系として整理中です。公開リンクがあるコードは照明やLED/VJなど、関連する個別記事の中に置いていきます。

### 雑誌・書籍

バーチャルライブやXRライブまわりは、VIDEO SALONの関連号も入口になった。

VIDEO SALON 2022年5月号: XR LIVE演出&配信の世界

https://www.genkosha.co.jp/book/b10094770.html

Amazonで探す: VIDEO SALON 2022年5月号 XR LIVE

https://www.amazon.co.jp/s?k=VIDEO+SALON+2022%E5%B9%B45%E6%9C%88%E5%8F%B7+XR+LIVE

VIDEO SALON 2023年7月号: ゲームエンジン特集

https://videosalon.jp/blog/video-salon202307/

Amazonで探す: VIDEO SALON 2023年7月号 ゲームエンジン

https://www.amazon.co.jp/s?k=VIDEO+SALON+2023%E5%B9%B47%E6%9C%88%E5%8F%B7+%E3%82%B2%E3%83%BC%E3%83%A0%E3%82%A8%E3%83%B3%E3%82%B8%E3%83%B3

VIDEO SALON 2022年2月号: 映像制作のためのUnreal Engine入門講座

https://videosalon.jp/blog/video-salon202202-unrealengine/

Amazonで探す: VIDEO SALON 2022年2月号 Unreal Engine

https://www.amazon.co.jp/s?k=VIDEO+SALON+2022%E5%B9%B42%E6%9C%88%E5%8F%B7+Unreal+Engine

こういうリンクは、全部読むというより、まず表紙や目次を見て「ライブ制作って照明、配信、カメラ、ゲームエンジン、ステージ制作がつながっているんだな」と知るための入口だった。

GitHubや雑誌は、完成された教科書というより「こういう方向があるんだ」と知る入口として見ていた。

## 入口として見ていた公開リンクの画像を置く

最初の入口になっていた公開リンクから、画像つきで見返しやすいものをいくつか拾っておく。
こういうものを眺めながら、ライブ映像、照明、Unity、ステージまわりの見方を少しずつ増やしていた。

![初心者のためのライブ照明入門のサムネイル](https://i.ytimg.com/vi/J73YuUzgT6A/maxresdefault.jpg)
*ライブ照明の入口として貼っていた動画。照明用語や灯体の見方を知る入口。*

![Kizuna AI The Last Liveのサムネイル](https://i.ytimg.com/vi/GTa2HxIsBPM/maxresdefault.jpg)
*バーチャルライブ映像を1曲単位で見る入口として貼っていたもの。*

![Unity Timeline Playable API勉強会の画像](https://media.connpass.com/thumbs/0c/67/0c675374a25a7787e808674cdb3f02b1.png)
*TimelineやPlayable APIをライブ表現側で見ていく入口。*

![keijiro GammaのGitHub OGP画像](https://opengraph.githubassets.com/24160d5c7713e333b396cdf7ea6202178b0cd994c3e1e09d210ccb8ad86d1050/keijiro/Gamma)
*keijiroさんのGitHubを見ていた頃の入口のひとつ。*

![Volumetric Lights 2のAsset Store画像](https://assetstorev1-prd-cdn.unity3d.com/key-image/7aa23051-9e49-4422-a1c2-5c0a43b8df58.png)
*UnityのAsset Storeから照明っぽい表現を触ってみる入口。*

ここでは公開リンク側の画像を使っています。

## おわり

自分が最初に知りたかったのは、たぶん「どのソフトを順番に覚えるか」だけではなくて、ライブっぽさを作っている要素の名前だった。

主役のアクト、照明、カメラ、VJ、特効、ステージ、小物、信号、書き出し。
名前が分かると、ライブ映像を見たときにも、Unityのアセットを探すときにも、少しだけ迷子になりにくい。

最初から全部は無理なので、まずは気になるものを触ってみる。
好きなライブ映像を見ながら、「これは何でできているんだろう」と検索できる名前を増やしていく。

そのくらいの入口が、自分は最初にほしかった気がする。

この記事自体も、正解リストというより、最初に迷いやすい入口を整理したものです。

## 関連記事

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
