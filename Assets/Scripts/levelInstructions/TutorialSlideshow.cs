using System.Collections.Generic;

using UnityEngine;

using UnityEngine.UI;



public class TutorialSlideshow : MonoBehaviour

{

[Header("Wiring")]

[SerializeField] private GameObject rootPanel;

[SerializeField] private Image slideImage;

[SerializeField] private Button prevButton;

[SerializeField] private Button nextButton;



[Header("Slides")]

[SerializeField] private List<Sprite> slides = new List<Sprite>();



[Header("Behavior")]

[Tooltip("כאשר לוחצים 'ימינה' על השקופית האחרונה – לסגור את הפאנל")]

[SerializeField] private bool closeOnLastNext = true;



[Tooltip("להציג רק פעם אחת בשלב הזה (נשמר ב-PlayerPrefs)")]

[SerializeField] private bool showOnce = true;



[Tooltip("מפתח לשמירה אם כבר הוצג (החליפי לפי שם השלב)")]

[SerializeField] private string playerPrefsKey = "Level1_TutorialShown";



private int index = 0;



private void Awake()

{

if (prevButton) prevButton.onClick.AddListener(OnPrev);

if (nextButton) nextButton.onClick.AddListener(OnNext);



// אם אין הפניות – ננסה לנחש את ה-rootPanel כאובייקט הנוכחי

if (!rootPanel) rootPanel = gameObject;

}



private void Start()

{

// להציג פעם אחת בלבד?

if (showOnce && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)

{

if (rootPanel) rootPanel.SetActive(false);

enabled = false;

return;

}



// להבטיח שיש מה להציג

if (slides == null || slides.Count == 0 || slideImage == null)

{

Debug.LogWarning("[TutorialSlideshow] No slides or slideImage assigned");

if (rootPanel) rootPanel.SetActive(false);

enabled = false;

return;

}



index = 0;

if (rootPanel) rootPanel.SetActive(true);

ApplySlide();

UpdateButtons();

}



private void OnPrev()

{

if (index > 0)

{

index--;

ApplySlide();

UpdateButtons();

}

}



private void OnNext()

{

if (index < slides.Count - 1)

{

index++;

ApplySlide();

UpdateButtons();

}

else

{

// אנחנו בשקופית האחרונה

if (closeOnLastNext)

{

ClosePanel();

}

}

}



private void ApplySlide()

{

if (slideImage && index >= 0 && index < slides.Count)

{

slideImage.sprite = slides[index];



//slideImage.SetNativeSize();

}

}



private void UpdateButtons()

{

if (prevButton) prevButton.interactable = index > 0;



if (nextButton)

{

nextButton.interactable = true;

}

}



private void ClosePanel()

{

if (showOnce) PlayerPrefs.SetInt(playerPrefsKey, 1);

if (rootPanel) rootPanel.SetActive(false);



}


private void Update()

{

if (!rootPanel || !rootPanel.activeSelf) return;



if (Input.GetKeyDown(KeyCode.RightArrow)) OnNext();

if (Input.GetKeyDown(KeyCode.LeftArrow)) OnPrev();

if (Input.GetKeyDown(KeyCode.Escape)) ClosePanel();

}

}