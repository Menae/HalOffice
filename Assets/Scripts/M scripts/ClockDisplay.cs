using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ClockDisplay : MonoBehaviour
{
    public TextMeshProUGUI clockText;

    void Update()
    {
        clockText.text = DateTime.Now.ToString("HH:mm:ss");
    }
}

//　時計→何日目かに変更
