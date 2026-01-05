namespace Player.Sugarcontrol.InsulinPump
{
    using UnityEngine;
    using UnityEngine.UI;

    public class LowSugarWarningUI : MonoBehaviour
    {
        [SerializeField] private SugarPredictor predictor;
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private Text warningText;

        private void Awake()
        {
            if (predictor == null) predictor = FindFirstObjectByType<SugarPredictor>();
            if (warningPanel) warningPanel.SetActive(false);
        }

        private void Update()
        {
            if (predictor == null) return;

            bool show = predictor.LastWillHit;
            if (warningPanel && warningPanel.activeSelf != show)
                warningPanel.SetActive(show);

            if (show && warningText)
                warningText.text = $":הבאשמ תארתה\nתוקד 30 דועב\n70 לע היהי רכוסה {Mathf.CeilToInt(predictor.LastEtaGameMin)} ";
        }
    }


}