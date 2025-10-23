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

だから、もしこちら側の存在に気づいてしまうと、彼女は正気を失い、このソフト⾃体がクラッシュしてしまう。

ここは評価にも⼤きく関わってくるからな。

# highlight: DetectionMeter
AI の違和感は可視化されている、よく⾒ておくように 。


# prefab: right
# speaker: Player
わかりました

# prefab: left
# speaker: Boss
では、画⾯の説明をする。

# highlight: Right
右にあるのが君に課せられたAIの調整内容だ。これに沿うように作業してくれ。

# highlight: Left
左は AI を調整するためのオブジェクトだ。

これを部屋に配置したり、逆に不適切な物を削除したりする。

環境は人格を形作る。部屋を変えることで、AI を依頼に沿うように調整するんだ。

# prefab: right
# speaker: Player
なるほど。

# prefab: left
# speaker: Boss
#reset_effects
では具体的な操作方法についても説明する。

#show_gif:DD
オブジェクトを選んで、左クリックで設置場所までドラッグ。これで設置する。

 
#show_gif:trash
部屋ものを削除する時も、同じように左クリックでゴミ箱まで持っていけばいい。


#highlight:Log
オブジェクトをクリックすれば、下に説明も出る。


-> check_understanding

=== check_understanding ===
#reset_effects
ここまでは理解できたか？
* [はい]
    -> final_conversation
* [いいえ]
    -> confirm_repeat

== confirm_repeat ==
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
よし、では今回は教育⽤ AI の開発だ。早速取り掛かってくれ！

#tutorial_end
-> END
