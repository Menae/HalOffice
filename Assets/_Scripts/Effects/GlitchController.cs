using UnityEngine;

/// <summary>
/// グリッチエフェクト用のコントローラ。マテリアルのシェーダープロパティを毎フレーム同期するコンポーネント。
/// </summary>
/// <remarks>
/// Updateで毎フレームマテリアルのプロパティを設定する。Playモードでのみ設定値を反映し、エディタ上で再生していない場合は値を0にしてエフェクトを無効化する。
/// 必要に応じて [ExecuteAlways] を付与してエディタでも常時更新する。
/// </remarks>
public class GlitchController : MonoBehaviour
{
    /// <summary>
    /// エフェクトを適用するマテリアル。Inspectorでドラッグ&ドロップして設定する。
    /// nullの場合はUpdate内で処理をスキップする。
    /// </summary>
    public Material mat;

    /// <summary>
    /// ノイズ量。マテリアルのシェーダー変数 "_NoiseAmount" に対応。Inspectorで調整。
    /// </summary>
    public float noiseAmount;

    /// <summary>
    /// グリッチの強さ。マテリアルのシェーダー変数 "_GlitchStrength" に対応。Inspectorで調整。
    /// </summary>
    public float glitchStrength;

    /// <summary>
    /// 毎フレーム呼ばれる。Unityのフレーム更新タイミングで実行。
    /// </summary>
    /// <remarks>
    /// Application.isPlaying が true の場合に設定値をマテリアルへ反映する。
    /// エディタで再生していない場合は見た目を無効化するために値を0に設定する。
    /// マテリアルが null の場合は何もしない。
    /// </remarks>
    void Update()
    {
        // Playモード時のみ設定値を反映する
        if (Application.isPlaying)
        {
            if (mat != null)
            {
                mat.SetFloat("_NoiseAmount", noiseAmount);
                mat.SetFloat("_GlitchStrength", glitchStrength);
            }
        }
        else
        {
            // エディタで再生していない場合はエフェクトを無効化する
            if (mat != null)
            {
                mat.SetFloat("_NoiseAmount", 0f);
                mat.SetFloat("_GlitchStrength", 0f);
            }
        }
    }
}