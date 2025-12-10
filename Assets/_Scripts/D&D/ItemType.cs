/// <summary>
/// ドラッグ＆ドロップで扱うアイテムの種類を定義する列挙型。
/// UI表示やスロット判定に基づき、描画・配置ルールや相互作用を決定する識別子として使用する。
/// 値の追加時は表示マッピングやシリアライズ先の連携設定を合わせて更新すること。
/// </summary>
public enum ItemType
{
    // ここにあなたのゲームに登場するアイテムの種類を追加していく
    General,         // 一般的なもの（どのスロットにも置ける場合など）
    SceneryA,       // 景色A
    SceneryB,        // 景色B
    Mannequin,      // マネキン
    Books,           // 本
    Chair,          // 椅子
    Gramophone,    // 蓄音機
    Mathematic,      // 数学道具
    WorldMap,       //世界地図
    Documents,      //散乱した書類
    Aroma,         //アロマキャンドル
    Banksy,        //バンクシーの絵
    Headphone,      //ヘッドホン
    Poster,         //ポスター
    Drawings,        //スケッチ画
    cosmetics,      //化粧品
    GameConsole,   //ゲーム機
    Stereo,         //ステレオ
    TeddyBear,      //テディベア
    Yogibo,        //ヨギボー
    none,            //なし
    AnimePoster,   //アニメポスター

}