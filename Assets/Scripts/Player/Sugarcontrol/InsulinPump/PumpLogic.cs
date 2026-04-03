using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player.Sugarcontrol.InsulinPump
{
    public class PumpLogic : MonoBehaviour
    {
        public static PumpLogic Instance { get; private set; }

        [Header("Temp Target 140 (Activity Mode)")]
        [Tooltip("מאיזה שלב הפיצ'ר פעיל")]
        [SerializeField] private int enableTempTargetBuildIndex = 6;
        [SerializeField] private float tempTargetValue = 140f;
        [Tooltip("לפי איזה קצב זמן לחשב את העלייה ל-140 (בדקות משחק)")]
        [SerializeField] private float tempRiseDurationGameMin = 60f;

        [SerializeField] private GameObject slider;

        private float _baseTargetSugar; 
        private bool _isTempTargetActive;
        private double _nextTempTargetCheckGM; // טיימר לבדיקות העלייה
        
        [Header("Enable (match your report scene gate)")]
        [SerializeField] private int enableFromBuildIndex = 5;

        [Header("Target")]
        [SerializeField] private float targetSugar = 105f;

        [Header("Physiology (game model)")]
        [Tooltip("1 unit lowers this many mg/dL over the whole action window.")]
        [SerializeField] private float mgdlPerUnitTotal = 30f;

        [Tooltip("Insulin action window in GAME minutes (3 hours = 180).")]
        [SerializeField] private float insulinActionGameMin = 180f;

        [Tooltip("Optional onset delay before insulin starts acting (GAME minutes).")]
        [SerializeField] private float insulinStartDelayGameMin = 0f;

        [Header("Delivery precision / clamps")]
        [SerializeField] private float bolusStepUnits = 0.025f;
        [SerializeField] private float maxUnitsPerCheck = 1.0f;
        [SerializeField] private float maxTotalUnitsForMealWindow = 15f;

        [Header("Controller timing")]
        [SerializeField] private float checkEveryGameMin = 5f;

        [Header("Safety (game)")]
        [SerializeField] private float doNotDoseBelow = 90f;
        [SerializeField] private float hardNoDoseBelow = 70f;
        [SerializeField] private float deadbandMgdl = 2f;

        [Header("Always-on correction (no meal)")]
        [SerializeField] private bool alwaysOnCorrection = true;
        [SerializeField] private float maxUnitsPerCheckNoMeal = 0.3f;
        [SerializeField] private float maxUnitsPerHourNoMeal = 1.5f;

        [Header("Meal IOB handling")]
        [Tooltip("How much of IOB to subtract during meal. 1 = full, 0.5 = softer (recommended).")]
        [Range(0f, 1f)]
        [SerializeField] private float mealIobFactor = 0.5f;

        [Header("Trend gating (no PID)")]
        [Tooltip("If slope > this => still rising => no additional correction dose.")]
        [SerializeField] private float risingSlopeEps = 0.2f;   // mg/dL per game minute
        [Tooltip("|slope| <= this => plateau candidate.")]
        [SerializeField] private float plateauSlopeEps = 0.2f;  // mg/dL per game minute
        [Tooltip("How many consecutive ticks must be plateau before dosing.")]
        [SerializeField] private int plateauTicksRequired = 2;
        [Header("Correction threshold (plateau)")]
        [SerializeField] private float plateauCorrectionMinSugar = 145f;


        // --- internal time in game minutes ---
        private double _absGameMinutes;

        // --- meal window state ---
        private bool _active;
        private double _activeUntilGM;
        private double _nextMealTickGM;
        private float _remainingMealBudgetUnits;

        // --- always-on state ---
        private double _nextNoMealTickGM;

        private struct NoMealDose
        {
            public float units;
            public double timeGM;
        }
        private readonly List<NoMealDose> _noMealDoses = new List<NoMealDose>();

        // --- Trend sample state ---
        private float _lastGlucose;
        private double _lastSampleGM;
        private bool _hasLastSample;
        private int _plateauTicks;

        // --- IOB tracking ---
        private struct Dose
        {
            public float units;
            public double startGM;
            public double endGM;
        }
        private readonly List<Dose> _doses = new List<Dose>();

        private bool EnabledThisScene()
            => SceneManager.GetActiveScene().buildIndex >= enableFromBuildIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _baseTargetSugar = targetSugar; 
        }

        private void Update()
        {
            float dtReal = Time.deltaTime;
            if (dtReal <= 0f) return;

            float gsrs = (Timer.Instance != null) ? Timer.Instance.GameSecondsPerRealSecond : 30f;
            float gmThisFrame = dtReal * (gsrs / 60f);
            _absGameMinutes += gmThisFrame;

            CleanupOldDoses();

            if (!EnabledThisScene()) return;
            if (SugarMeter.Instance == null) return;
            
            if (_isTempTargetActive)
            {
                if (_absGameMinutes >= _nextTempTargetCheckGM)
                {
                    _nextTempTargetCheckGM = _absGameMinutes + Math.Max(0.5f, checkEveryGameMin);
                    
                    float currentSugar = SugarMeter.Instance.GetSugarLevel();
                    if (currentSugar < tempTargetValue)
                    {
                        // חישוב הקצב: כמה סוכר חסר חלקי 60 דקות
                        float diff = tempTargetValue - currentSugar;
                        float ratePerMinute = diff / tempRiseDurationGameMin;
                        
                        // מחשבים את המנה שצריך להוסיף רק עבור חלון הבדיקה הנוכחי (למשל 5 דקות)
                        float chunkAmount = ratePerMinute * checkEveryGameMin;
                        
                        // מוסיפים את הסוכר בהדרגה לאורך 5 דקות המשחק הקרובות
                        SugarMeter.Instance.AddSugarGame(chunkAmount, checkEveryGameMin, 0f);
                    }
                }
            }

            // 1) no meal: always-on correction
            if (!_active && alwaysOnCorrection)
            {
                if (_absGameMinutes >= _nextNoMealTickGM)
                {
                    double nowGM = _absGameMinutes;
                    double prevGM = _nextNoMealTickGM;

                    _nextNoMealTickGM = nowGM + Math.Max(0.5f, checkEveryGameMin);
                    ControlStepNoMeal(nowGM, prevGM);
                }
                return;
            }

            // 2) meal active
            if (!_active) return;

            if (_absGameMinutes >= _activeUntilGM)
            {
                _active = false;
                return;
            }

            if (_absGameMinutes < _nextMealTickGM) return;

            double nowMealGM = _absGameMinutes;
            double prevMealGM = _nextMealTickGM;

            _nextMealTickGM = nowMealGM + Math.Max(0.5f, checkEveryGameMin);
            ControlStepMeal(nowMealGM, prevMealGM);
        }

        /// <summary>
        /// Call ONLY when player confirms meal report.
        /// foodDelayGameMin: הדיליי שנבחר בכפתורים (0/15/30/45) עבור תחילת ההזרקה.
        /// expectedFoodRiseMgdl: כמה mg/dL האוכל צפוי להעלות סה"כ.
        /// foodDurationGameMin: משך העלייה (כמה זמן האוכל "עולה").
        /// </summary>
        public void OnMealReportedPID(
            int carbsGrams,
            float expectedFoodRiseMgdl,
            float foodDelayGameMin,
            float foodDurationGameMin
        )
        {
            if (!EnabledThisScene()) return;
            if (SugarMeter.Instance == null) return;

            double startGM = _absGameMinutes + Math.Max(0f, foodDelayGameMin);

            _active = true;
            _nextMealTickGM = startGM;

            // active long enough: rise window + insulin action window
            double mealEndGM = startGM + Math.Max(0f, foodDurationGameMin);
            _activeUntilGM = mealEndGM + Math.Max(0f, insulinActionGameMin);

            // reset trend gating
            _hasLastSample = false;
            _plateauTicks = 0;

            _remainingMealBudgetUnits = maxTotalUnitsForMealWindow;

            // Total needed = food rise + correction above target NOW
            float glucoseNow = SugarMeter.Instance.GetSugarLevel();
            float mgdlToFix = Mathf.Max(0f, expectedFoodRiseMgdl) + Mathf.Max(0f, glucoseNow - targetSugar);
            float unitsTotal = (mgdlPerUnitTotal <= 0.0001f) ? 0f : (mgdlToFix / mgdlPerUnitTotal);

            // Prebolus: 80% (same behavior you had), delayed by chosen delay
            float preBolus = Mathf.Min(unitsTotal * 0.8f, _remainingMealBudgetUnits);
            preBolus = RoundToStep(preBolus, bolusStepUnits);

            if (preBolus > 0.0001f)
            {
                DeliverMicroBolus(preBolus, foodDelayGameMin);
                _remainingMealBudgetUnits -= preBolus;
            }
        }

        // ===== MEAL (no PID) =====
        private void ControlStepMeal(double nowGM, double prevTickGM)
        {
            float glucose = SugarMeter.Instance.GetSugarLevel();
            float dtGM = (float)Math.Max(0.5, nowGM - prevTickGM);

            if (glucose <= doNotDoseBelow)
            {
                _plateauTicks = 0;
                RememberSample(glucose, nowGM);
                return;
            }

            float error = glucose - targetSugar;
            if (Mathf.Abs(error) < deadbandMgdl) error = 0f;

            if (error <= 0f)
            {
                _plateauTicks = 0;
                RememberSample(glucose, nowGM);
                return;
            }

            // slope (mg/dL per game minute)
            float slope = 0f;
            if (_hasLastSample)
            {
                float dt = (float)Math.Max(0.5, nowGM - _lastSampleGM);
                slope = (glucose - _lastGlucose) / dt;
            }

            bool isRising = _hasLastSample && (slope > risingSlopeEps);
            bool isPlateau = _hasLastSample && (Mathf.Abs(slope) <= plateauSlopeEps);

            if (isRising)
            {
                // עדיין עולה => לא מוסיפים תיקון נוסף
                _plateauTicks = 0;
                RememberSample(glucose, nowGM);
                return;
            }

            // confirm plateau for N consecutive ticks
            if (isPlateau) _plateauTicks++;
            else _plateauTicks = 0;

            if (_plateauTicks < plateauTicksRequired)
            {
                RememberSample(glucose, nowGM);
                return;
            }
            
            if (glucose < plateauCorrectionMinSugar)
            {
                RememberSample(glucose, nowGM);
                return;
            }

            // plateau + above target => dose correction
            float unitsNeeded = (mgdlPerUnitTotal <= 0.0001f) ? 0f : (error / mgdlPerUnitTotal);

            float units = Mathf.Min(unitsNeeded, maxUnitsPerCheck);
            units = Mathf.Min(units, _remainingMealBudgetUnits);

            // during meal: subtract only part of started IOB (not future not started)
            float iobStartedOnly = ComputeIOBUnits(includeFutureNotStarted: false);
            units = Mathf.Max(0f, units - (iobStartedOnly * mealIobFactor));

            units = RoundToStep(units, bolusStepUnits);

            if (units <= 0.0001f)
            {
                RememberSample(glucose, nowGM);
                return;
            }

            DeliverMicroBolus(units);
            _remainingMealBudgetUnits = Mathf.Max(0f, _remainingMealBudgetUnits - units);

            // prevent immediate repeat next tick
            _plateauTicks = 0;

            RememberSample(glucose, nowGM);
        }

        // ===== ALWAYS-ON (NO MEAL) (no PID) =====
        private void ControlStepNoMeal(double nowGM, double prevTickGM)
        {
            float glucose = SugarMeter.Instance.GetSugarLevel();
            float dtGM = (float)Math.Max(0.5, nowGM - prevTickGM);

            if (glucose <= doNotDoseBelow)
            {
                _plateauTicks = 0;
                RememberSample(glucose, nowGM);
                return;
            }

            float error = glucose - targetSugar;
            if (Mathf.Abs(error) < deadbandMgdl) error = 0f;

            if (error <= 0f)
            {
                _plateauTicks = 0;
                RememberSample(glucose, nowGM);
                return;
            }

            float slope = 0f;
            if (_hasLastSample)
            {
                float dt = (float)Math.Max(0.5, nowGM - _lastSampleGM);
                slope = (glucose - _lastGlucose) / dt;
            }

            bool isRising = _hasLastSample && (slope > risingSlopeEps);
            bool isPlateau = _hasLastSample && (Mathf.Abs(slope) <= plateauSlopeEps);

            if (isRising)
            {
                _plateauTicks = 0;
                RememberSample(glucose, nowGM);
                return;
            }

            if (isPlateau) _plateauTicks++;
            else _plateauTicks = 0;

            if (_plateauTicks < plateauTicksRequired)
            {
                RememberSample(glucose, nowGM);
                return;
            }

            float unitsNeeded = (mgdlPerUnitTotal <= 0.0001f) ? 0f : (error / mgdlPerUnitTotal);

            float units = Mathf.Min(unitsNeeded, maxUnitsPerCheckNoMeal);
            units = Mathf.Min(units, RemainingUnitsThisHourNoMeal(nowGM));

            // no-meal: conservative IOB (include future)
            float iob = ComputeIOBUnits(includeFutureNotStarted: true);
            units = Mathf.Max(0f, units - iob);

            units = RoundToStep(units, bolusStepUnits);

            if (units <= 0.0001f)
            {
                RememberSample(glucose, nowGM);
                return;
            }

            DeliverMicroBolus(units);
            RegisterNoMealDose(units, nowGM);

            _plateauTicks = 0;
            RememberSample(glucose, nowGM);
        }

        private void DeliverMicroBolus(float units, float extraDelayGameMin = 0f)
        {
            if (SugarMeter.Instance == null) return;
            if (units <= 0.0001f) return;

            float glucoseNow = SugarMeter.Instance.GetSugarLevel();
            if (glucoseNow <= hardNoDoseBelow) return;

            float totalDelay = Mathf.Max(0f, insulinStartDelayGameMin + extraDelayGameMin);

            float totalDrop = units * mgdlPerUnitTotal;

            SugarMeter.Instance.DecreaseSugarGame(
                amount: totalDrop,
                durationGameMin: insulinActionGameMin,
                delayGameMin: totalDelay
            );

            double start = _absGameMinutes + totalDelay;
            double end = start + Math.Max(0.5f, insulinActionGameMin);

            _doses.Add(new Dose { units = units, startGM = start, endGM = end });
        }

        // includeFutureNotStarted:
        // true  => before start, count full units as "on board" (conservative)
        // false => before start, count 0 (so meal corrections won't be choked by future insulin)
        private float ComputeIOBUnits(bool includeFutureNotStarted)
        {
            double now = _absGameMinutes;
            float sum = 0f;

            for (int i = 0; i < _doses.Count; i++)
            {
                var d = _doses[i];

                if (now <= d.startGM)
                {
                    if (includeFutureNotStarted)
                        sum += d.units;
                    continue;
                }

                if (now >= d.endGM) continue;

                double t = (now - d.startGM) / (d.endGM - d.startGM);
                float remaining = d.units * (1f - (float)t);
                sum += Mathf.Max(0f, remaining);
            }

            return sum;
        }

        private void CleanupOldDoses()
        {
            double now = _absGameMinutes;
            for (int i = _doses.Count - 1; i >= 0; i--)
            {
                if (now >= _doses[i].endGM)
                    _doses.RemoveAt(i);
            }
        }

        private void RememberSample(float glucose, double nowGM)
        {
            _lastGlucose = glucose;
            _lastSampleGM = nowGM;
            _hasLastSample = true;
        }

        private static float RoundToStep(float value, float step)
        {
            if (step <= 0.0001f) return value;
            return Mathf.Round(value / step) * step;
        }

        private void RegisterNoMealDose(float units, double nowGM)
        {
            _noMealDoses.Add(new NoMealDose { units = units, timeGM = nowGM });

            for (int i = _noMealDoses.Count - 1; i >= 0; i--)
            {
                if (nowGM - _noMealDoses[i].timeGM > 60.0)
                    _noMealDoses.RemoveAt(i);
            }
        }

        private float RemainingUnitsThisHourNoMeal(double nowGM)
        {
            float used = 0f;

            for (int i = 0; i < _noMealDoses.Count; i++)
            {
                if (nowGM - _noMealDoses[i].timeGM <= 60.0)
                    used += _noMealDoses[i].units;
            }

            return Mathf.Max(0f, maxUnitsPerHourNoMeal - used);
        }
        
        /// <summary>
        /// מופעל על ידי כפתור ה-Toggle ב-UI (לשים לב שה-V מסומן ב-Inspector!)
        /// </summary>
        public void ToggleTempTarget140(bool activate)
        {
            if (SceneManager.GetActiveScene().buildIndex < enableTempTargetBuildIndex) return;

            _isTempTargetActive = activate;
            slider.SetActive(true);
            
            if (_isTempTargetActive)
            {
                // משנים את יעד התיקון של המשאבה ל-140 (היא תפסיק לתת אינסולין מתחת לזה)
                targetSugar = tempTargetValue;
                
                // מאפסים את הטיימר כדי שהבדיקה הראשונה ב-Update תקרה מיד
                _nextTempTargetCheckGM = _absGameMinutes;
            }
            else
            {
                // מכבים את המצב וחוזרים ליעד הרגיל (105)
                targetSugar = _baseTargetSugar;
            }
        }
    }
}
