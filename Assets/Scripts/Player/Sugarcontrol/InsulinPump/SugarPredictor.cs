namespace Player.Sugarcontrol.InsulinPump
{
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SugarPredictor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SugarMeter sugarMeter;

    [Header("Prediction settings")]
    [Tooltip("סף סוכר (למשל 70)")]
    [SerializeField] private float threshold = 70f;

    [Tooltip("חלון זמן קדימה בדקות משחק (למשל 30)")]
    [SerializeField] private float lookaheadGameMinutes = 66f;

    [Tooltip("צעד סימולציה בדקות משחק (דיוק/עלות). 0.5-1 מומלץ")]
    [SerializeField] private float stepGameMinutes = 1f;

    [Tooltip("כל כמה שניות אמיתיות להריץ בדיקה מחדש (לא כל פריים)")]
    [SerializeField] private float checkEveryRealSeconds = 0.25f;
    
    public event Action<float /*etaGameMin*/, float /*predictedSugarAtHit*/> WillHitThresholdSoon;
    public event Action SafeAgain;

    private float _timer;
    private bool _inWarning;

    // Reflection handles (כדי לא לשנות SugarMeter)
    private FieldInfo _fiSugarLevel;
    private FieldInfo _fiAbsGM;
    private FieldInfo _fiEffects;
    private FieldInfo _fiTransients;
    private PropertyInfo _piBaselineRate;
    
    public bool LastWillHit { get; private set; }
    public float LastEtaGameMin { get; private set; }
    public float LastSugarAtHit { get; private set; }


    private void Awake()
    {
        if (sugarMeter == null) sugarMeter = SugarMeter.Instance ?? FindFirstObjectByType<SugarMeter>();
        CacheReflection();
    }

    private void CacheReflection()
    {
        var t = typeof(SugarMeter);
        _fiSugarLevel  = t.GetField("sugarLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        _fiAbsGM       = t.GetField("_absGameMinutes", BindingFlags.Instance | BindingFlags.NonPublic);
        _fiEffects     = t.GetField("_effects", BindingFlags.Instance | BindingFlags.NonPublic);
        _fiTransients  = t.GetField("_transients", BindingFlags.Instance | BindingFlags.NonPublic);
        _piBaselineRate= t.GetProperty("BaselineRatePerGameMin", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void Update()
    {
        
        if (sugarMeter == null) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer < checkEveryRealSeconds) return;
        _timer = 0f;

        bool willHit = PredictHitWithinWindow(out float eta, out float sugarAtHit);
        Debug.Log($"[Predict] willHit={willHit} eta={eta:F1} sugarAtHit={sugarAtHit:F1} threshold={threshold}");

        if (willHit)
        {
            if (!_inWarning)
            {
                _inWarning = true;
                WillHitThresholdSoon?.Invoke(eta, sugarAtHit);
            }
        }
        else
        {
            if (_inWarning)
            {
                _inWarning = false;
                SafeAgain?.Invoke();
            }
        }
        LastWillHit = willHit;
        LastEtaGameMin = eta;
        LastSugarAtHit = sugarAtHit;

        
    }

    private bool PredictHitWithinWindow(out float etaGameMin, out float sugarAtHit)
    {
        
        etaGameMin = -1f;
        sugarAtHit = float.NaN;
        
        float sugar = GetPrivateFloat(_fiSugarLevel);
        double nowGM = GetPrivateDouble(_fiAbsGM);

        if (sugar <= threshold)
        {
            etaGameMin = 0f;
            sugarAtHit = sugar;
            return true;
        }

        float baselineRate = GetPrivateBaselineRate(); // כבר 0 החל מסצנה 5 אצלך

        var effects = ReadEffects();
        var transients = ReadTransients();

        // סימולציה קדימה
        float t = 0f;
        float step = Mathf.Max(0.05f, stepGameMinutes);

        while (t < lookaheadGameMinutes - 1e-4f)
        {
            float dt = Mathf.Min(step, lookaheadGameMinutes - t);
            double gm = nowGM + t;

            float rate = 0f;

            // Effects פעילים בזמן gm
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (gm >= e.startAtGM && gm < e.endAtGM)
                    rate += e.ratePerGM;
            }

            // אם אין אפקטים פעילים – baseline
            if (Mathf.Abs(rate) < 1e-6f)
                rate = baselineRate;

            // Transients: מוסיפים את הקצב שלהם כל עוד נשאר להם זמן
            for (int i = 0; i < transients.Count; i++)
            {
                if (transients[i].remainingGM > 0f)
                    rate += transients[i].ratePerGM;
            }

            // עדכון סוכר
            sugar += rate * dt;

            // “בזבוז” זמן מה־transients
            for (int i = 0; i < transients.Count; i++)
            {
                float rem = transients[i].remainingGM;
                if (rem <= 0f) continue;
                rem -= dt;
                transients[i] = (transients[i].ratePerGM, rem);
            }

            t += dt;

            if (sugar <= threshold)
            {
                etaGameMin = t;
                sugarAtHit = sugar;
                return true;
            }
        }

        return false;
    }

    // ===== Reflection read helpers =====

    private float GetPrivateFloat(FieldInfo fi) => fi != null ? (float)fi.GetValue(sugarMeter) : 0f;
    private double GetPrivateDouble(FieldInfo fi) => fi != null ? (double)fi.GetValue(sugarMeter) : 0.0;

    private float GetPrivateBaselineRate()
    {
        if (_piBaselineRate == null) return 0f;
        return (float)_piBaselineRate.GetValue(sugarMeter);
    }

    private struct EffectSnap
    {
        public float ratePerGM;
        public double startAtGM;
        public double endAtGM;
    }

    private List<EffectSnap> ReadEffects()
    {
        var list = new List<EffectSnap>();
        if (_fiEffects == null) return list;

        var effectsObj = _fiEffects.GetValue(sugarMeter);
        if (effectsObj is System.Collections.IEnumerable enumerable)
        {
            foreach (var e in enumerable)
            {
                var et = e.GetType();
                float rate = (float)et.GetField("ratePerGameMin").GetValue(e);
                double start = (double)et.GetField("startAtGameMin").GetValue(e);
                double end = (double)et.GetField("endAtGameMin").GetValue(e);
                list.Add(new EffectSnap { ratePerGM = rate, startAtGM = start, endAtGM = end });
            }
        }
        return list;
    }

    private List<(float ratePerGM, float remainingGM)> ReadTransients()
    {
        var list = new List<(float, float)>();
        if (_fiTransients == null) return list;

        var trObj = _fiTransients.GetValue(sugarMeter);
        if (trObj is System.Collections.IEnumerable enumerable)
        {
            foreach (var tr in enumerable)
            {
                var tt = tr.GetType();
                float rate = (float)tt.GetField("ratePerGameMin").GetValue(tr);
                float remaining = (float)tt.GetField("remainingGameMin").GetValue(tr);
                list.Add((rate, remaining));
            }
        }
        return list;
    }
}

}