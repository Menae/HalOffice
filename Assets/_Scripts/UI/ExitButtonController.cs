using UnityEngine;

/// <summary>
/// Exitボタンの挙動を制御するコンポーネント。UIのOnClickから呼び出す。
/// Inspectorで`confirmationDialog`と`evaluationTrigger`をアサインすること。nullの場合は実行時例外が発生する可能性があるため注意。
/// </summary>
public class ExitButtonController : MonoBehaviour
{
    /// <summary>
    /// 確認ダイアログ。InspectorでD&Dしてアサインすること。
    /// ダイアログ表示と「はい」押下時のコールバックを管理する。
    /// </summary>
    public ConfirmationDialog confirmationDialog;

    /// <summary>
    /// 評価処理を開始するトリガー。InspectorでD&Dしてアサインすること。
    /// EndDayAndEvaluateを呼び出して評価とシーン遷移を行う役割。
    /// </summary>
    public EvaluationTrigger evaluationTrigger;

    /// <summary>
    /// ExitボタンのOnClickイベントハンドラ。UIボタンから呼び出す。
    /// 確認ダイアログを表示し、ユーザーが「はい」を選択した場合に評価処理を開始する。
    /// </summary>
    public void OnExitButtonClicked()
    {
        // 確認ダイアログを表示し、「はい」が押されたら評価処理を呼び出す
        confirmationDialog.Show(
            "作業を終了しますか？",
            () => { evaluationTrigger.EndDayAndEvaluate(); }
        );
    }
}