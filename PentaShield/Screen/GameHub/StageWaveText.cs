using UnityEngine;
using TMPro;

public class StageWaveText : MonoBehaviour
{
    private TextMeshProUGUI waveText;
    public TextMeshProUGUI WaveText => waveText;
    private void Awake()
    {
        waveText = GetComponentInChildren<TextMeshProUGUI>();
        if (waveText == null)
        {
            waveText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void SetWaveText(int wave)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.text = wave.ToString();
    }
}
