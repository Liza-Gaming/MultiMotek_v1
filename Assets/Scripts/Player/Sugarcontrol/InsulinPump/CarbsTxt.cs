namespace Player.Sugarcontrol.InsulinPump
{
    using UnityEngine;
    using UnityEngine.UI;
    
    public class CarbsTxt : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        
        [SerializeField] private Text _sliderText;

        void Start()
        {
            _slider.onValueChanged.AddListener((v) => {
                _sliderText.text = v.ToString("0");
            });

        }
    }
}