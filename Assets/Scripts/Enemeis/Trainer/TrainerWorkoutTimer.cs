using UnityEngine;
using UnityEngine.UI;

public class TrainerWorkoutTimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyStartWorkout source;
    [SerializeField] private Text label;

    [Header("Look & Placement")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private bool billboard2D = true;

    [Header("Config")]
    [SerializeField] private int defaultMinutes = 30;
    [SerializeField] private Color cooldownColor = new Color(1f, 0.85f, 0.35f, 1f);

    private enum Mode { Idle, Active, Cooldown }
    private Mode _mode = Mode.Idle;

    private float _remainingGameSeconds;
    private float _remainingRealSeconds;

    private Color _originalLabelColor;
    private bool _hasOriginalColor;

    private void Reset()
    {
        if (!source) source = GetComponentInParent<EnemyStartWorkout>();
    }

    private void Awake()
    {
        transform.localPosition = localOffset;

        if (label)
        {
            _originalLabelColor = label.color;
            _hasOriginalColor = true;
        }

        ShowIdle();
    }

    private void OnEnable()
    {
        if (!source) source = GetComponentInParent<EnemyStartWorkout>();
        if (source != null)
        {
            source.WorkoutStartedGameSeconds += OnStartSession;
            source.WorkoutEnded += OnEndSession;
            source.CooldownStartedRealSeconds += OnCooldownStart;
        }
    }

    private void OnDisable()
    {
        if (source != null)
        {
            source.WorkoutStartedGameSeconds -= OnStartSession;
            source.WorkoutEnded -= OnEndSession;
            source.CooldownStartedRealSeconds -= OnCooldownStart;
        }

        if (label && _hasOriginalColor)
            label.color = _originalLabelColor;
    }

    private void LateUpdate()
    {
        if (billboard2D && Camera.main)
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        switch (_mode)
        {
            case Mode.Active:
            {
                float deltaGame = GameTime.RealSecondsToGameSeconds(Time.deltaTime);
                _remainingGameSeconds -= deltaGame;

                if (_remainingGameSeconds <= 0f)
                {
                    _remainingGameSeconds = 0f;
                    UpdateLabelSeconds(0);
                }
                else
                {
                    UpdateLabelSeconds(_remainingGameSeconds);
                }
                break;
            }

            case Mode.Cooldown:
            {
                _remainingRealSeconds -= Time.deltaTime;

                if (_remainingRealSeconds <= 0f)
                {
                    _remainingRealSeconds = 0f;
                    ShowIdle();
                }
                else
                {
                    UpdateLabelSeconds(_remainingRealSeconds);
                }
                break;
            }
        }
    }

    private void OnStartSession(float totalGameSeconds)
    {
        _mode = Mode.Active;
        _remainingGameSeconds = totalGameSeconds;

        if (label && _hasOriginalColor)
            label.color = _originalLabelColor;

        UpdateLabelSeconds(_remainingGameSeconds);
    }

    private void OnEndSession()
    {
    }

    private void OnCooldownStart(float cooldownRealSeconds)
    {
        _mode = Mode.Cooldown;
        _remainingRealSeconds = cooldownRealSeconds;

        if (label)
            label.color = cooldownColor;

        UpdateLabelSeconds(_remainingRealSeconds);
    }

    private void ShowIdle()
    {
        _mode = Mode.Idle;
        if (!label) return;

        if (_hasOriginalColor)
            label.color = _originalLabelColor;

        label.text = $"{defaultMinutes:00}:00";
    }

    private void UpdateLabelSeconds(float seconds)
    {
        if (!label) return;

        int t = Mathf.CeilToInt(seconds);
        int mm = t / 60;
        int ss = t % 60;
        label.text = $"{mm:00}:{ss:00}";
    }
}