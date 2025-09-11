// === Day1_Introduction ===
// このノットが会話の開始点

# prefab: left
# speaker: Boss
やあ、新人。本日から君の上司になった者だ。よろしく。

# prefab: right
# speaker: Player
はい、こちらこそよろしくお願いいたします！

# prefab: left
# speaker: Boss
早速だが、今日は君にやってもらう仕事がある。デスクトップにある「通知アプリ」のアイコンを見てくれ。

* [わかりました]
    # prefab: right
    # speaker: Player
    はい、確認します。
    -> JobExplanation

* [少し待ってください]
    # prefab: right
    # speaker: Player
    すみません、PCのセットアップがまだで…。
    -> JobExplanation

= JobExplanation
# prefab: left
# speaker: Boss
アイコンの上に赤いバッジが付いているだろう。それが、君が今日処理すべき案件の合図だ。

# prefab: left
# speaker: Boss
まずはそのアプリを開いて、最初の案件の内容を確認してくれたまえ。健闘を祈る。

-> END