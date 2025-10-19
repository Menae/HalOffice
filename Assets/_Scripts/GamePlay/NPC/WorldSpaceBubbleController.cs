using UnityEngine;
using TMPro; // TextMeshProを扱うために必要
using System.Collections; // コルーチンを扱うために必要

/// <summary>
/// ワールド空間（3D空間）に追従する吹き出しを管理するコンポーネント。
/// 指定されたメッセージを指定時間表示し、その後自動で消滅します。
/// キャラクターの頭上などに配置することを想定しています。
/// </summary>
public class WorldSpaceBubbleController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("メッセージを表示するためのTextMeshProコンポーネント")]
    [SerializeField] private TextMeshPro textMesh;

    private Transform mainCameraTransform; // 常にカメラの方向を向く（ビルボード）処理で使用
    private Coroutine selfDestructCoroutine; // 自身の消滅処理を管理するコルーチン

    /// <summary>
    /// オブジェクトが有効になった最初のフレームで一度だけ呼び出されます。
    /// 主にコンポーネントの初期化や参照のキャッシュを行います。
    /// </summary>
    private void Start()
    {
        // パフォーマンス向上のため、メインカメラのTransformを一度だけ取得して変数に保存しておく
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("シーンにメインカメラ（MainCameraタグが付いたカメラ）が見つかりません。", this.gameObject);
        }

        // textMeshがインスペクタから設定されていなければ、自動で取得を試みる
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
    }

    /// <summary>
    /// 吹き出しにメッセージを表示します。このメソッドが外部からのエントリーポイントです。
    /// </summary>
    /// <param name="message">表示したい文字列</param>
    /// <param name="duration">表示する時間（秒）</param>
    public void ShowMessage(string message, float duration)
    {
        // textMeshが有効か確認
        if (textMesh == null)
        {
            Debug.LogError("TextMeshコンポーネントが設定されていません。", this.gameObject);
            return;
        }

        // メッセージをセット
        textMesh.text = message;

        // もし既に消滅コルーチンが動いていたら、一度停止する
        // (短時間に連続でShowMessageが呼ばれた場合に対応するため)
        if (selfDestructCoroutine != null)
        {
            StopCoroutine(selfDestructCoroutine);
        }

        // 新しく消滅コルーチンを開始し、その参照を保持する
        selfDestructCoroutine = StartCoroutine(SelfDestructRoutine(duration));
    }

    /// <summary>
    /// 指定された秒数だけ待機した後、このゲームオブジェクトを破棄するコルーチン。
    /// </summary>
    /// <param name="delay">破棄するまでの待機時間（秒）</param>
    private IEnumerator SelfDestructRoutine(float delay)
    {
        // 指定された時間、処理を待機する
        yield return new WaitForSeconds(delay);

        // この吹き出しオブジェクト自身をシーンから削除する
        Destroy(this.gameObject);
    }
}