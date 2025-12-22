using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float duration = 1.0f;

    [Header("Fade")]
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Text _text;
    private Color _startColor;
    
    private Transform _parent;
    private Vector3 _baseLocalScale;

    private void Awake()
    {
        _text = GetComponent<Text>();
        if (_text == null)
            _text = GetComponentInChildren<Text>();

        if (_text != null)
            _startColor = _text.color;
        else
            Debug.LogError("FloatingText: No Text component found!");
        
        _baseLocalScale = transform.localScale;
        _parent = transform.parent;
    }

    public void Initialize(float value, Vector3 worldPosition, Color color)
    {

        int rounded = Mathf.RoundToInt(value);
        
        if (rounded > 0)
            _text.text = "+" + rounded + " carbs";
        else
            _text.text = rounded.ToString() + " mg/dl";

        _text.color = color;
        _startColor = color;

        transform.position = worldPosition;

        StartCoroutine(AnimateRoutine());
    }
    
    private void LateUpdate()
    {

        if (_parent != null)
        {
            float parentScaleX = _parent.lossyScale.x;

            float sign = parentScaleX < 0 ? -1f : 1f;

            Vector3 ls = _baseLocalScale;
            ls.x *= sign;
            transform.localScale = ls;
            
            transform.rotation = Quaternion.identity;
        }
    }


    private IEnumerator AnimateRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {

            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);


            if (timer >= duration - fadeOutTime)
            {
                float fadeTimer = timer - (duration - fadeOutTime);
                float t = Mathf.Clamp01(fadeTimer / fadeOutTime);
                float alpha = fadeCurve.Evaluate(t);

                if (_text != null)
                {
                    Color newColor = _startColor;
                    newColor.a = alpha;
                    _text.color = newColor;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
    
    
}
