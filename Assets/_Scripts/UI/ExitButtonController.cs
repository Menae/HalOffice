// ファイル名: ExitButtonController.cs
using UnityEngine;

public class ExitButtonController : MonoBehaviour
{
    public ConfirmationDialog confirmationDialog;
    public EvaluationTrigger evaluationTrigger;

    public void OnExitButtonClicked()
    {
        // 確認ダイアログを表示し、「はい」が押されたら評価処理を呼び出す
        confirmationDialog.Show(
            "作業を終了しますか？",
            () => { evaluationTrigger.EndDayAndEvaluate(); }
        );
    }
}