using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// ワールド空間（3D空間）に追従する吹き出しを管理するコンポーネント。
/// 指定されたメッセージを指定時間表示し、その後自動で削除する。キャラクターの頭上などに配置する想定。
/// </summary>
public class WorldSpaceBubbleController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("メッセージ表示用のTextMeshPro。InspectorでD&Dして設定することを推奨。Startで未設定なら自動取得を試みる。")]
    [SerializeField] private TextMeshPro textMesh;

    /// <summary>
    /// カメラのTransformを一度だけ取得してキャッシュする。ビルボード処理で使用。
    /// Startで設定されない場合はnullの可能性あり。nullチェックを行うこと。 
    /// </summary>
    private Transform mainCameraTransform; // カメラ方向に向けるための参照をキャッシュ

    /// <summary>
    /// 自身の消滅処理を管理するコルーチン参照。ShowMessageの再呼び出し時に停止して上書きするために保持。
    /// </summary>
    private Coroutine selfDestructCoroutine; // 実行中の消滅コルーチン参照

    /// <summary>
    /// オブジェクトが有効化された最初のフレームで一度だけ呼ばれる。主に参照の初期化を行う。
    /// Startの実行順序に依存する処理がある場合は注意。MainCameraタグを持つカメラが存在しないとエラーをログ出力する。
    /// </summary>
    private void Start()
    {
        // パフォーマンス向上のため、メインカメラのTransformを一度だけ取得してキャッシュ
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("シーンにメインカメラ（MainCameraタグが付いたカメラ）が見つかりません。", this.gameObject);
        }

        // Inspectorで未設定の場合、同一オブジェクトからTextMeshProを自動取得して動作保証
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
    }

    /// <summary>
    /// 吹き出しにメッセージを表示するエントリーポイント。外部から呼び出して表示内容と表示時間を指定する。
    /// textMeshが設定されていなければエラーをログ出力して処理を中断する。
    /// </summary>
    /// <param name="message">表示したい文字列</param>
    /// <param name="duration">表示時間（秒）。0以下の値は即時削除となる。</param>
    public void ShowMessage(string message, float duration)
    {
        // textMeshが有効か確認。設定されていない場合は処理しない。
        if (textMesh == null)
        {
            Debug.LogError("TextMeshコンポーネントが設定されていません。", this.gameObject);
            return;
        }

        // 表示する文字列をセット
        textMesh.text = message;

        // 既に消滅コルーチンが動作中であれば停止してから新しいコルーチンを開始
        if (selfDestructCoroutine != null)
        {
            StopCoroutine(selfDestructCoroutine);
        }

        // 指定時間後に自身を削除するコルーチンを開始して参照を保持
        selfDestructCoroutine = StartCoroutine(SelfDestructRoutine(duration));
    }

    /// <summary>
    /// 指定秒数待機した後、このゲームオブジェクトをシーンから削除するコルーチン。
    /// Destroyによってこのコンポーネントと関連するオブジェクトが破棄される。
    /// </summary>
    /// <param name="delay">削除までの待機時間（秒）。負の値でもWaitForSecondsは0扱いとなる可能性あり。</param>
    private IEnumerator SelfDestructRoutine(float delay)
    {
        // 指定時間待機
        yield return new WaitForSeconds(delay);

        // この吹き出しオブジェクトをシーンから削除
        Destroy(this.gameObject);
    }
}