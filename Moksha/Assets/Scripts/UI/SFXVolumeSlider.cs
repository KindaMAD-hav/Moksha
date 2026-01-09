using UnityEngine;
using UnityEngine.UI;

public class SFXVolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (SFXManager.Instance != null)
        {
            slider.SetValueWithoutNotify(SFXManager.Instance.GetVolume());
        }

        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetVolume(value);
    }
}
