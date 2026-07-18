# 🐙 Multimotek

**An interactive Unity game designed to improve knowledge and daily management of Type 1 Diabetes (T1D)**

Developed in collaboration with **The Center for Diabetes in Israel** (HaMerkaz LeMa'an Cholei Sukaret BeYisrael).

---

## 📖 About the Project

Type 1 Diabetes (T1D) is a chronic autoimmune condition that requires continuous, precise daily management — blood glucose monitoring, carbohydrate counting, and timely insulin administration. For newly diagnosed patients and their families, learning these skills can be overwhelming, and traditional educational materials often fail to sustain engagement.

**Multimotek** is a 2D platformer that turns real-world diabetes management into gameplay. Players guide **Multimotek**, a purple octopus living with T1D (its eight tentacles symbolizing the constant multitasking required to manage the disease), through a narrative-driven adventure. Player choices around food, insulin dosing, and daily routines directly affect the character's physiological state — letting patients and caregivers build confidence and practice decision-making without real-world risk.

The game was developed in close collaboration with clinical experts to ensure its mechanics are medically accurate, and was evaluated through structured playtesting with real patients and community members.

## ✨ Key Features

- **8 progressive levels**, moving from manual insulin syringes and finger-prick testing to Continuous Glucose Monitoring (CGM) and a full Automated Insulin Delivery (AID) pump system
- **Realistic pharmacokinetic simulation** — a "Superposition Engine" models how carbohydrates and insulin affect blood glucose over time, based on real glycemic index and absorption profiles
- **Dynamic HUD** with a glucose monitor, accelerated in-game clock, inventory, and an in-game reference guide
- **Full player autonomy** — players choose freely what to eat and how to treat, rather than following scripted choices
- **Emotional feedback system** — a Time-in-Range (TIR) score converts glycemic performance into a 0–5 "heart" rating reflecting the character's emotional well-being
- **Designed for the whole support ecosystem** — patients, family members, and caregivers alike
- **Cross-platform** — playable on Web (keyboard/mouse) and Mobile (touch controls)

## 🎮 How to Play

1. Navigate Multimotek through platforming levels while keeping blood glucose within the healthy target range of **70–180 mg/dL**
2. Collect food items and insulin syringes, and manage meals via the carbohydrate-counting interface
3. Find the hidden artifact in each level to unlock the exit
4. Avoid hazards (spikes, pits, hostile enemies) that affect glucose levels
5. Complete the level and review your Time-in-Range performance before moving on

| Level | Focus | Technology Unlocked |
|---|---|---|
| 1–2 | Manual glucose testing & syringe dosing | Manual only |
| 3 | Automatic real-time monitoring | CGM |
| 4 | Nocturnal hypoglycemia scenario | CGM |
| 5 | Introduction to automated dosing | Basic Insulin Pump |
| 6 | Advanced pump target tuning | Advanced Pump |
| 7 | Full management mastery | Advanced Pump |
| 8 | Narrative resolution & reflection | Advanced Pump |

## 🛠️ Built With

- **[Unity](https://unity.com/)** — 2D physics, tilemaps, and UI Toolkit
- **C#** — game logic and physiological simulation, using OOP and a modular component-based architecture
- **Universal Render Pipeline (URP)** — post-processing effects (blur, vignette, dynamic lighting) to visualize hypoglycemia and hyperglycemia
- **Git / GitHub** — version control with a branching strategy supporting parallel WebGL and Android builds

## 📊 Evaluation Results

Multimotek was evaluated through a pre-test/post-test study with **27 participants** (T1D patients, caregivers, and the general public) who each completed at least 7 levels of the game.

| Group | Pre-Test | Post-Test | Gain |
|---|---|---|---|
| T1D Patients | 75.8% | 94.0% | +18.2% |
| Support Ecosystem (family/caregivers) | 74.5% | 94.5% | +20.0% |
| General Public | 67.5% | 92.5% | +25.0% |
| **Overall** | **74.0%** | **94.0%** | **+20.0%** |

Results indicate a significant, consistent improvement in diabetes-related knowledge across all participant groups after playing.

## 🚀 Future Work

- Full mobile deployment (Android/iOS optimization)
- Multi-language localization (currently Hebrew; English, Russian, Arabic planned)
- Improved animations and professional art assets
- Original music and reactive sound effects
- Additional levels covering scenarios like sick days and eating out

## 🙏 Acknowledgements

Special thanks to Prof. Erel Segal-Halevi for academic guidance, and to Elinor Benizri (CEO, The Center for Diabetes in Israel) and Yonatan Izhaki for their clinical expertise, playtesting support, and help recruiting study participants.

---

*This project was developed as part of a final project at the School of Computer Science, Ariel University.*
