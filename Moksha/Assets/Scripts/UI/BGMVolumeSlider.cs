using UnityEngine;
using UnityEngine.UI;

public class BGMVolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        // Initialize slider from BGMManager
        if (BGMManager.Instance != null)
        {
            slider.SetValueWithoutNotify(BGMManager.Instance.GetVolume());
        }

        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.SetVolume(value);
        }
    }
}
