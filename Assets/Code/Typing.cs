#if UNITY_WEBGL
	#define BACKSPACE_KEY_WORKAROUND
#endif

using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Typing : MonoBehaviour {
	
	/*
	 * TODO: Decouple the UI code from the typing progress logic, so modes can be implemented
	 *
	 * Maybe this could be turned into a 'Base UI' class, for things like:
	 *		- Test itself
	 *		- Buttons on the bottom
	 *		- Text UI on top, controlled by external code for the actual game mode
	 * 
	 * It may be easiest to create a new file and use this as a reference to copy from.
	 * The new file(s) should consider the following:
	 *		- Theme data should still be stored in one single location (such as this 'Typing' singleton class)
	 *		- All UI and mode-specific code should be done in separate, mode-specific classes
	 *		- The base class would likely handle quote selection, typing progress, timing, score calculations (per-key, WPM, etc.)
	 *
	 * Ideas for 'mode' implementation:
	 *		1: Decouple the UI/theme and other singleton code from the test logic
	 *			For example, if the modes inherit from 'Typing', they don't need to have their own themes and settings defined for each one)
	 *			All of that code would be moved elsewhere, and this class would become something that modes can inherit from.
	 *		2: This class is stripped of all test logic, and calls the test functions from an external reference
	 *			This class would remain a singleton and handle UI/graphs (likely renamed to 'BaseUI'),
	 *			but it would keep a reference to a 'TypingTest' instance - which the actual modes could inherit from,
	 *			and would call functions from there. The 'BaseUI' class could still handle the progress logic
	 *			and call 'Done()' on the 'TypingTest', so it can decide what to do. They may need to communicate with each other.
	 *
	 * For consideration: This class currently handles the following:
	 *		- Selects new quotes
	 *		- The test itself
	 *		- Test-related UI/info, as well as transitions
	 *		- General UI like the buttons at the bottom
	 *		- The themes used in the application
	 *		- Controls what the buttons at the bottom do
	 *		- Manages opening/closing other UI elements (settings window, graph UI)
	 * Which of these are fine to keep in a singleton class, and which should be considered mode-specific?
	 *		- Quote picking/progress and the choice of what to display during tests should be done in a dedicated class which modes can inherit from
	 *		- Theming, buttons/button logic, transitions, any actual drawing of the UI could still be handled here
	 * 
	 *		For example: the test chooses to display the WPM, the average scores, and top scores, which this class then draws.
	 *		During the test, the typing progress (and all KeyManager/score calculation) is handled by the mode itself. It constructs a 'progress' string
	 *		which this class then uses to display the text/errors/etc on screen.
	 *		After the test, the mode tells this class what to do (e.g., draw graph, score the player using a star system, etc.)
	 * 
	 * Modes:
	 *		- Consistency Mode
	 *		- Accuracy Mode
	 *		- Speed Mode
	 *		- Guided Mode
	 */
 
	/*
	 * IDEA: Attempt to select quotes that the user can type in under a minute (or a chosen timeframe)
	 * or allow trimming the text to still practice the letters/characters chosen for the lesson
	 */

	public static Typing instance;
	public static Theme currentTheme;
	
	public DrawGraph graph;
	public Image graphOutline;
	public RectTransform graphOutlineTransform;
	
	public RectTransform graphTooltipSpeed;
	public TMP_Text graphTooltipSpeedText;
	
	public RectTransform graphTooltipWordSpeed;
	public TMP_Text graphTooltipWordSpeedText;
	
	public RectTransform graphTooltipSeekTime;
	public TMP_Text graphTooltipSeekTimeText;
	
	public RectTransform graphTooltipAccuracy;
	public TMP_Text graphTooltipAccuracyText;
	
	public RectTransform graphTooltipTimestamp;
	public TMP_Text graphTooltipTimestampText;

	public GameObject keyboardOverlay;
	
	// public Font interfaceFont;
	string text = "If you're seeing this text, it means something went wrong with the application",
	       input = "",
	       lastInput = "";
	int loc = -1;
	int lastLength,lastMaxLength = -1;
	float seekTime,wordTime,totalTestTime;
	bool incorrect,done,started = true;
	public static int curPracticeIndex;
	public static string quoteTitle;
	public TMP_InputField textDisplay;
	TMP_Text textDisplayText;
	public TMP_Text lessonInfo,
	                WPMInfo,
	                averageWPMInfo,
	                quoteInfo;
	public Button quoteInfoButton;
	
	public GameObject settingsUI;
	public GameObject[] settingsMenus;
	public Toggle practiceUppercase,
	              practiceNumbers,
	              practiceSymbols;
	public TMP_Dropdown errorMode;
	public Slider charVarietySlider,
	              quoteDifficultySlider,
	              modeBiasSlider;
	public RectTransform textTransform;
	public GameObject lightModeButton,
	                  darkModeButton;
	
	public Image backgroundImage,fadeImage;
	[Range(0, 1)]public float defaultFade = 0f, fadeAmount = .5f;
	float backgroundFade;	// 0 to 1
	bool fade, lastFade;
	Vector2 lastMousePos;
	Color targetFadeColor = new Color(0, 0, 0, 0);
	
	RectTransform caretTransform;
	Vector3 initialTextPos, initialCaretPos;
	
	[Serializable]
	public struct Theme {
		public string name;
		public Color backgroundColor,
		             textColorUI,
		             textColorQuote,
		             textColorError,
		             textColorWarning,
		             textColorCorrect,
		             caretColor,
		             caretColorError,
		             improvementColor,
		             regressionColor,
		             mildRegressionColor,
		             buttonColor,
		             iconColor;
		public Color tooltipBackgroundColor,
		             tooltipTextColor;
		[NonSerialized]
		public string textColorErrorTag,
		              textColorWarningTag,
		              textColorCorrectTag,
		              improvementColorTag,
		              regressionColorTag,
		              mildRegressionColorTag;
	}
	public int selectedTheme,lastSelectedTheme;
	public Theme[] themes;
	public GameObject[] themeableElements;
	public Image[] themableTooltips;
	
	public bool showGraphWhenDone=true;
	
	RectTransform graphTransform;
	int lastHoverIndex = -1;
	bool showGraph,lastShowGraph;
	float graphBlend;
	float defaultGraphHeight;
	Vector3 defaultGraphPos;
	
	int hitCount,missCount;
	string[] words;
	int wordIndex;
	
	bool showMenu;

	void Start() {
		Application.targetFrameRate = (int)(Screen.currentResolution.refreshRateRatio.value + .49);
		
		Load();

		textDisplayText = textDisplay.GetComponentInChildren<TMP_Text>();
		
		// graphTransform = graph.rectTransform;
		graphTransform = graph.transform.parent.GetComponent<RectTransform>();
		defaultGraphHeight = graphTransform.rect.height;
		defaultGraphPos = graphTransform.anchoredPosition;

		initialTextPos = textTransform.localPosition;
		caretTransform = textTransform.parent.Find("Caret").GetComponent<RectTransform>();
		initialCaretPos = caretTransform.localPosition;
		instance = this;
		NextLesson();
		
		UpdateTheme();
		ChangeTheme(PlayerPrefs.GetInt("selectedTheme",selectedTheme));
		lightModeButton.SetActive(selectedTheme == 0);
		darkModeButton.SetActive(selectedTheme != 0);
		
		OnTextInputHandler();
	}
	public void Save() {
		PlayerPrefs.SetInt("includeUppercase", KeyManager.includeUppercase ? 1 : 0);
		PlayerPrefs.SetInt("includeNumbers", KeyManager.includeNumbers ? 1 : 0);
		PlayerPrefs.SetInt("includeSymbols", KeyManager.includeSymbols ? 1 : 0);
		PlayerPrefs.SetFloat("charPracticeDifficulty", KeyManager.charPracticeDifficulty);
		PlayerPrefs.SetFloat("quoteDifficulty", KeyManager.quoteDifficulty);
		PlayerPrefs.SetFloat("practiceModeBias", KeyManager.modeBias);
		PlayerPrefs.SetInt("errorMode", errorMode.value);
		PlayerPrefs.SetInt("selectedTheme", selectedTheme);
	}
	
	public void Load() {
		practiceUppercase.isOn = KeyManager.includeUppercase =
			PlayerPrefs.GetInt("includeUppercase", KeyManager.includeUppercase ? 1 : 0) == 1;
		practiceNumbers.isOn = KeyManager.includeNumbers =
			PlayerPrefs.GetInt("includeNumbers", KeyManager.includeNumbers ? 1 : 0) == 1;
		practiceSymbols.isOn = KeyManager.includeSymbols =
			PlayerPrefs.GetInt("includeSymbols", KeyManager.includeSymbols ? 1 : 0) == 1;
		charVarietySlider.value = KeyManager.charPracticeDifficulty =
			PlayerPrefs.GetFloat("charPracticeDifficulty", KeyManager.charPracticeDifficulty);
		charVarietySlider.value *= charVarietySlider.value;
		quoteDifficultySlider.value = KeyManager.quoteDifficulty =
			PlayerPrefs.GetFloat("quoteDifficulty", KeyManager.quoteDifficulty);
		quoteDifficultySlider.value *= quoteDifficultySlider.value;
		modeBiasSlider.value = KeyManager.modeBias =
			PlayerPrefs.GetFloat("practiceModeBias", KeyManager.modeBias);
		errorMode.value = PlayerPrefs.GetInt("errorMode", 0);
	}
	
	public void UpdateTheme() {
		textDisplayText.color = themes[selectedTheme].textColorQuote;
		var quoteInfoColors = quoteInfoButton.colors;
		lessonInfo.color =
			WPMInfo.color =
			averageWPMInfo.color =
			quoteInfoColors.normalColor =
			quoteInfoColors.selectedColor =
			themes[selectedTheme].textColorUI;
		quoteInfoButton.colors = quoteInfoColors;
		targetFadeColor =
			backgroundImage.color =
			fadeImage.color =
			themes[selectedTheme].backgroundColor;
		
		currentTheme = themes[selectedTheme];
		graph.color = themes[selectedTheme].textColorCorrect;
		graph.diamondColor = themes[selectedTheme].textColorUI;
		
		themes[selectedTheme].textColorErrorTag =
			"<color=#" + ColorUtility.ToHtmlStringRGB(themes[selectedTheme].textColorError) + ">";
		themes[selectedTheme].textColorWarningTag =
			"<color=#" + ColorUtility.ToHtmlStringRGB(themes[selectedTheme].textColorWarning) + ">";
		themes[selectedTheme].textColorCorrectTag =
			"<color=#" + ColorUtility.ToHtmlStringRGB(themes[selectedTheme].textColorCorrect) + ">";
		themes[selectedTheme].improvementColorTag =
			"<color=#" + ColorUtility.ToHtmlStringRGB(themes[selectedTheme].improvementColor) + ">";
		themes[selectedTheme].regressionColorTag =
			"<color=#" + ColorUtility.ToHtmlStringRGB(themes[selectedTheme].regressionColor) + ">";
		themes[selectedTheme].mildRegressionColorTag =
			"<color=#" + ColorUtility.ToHtmlStringRGB(themes[selectedTheme].mildRegressionColor) + ">";

		WPMInfo.text = WPMInfo.text
			.Replace(themes[lastSelectedTheme].improvementColorTag,themes[selectedTheme].improvementColorTag)
			.Replace(themes[lastSelectedTheme].regressionColorTag,themes[selectedTheme].regressionColorTag)
			.Replace(themes[lastSelectedTheme].mildRegressionColorTag,themes[selectedTheme].mildRegressionColorTag);
		averageWPMInfo.text = averageWPMInfo.text
			.Replace(themes[lastSelectedTheme].improvementColorTag,themes[selectedTheme].improvementColorTag)
			.Replace(themes[lastSelectedTheme].regressionColorTag,themes[selectedTheme].regressionColorTag)
			.Replace(themes[lastSelectedTheme].mildRegressionColorTag,themes[selectedTheme].mildRegressionColorTag);
		lessonInfo.text = lessonInfo.text
			.Replace(themes[lastSelectedTheme].improvementColorTag,themes[selectedTheme].improvementColorTag)
			.Replace(themes[lastSelectedTheme].regressionColorTag,themes[selectedTheme].regressionColorTag)
			.Replace(themes[lastSelectedTheme].mildRegressionColorTag,themes[selectedTheme].mildRegressionColorTag);
		textDisplay.text = textDisplay.text
			.Replace(themes[lastSelectedTheme].textColorCorrectTag,themes[selectedTheme].textColorCorrectTag);
		
		if (KeyConfidenceMap.instance)
			KeyConfidenceMap.instance.UpdateTheme();
		
		foreach (var tooltip in themableTooltips){
			tooltip.color = currentTheme.tooltipBackgroundColor;
			foreach (var text in tooltip.GetComponentsInChildren<Text>(true)) {
				text.color = currentTheme.tooltipTextColor;
			}
			foreach (var text in tooltip.GetComponentsInChildren<TMP_Text>(true)){
				text.color = currentTheme.tooltipTextColor;
			}
		}
		foreach (var element in themeableElements) {
			foreach (var img in element.GetComponentsInChildren<Image>(true)) {
				switch(img.name) {
					case "Arrow": case "Item Checkmark": {
						img.color = currentTheme.textColorUI;
						continue;
					}
					case "Icon": {
						img.color = currentTheme.iconColor;
						continue;
					}
					default: {
						img.color = currentTheme.buttonColor;
						continue;
					}
				}
			}
			foreach (var text in element.GetComponentsInChildren<TMP_Text>(true)) {
				text.color = currentTheme.textColorUI;
			}
		}
	}
	public void ChangeTheme(int theme) {
		if (theme != selectedTheme) lastSelectedTheme = selectedTheme;
		selectedTheme = theme;
		UpdateTheme();
	}

	private void OnApplicationFocus(bool hasFocus) {
		if (!hasFocus && !done) ResetLesson();
	}

	public void SelectPracticeByCharacter(char c) {
		textTransform.localPosition = initialTextPos;
		caretTransform.localPosition = initialCaretPos;
		curPracticeIndex = KeyManager.GetKeyIndex(c);
		text = KeyManager.GetQuoteByCharFrequency(ref curPracticeIndex, ref quoteTitle);
		ResetLesson();
	}
	
	public void NextLesson() {
		textTransform.localPosition = initialTextPos;
		caretTransform.localPosition = initialCaretPos;
		if (UnityEngine.Random.Range(0f, 1f) > KeyManager.modeBias) {
			curPracticeIndex = KeyManager.GetLowConfidenceCharacter();
			text = KeyManager.GetQuoteByCharFrequency(ref curPracticeIndex, ref quoteTitle);
		} else {
			curPracticeIndex = -2; 
			text = KeyManager.GetQuoteByOverallScore(ref quoteTitle, ref curCharPractice);
		}
		ResetLesson();
	}
	public void ResetLesson() {
		started = true;
		done = false;
		MoveTooltipsOffScreen();
		
		ToggleGraphUI(false);
		graph.speedValues = new float[text.Length-1];
		graph.accuracy = new float[text.Length-1];
		graph.misses = new int[text.Length-1];
		graph.seekTimes = new float[text.Length-1];
		graph.times = new float[text.Length-1];
		words = KeyManager.GetWordsInText(text);
		graph.wordTimes = new float[words.Length];
		graph.wordSpeedValues = new float[words.Length];
		wordIndex = 0;
		lastHoverIndex = graph.currentIndex = -1;
		graph.speedValueScale = graph.wordSpeedScale = graph.timeScale = 1;
		graph.SetVerticesDirty();
		
		UpdateCurrentPracticeUI();
		hitCount = missCount = 0;
		input = lastInput = "";
		lastFrameIncorrect = true;
		incorrect = done = fade = false;
		textDisplay.readOnly = false;
		seekTime = wordTime = totalTestTime = 0;
		loc = lastLength = lastMaxLength = -1;
		
		graphTooltipTimestampTargetPos =
			graphTooltipSpeedTargetPos =
			graphTooltipWordSpeedTargetPos =
			graphTooltipSeekTimeTargetPos =
			graphTooltipAccuracyTargetPos =
			new Vector2((float)Screen.width / 2, -100);
		
		FocusInputField();
	}
	public void ToggleGraphUI() {
		showGraph = done && !showGraph;
	}
	public void ToggleGraphUI(bool show) {
		showGraph = show;
	}
	public void OpenWikiPage() {
		if (quoteTitle == null)
			return;
		string wikiPage = $"https://en.wikipedia.org/wiki/{quoteTitle.Split("Wikipedia - ")[^1].Replace(' ', '_')}";
		Debug.Log(wikiPage);
		Application.OpenURL(wikiPage);
	}
	public bool settingsOpen;
	public void ToggleSettingsUI() {
		settingsOpen = !settingsOpen;
		settingsUI.SetActive(settingsOpen);
		foreach (GameObject menu in settingsMenus) {
			menu.SetActive(menu.activeSelf && settingsOpen);
		}
		textDisplay.readOnly = !settingsOpen && !done;

		if (!settingsOpen) {
			KeyManager.Save();
		}
	}
	
	public string TimeFormattedString(float time, bool keepMinutes = false, bool keepFractions = false) {
		bool negative = time < 0;
		time = Mathf.Abs(time);
		int seconds = Mathf.FloorToInt(time % 60);
		string fractions = ((float)Math.Round(
			time - Mathf.FloorToInt(time), 3) % 1
		).ToString().Split('.')[^1];
		for (int i = fractions.Length; i < 3; i++) {
			fractions += '0';
		}
		return time > 60 || keepMinutes ? 
			$"{(negative ? '-' : "")}{Mathf.FloorToInt(time / 60)}:{(seconds < 10 ? "0" : "")}{seconds}{(keepFractions ? $":{fractions}" : "")}":
			$"{(negative ? '-' : "")}{seconds}{(keepFractions ? $":{fractions}" : "")}";
	}
	
	KeyManager.KeyConfidenceData curCharPractice;
	char curCharacterPractice;
	string curCharacterSpeedTrend;
	string curCharacterSeekTime;
	string curCharacterNextSeekTime;
	string curCharacterWPM;
	string curCharacterAccuracy;

	void UpdateCurrentPracticeUI(int comparisonIndex = -1) {
		KeyManager.KeyConfidenceData updatedCharPractice =
			curPracticeIndex == -2 ?
				KeyManager.GetQuoteConfidenceData(text):
				KeyManager.instance.confidenceDatabase[curPracticeIndex];
		curCharacterPractice = updatedCharPractice.keyName;
		curCharacterSpeedTrend = $"{System.Math.Round(updatedCharPractice.speedTrend, 2)} WPM";
		curCharacterSeekTime = updatedCharPractice.seekTime > 0 ?
			Math.Round(updatedCharPractice.seekTime * 1000, 2) + " ms":
			"-";
		curCharacterNextSeekTime = updatedCharPractice.nextKeySeekTime > 0 ?
			Math.Round(
				Mathf.Max(
					updatedCharPractice.nextKeySeekTime * 1000,
					updatedCharPractice.previousKeySeekTime * 1000
				),
				2
			) + " ms":
			"-";
		curCharacterWPM = updatedCharPractice.wordSpeed > 0 ?
			Math.Round(updatedCharPractice.wordSpeed , 1) + " WPM":
			"-";
		curCharacterAccuracy = updatedCharPractice.hits + updatedCharPractice.misses > 0 ?
			Math.Round(
				updatedCharPractice.accuracy * 100,
				1
			) + "%":
			"-";

		if (comparisonIndex != -1) {
			KeyManager.KeyConfidenceData compare =
				comparisonIndex == -2 ?
					KeyManager.GetQuoteConfidenceData(text):
					KeyManager.instance.confidenceDatabase[comparisonIndex];
			float diff = (float)Math.Round(
				updatedCharPractice.seekTime * 1000 - curCharPractice.seekTime * 1000,
				3
			);
			curCharacterSeekTime = diff <= 0 ?
				$"{themes[selectedTheme].improvementColorTag + curCharacterSeekTime} ({diff})</color>":
				$"{themes[selectedTheme].regressionColorTag + curCharacterSeekTime} (+{diff})</color>";
			diff = (float)Math.Round(
				Mathf.Max(
					updatedCharPractice.nextKeySeekTime,
					updatedCharPractice.previousKeySeekTime
				) * 1000 - Mathf.Max(
					curCharPractice.nextKeySeekTime,
					curCharPractice.previousKeySeekTime
				) * 1000,
				3
			);
			curCharacterNextSeekTime = diff is <= 0 or float.NaN?
				$"{themes[selectedTheme].improvementColorTag + curCharacterNextSeekTime} ({diff})</color>":
				$"{themes[selectedTheme].regressionColorTag + curCharacterNextSeekTime} (+{diff})</color>";
			diff = (float)Math.Round(updatedCharPractice.wordSpeed - curCharPractice.wordSpeed, 3);
			if (updatedCharPractice.speedTrend != 0) {
				curCharacterSpeedTrend=$"{(updatedCharPractice.speedTrend<Mathf.Min(0,curCharPractice.speedTrend)?themes[selectedTheme].regressionColorTag:(updatedCharPractice.speedTrend>0?themes[selectedTheme].improvementColorTag:themes[selectedTheme].mildRegressionColorTag))}{curCharacterSpeedTrend}</color>";
			}
			curCharacterWPM = diff >= 0 ?
				$"{themes[selectedTheme].improvementColorTag + curCharacterWPM} (+{diff})</color>":
				$"{themes[selectedTheme].regressionColorTag + curCharacterWPM} ({diff})</color>";
			float newAccuracy = updatedCharPractice.accuracy * 100;
			diff = Misc.ValidateIfNaN(
				(float)Math.Round(
					newAccuracy - curCharPractice.accuracy * 100,
					3
				),
				newAccuracy
			);
			curCharacterAccuracy = diff >= 0 ?
				$"{themes[selectedTheme].improvementColorTag + curCharacterAccuracy} (+{diff})</color>":
				$"{themes[selectedTheme].regressionColorTag + curCharacterAccuracy} ({diff})</color>";
		}
		curCharPractice = updatedCharPractice;
		
		lessonInfo.text =
			$"<b>Practice Focus: {(curCharacterPractice=='\0'?"multiple keys":curCharacterPractice)}</b>"+
			$"\n<b>Average Stats </b>(for <b>{(curCharacterPractice=='\0'?"multiple keys":curCharacterPractice)}</b>)<b>:</b>"+
			$"\nSeek Time: {curCharacterSeekTime}"+
			$"\nContextual Seek Time: {curCharacterNextSeekTime}"+
			$"\nImprovement Trend: {curCharacterSpeedTrend}"+
			// $"\nWord Speed: {curCharacterWPM}"+
			$"\nAccuracy: {curCharacterAccuracy}";
		
		//TODO: Idea: Show practice difficulty (could be a slider (with a gradient background), a bar with representative colors (red/orange/green), a star system, pre-defined words (e.g. "easy", "mildly difficult", "very difficult"), etc.)
		
		wpm = loc / totalTestTime * 60 / 5;
		accuracy = (float)hitCount / (hitCount + missCount) * 100;
		float oldAverageAccuracy = KeyManager.averageAccuracy;
		float oldAverageSpeed = KeyManager.averageWPM;
		float oldTopSpeed = KeyManager.topWPM;
		if (done) {
			if (wpm > KeyManager.topWPM) KeyManager.topWPM = wpm;
			if (accuracy > 0)
				KeyManager.averageAccuracy = Mathf.Lerp(
					KeyManager.averageAccuracy,
					accuracy,
					KeyManager.averageAccuracy > 0 ? .12f : 1
				);
			if (wpm > 0)
				KeyManager.averageWPM =
					Mathf.Lerp(KeyManager.averageWPM, wpm, KeyManager.averageWPM > 0 ? .12f : 1);
			if (float.IsNaN(accuracy)) accuracy = 100;
			WPMInfo.text =
				"Accuracy: " +
					(accuracy >= oldAverageAccuracy ?
						$"{themes[selectedTheme].improvementColorTag+Math.Round(accuracy,2)}% (+{Math.Round(accuracy-oldAverageAccuracy,2)} from average)</color>":
						$"{themes[selectedTheme].regressionColorTag+Math.Round(accuracy,2)}% ({Math.Round(accuracy-oldAverageAccuracy,2)} from average)</color>")+
				"\nSpeed: " +
					(wpm >= oldAverageSpeed ?
						$"{themes[selectedTheme].improvementColorTag+Math.Round(wpm,2)} WPM (+{Math.Round(wpm-oldAverageSpeed,2)} from average)</color>":
						$"{themes[selectedTheme].regressionColorTag+Math.Round(wpm,2)} WPM ({Math.Round(wpm-oldAverageSpeed,2)} from average)</color>")+
				$"\nTime: " +
					(totalTestTime < estimatedTime ?
						$"{themes[selectedTheme].improvementColorTag}{TimeFormattedString(totalTestTime,true)} ({TimeFormattedString(totalTestTime-estimatedTime,true,true)} from estimate)</color>":
						$"{themes[selectedTheme].regressionColorTag}{TimeFormattedString(totalTestTime,true)} (+{TimeFormattedString(totalTestTime-estimatedTime,true,true)} from estimate)</color>");
			
			averageWPMInfo.text =
				"Average Accuracy: " +
					(KeyManager.averageAccuracy >= oldAverageAccuracy ?
						$"{themes[selectedTheme].improvementColorTag}{Math.Round(KeyManager.averageAccuracy,2)}% (+{Math.Round(KeyManager.averageAccuracy-oldAverageAccuracy,2)})</color>":
						$"{themes[selectedTheme].regressionColorTag}{Math.Round(KeyManager.averageAccuracy,2)}% ({Math.Round(KeyManager.averageAccuracy-oldAverageAccuracy,2)})</color>")+
				"\nAverage Speed: " +
					(KeyManager.averageWPM >= oldAverageSpeed ? 
						$"{themes[selectedTheme].improvementColorTag}{(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")} WPM (+{Math.Round(KeyManager.averageWPM-oldAverageSpeed,3)})</color>":
						$"{themes[selectedTheme].regressionColorTag}{(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")} WPM ({Math.Round(KeyManager.averageWPM-oldAverageSpeed,3)})</color>")+
				"\nTop Speed: " +
					(KeyManager.topWPM > oldTopSpeed ?
						$"{themes[selectedTheme].improvementColorTag}{(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")} WPM(+{KeyManager.topWPM-oldTopSpeed})</color>":
						$"{(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")} WPM");
		} else {	
			averageWPMInfo.text =
				$"Average Accuracy: {Math.Round(KeyManager.averageAccuracy,2)}%" + 
				$"\nAverage Speed: {(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")} WPM" + 
				$"\nTop Speed: {(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")} WPM";
		}
		
		quoteInfo.text = quoteTitle;
		
		if (Time.time > 5) KeyManager.Save();
	}
	
	bool inputFieldFocused;
	
	public void FocusInputField() {
		inputFieldFocused = true;
	}
	public void UnfocusInputField() {
		inputFieldFocused = false;
	}
	
	bool lastFrameIncorrect = true;
	void SetTextColor() {
		int cappedLoc = Mathf.Min(loc + 1,text.Length);
		string content = $"{(incorrect?themes[selectedTheme].textColorWarningTag:themes[selectedTheme].textColorCorrectTag)}{text.Insert(cappedLoc, "</color>")}";
		if (incorrect) {
			int lengthDiff = content.Length - text.Length;
			
			const int
				showTypos = 0,
				highlightTypos = 1,
				eraseErrors = 2,
				eraseWords = 3;
				
			switch (errorMode.value) {
				case showTypos: {
					if (input.Length <=  loc)	return;
					char[] chars = input.Remove(0, loc + 1).ToCharArray();
					for (int i = 0; i < chars.Length; i++) {
						switch (chars[i]) {
							default: {
								continue;
							}
							case ' ': {
								chars[i] = '␣';
								break;
							}
							case '\t': {
								chars[i] = '↹';
								break;
							}
							case '\n': {
								chars[i] = '↵';
								break;
							}
						}
					}
					content = content.Insert(
						cappedLoc + lengthDiff,
						$"{themes[selectedTheme].textColorErrorTag}<u>{new string(chars)}</u></color>"
					);
					break;
				}
				case highlightTypos: {
					int incorrectStart = cappedLoc + lengthDiff,
					    incorrectEnd = Mathf.Min(input.Length, text.Length) + lengthDiff;
					char[] chars = content.ToCharArray();
					for (int i = Mathf.Max(incorrectStart - 1, 0); i < incorrectEnd; i++) {
						switch (chars[i]) {
							default: {
								continue;
							}
							case ' ': {
								chars[i] = '␣';
								break;
							}
							case '\t': {
								chars[i] = '↹';
								break;
							}
							case '\n': {
								chars[i] = '↵';
								break;
							}
						}
					}
					content = new string(chars)
						.Insert(incorrectEnd, "</u></color>")
						.Insert(incorrectStart, "<color=red><u>");
					break;
				}
				case eraseErrors: {
					EraseInputCharacter(false);
					break;
				}
				case eraseWords: {
					EraseInputCharacter(true);
					break;
				}
			}
		}
		textDisplay.text = content;
		textDisplay.caretPosition = Mathf.Min(input.Length, text.Length);
	}
	
	public void ProcessEscapeKey(InputAction.CallbackContext context) {
		if (!context.started) return;
		if (fade){
			ResetLesson();
			return;
		}
		if (settingsOpen) {
			ToggleSettingsUI();
		} else {
			ToggleGraphUI();
		}
	}
	
	public void ProcessReturnKey(InputAction.CallbackContext context) {
		if (!context.started) return;
		if (!fade) {
			if (settingsOpen) {
				ToggleSettingsUI();
			} else {
				ToggleGraphUI();
			}
		}
		if (done) {
			NextLesson();
		}
	}
	
	public void ProcessTabKey(InputAction.CallbackContext context){
		if (!context.started || settingsOpen) return;
		NextLesson();
	}
	
	void OnTextInputHandler() {
		Keyboard.current.onTextInput += inputChar => {
			if (done || settingsOpen || !inputFieldFocused) {
				return;
			}
			
			//Debug.Log($"{(byte)inputChar}:	{inputChar}");
			switch ((byte)inputChar) {
				case 8:		// Backspace
					#if !BACKSPACE_KEY_WORKAROUND
						EraseInputCharacter(false);
					#endif
					break;
				case 9:     // Tab
				case 27:    // Escape
				case 127:   // Delete
					break;	// ^ Ignore the above inputs
				case 13:    // Enter
					if (totalTestTime > .1f)
						input += '\n';
					break;
				case <127:
					if (Keyboard.current.ctrlKey.isPressed) break;
					input += inputChar;
					break;
				default:
					Debug.Log($"Pressed: {(byte)inputChar}: '{inputChar}'");
					break;
			}
		};
	}
	
	void EraseInputCharacter(bool word) {
		if (done || settingsOpen || !inputFieldFocused) return;
		int rm = 1;
		if ((!word && !Keyboard.current.ctrlKey.isPressed &&
			!Keyboard.current.leftAppleKey.isPressed) ||
			input.Length < 2
		) {
			goto set_input;
		}
		for (rm = 2; rm < input.Length; rm++) {
			if (!KeyManager.IsAlphanumericCharacter(input[^rm])) {
				rm--;
				goto set_input;
			}
			
			// switch (input[^rm]) {
			// 	case '\n': case ' ': case '\t': {
			// 		rm--;
			// 		goto set_input;
			// 	}
			// }
		}
		set_input: {
			rm = Mathf.Min(rm, input.Length);
			input = input.Remove(input. Length - rm, rm);
		}
		if (input.Length == 0) ResetLesson();
	}
	
	#if BACKSPACE_KEY_WORKAROUND
		const float backspaceRepeatInterval = 0.03f;
		const float backspaceRepeatDelay = 0.4f;
		float backspaceHeldTime = 0;
		float backspaceRepeatTimer = Mathf.Infinity;
		bool backspaceHeld = false;
	#endif
	float accuracy,wpm,estimatedTime;
	void Update() {
		#if BACKSPACE_KEY_WORKAROUND
			if (Keyboard.current.backspaceKey.wasPressedThisFrame){
				EraseInputCharacter(false);
				backspaceHeld = true;
			}
			if (Keyboard.current.backspaceKey.wasReleasedThisFrame){
				backspaceHeld = false;
				backspaceHeldTime = 0;
				backspaceRepeatTimer = Mathf.Infinity;
			}
			if (backspaceHeld){
				backspaceHeldTime += Time.deltaTime;
				backspaceRepeatTimer += Time.deltaTime;
				if (backspaceRepeatTimer >= backspaceRepeatInterval &&
					backspaceHeldTime >= backspaceRepeatDelay
				) {
					EraseInputCharacter(false);
					backspaceRepeatTimer = 0;
				}
			}
		#endif
		
		SetTextColor();
		GraphUpdate();
		UpdateTooltipPos();
		
		if (lastFrameIncorrect != incorrect) {
			textDisplay.caretColor = incorrect ?
				themes[selectedTheme].caretColorError:
				themes[selectedTheme].caretColor;
			lastFrameIncorrect = incorrect;
		}
		
		if (fade != lastFade || backgroundFade > 0 || backgroundFade < 1)
			FadeUpdate();
		if (done) return;
		if (totalTestTime < .001f) {
			if (!started) return;
			started = false;
		}
		accuracy = (float)hitCount / (hitCount+missCount) * 100;
		wpm = loc / totalTestTime * 60 / 5;
		if (hitCount + missCount == 0) accuracy = 100;
		WPMInfo.text =
			$"Accuracy: {Mathf.RoundToInt(accuracy)}%\nSpeed: {(totalTestTime==0?"-":Mathf.RoundToInt(wpm))} WPM\nTime: {TimeFormattedString(totalTestTime,true)}";
		
		if (totalTestTime < .001f) {
			estimatedTime = KeyManager.GetEstimatedTypingTimeSeconds(text);
			WPMInfo.text += $" (est.: {TimeFormattedString(estimatedTime, true)})";
		}
		
		if (InputHandler.mousePosition != lastMousePos) {
			fade = false;
			lastMousePos = InputHandler.mousePosition;
		}
	}

	void LateUpdate() {
		graph.SetVerticesDirty();
		incorrect = !text.StartsWith(input);
		
		switch (lastMaxLength) {
			case >= 0 when input.Length < text.Length || incorrect: {
				started = true;
				seekTime += Time.deltaTime;
				wordTime += Time.deltaTime;
				totalTestTime += Time.deltaTime;
				graph.timeScale = totalTestTime;
				break;
			}
			case > 0 when done: {
				return;
			}
		}
		int length = input.Length;
		if (length == lastLength) {
			return;
		}
		
		if (totalTestTime > 0 || length > 0)
			fade = true;
		incorrect = false;
		if (loc > length - 1)
			loc = length - 1;
		while(loc < length - 1) {
			char inputChar = input[loc + 1];
			char compareChar = loc < text.Length - 1 ? text[loc + 1] : '⌫';
			
			if (inputChar == compareChar) {
				int keyIndex = KeyManager.GetKeyIndex(inputChar);
				hitCount++;
				loc++;
				if (loc > lastMaxLength) {
					lastMaxLength = loc;
					int index = Mathf.Max(0, loc - 1);
					graph.accuracy[index] = accuracy = (float)hitCount / (hitCount + missCount) * 100;
					graph.speedValues[index] = wpm = loc / totalTestTime * 60 / 5;
					graph.times[index] = totalTestTime;
					graph.seekTimes[index] = seekTime;
					graph.timeScale = totalTestTime;
					graph.currentIndex = loc - 1;
					if (graph.speedValueScale < wpm) {
						graph.speedValueScale = wpm;
					}
					
					// graph.SetVerticesDirty();
					if (KeyManager.IsWhitespaceIndex(keyIndex) || input.Length == text.Length) {
						//BUG: Can result in infinity WPM and wrong words being registered, if not typing at the end of the field (eg. pressing left arrow key)
						if (loc > 0) {
							graph.wordSpeedValues[wordIndex] =
								KeyManager.UpdateWordSpeed(KeyManager.GetLastWord(input,loc),wordTime);
							graph.wordTimes[wordIndex] = totalTestTime;
							// Maybe skip scaling for the first word? Maybe only if it's less than some length?
							if (graph.wordSpeedValues[wordIndex] > graph.wordSpeedScale) {
								graph.wordSpeedScale = graph.wordSpeedValues[wordIndex];
							}
							wordIndex++;
						}
						wordTime = 0;
					}
				}
				KeyManager.RegisterKeyHit(keyIndex);
				if (loc > 0) {
					if (input[loc] != input[loc - 1]) {
						KeyManager.UpdateKeySeekTime(keyIndex, seekTime);
						KeyManager.UpdateNextKeySeekTime(KeyManager.GetKeyIndex(input[loc - 1]), seekTime);
					}
					if (loc < text.Length - 1) {
						KeyManager.UpdatePreviousKeySeekTime(KeyManager.GetKeyIndex(text[loc + 1]), seekTime);
					}
				}
				seekTime = 0;
			} else {
				if (input.Length > lastLength) {
					graph.accuracy[Mathf.Max(0, loc)] = accuracy =
						(float)hitCount / (hitCount + missCount) * 100;
					graph.misses[Mathf.Max(0, loc)]++;
					missCount++;

					if(input.Length == lastLength + 1) {
						KeyManager.RegisterKeyMiss(KeyManager.GetKeyIndex(compareChar));

						// Don't register a miss for the input character if it's correct in the next position
						if (text.Length > lastLength + 1 && text[lastLength + 1] == inputChar) break;

						KeyManager.RegisterKeyMiss(KeyManager.GetKeyIndex(inputChar));
					}
				}
				incorrect = true;
				
				break;
			}
		}
		wpm = loc / totalTestTime * 60 / 5;
		lastLength = input.Length;
		lastInput = input;
		
		// Compare substring in case of trailing characters
		incorrect = !text.StartsWith(input[..Mathf.Min(input.Length, text.Length)]);

		if (incorrect || input.Length < text.Length)
			return;
		if (input.Length > text.Length)
			input = text;

		textDisplay.readOnly = done = true;
		fade = false;
		int charOccurrences = 0;
		for (int i = 0; i < input.Length; i++) {
			if (input[i] == curCharacterPractice)
				charOccurrences++;
		}
		KeyManager.RemoveHitsAndMisses(charOccurrences / 2);
		UpdateCurrentPracticeUI(curPracticeIndex);
		if (showGraphWhenDone)
			ToggleGraphUI(true);
		UnfocusInputField();
		// KeyManager.Save();
	}
	void FadeUpdate() {
		if (fade != lastFade) {
			backgroundFade = fade ?
				backgroundFade * backgroundFade * backgroundFade :
				Mathf.Sqrt(Mathf.Sqrt(backgroundFade));
			Cursor.visible = !fade;
			lastFade = fade;
			int refreshRate = (int)(Screen.currentResolution.refreshRateRatio.value + .49);
			Application.targetFrameRate = fade ? refreshRate * 2 : refreshRate;
		}
		if (fade) {
			backgroundFade = Mathf.Clamp01(backgroundFade + Time.deltaTime * 2f);
			targetFadeColor.a = Mathf.Sqrt(backgroundFade) * (fadeAmount - defaultFade) + defaultFade;
		} else {
			backgroundFade = Mathf.Clamp01(backgroundFade - Time.deltaTime * 2f);
			targetFadeColor.a = backgroundFade * backgroundFade * (fadeAmount - defaultFade) + defaultFade;
		}
		fadeImage.color = targetFadeColor;
	}
	Vector2 graphTooltipTimestampTargetPos,
	        graphTooltipSpeedTargetPos,
	        graphTooltipAccuracyTargetPos,
	        graphTooltipSeekTimeTargetPos,
	        graphTooltipWordSpeedTargetPos;
	void GraphUpdate() {
		float targetGraphHeight = 0;
		if (showGraph != lastShowGraph || graphBlend is > 0 or < 1) {
			if (showGraph != lastShowGraph) {
				graphBlend = showGraph ?
					graphBlend * graphBlend * graphBlend :
					Mathf.Sqrt(Mathf.Sqrt(graphBlend));
				lastShowGraph = showGraph;
			}
			float curBlend;
			if (showGraph) {
				graphBlend = Mathf.Clamp01(graphBlend + Time.deltaTime * 2f);
				curBlend = Mathf.Sqrt(graphBlend);
			} else {
				graphBlend = Mathf.Clamp01(graphBlend - Time.deltaTime * 2f);
				curBlend = graphBlend * graphBlend;
			}
			Color textColor = textDisplayText.color;
			textColor.a = 1f - curBlend;
			textDisplayText.color = textColor;
			
			Color graphOutlineColor = graphOutline.color;
			graphOutlineColor.a = 1f - curBlend;
			graphOutline.color = graphOutlineColor;
			
			graphTransform.anchoredPosition  =  new Vector2(
				defaultGraphPos.x,
				defaultGraphPos.y + (curBlend * 62)
			);
			targetGraphHeight = 700f - graphTransform.anchoredPosition.y +
				lessonInfo.rectTransform.anchoredPosition.y - lessonInfo.rectTransform.rect.height - 15;
			graphTransform.SetSizeWithCurrentAnchors(
				RectTransform.Axis.Vertical,
				Mathf.Lerp(defaultGraphHeight, targetGraphHeight, curBlend)
			);

			graph.expandedBlend = curBlend;
		}
		if (!done || !showGraph ||
			(graph.hoverIndex == lastHoverIndex && graphBlend >= .999f) ||
			graph.hoverIndex == -1 || graph.hoverWordIndex == -1 ||
			settingsOpen
		) {
			if (!showGraph ||
				((graph.hoverIndex == -1||graph.hoverWordIndex == -1) && lastHoverIndex != -1)
			) {
				MoveTooltipsOffScreen();
			}
			return;
		}
		
		// Tooltips

		/*
		 * For drawing the tooltip, draw them one after another, and always set the Y pos to either the value in the graph, or Mathf.Max of the previous Y pos + UI element height
		 * That way they won't overlap, and they will always be shown close to where they are presented in the graph
		 * May need to preform some logic to draw them in the correct order
		 * Remember to hide the text/background of the tooltips when nothing is selected
		 */

		// Rect graphRect = graphOutlineTransform.rect;
		Rect graphRect = graph.rectTransform.rect;
		graphRect.height = targetGraphHeight;
		float tooltipHeight = graphTooltipSpeed.rect.height;
		float wpmScale = Mathf.Max(graph.speedValueScale, graph.wordSpeedScale);
		const float paddingDistance = 15;
		const float verticalPadding = 2;
		lastHoverIndex = graph.hoverIndex;
		Vector2 baseTooltipPos =
			graphOutlineTransform.anchoredPosition + graph.rectTransform.anchoredPosition;
		baseTooltipPos.x = Mathf.Clamp(
			baseTooltipPos.x + graph.times[lastHoverIndex] / graph.timeScale * graphRect.width,
			graphTooltipWordSpeed.rect.width + paddingDistance,
			Screen.width - graphTooltipSpeed.rect.width - paddingDistance
		);
		Vector2 tooltipOffset = Vector2.zero;

		graphTooltipTimestampText.text =
			$"Time: {TimeFormattedString(graph.times[lastHoverIndex], true, true)}";
		graphTooltipTimestampTargetPos = baseTooltipPos + tooltipOffset;
		
		tooltipOffset = Vector2.right * paddingDistance;
		
		graphTooltipAccuracyText.text = $"Errors: {graph.misses[lastHoverIndex]}";
		tooltipOffset.y = Mathf.Clamp(
			(1f - graph.accuracy[lastHoverIndex] / 100) * graphRect.height,
			tooltipHeight / 2 + verticalPadding,
			graphRect.height - tooltipHeight / 2 - verticalPadding
		);
		graphTooltipAccuracyTargetPos = baseTooltipPos + tooltipOffset;
		if (graph.misses[lastHoverIndex] < 1) {
			graphTooltipAccuracyTargetPos.y =  -100;
		}
		float accuracyTooltipY = tooltipOffset.y;
		
		graphTooltipSpeedText.text=$"Speed: {Math.Round(graph.speedValues[lastHoverIndex], 2)} WPM";
		tooltipOffset.y = Mathf.Clamp(
			graph.speedValues[lastHoverIndex] / wpmScale * graphRect.height,
			tooltipHeight / 2 + verticalPadding,
			graphRect.height - tooltipHeight / 2 - verticalPadding
		);
		tooltipOffset.y = tooltipOffset.y < accuracyTooltipY &&
			accuracyTooltipY - tooltipHeight >= verticalPadding ?
				Mathf.Min(accuracyTooltipY - tooltipHeight - verticalPadding, tooltipOffset.y):
				Mathf.Max(accuracyTooltipY + tooltipHeight + verticalPadding, tooltipOffset.y);
		graphTooltipSpeedTargetPos = baseTooltipPos + tooltipOffset;

		tooltipOffset = Vector2.left * paddingDistance;
		
		//TODO: Idea: Display the current word in a new tooltip, centered above the diamond? 
		string wordWithHighlightedLetter = "";
		int indexOffset = lastHoverIndex;
		try {
			while (true) {
				switch (text[indexOffset]) {
					case ' ':case '\t':case '\n':
						break;
					default:
						indexOffset--;
						continue;
				}
				break;
			}
		} catch {
			indexOffset = 0;
		}
		for (int i = 0; i < words[graph.hoverWordIndex].Length; i++) {
			char c = words[graph.hoverWordIndex][i] switch {
				' '  => '␣',
				'\t' => '↹',
				'\n' => '↵',
				_    => words[graph.hoverWordIndex][i]
			};
			wordWithHighlightedLetter += 
				i+indexOffset == lastHoverIndex ? $"<u><b>{c}</b></u>" : c;
		}
		float graphSeekTime = (float)Math.Round(graph.seekTimes[lastHoverIndex] * 1000, 2);
		graphTooltipSeekTimeText.text =
			$"Seek Time: {(graphSeekTime>0?graphSeekTime:"<"+Math.Round(1.0/Application.targetFrameRate*1000,2))} ms\n{wordWithHighlightedLetter}</i>";
		//TODO: Determine the Y pos of each tooltip beforehand, prevent overlap in the proper order
		float seekTimeTooltipY = Mathf.Clamp(
			graph.seekTimes[lastHoverIndex] / 4 * graphRect.height,
			tooltipHeight + verticalPadding,
			graphRect.height - tooltipHeight - verticalPadding
		);
		tooltipOffset.y = seekTimeTooltipY;
		graphTooltipSeekTimeTargetPos = baseTooltipPos + tooltipOffset;
		
		graphTooltipWordSpeedText.text =
			$"Word Speed: {Math.Round(graph.wordSpeedValues[graph.hoverWordIndex], 2)} WPM\nWord: <i>{wordWithHighlightedLetter}</i>";
		tooltipOffset.y = Mathf.Clamp(
			graph.wordSpeedValues[graph.hoverWordIndex] / wpmScale * graphRect.height,
			tooltipHeight + verticalPadding,
			graphRect.height - tooltipHeight - verticalPadding
		);
		tooltipOffset.y =
			tooltipOffset.y < seekTimeTooltipY&&seekTimeTooltipY - tooltipHeight * 2 >= verticalPadding ?
				Mathf.Min(seekTimeTooltipY - tooltipHeight * 2 - verticalPadding,tooltipOffset.y):
				Mathf.Max(seekTimeTooltipY + tooltipHeight * 2 + verticalPadding,tooltipOffset.y);
		graphTooltipWordSpeedTargetPos = baseTooltipPos + tooltipOffset;
	}
	
	void MoveTooltipsOffScreen() {
		//TODO: Idea: Transition the scale to make the tooltips smoothly appear/disappear
		graphTooltipTimestampTargetPos.y = -100;
		graphTooltipSpeedTargetPos.y = -100;
		graphTooltipWordSpeedTargetPos.y = -100;
		graphTooltipSeekTimeTargetPos.y = -100;
		graphTooltipAccuracyTargetPos.y = -100;
		lastHoverIndex = -1;
	}
	
	const float tooltipMoveSpeed = 9f;
	void UpdateTooltipPos() {
		graphTooltipTimestamp.anchoredPosition = Vector2.Lerp(
			graphTooltipTimestamp.anchoredPosition,
			graphTooltipTimestampTargetPos,
			Time.deltaTime * tooltipMoveSpeed
		);
		graphTooltipAccuracy.anchoredPosition = Vector2.Lerp(
			graphTooltipAccuracy.anchoredPosition,
			graphTooltipAccuracyTargetPos,
			Time.deltaTime * tooltipMoveSpeed
		);
		graphTooltipSpeed.anchoredPosition = Vector2.Lerp(
			graphTooltipSpeed.anchoredPosition,
			graphTooltipSpeedTargetPos,
			Time.deltaTime * tooltipMoveSpeed
		);
		graphTooltipSeekTime.anchoredPosition = Vector2.Lerp(
			graphTooltipSeekTime.anchoredPosition,
			graphTooltipSeekTimeTargetPos,
			Time.deltaTime * tooltipMoveSpeed
		);
		graphTooltipWordSpeed.anchoredPosition = Vector2.Lerp(
			graphTooltipWordSpeed.anchoredPosition,
			graphTooltipWordSpeedTargetPos,
			Time.deltaTime * tooltipMoveSpeed
		);
	}

	public void AllowCapitalLetters() {
		KeyManager.includeUppercase = practiceUppercase.isOn;
		if (!done)	ResetLesson();
		KeyManager.unsavedPrefs = true;
	}
	public void AllowNumbers() {
		KeyManager.includeNumbers = practiceNumbers.isOn;
		if (!done)	ResetLesson();
		KeyManager.unsavedPrefs = true;
	}
	public void AllowSymbols() {
		KeyManager.includeSymbols = practiceSymbols.isOn;
		if (!done)	ResetLesson();
		KeyManager.unsavedPrefs = true;
	}
	public void UpdateCharDifficulty() {
		float value = quoteDifficultySlider.value;
		KeyManager.charPracticeDifficulty = value > 0 ? Mathf.Sqrt(value) : 0;
		KeyManager.unsavedPrefs = true;
	}
	public void UpdateQuoteDifficulty() {
		float value = quoteDifficultySlider.value;
		KeyManager.quoteDifficulty = value > 0 ? Mathf.Sqrt(value) : 0;
		KeyManager.unsavedPrefs = true;
	}
	public void UpdateModeBias() {
		KeyManager.modeBias = modeBiasSlider.value;
		KeyManager.unsavedPrefs = true;
	}
	
	//TODO: Make a proper UI plan and redesign the settings menu(?)
	// Also consider where the progress graph will be drawn, if that is to be implemented
}
