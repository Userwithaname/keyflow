using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Typing : MonoBehaviour {
	
	/*
	 * Idea: game modes:
	 *		top speed mode: Words are shown one after another, and become active after a countdown. You're supposed to type out the word as fast as possible
	 *		consistency mode: The text starts scrolling at your average speed, and you must take care to not fall behind, or the text will go offscreen
	 *		accuracy mode: Similar to consistency, but the scrolling speed follows yours, and increases even with errors 
	 *		hard words mode: Picks random words containing letters and letter combos you're not as good with
	 *
	 *		Stay in flight:
	 *			A 2D rendered plane is flying as you type.
	 *			If you slow down, the plane starts to dip.
	 *			If you speed up, it starts to rise.
	 *			If you get too slow, the plane crashes into the ground.
	 *		Solo Office Worker:
	 *			First-person game where you jump from computer to computer and have to type out as many quotes as you can in a certain time limit before the boss comes
	 *		Gibberish Mode:
	 *			A string of random characters for the user to type out
	 */
	/*
	 * TODO: Progress tracking (save the average accuracy/speed for each day, draw a graph that the user can check whenever they want)
	 *
	 * TODO: Create a UI to make all colors configurable
	 */

	public static Typing instance;
	public static Theme currentTheme;
	
	public DrawGraph graph;
	public TMP_Text graphInfo;
	public Image graphOutline;
	
	// public Font interfaceFont;
	string text="If you're seeing this text, it means something went wrong with the application",
	       input="",
	       lastInput="";
	int loc=-1;
	int lastLength,lastMaxLength=-1;
	float seekTime,wordTime,totalTestTime;
	bool incorrect,done;
	public static int curPracticeIndex;
	public static string quoteTitle;
	public TMP_InputField textDisplay;
	TMP_Text textDisplayText;
	public TMP_Text lessonInfo,WPMInfo,averageWPMInfo,quoteInfo;
	public Button quoteInfoButton;
	
	public Image[] themeableButtons,
	               themeableIcons;
	public GameObject settingsUI;
	public Toggle practiceUppercase,
	              practiceNumbers,
	              practiceSymbols,
	              practiceWhitespace,
	              showIncorrectCharacters;
	public Slider charVarietySlider,
	              quoteDifficultySlider,
	              modeBiasSlider;
	public RectTransform textTransform;
	public GameObject lightModeButton,
	                  darkModeButton;
	
	public Image backgroundImage,fadeImage;
	[Range(0,1)]public float defaultFade=0f,fadeAmount=.5f;
	float backgroundFade;	// 0 to 1
	bool fade,lastFade;
	Vector3 lastMousePos;
	Color targetFadeColor=new Color(0,0,0,0);
	
	RectTransform caretTransform;
	Vector3 initialTextPos,initialCaretPos;
	
	[Serializable]public struct Theme{
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
		             buttonColor,
		             iconColor;
		[NonSerialized]
		public string textColorErrorTag,
		              textColorWarningTag,
		              textColorCorrectTag,
		              improvementColorTag,
		              regressionColorTag;
	}
	public int selectedTheme,lastSelectedTheme;
	public Theme[] themes;
	
	public bool showGraphWhenDone=true;
	
	RectTransform graphTransform;
	int lastHoverIndex=-1;
	bool showGraph,lastShowGraph;
	float graphBlend;
	float defaultGraphHeight;
	Vector3 defaultGraphPos;
	float[] wpmGraph,
	        accuracyGraph,
	        timeGraph,
	        fullWordWpmGraph;	//TODO: Create properly sized array and store the full-word speeds for each word typed (I was thinking this part of the graph should be drawn with sharp, 90 degree angles (vertical/horizontal), instead of the usual diagonal ones) Hover caption could be something like 'Full-word speed: 10 WPM ("square ")'
	//TODO: Idea: Record all keypresses and store times, allow viewing replay of the lesson
	
	int hitCount,missCount;
	
	bool showMenu;

	void Start(){
		Application.targetFrameRate=Screen.currentResolution.refreshRate*2; //TODO: Framerate limit could maybe be set higher or lower whenever 'fade' becomes true or false(?)
		
		practiceUppercase.isOn=KeyManager.includeUppercase=PlayerPrefs.GetInt("includeUppercase",KeyManager.includeUppercase?1:0)==1;
		practiceNumbers.isOn=KeyManager.includeNumbers=PlayerPrefs.GetInt("includeNumbers",KeyManager.includeNumbers?1:0)==1;
		practiceSymbols.isOn=KeyManager.includeSymbols=PlayerPrefs.GetInt("includeSymbols",KeyManager.includeSymbols?1:0)==1;
		charVarietySlider.value=KeyManager.charPracticeDifficulty=PlayerPrefs.GetFloat("charPracticeDifficulty",KeyManager.charPracticeDifficulty);
		quoteDifficultySlider.value=KeyManager.quoteDifficulty=PlayerPrefs.GetFloat("quoteDifficulty",KeyManager.quoteDifficulty);
		modeBiasSlider.value=KeyManager.modeBias=PlayerPrefs.GetFloat("practiceModeBias",KeyManager.modeBias);
		showIncorrectCharacters.isOn=PlayerPrefs.GetInt("showTypos",showIncorrectCharacters.isOn?1:0)==1;
		selectedTheme=PlayerPrefs.GetInt("selectedTheme",selectedTheme);
		
		lightModeButton.SetActive(selectedTheme==0);
		darkModeButton.SetActive(selectedTheme!=0);
		
		textDisplayText=textDisplay.GetComponentInChildren<TMP_Text>();
		
		// graphTransform=graph.rectTransform;
		graphTransform=graph.transform.parent.GetComponent<RectTransform>();
		defaultGraphHeight=graphTransform.rect.height;
		defaultGraphPos=graphTransform.anchoredPosition;

		UpdateTheme();
		
		initialTextPos=textTransform.localPosition;
		caretTransform=textTransform.parent.Find("Caret").GetComponent<RectTransform>();
		initialCaretPos=caretTransform.localPosition;
		instance=this;
		NextLesson();
	}
	public void Save(){
		PlayerPrefs.SetInt("includeUppercase",KeyManager.includeUppercase?1:0);
		PlayerPrefs.SetInt("includeNumbers",KeyManager.includeNumbers?1:0);
		PlayerPrefs.SetInt("includeSymbols",KeyManager.includeSymbols?1:0);
		PlayerPrefs.SetFloat("charPracticeDifficulty",KeyManager.charPracticeDifficulty);
		PlayerPrefs.SetFloat("quoteDifficulty",KeyManager.quoteDifficulty);
		PlayerPrefs.SetFloat("practiceModeBias",KeyManager.modeBias);
		PlayerPrefs.SetInt("showTypos",showIncorrectCharacters.isOn?1:0);
		PlayerPrefs.SetInt("selectedTheme",selectedTheme);
	}
	
	void UpdateTheme(){
		textDisplayText.color=themes[selectedTheme].textColorQuote;
		var quoteInfoColors=quoteInfoButton.colors;
		lessonInfo.color=WPMInfo.color=averageWPMInfo.color=quoteInfoColors.normalColor=quoteInfoColors.selectedColor=graphInfo.color=themes[selectedTheme].textColorUI;
		quoteInfoButton.colors=quoteInfoColors;
		targetFadeColor=backgroundImage.color=fadeImage.color=themes[selectedTheme].backgroundColor;
		
		currentTheme=themes[selectedTheme];
		graph.color=themes[selectedTheme].textColorCorrect;
		graph.diamondColor=themes[selectedTheme].textColorUI;
		
		themes[selectedTheme].textColorErrorTag="<color=#"+ColorUtility.ToHtmlStringRGB(themes[selectedTheme].textColorError)+">";
		themes[selectedTheme].textColorWarningTag="<color=#"+ColorUtility.ToHtmlStringRGB(themes[selectedTheme].textColorWarning)+">";
		themes[selectedTheme].textColorCorrectTag="<color=#"+ColorUtility.ToHtmlStringRGB(themes[selectedTheme].textColorCorrect)+">";
		themes[selectedTheme].improvementColorTag="<color=#"+ColorUtility.ToHtmlStringRGB(themes[selectedTheme].improvementColor)+">";
		themes[selectedTheme].regressionColorTag="<color=#"+ColorUtility.ToHtmlStringRGB(themes[selectedTheme].regressionColor)+">";

		WPMInfo.text=WPMInfo.text
			.Replace(themes[lastSelectedTheme].improvementColorTag,themes[selectedTheme].improvementColorTag)
			.Replace(themes[lastSelectedTheme].regressionColorTag,themes[selectedTheme].regressionColorTag);
		averageWPMInfo.text=averageWPMInfo.text
			.Replace(themes[lastSelectedTheme].improvementColorTag,themes[selectedTheme].improvementColorTag)
			.Replace(themes[lastSelectedTheme].regressionColorTag,themes[selectedTheme].regressionColorTag);
		lessonInfo.text=lessonInfo.text
			.Replace(themes[lastSelectedTheme].improvementColorTag,themes[selectedTheme].improvementColorTag)
			.Replace(themes[lastSelectedTheme].regressionColorTag,themes[selectedTheme].regressionColorTag);
		textDisplay.text=textDisplay.text
			.Replace(themes[lastSelectedTheme].textColorCorrectTag,themes[selectedTheme].textColorCorrectTag);
		
		foreach(Image button in themeableButtons){
			button.color=currentTheme.buttonColor;
		}
		foreach(Image icon in themeableIcons){
			icon.color=currentTheme.iconColor;
		}
	}
	public void ChangeTheme(int theme){
		if(theme!=selectedTheme) lastSelectedTheme=selectedTheme;
		selectedTheme=theme;
		UpdateTheme();
	}

	private void OnApplicationFocus(bool hasFocus){
		if(!hasFocus&&!done) ResetLesson();
	}

	public void NextLesson(){
		textTransform.localPosition=initialTextPos;
		caretTransform.localPosition=initialCaretPos;
		if(UnityEngine.Random.Range(0f,1f)>KeyManager.modeBias){
			curPracticeIndex=KeyManager.GetLowConfidenceCharacter();
			text=KeyManager.GetQuoteByCharFrequency(ref curPracticeIndex,ref quoteTitle);
		}else{
			curPracticeIndex=-2; 
			text=KeyManager.GetQuoteByOverallScore(ref quoteTitle,ref curCharPractice);
		}
		ResetLesson();
	}
	public void ResetLesson(){
		done=false;
		
		ToggleGraphUI(false);
		graph.values=wpmGraph=new float[text.Length-1];
		graph.accuracy=accuracyGraph=new float[text.Length-1];
		graph.misses=new int[text.Length-1];
		graph.seekTimes=new float[text.Length-1];
		graph.times=timeGraph=new float[text.Length-1];
		lastHoverIndex=-1;
		graph.valueScale=1;
		graph.timeScale=1;
		graph.currentIndex=-1;
		graph.SetVerticesDirty();
		
		UpdateCurrentPracticeUI();
		hitCount=missCount=0;
		input=lastInput="";
		lastFrameIncorrect=true;
		incorrect=done=fade=false;
		textDisplay.readOnly=false;
		seekTime=wordTime=totalTestTime=0;
		loc=lastLength=lastMaxLength=-1;
		// EventSystem.current.SetSelectedGameObject(textDisplayObject);
	}
	public void ToggleGraphUI(){
		//TODO: When there is no graph data available for the quote (e.g. not finished typing), show the daily progress instead (always show a tab for the daily progress as well). If there is no data at all, display a message explaining that
		
		showGraph=done&&!showGraph;
	}
	public void ToggleGraphUI(bool show){
		showGraph=show;
	}
	public void OpenWikiPage(){
		if(quoteTitle==null)
			return;
		string wikiPage="https://en.wikipedia.org/wiki/"+quoteTitle.Split("Wikipedia - ")[^1].Replace(' ','_');
		Debug.Log(wikiPage);
		Application.OpenURL(wikiPage);
	}
	bool settingsOpen;
	public void ToggleSettingsUI(){
		settingsOpen=!settingsOpen;
		textDisplay.readOnly=!settingsOpen;
		settingsUI.SetActive(settingsOpen);
	}
	
	KeyManager.KeyConfidenceData curCharPractice;
	char curCharacterPractice;
	string curCharacterSeekTime;
	string curCharacterNextSeekTime;
	string curCharacterWPM;
	string curCharacterAccuracy;
	void UpdateCurrentPracticeUI(int comparisonIndex=-1){
		KeyManager.KeyConfidenceData updatedCharPractice=curPracticeIndex==-2?
		                                                 KeyManager.GetQuoteConfidenceData(text):
		                                                 KeyManager.instance.confidenceDatabase[curPracticeIndex];
		curCharacterPractice=updatedCharPractice.keyName;
		curCharacterSeekTime=updatedCharPractice.seekTime>0?Math.Round(updatedCharPractice.seekTime*1000,2)+" ms":"-";
		curCharacterNextSeekTime=updatedCharPractice.nextKeySeekTime>0?Math.Round(updatedCharPractice.nextKeySeekTime*1000,2)+" ms":":";
		curCharacterWPM=updatedCharPractice.wpm>0?Math.Round(updatedCharPractice.wpm,1)+" WPM":"-";
		curCharacterAccuracy=updatedCharPractice.hits+updatedCharPractice.misses>0?Math.Round((float)updatedCharPractice.hits/(updatedCharPractice.hits+updatedCharPractice.misses)*100,1)+"%":"-";

		if(comparisonIndex!=-1){
			KeyManager.KeyConfidenceData compare=comparisonIndex==-2?
		                                        KeyManager.GetQuoteConfidenceData(text):
		                                        KeyManager.instance.confidenceDatabase[comparisonIndex];
			float diff=(float)Math.Round(updatedCharPractice.seekTime*1000-curCharPractice.seekTime*1000,3);
			curCharacterSeekTime=diff<=0?
			                     themes[selectedTheme].improvementColorTag+curCharacterSeekTime+" ("+diff+")</color>":
			                     themes[selectedTheme].regressionColorTag+curCharacterSeekTime+" (+"+diff+")</color>";
			diff=(float)Math.Round(updatedCharPractice.nextKeySeekTime*1000-curCharPractice.nextKeySeekTime*1000,3);
			curCharacterNextSeekTime=diff<=0||float.IsNaN(diff)?
			                     themes[selectedTheme].improvementColorTag+curCharacterNextSeekTime+" ("+diff+")</color>":
			                     themes[selectedTheme].regressionColorTag+curCharacterNextSeekTime+" (+"+diff+")</color>";
			diff=(float)Math.Round(updatedCharPractice.wpm-curCharPractice.wpm,3);
			curCharacterWPM=diff>=0?
			                     themes[selectedTheme].improvementColorTag+curCharacterWPM+" (+"+diff+")</color>":
			                     themes[selectedTheme].regressionColorTag+curCharacterWPM+" ("+diff+")</color>";
			float newAccuracy=(float)updatedCharPractice.hits/(updatedCharPractice.hits+updatedCharPractice.misses)*100;
			diff=(float)Math.Round(newAccuracy-(float)curCharPractice.hits/(curCharPractice.hits+curCharPractice.misses)*100,3);
			if(float.IsNaN(diff)) diff=newAccuracy;
			curCharacterAccuracy=diff>=0?
			                     themes[selectedTheme].improvementColorTag+curCharacterAccuracy+" (+"+diff+")</color>":
			                     themes[selectedTheme].regressionColorTag+curCharacterAccuracy+" ("+diff+")</color>";
		}
		curCharPractice=updatedCharPractice;
		
		lessonInfo.text=
			"<b>Current Practice: "+(curCharacterPractice=='\0'?"multiple keys":curCharacterPractice)+"</b>"+
			"\n<b>Average Stats </b>(for <b>"+(curCharacterPractice=='\0'?"multiple keys":curCharacterPractice)+"</b>)<b>:</b>"+
			"\nSeek Time: "+curCharacterSeekTime+
			"\nNext Key Seek Time: "+curCharacterNextSeekTime+
			"\nFull-Word: "+curCharacterWPM+
			"\nAccuracy: "+curCharacterAccuracy;
		
		wpm=loc/totalTestTime*60/5;
		accuracy=(float)hitCount/(hitCount+missCount)*100;
		float oldAverageAccuracy=KeyManager.averageAccuracy;
		float oldAverageSpeed=KeyManager.averageWPM;
		float oldTopSpeed=KeyManager.topWPM;
		if(done){
			if(wpm>KeyManager.topWPM) KeyManager.topWPM=wpm;
			if(accuracy>0)
				KeyManager.averageAccuracy=Mathf.Lerp(KeyManager.averageAccuracy,accuracy,KeyManager.averageAccuracy>0?.12f:1);
			if(wpm>0)
				KeyManager.averageWPM=Mathf.Lerp(KeyManager.averageWPM,wpm,KeyManager.averageWPM>0?.12f:1);
			int seconds=Mathf.FloorToInt(totalTestTime%60);
			string fractions=((float)Math.Round(totalTestTime-Mathf.FloorToInt(totalTestTime),3)%1).ToString().Split('.')[^1];
			for(int i=fractions.Length;i<3;i++){
				fractions+='0';
			}
			if(float.IsNaN(accuracy)) accuracy=100;
			WPMInfo.text=
				"Accuracy: "+
					(accuracy>=oldAverageAccuracy?
						themes[selectedTheme].improvementColorTag+Math.Round(accuracy,2)+"% (+"+Math.Round(accuracy-oldAverageAccuracy,2)+" from average)</color>":
						themes[selectedTheme].regressionColorTag+Math.Round(accuracy,2)+"% ("+Math.Round(accuracy-oldAverageAccuracy,2)+" from average)</color>")+
				"\nSpeed: "+
					(wpm>=oldAverageSpeed?
						themes[selectedTheme].improvementColorTag+Math.Round(wpm,2)+" WPM (+"+Math.Round(wpm-oldAverageSpeed,2)+" from average)</color>":
						themes[selectedTheme].regressionColorTag+Math.Round(wpm,2)+" WPM ("+Math.Round(wpm-oldAverageSpeed,2)+" from average)</color>")+
				"\nTime: "+Mathf.FloorToInt(totalTestTime/60)+':'+(seconds<10?"0":"")+seconds+':'+fractions;
			
			averageWPMInfo.text=
				"Average Accuracy: "+
					(KeyManager.averageAccuracy>=oldAverageAccuracy?
						themes[selectedTheme].improvementColorTag+Math.Round(KeyManager.averageAccuracy,2)+"% (+"+Math.Round(KeyManager.averageAccuracy-oldAverageAccuracy,2)+")</color>":
						themes[selectedTheme].regressionColorTag+Math.Round(KeyManager.averageAccuracy,2)+"% ("+Math.Round(KeyManager.averageAccuracy-oldAverageAccuracy,2)+")</color>")+
				"\nAverage Speed: "+
					(KeyManager.averageWPM>=oldAverageSpeed?
						themes[selectedTheme].improvementColorTag+(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")+" WPM (+"+Math.Round(KeyManager.averageWPM-oldAverageSpeed,3)+")</color>":
						themes[selectedTheme].regressionColorTag+(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")+" WPM ("+Math.Round(KeyManager.averageWPM-oldAverageSpeed,3)+")</color>")+
				"\nTop Speed: "+
					(KeyManager.topWPM>oldTopSpeed?
						themes[selectedTheme].improvementColorTag+(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")+" WPM(+"+(KeyManager.topWPM-oldTopSpeed)+")</color>":
						(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")+" WPM");
		}else{	
			averageWPMInfo.text=
				"Average Accuracy: "+Math.Round(KeyManager.averageAccuracy,2)+"%"+
				"\nAverage Speed: "+(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")+" WPM"+
				"\nTop WPM: "+(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")+" WPM";
		}
		
		quoteInfo.text=quoteTitle;
	}

	bool focusInputField,unfocusInputField;
	void OnGUI(){
		if(!done&&!settingsOpen){
			GUI.SetNextControlName("1");
			input=GUI.TextArea(new Rect(300,Screen.height+50,Screen.width-600,30),input);
		}
		if(focusInputField){
			GUI.FocusControl("1");
		}
		if(unfocusInputField){
			GUI.FocusControl(null);
			unfocusInputField=false;
			if(!done)
				ResetLesson();
		}
		
		SetTextColor();
	}
	
	public void FocusInputField(){
		focusInputField=true;
	}
	public void UnfocusInputField(){
		unfocusInputField=true;
	}

	bool lastFrameIncorrect=true;
	void SetTextColor(){
		int cappedLoc=Mathf.Min(loc+1,text.Length);
		string content=(incorrect?themes[selectedTheme].textColorWarningTag:themes[selectedTheme].textColorCorrectTag)+text.Insert(cappedLoc,"</color>");
		if(incorrect){
			int lengthDiff=content.Length-text.Length;
			
			if(showIncorrectCharacters.isOn){
				char[] chars=input.Remove(0,loc+1).ToCharArray();
				for(int i=0;i<chars.Length;i++){
					switch(content[i]){
						default:{
							continue;
						}
						case ' ':{
							chars[i]='␣';
							break;
						}
						case '\t':{
							chars[i]='↹';
							break;
						}
						case '\n':{
							chars[i]='↵';
							break;
						}
					}
				}
				content=content.Insert(cappedLoc+lengthDiff,themes[selectedTheme].textColorErrorTag+"<u>"+new string(chars)+"</u></color>");
			}else{
				int incorrectStart=cappedLoc+lengthDiff,
				    incorrectEnd=Mathf.Min(input.Length,text.Length)+lengthDiff;
				char[] chars=content.ToCharArray();
				for(int i=Mathf.Max(incorrectStart-1,0);i<incorrectEnd;i++){
					switch(content[i]){
						default:{
							continue;
						}
						case ' ':{
							chars[i]='␣';
							break;
						}
						case '\t':{
							chars[i]='↹';
							break;
						}
						case '\n':{
							chars[i]='↵';
							break;
						}
					}
				}
				content=new string(chars);
				content=content.Insert(incorrectEnd,"</u></color>").Insert(incorrectStart,"<color=red><u>");
			}
		}
		textDisplay.text=content;
		textDisplay.caretPosition=Mathf.Min(input.Length,text.Length);
	}
	
	float accuracy,wpm;
	void Update(){
		if(done&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Escape))){
			NextLesson();
		}
		
		SetTextColor();
		GraphUpdate();
		
		if(lastFrameIncorrect!=incorrect){
			// textDisplay.caretColor=incorrect?caretErrorColor:caretColor;
			textDisplay.caretColor=incorrect?themes[selectedTheme].caretColorError:themes[selectedTheme].caretColor;
			lastFrameIncorrect=incorrect;
		}
		
		
		if(fade!=lastFade||backgroundFade>0||backgroundFade<1)
			FadeUpdate();
		if(done) return;
		int seconds=Mathf.FloorToInt(totalTestTime%60);
		accuracy=(float)hitCount/(hitCount+missCount)*100;
		wpm=loc/totalTestTime*60/5;
		if(hitCount+missCount==0) accuracy=100;
		WPMInfo.text=
			"Accuracy: "+Mathf.RoundToInt(accuracy)+"%"+
			"\nSpeed: "+(totalTestTime==0?"-":Mathf.RoundToInt(wpm))+" WPM"+
		   "\nTime: "+Mathf.FloorToInt(totalTestTime/60)+':'+(seconds<10?"0":"")+seconds;
		
		if(Input.mousePosition!=lastMousePos){
			fade=false;
			lastMousePos=Input.mousePosition;
		}
	}
	void LateUpdate(){
		graph.SetVerticesDirty();
		incorrect=!text.StartsWith(input);
		switch(lastMaxLength){
			case >= 0 when input.Length<text.Length||incorrect:{
				seekTime+=Time.deltaTime;
				wordTime+=Time.deltaTime;
				totalTestTime+=Time.deltaTime;
				graph.timeScale=totalTestTime;
				break;
			}
			case > 0 when done:{
				return;
			}
		}
		int length=input.Length;
		if(length==lastLength){
			return;
		}
		
		if(totalTestTime>0||length>0)
			fade=true;
		incorrect=false;
		if(loc>length-1)
			loc=length-1;
		while(loc<length-1){
			char inputChar = input[loc+1];
			char compareChar = text[loc+1];
			int keyIndex=KeyManager.GetKeyIndex(inputChar);
			KeyManager.UpoateAccuracy(inputChar,compareChar);
			
			if(inputChar==compareChar){
				hitCount++;
				loc++;
				if(loc>lastMaxLength){
					lastMaxLength=loc;
					int index=Mathf.Max(0,loc-1);
					accuracyGraph[index]=accuracy=(float)hitCount/(hitCount+missCount)*100;
					wpmGraph[index]=wpm=loc/totalTestTime*60/5;
					timeGraph[index]=totalTestTime;
					
					graph.values=wpmGraph;
					if(graph.valueScale<wpm){
						graph.valueScale=wpm;
					}
					graph.times=timeGraph;
					graph.accuracy=accuracyGraph;
					graph.seekTimes[index]=seekTime;
					graph.timeScale=totalTestTime;
					graph.currentIndex=loc-1;
					
					// graph.SetVerticesDirty();
				}
				KeyManager.RegisterKeyHit(keyIndex);
				if(loc>0&&input[loc]!=input[loc-1]){
					KeyManager.UpdateSeekTime(keyIndex,seekTime);
					KeyManager.UpdateNextKeySeekTime(KeyManager.GetKeyIndex(input[loc-1]),seekTime);
				}
				if(!KeyManager.IsAlphaNumericIndex(keyIndex)&&length>lastMaxLength||input.Length==text.Length){
					//TODO: Save all word WPMs to an array, show them as a raw speed graph at the end of the game (also track real WPM in the same way)
					//BUG: Can result in infinity WPM and wrong words being registered, if not typing at the end of the field (eg. pressing left arrow key)
					if(loc>0&&input[loc]!=input[loc-1])
						KeyManager.UpdateWordSpeed(KeyManager.ValidateWord(lastInput),wordTime); 
					wordTime=0;
				}

				seekTime=0;
			}else{
				accuracyGraph[Mathf.Max(0,loc)]=accuracy=(float)hitCount/(hitCount+missCount)*100;
				graph.accuracy=accuracyGraph;
				graph.misses[Mathf.Max(0,loc)]++;
				missCount++;
				KeyManager.RegisterKeyMiss(keyIndex);
				incorrect=true;
				
				break;
			}
		}
		wpm=loc/totalTestTime*60/5;

		incorrect=!text.StartsWith(input);
		
		lastLength=input.Length;
		// if(!incorrect&&lastLength>lastMaxLength)
		// 	lastMaxLength=lastLength;
		lastInput=input;
		
		if(incorrect||input.Length<text.Length)	return;
		// KeyManager.RemoveHitsAndMisses(curPracticeIndex);
		done=textDisplay.readOnly=true;
		fade=false;
		int charOccurrences=0;
		for(int i=0;i<input.Length;i++){
			if(input[i]==curCharacterPractice)
				charOccurrences++;
		}
		KeyManager.RemoveHitsAndMisses(charOccurrences/2);
		UpdateCurrentPracticeUI(curPracticeIndex);
		if(showGraphWhenDone)
			ToggleGraphUI(true);
		UnfocusInputField();
		// KeyManager.Save();
	}
	void FadeUpdate(){
		if(fade!=lastFade){
			backgroundFade=fade?backgroundFade*backgroundFade*backgroundFade:Mathf.Sqrt(Mathf.Sqrt(backgroundFade));
			Cursor.visible=!fade;
			lastFade=fade;
		}
		if(fade){
			backgroundFade=Mathf.Clamp01(backgroundFade+Time.deltaTime*2f);
			targetFadeColor.a=Mathf.Sqrt(backgroundFade)*(fadeAmount-defaultFade)+defaultFade;
		}else{
			backgroundFade=Mathf.Clamp01(backgroundFade-Time.deltaTime*2f);
			targetFadeColor.a=backgroundFade*backgroundFade*(fadeAmount-defaultFade)+defaultFade;
		}
		fadeImage.color=targetFadeColor;
	}
	void GraphUpdate(){
		if(showGraph!=lastShowGraph||graphBlend>0||graphBlend<1){
			if(showGraph!=lastShowGraph){
				graphBlend=showGraph?graphBlend*graphBlend*graphBlend:Mathf.Sqrt(Mathf.Sqrt(graphBlend));
				lastShowGraph=showGraph;
			}
			float curBlend;
			if(showGraph){
				graphBlend=Mathf.Clamp01(graphBlend+Time.deltaTime*2f);
				curBlend=Mathf.Sqrt(graphBlend);
			}else{
				graphBlend=Mathf.Clamp01(graphBlend-Time.deltaTime*2f);
				curBlend=graphBlend*graphBlend;
			}
			Color textColor=textDisplayText.color;
			textColor.a=1f-curBlend;
			textDisplayText.color=textColor;
			
			textColor=graphInfo.color;
			textColor.a=curBlend;
			graphInfo.color=textColor;
			
			Color graphOutlineColor=graphOutline.color;
			graphOutlineColor.a=1f-curBlend;
			graphOutline.color=graphOutlineColor;
			graphTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Lerp(defaultGraphHeight,Screen.height-graphInfo.rectTransform.position.y-(graphInfo.rectTransform.rect.height/2),curBlend));
			
			graphTransform.anchoredPosition=new Vector2(defaultGraphPos.x,defaultGraphPos.y+(curBlend*62));

			graph.expandedBlend=curBlend;
		}
		if(!done||graph.hoverIndex==lastHoverIndex) return;
		if(graph.hoverIndex!=-1){	//TODO: Turn the graph info text into a tooltip at the mouse position, or better, write text at the graph's height for that particular stat (but make sure the text won't overlap) 
			lastHoverIndex=graph.hoverIndex;
			int seconds=Mathf.FloorToInt(timeGraph[lastHoverIndex]%60);
			string fractions=((float)Math.Round(timeGraph[lastHoverIndex]-Mathf.FloorToInt(timeGraph[lastHoverIndex]),3)%1).ToString().Split('.')[^1];
			for(int i=fractions.Length;i<3;i++){
				fractions+='0';
			}
			graphInfo.text=
				"Time: "+Mathf.FloorToInt(timeGraph[lastHoverIndex]/60)+':'+(seconds<10?"0":"")+seconds+':'+fractions+
				" (key: '"+text[lastHoverIndex]+"')"+
				"\nSeek Time: "+Math.Round(graph.seekTimes[lastHoverIndex]*1000,2)+" ms"+
				"\nSpeed: "+Math.Round(wpmGraph[lastHoverIndex],2)+" WPM"+
				"\nError Rate: "+Math.Round(100f-accuracyGraph[lastHoverIndex],2)+"%";
			// Full-Word: 0 WPM ("potato ")
			// Seek Time ('a'): 0 ms";
		}else{
			lastHoverIndex=-1;
			graphInfo.text=
				"\n\n-- Move your mouse over the graph to show details --";
		}
	}
	
	public void AllowCapitalLetters(){
		KeyManager.includeUppercase=practiceUppercase.isOn;
	}
	public void AllowNumbers(){
		KeyManager.includeNumbers=practiceNumbers.isOn;
	}
	public void AllowSymbols(){
		KeyManager.includeSymbols=practiceSymbols.isOn;
	}
	public void UpdateCharDifficulty(){
		KeyManager.charPracticeDifficulty=charVarietySlider.value;
	}
	public void UpdateQuoteDifficulty(){
		KeyManager.quoteDifficulty=quoteDifficultySlider.value;
	}
	public void UpdateModeBias(){
		KeyManager.modeBias=modeBiasSlider.value;
	}
	public void ResetKeyData(){
		KeyManager.InitializeKeyDatabase();
	}
	
	//TODO: Make a proper UI plan and redesign the settings menu
	// Also consider where the progress graph will be drawn, if that is to be implemented
}
