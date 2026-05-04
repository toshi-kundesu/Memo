---
title: "Unity uLipSync/OSCでリップシンクを送受信する: FacialReceiver/BlendShapeメモ"
emoji: "🗣️"
type: "idea"
topics: ["unity", "ulipsync", "osc", "blendshape", "vlive"]
published: true
---

[バーチャルライブ制作メモ総目次](./vlive-production-memo-index) に戻る。

リップシンクやフェイシャル制御は、Unity内で全部やろうとすると重くなりがちです。
uLipSyncでAIUEO値を取り、OSCで外へ出し、別PC側でBlendShapeを動かせる形にすると、音声解析と表示を分けやすくなります。

実践リポジトリはVLiveKit系として整理中です。uLipSync/OSCまわりは、フェイシャル信号の送受信サンプルとして整理したいです。

## この記事の持ち帰り: リップシンクは解析と表示を分ける

- uLipSyncは、事前解析や音声キャリブレーションができるリップシンク入口として見ていた。
- `uLipSyncBlendShape` からAIUEO値を取り、OSCで送ると、別PCでフェイシャル制御しやすくなる。
- `FacialReceiver.cs` と `FacialBlendShapeController.cs` は、OSC受信値をBlendShapeへ戻す棚になる。
- Joystickやカメラ座標もOSCで出せるので、フェイシャル制御とカメラ制御を同じ信号設計で見られる。

## 結論: Unityは表情を出す側、音声解析は外へ逃がせる形にする

uLipSyncはUnity内で完結できる便利な入口です。
ただ、ライブ系の検証では、Unity側をレンダリングや表示に集中させたい場面があります。

そのため、考え方としてはこうです。

- uLipSyncでAIUEO値を取る。
- 値をOSCで送る。
- Unity側、または別Unity/別PC側でOSCを受ける。
- 受けた値でBlendShapeを動かす。

これで、音声解析とキャラ表示を分けられます。
「リップシンクが動く」だけではなく、どこで解析し、どこでBlendShapeへ戻すかを分けるのがポイントです。

uLipSyncの入口はこちらです。

https://github.com/hecomi/uLipSync

## uLipSyncからAIUEO値を取り、OSCで送る

手元では、`uLipSyncBlendShapeDebugger.cs` をAIUEO数値取得サンプルとして置いていました。
`uLipSyncBlendShape` がついているオブジェクトにアタッチして、AIUEO値を見る方向です。

次に `uLipSyncOscSender.cs` を貼って、AIUEOをOSCで出す流れを見ています。
メモとしては「OSCでAIUEO出す」「これをReceiveしてBlendShape動かせば安全」という判断でした。

最小の流れはこうです。

- uLipSyncで音声からAIUEO値を出す。
- Debuggerで値が出ているか確認する。
- `uLipSyncOscSender.cs` でOSC送信する。
- 受信側でAIUEO値を受ける。
- BlendShapeへマッピングする。

ここで大事なのは、AIUEO値が出た時点で満足しないことです。
最終的にBlendShapeへ戻して、顔アップで見たときに破綻していないかを確認します。

## FacialReceiverは、別PCフェイシャル制御の入口になる

`FacialReceiver.cs` は、フェイシャルの信号を受けるスクリプトとして残しています。
AIUEOをOSCで出して、別PCでフェイシャル制御するための入口です。

`FacialBlendShapeController.cs` は、リップシンクをOSCで送って受けるコードとして置いていました。
音声解析を別PCに任せることができる、というのがポイントです。

受信側で見たいのはこうです。

- OSCのIP/Portが合っているか。
- AIUEOの値域がBlendShapeに合っているか。
- 値のスムージングで口が遅れすぎていないか。
- 急な口形変化が汚く見えていないか。
- フェイシャルセレクターや手付け表情と競合していないか。

リップシンクは、単体だと動いて見えても、表情や目線と混ざると顔が硬くなることがあります。
OSC受信後のBlendShape制御は、フェイシャル全体の設計と分けずに見る必要があります。

## OscJackは便利だが、ポート解放まわりは切り分ける

OSCまわりでは、keijiroさんのOscJackを見ていました。

https://github.com/keijiro/OscJack

Playmode停止後にPortが閉じられない問題については、Issueも見ていました。

https://github.com/keijiro/OscJack/issues/34

手元では、サーバーを共有する形式のため、どのサーバーが主体になって破棄するかが問題になっていそう、という見方をしていました。
OSCは、値が届くかだけでなく、Playmodeを止めたあとにPortが残っていないかも確認した方が良いです。

確認するなら、次を分けます。

- Unity起動直後にPortを開けるか。
- Playmode停止後にPortが残っていないか。
- 再生し直して同じPortを開けるか。
- 複数Receiverが同じPortを見たときに衝突しないか。

## Joystickやカメラ座標もOSCで扱う

リップシンクだけでなく、Joystickやカメラ座標もOSCで送る検証をしていました。
`JoyStickReceiver.cs` は、JoyStickOSCの受けスクリプトとして残しています。

メモとしては、別PCのドローンコントローラーからカメラ座標などをOSCで送り、NDIで映像を戻してモニタリングする、という方向も見ています。
また、JoystickをOSCで出してUnityカメラで受けるテストもしています。

ここから分かるのは、OSCをリップシンク専用にしない方が良いということです。
フェイシャル、カメラ、Joystick、Timecode、照明トリガーを同じ信号設計の棚で見ると、あとで整理しやすいです。

## 関連記事

- [UnityはLTCを直接読まずOSCで受けるメモ](./unity-timecode-touchdesigner-osc-memo)
- [Unityフェイシャルセレクター検証: 下瞼/口移動/左右非対称メモ](./vlive-facial-selector-shape-design-memo)
- [Unityキャラ自動アニメーション: 呼吸/目パチ/EyeDirtの実装メモ](./unity-character-auto-animation-scripts-memo)
- [Unity ArtNet受信はPrefabに寄せるメモ](./unity-artnet-prefab-receiver-dmx-rec-memo)

## 総目次

制作メモ全体の入口は [バーチャルライブ制作メモ総目次](./vlive-production-memo-index) にまとめています。

---

最終更新: 2026-05-04

バーチャルライブの個人制作・個人検証まわりの話は、個人主催のDiscordサーバー「VLiveHouse!!!」でもしています。
誰でも参加OKで、初心者の方も大歓迎です。

https://discord.gg/sufusTsAcJ
