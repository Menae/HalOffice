-> main

== main ==
# reset_effects
これはカーテンです。
# highlight: Right
右クリックで開閉できます。

これはアイテムです。
# show_gif: drag_and_drop
ドラッグ＆ドロップで移動できます。

-> check_understanding

=== check_understanding ===
説明は以上です。理解できましたか？
* [はい]
    # tutorial_end
    -> END
* [いいえ]
    -> confirm_repeat

== confirm_repeat ==
もう一度説明を聞きますか？
* [はい]
    -> main
* [いいえ]
    # tutorial_end
    -> END