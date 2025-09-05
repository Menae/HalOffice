using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteAlways]は必要であれば後で追加する
public class GlitchController : MonoBehaviour
{
    public Material mat;

    public float noiseAmount;
    public float glitchStrength;

    void Update()
    {
        // アプリケーションが実行中の場合のみエフェクトを更新する
        // IsPlayingInEditorはエディタ上でプレイモードの場合にtrue
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
            // エディタモード中はエフェクトの値を0にしてオフにする
            if (mat != null)
            {
                mat.SetFloat("_NoiseAmount", 0f);
                mat.SetFloat("_GlitchStrength", 0f);
            }
        }
    }
}