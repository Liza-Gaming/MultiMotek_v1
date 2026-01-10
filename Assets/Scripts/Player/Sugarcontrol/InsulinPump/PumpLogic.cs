using System;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.SceneManagement;



namespace Player.Sugarcontrol.InsulinPump

{

public class PumpLogic : MonoBehaviour

{

public static PumpLogic Instance { get; private set; }



[Header("Enable (match your report scene gate)")]

[SerializeField] private int enableFromBuildIndex = 5; // stage 5 => buildIndex >= 5



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

[SerializeField] private float bolusStepUnits = 0.1f;

[SerializeField] private float maxUnitsPerCheck = 1.0f;

[SerializeField] private float maxTotalUnitsForMealWindow = 15f;



[Header("PID (tune in Inspector)")]

[Tooltip("Proportional gain. Units: mg/dL compensation per (mg/dL error). Start ~0.6-1.2")]

[SerializeField] private float Kp = 0.9f;



[Tooltip("Integral gain. Units: mg/dL compensation per (mg/dL*min). Start small ~0.01-0.05")]

[SerializeField] private float Ki = 0.02f;



[Tooltip("Derivative gain. Units: mg/dL compensation per (mg/dL/min). Start ~0.5-2.0")]

[SerializeField] private float Kd = 1.2f;



[Header("Controller timing")]

[SerializeField] private float checkEveryGameMin = 5f;



[Header("Safety (game)")]

[SerializeField] private float doNotDoseBelow = 90f;

[SerializeField] private float deadbandMgdl = 2f; // ignore tiny noise around target



// --- internal time in game minutes ---

private double _absGameMinutes;



// --- meal window state ---

private bool _active;

private double _activeUntilGM;

private double _nextTickGM;

private float _remainingMealBudgetUnits;



// --- PID state ---

private float _integral;

private float _lastGlucose;

private double _lastSampleGM;

private bool _hasLastSample;



// --- IOB tracking (very simple linear decay over action window) ---

private struct Dose

{

public float units;

public double startGM; // when effect starts

public double endGM; // when effect ends

}

private readonly List<Dose> _doses = new();



private bool EnabledThisScene()

=> SceneManager.GetActiveScene().buildIndex >= enableFromBuildIndex;



private void Awake()

{

if (Instance != null && Instance != this) { Destroy(gameObject); return; }

Instance = this;

}



private void Update()

{

// keep absolute game minutes

float dtReal = Time.deltaTime;

if (dtReal <= 0f) return;



float gsrs = (Timer.Instance != null) ? Timer.Instance.GameSecondsPerRealSecond : 30f;

float gmThisFrame = dtReal * (gsrs / 60f);

_absGameMinutes += gmThisFrame;



CleanupOldDoses();



if (!_active) return;

if (_absGameMinutes >= _activeUntilGM)

{

_active = false;

return;

}



if (_absGameMinutes < _nextTickGM) return;



// run one control step

double nowGM = _absGameMinutes;

double prevTick = _nextTickGM;

_nextTickGM = nowGM + Math.Max(0.5f, checkEveryGameMin);



ControlStep(nowGM, prevTick);

}



/// <summary>

/// Call ONLY when player confirms meal report.

/// expectedFoodRiseMgdl: total planned rise (your "amountSigned" is fine here).

/// foodDelayGameMin: delay before rise begins (entryGameMin).

/// foodDurationGameMin: duration of rise.

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



// Activate controller starting when food starts rising (so it acts "during the rise").

double startGM = _absGameMinutes + Math.Max(0f, foodDelayGameMin);



_active = true;

_nextTickGM = startGM;



// Keep it active through the meal rise + some insulin tail

double mealEnd = startGM + Math.Max(0f, foodDurationGameMin);

_activeUntilGM = mealEnd + Math.Max(0f, insulinActionGameMin);



// Optional: reset PID state per meal (usually better in games)

_integral = 0f;

_hasLastSample = false;



// meal "budget": prevent endless dosing

_remainingMealBudgetUnits = maxTotalUnitsForMealWindow;



// (Optional) you could store carbsGrams/expectedFoodRiseMgdl if you want a feedforward term later.

// For now we do pure feedback PID on glucose + slope.

float unitsNeeded = expectedFoodRiseMgdl / mgdlPerUnitTotal;


// אופציונלי: אפשר לתת רק אחוז מסוים (למשל 80%) ולהשאיר ל-PID לתקן את השאר

float preBolus = unitsNeeded * 0.8f;


// בדיקת גבולות תקציב

preBolus = Mathf.Min(preBolus, _remainingMealBudgetUnits);


if (preBolus > 0)

{

DeliverMicroBolus(preBolus);

_remainingMealBudgetUnits -= preBolus;

// חשוב: זה מעדכן את ה-IOB מיידית, אז ה-PID לא יוסיף עוד סתם

}

}



private void ControlStep(double nowGM, double prevTickGM)

{

if (SugarMeter.Instance == null) return;



float glucose = SugarMeter.Instance.GetSugarLevel();



// time step (game minutes)

float dtGM = (float)Math.Max(0.5, nowGM - prevTickGM);



// Safety: don't dose if already low-ish

if (glucose <= doNotDoseBelow)

{

// bleed integral toward 0 so it doesn't "kick" later

_integral = Mathf.MoveTowards(_integral, 0f, 10f * dtGM);

RememberSample(glucose, nowGM);

return;

}



// Error with deadband

float error = glucose - targetSugar;

if (Mathf.Abs(error) < deadbandMgdl) error = 0f;



// Derivative on measurement (dG/dt) for noise robustness

float dGdt = 0f;

if (_hasLastSample)

{

float dt = (float)Math.Max(0.5, nowGM - _lastSampleGM);

dGdt = (glucose - _lastGlucose) / dt; // mg/dL per game min

}



// If below target (error<=0) we typically do NOT deliver in a pump-only model.

if (error <= 0f)

{

_integral = Mathf.MoveTowards(_integral, 0f, 5f * dtGM);

RememberSample(glucose, nowGM);

return;

}



// Candidate integral update (anti-windup: only commit if we end up dosing)

float integralCandidate = _integral + error * dtGM;



// PID output in "mg/dL compensation" (game abstraction)

float pidMgdl =

(Kp * error) +

(Ki * integralCandidate) +

(Kd * dGdt);



// Don't allow negative request from D term

pidMgdl = Mathf.Max(0f, pidMgdl);



// Convert mg/dL compensation -> units (1U == 30 mg/dL total over action window)

float unitsRequested = (mgdlPerUnitTotal <= 0.0001f) ? 0f : (pidMgdl / mgdlPerUnitTotal);



// Clamp per tick + remaining budget

unitsRequested = Mathf.Min(unitsRequested, maxUnitsPerCheck);

unitsRequested = Mathf.Min(unitsRequested, _remainingMealBudgetUnits);



// Subtract IOB (remaining active insulin)

float iob = ComputeIOBUnits();

unitsRequested = Mathf.Max(0f, unitsRequested - iob);



// Round to pump precision

unitsRequested = RoundToStep(unitsRequested, bolusStepUnits);



if (unitsRequested <= 0.0001f)

{

// If we didn't dose, don't accumulate integral (anti-windup)

RememberSample(glucose, nowGM);

return;

}



// Commit integral update since we actually dose

_integral = integralCandidate;



DeliverMicroBolus(unitsRequested);



_remainingMealBudgetUnits = Mathf.Max(0f, _remainingMealBudgetUnits - unitsRequested);



RememberSample(glucose, nowGM);

}



private void DeliverMicroBolus(float units)

{

if (SugarMeter.Instance == null) return;

if (units <= 0.0001f) return;



// Total drop over full action window:

float totalDrop = units * mgdlPerUnitTotal;



// Apply via your existing scheduled effect system

SugarMeter.Instance.DecreaseSugarGame(

amount: totalDrop,

durationGameMin: insulinActionGameMin,

delayGameMin: insulinStartDelayGameMin

);



double start = _absGameMinutes + Math.Max(0f, insulinStartDelayGameMin);

double end = start + Math.Max(0.5f, insulinActionGameMin);

_doses.Add(new Dose { units = units, startGM = start, endGM = end });

}



private float ComputeIOBUnits()

{

// Linear decay: full units at start -> 0 at end

double now = _absGameMinutes;

float sum = 0f;



for (int i = 0; i < _doses.Count; i++)

{

var d = _doses[i];



if (now <= d.startGM)

{

sum += d.units; // not started yet; still "on board" for safety

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

if (now >= _doses[i].endGM) _doses.RemoveAt(i);

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

}

}