/// <summary>
/// ドラッグ＆ドロップで扱うアイテムの種類を定義する列挙型。
/// UI表示やスロット判定に基づき、描画・配置ルールや相互作用を決定する識別子として使用する。
/// 値の追加時は表示マッピングやシリアライズ先の連携設定を合わせて更新すること。
/// </summary>
public enum ItemType
{
    // ここにあなたのゲームに登場するアイテムの種類を追加していく
    General,            // 一般的なもの（どのスロットにも置ける場合など）
    SceneryA,           // 景色A
    SceneryB,           // 景色B
    Mannequin,          // マネキン
    Books,              // 本
    Chair,              // 椅子
    Gramophone,         // 蓄音機
    Mathematic,         // 数学道具
    WorldMap,           //世界地図
    Documents,          //散乱した書類
    Aroma,              //アロマキャンドル
    Banksy,             //バンクシーの絵
    Headphone,          //ヘッドホン
    Poster,             //ポスター
    Drawings,           //スケッチ画
    cosmetics,          //化粧品
    GameConsole,        //ゲーム機
    Stereo,             //ステレオ
    TeddyBear,          //テディベア
    Yogibo,             //ヨギボー
    none,               //なし
    AnimePoster,        //アニメポスター
    XXXXXXXXX,          // 区切り用（必要に応じて削除）
    BLTV,               //BLTV
    Cheki,              //チェキ
    CupNoodle,          //カップヌードル
    Cardboard1,         //段ボール1
    Cardboard2,         //段ボール2
    MoviePoster,        //映画ポスター
    Trash1,              //ゴミ1
    Trash2,             //ゴミ2
    Trash3,             //ゴミ3
    Comic,              //漫画
    Clothes,             //服
    Figure,             //フィギュア
    Petbottle,           //ペットボトル
    Bag,                 //バッグ
    Drama,                //ドラマ
    DogPortrait,          //犬の肖像画
    Bag2,                //バッグ2
    CuteClothes,           //かわいい服
    Doll,                 //人形
    HeelPoster,            //ヒールのポスター
    Recipe,                //レシピ
    Salad,                 //サラダ
    Vacuum,                //掃除機
    Plants,                //植物


}