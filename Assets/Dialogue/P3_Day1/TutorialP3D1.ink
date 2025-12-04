-> main

== main ==
# reset_effects
# prefab: left
# speaker: Boss
中央にいる人間、これが今から調整を⾏う AI だ。人間に近い思考回路を有している。

# prefab: right
# speaker: Player
かわいらしい部屋ですね。

# prefab: left
# speaker: Boss
AIにも好みがあるらしい。人間らしさを追求した結果、こうなったんだろうな。

# prefab: right
# speaker: Player
すごいですね。

# prefab: left
# speaker: Boss
我々が調整を⾏う AIはこちらの存在を知らない。⾃⾝が普通に”⽣きている”と思っているんだ。

# prefab: left
# speaker: Boss
だから、もしこちら側の存在に気づいてしまうと、彼女は正気を失い、このソフト⾃体がクラッシュしてしまう。

# prefab: left
# speaker: Boss
そんなインシデントを起こせばお前自身の評価にも響くからな、気を付けろよ。

# highlight: DetectionMeter
# prefab: left
# speaker: Boss
AI の違和感は可視化されている、よく⾒ておくように 。


# prefab: right
# speaker: Player
わかりました

# prefab: left
# speaker: Boss
では、画⾯の説明をする。

# highlight: Right
# prefab: left
# speaker: Boss
右に記されているのがAIのプロフィールやゴミ箱などのユーティリティだ。

# highlight: Left
# prefab: left
# speaker: Boss
左は AI を調整するためのオブジェクトだ。

# prefab: left
# speaker: Boss
これを部屋に配置したり、逆に不適切な物を削除したりする。

# prefab: left
# speaker: Boss
環境は人格を形作る。部屋を変えることで、AIの特性を調整するんだ。下部に調整方針なども書かれているぞ。

# prefab: right
# speaker: Player
なるほど。

# prefab: left
# speaker: Boss
#reset_effects
では具体的な操作方法についても説明する。習うより慣れろだ、実際にやってみたまえ。

# prefab: left
# speaker: Boss
# wait_for_drag
表示されたウィンドウにあるオブジェクトを掴んで、スロットまでドラッグして配置してみろ。

# prefab: left
# speaker: Boss
悪くない手際だ。

# prefab: left
# speaker: Boss
# wait_for_trash
次は削除だ。不要なものは、同じようにドラッグしてゴミ箱まで持っていけばいい。やってみろ。

# prefab: left
# speaker: Boss
うむ、完璧だ。


#highlight:Log
# prefab: left
# speaker: Boss
オブジェクトをクリックすれば、下に説明も出る。

#reset_effects
# prefab: left
# speaker: Boss
それともう気づいたかもしれないが、画面中央にカーソルを持ってくと操作しにくくなるぞ。

# prefab: left
# speaker: Boss
極めて複雑なアプリだからな。それだけマシンにも負荷がかかるんだ。

# prefab: left
# speaker: Boss
カーソルは急には止まれない。AIの違和感を溜めないよう十分用心するように。


-> check_understanding

=== check_understanding ===
#reset_effects
# prefab: left
# speaker: Boss
ここまでは理解できたか？
* [はい]
    -> final_conversation
* [いいえ]
    -> confirm_repeat

== confirm_repeat ==
# prefab: left
# speaker: Boss
#skip_target
もう一度説明してほしいか？
* [はい]
    # prefab: left
    # speaker: Boss
    ったく、よく聞いておけよ？
    -> main
    
* [いいえ]
    -> final_conversation
    
== final_conversation ==
# prefab: left
# speaker: Boss
作業を終了したいときは左下のボタンで完了するんだ。

# prefab: left
# speaker: Boss
17時までは作業を完了しろよ。遅い仕事は全体の効率に影響するからな。時計は右下だ。

# prefab: left
# speaker: Boss
よし、では今回は教育⽤ AI の開発だ。早速取り掛かってくれ！

# prefab: left
# speaker: Boss
#tutorial_end
ちなみにこのチャットは画面下部の「チャット」を押せばいつでも見返せるぞ、健闘を祈る！

-> END
