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
	 * TODO: Settings menu (for choosing filters, difficulty slider, etc)
	 * TODO: Personalized lesson type filters (eg, exclude code, quote, etc.)
	 *
	 * TODO: Progress tracking (save the average accuracy/speed for each day, draw a graph that the user can check whenever they want)
	 * TODO: Qutoe progress tracking (show a graph of how the speed/accuracy varied over time during the current quote)
	 */
	
	//TODO: Make all colors configurable (or even just a brightness slider for individual categories (background elements like the score, quote text, entered quote text, etc))
	//TODO: Auto-fade background elements while typing, reveal when mouse is moved or input field is unfocused (maybe a smooth, slow, non-linear transition to make it feel cinematic and awesome)
	
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
	public TMP_Text lessonInfo,WPMInfo,averageWPMInfo,quoteInfo;
	public Color caretColor=Color.white,caretErrorColor=Color.red;
	
	public GameObject settingsUI;
	public Toggle practiceUppercase,
	              practiceNumbers,
	              practiceSymbols,
	              practiceWhitespace,
	              showIncorrectCharacters;
	public Slider charVarietySlider,quoteDifficultySlider;
	
	int hitCount,missCount;
	
	bool showMenu;
	
	public RectTransform textTransform;
	RectTransform caretTransform;
	Vector3 initialTextPos,initialCaretPos;
	
	void Start(){
		// if(Resources.Load("CharFreqData/1.txt")==null){
		// 	Debug.LogError("Something went wrong while loading resources");
		// 	Application.Quit();
		// 	return;
		// }
		
		Application.targetFrameRate=Screen.currentResolution.refreshRate;
		
		practiceUppercase.isOn=KeyManager.includeUppercase;
		practiceNumbers.isOn=KeyManager.includeNumbers;
		practiceSymbols.isOn=KeyManager.includeSymbols;
		
		charVarietySlider.value=KeyManager.charPracticeDifficulty;
		quoteDifficultySlider.value=KeyManager.quoteDifficulty;
		
		//TODO: Function for practicing multiple characters together (for example, quotes that appear in lists 'a' and 'b', and score highly)
		initialTextPos=textTransform.localPosition;
		caretTransform=textTransform.parent.Find("Caret").GetComponent<RectTransform>();
		initialCaretPos=caretTransform.localPosition;
		NextLesson();
	}
	
	public void NextLesson(){
		textTransform.localPosition=initialTextPos;
		caretTransform.localPosition=initialCaretPos;
		curPracticeIndex=KeyManager.GetLowConfidenceCharacter();
		text=KeyManager.GetQuoteByCharFrequency(curPracticeIndex);
		ResetLesson();
	}
	public void ResetLesson(){
		done=false;
		UpdateCurrentPracticeUI();
		hitCount=missCount=0;
		input=lastInput="";
		incorrect=done=false;
		lastFrameIncorrect=true;
		textDisplay.readOnly=false;
		seekTime=wordTime=totalTestTime=0;
		loc=lastLength=lastMaxLength=-1;
		// EventSystem.current.SetSelectedGameObject(textDisplayObject);
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
		KeyManager.KeyConfidenceData updatedCharPractice=KeyManager.instance.confidenceDatabase[curPracticeIndex];
		curCharacterPractice=updatedCharPractice.keyName;
		curCharacterSeekTime=updatedCharPractice.seekTime>0?Math.Round(updatedCharPractice.seekTime*1000,2)+" ms":"-";
		curCharacterNextSeekTime=updatedCharPractice.nextKeySeekTime>0?Math.Round(updatedCharPractice.nextKeySeekTime*1000,2)+" ms":":";
		curCharacterWPM=updatedCharPractice.wpm>0?Math.Round(updatedCharPractice.wpm,1)+" WPM":"-";
		curCharacterAccuracy=updatedCharPractice.hits+updatedCharPractice.misses>0?Math.Round((float)updatedCharPractice.hits/(updatedCharPractice.hits+updatedCharPractice.misses)*100,1)+"%":"-";
		
		// const string improvementColor="<color=#208020>",
		//              regressionColor="<color=#802020>";
		const string improvementColor="<color=#28a028>",
		             regressionColor="<color=#a02828>";
		
		if(comparisonIndex>-1){
			KeyManager.KeyConfidenceData compare=KeyManager.instance.confidenceDatabase[comparisonIndex];
			float diff=(float)Math.Round(updatedCharPractice.seekTime*1000-curCharPractice.seekTime*1000,3);
			curCharacterSeekTime=diff<=0?
			                     improvementColor+curCharacterSeekTime+" ("+diff+")</color>":
			                     regressionColor+curCharacterSeekTime+" (+"+diff+")</color>";
			diff=(float)Math.Round(updatedCharPractice.nextKeySeekTime*1000-curCharPractice.nextKeySeekTime*1000,3);
			curCharacterNextSeekTime=diff<=0?
			                     improvementColor+curCharacterNextSeekTime+" ("+diff+")</color>":
			                     regressionColor+curCharacterNextSeekTime+" (+"+diff+")</color>";
			diff=(float)Math.Round(updatedCharPractice.wpm-curCharPractice.wpm,3);
			curCharacterWPM=diff>=0?
			                     improvementColor+curCharacterWPM+" (+"+diff+")</color>":
			                     regressionColor+curCharacterWPM+" ("+diff+")</color>";
			float newAccuracy=(float)updatedCharPractice.hits/(updatedCharPractice.hits+updatedCharPractice.misses)*100;
			diff=(float)Math.Round(newAccuracy-(float)curCharPractice.hits/(curCharPractice.hits+curCharPractice.misses)*100,3);
			if(float.IsNaN(diff)) diff=newAccuracy;
			curCharacterAccuracy=diff>=0?
			                     improvementColor+curCharacterAccuracy+" (+"+diff+")</color>":
			                     regressionColor+curCharacterAccuracy+" ("+diff+")</color>";
		}
		curCharPractice=updatedCharPractice;
		
		lessonInfo.text=
			"<b>Current Practice: "+curCharacterPractice+"</b>"+
			"\n<b>Average Stats </b>(for <b>"+curCharacterPractice+"</b>)<b>:</b>"+
			"\nSeek Time: "+curCharacterSeekTime+
			"\nNext Key Seek Time: "+curCharacterNextSeekTime+
			"\nFull-Word: "+curCharacterWPM+
			"\nAccuracy: "+curCharacterAccuracy;
		
		float wpm=loc/totalTestTime*60/5;
		float accuracy=(float)hitCount/(hitCount+missCount)*100;
		float oldAverageAccuracy=KeyManager.averageAccuracy;
		float oldAverageSpeed=KeyManager.averageWPM;
		float oldTopSpeed=KeyManager.topWPM;
		if(wpm>KeyManager.topWPM) KeyManager.topWPM=wpm;
		if(done){
			if(accuracy>0)
				KeyManager.averageAccuracy=Mathf.Lerp(KeyManager.averageAccuracy,accuracy,KeyManager.averageAccuracy>0?.333f:1);
			if(wpm>0)
				KeyManager.averageWPM=Mathf.Lerp(KeyManager.averageWPM,wpm,KeyManager.averageWPM>0?.333f:1);
			int seconds=Mathf.FloorToInt(totalTestTime%60);
			if(float.IsNaN(accuracy)) accuracy=100;
			WPMInfo.text=
				"Accuracy: "+
					(accuracy>=oldAverageAccuracy?
						improvementColor+Math.Round(accuracy,2)+"% (+"+Math.Round(accuracy-oldAverageAccuracy,2)+" from average)</color>":
						regressionColor+Math.Round(accuracy,2)+"% ("+Math.Round(accuracy-oldAverageAccuracy,2)+" from average)</color>")+
				"\nSpeed: "+
					(wpm>=oldAverageSpeed?
						improvementColor+Math.Round(wpm,2)+" WPM ("+Math.Round(wpm-oldAverageSpeed,2)+" from average)</color>":
						regressionColor+Math.Round(wpm,2)+" WPM ("+Math.Round(wpm-oldAverageSpeed,2)+" from average)</color>")+
				"\nTime: "+Mathf.FloorToInt(totalTestTime/60)+':'+
				(seconds<10?"0":"")+seconds+':'+
				((float)Math.Round(totalTestTime-Mathf.FloorToInt(totalTestTime),3)%1).ToString().Split('.')[^1];
			
			averageWPMInfo.text=
				"Average Accuracy: "+
					(KeyManager.averageAccuracy>=oldAverageAccuracy?
						improvementColor+Math.Round(KeyManager.averageAccuracy,2)+"% (+"+Math.Round(KeyManager.averageAccuracy-oldAverageAccuracy,2)+")</color>":
						regressionColor+Math.Round(KeyManager.averageAccuracy,2)+"% ("+Math.Round(KeyManager.averageAccuracy-oldAverageAccuracy,2)+")</color>")+
				"\nAverage Speed: "+
					(KeyManager.averageWPM>=oldAverageSpeed?
						improvementColor+(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")+" WPM (+"+Math.Round(KeyManager.averageWPM-oldAverageSpeed,3)+")</color>":
						regressionColor+(KeyManager.averageWPM>0?Math.Round(KeyManager.averageWPM,2):"-")+" WPM ("+Math.Round(KeyManager.averageWPM-oldAverageSpeed,3)+")</color>")+
				"\nTop Speed: "+
					(KeyManager.topWPM>oldTopSpeed?
						improvementColor+(KeyManager.topWPM>0?Math.Round(KeyManager.topWPM,2):"-")+"  WPM(+"+(KeyManager.topWPM-oldTopSpeed)+")</color>":
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
		
		//TODO: Different font and alignment for the Code category
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
		string content=(incorrect?"<color=#806000>":"<color=#808080>")+text.Insert(cappedLoc,"</color>");
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
							chars[i]='␣';
							break;
						}
					}
				}
				content=content.Insert(cappedLoc+lengthDiff,"<color=red><u>"+new string(chars)+"</u></color>");
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
							chars[i]='␣';
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
	
	void Update(){
		if(Input.GetKeyDown(KeyCode.Escape)){	//BUG: Doesn't work if the input field is focused
			NextLesson();
		}
		
		SetTextColor();
		// int cappedLoc=Mathf.Min(loc+1,text.Length);
		// string content=(incorrect?"<color=#806000>":"<color=#808080>")+text.Insert(cappedLoc,"</color>");
		// if(incorrect){
		// 	int lengthDiff=content.Length-text.Length;
		// 	content=content.Insert(Mathf.Min(input.Length,text.Length)+lengthDiff,"</color>").Insert(cappedLoc+lengthDiff,"<color=red>");
		// 	// content=content.Insert(cappedLoc+lengthDiff,"<color=#C00000>"+input.Remove(0,loc+1)+"</color>");
		// }
		// textDisplay.text=content;
		// textDisplay.caretPosition=Mathf.Min(input.Length,text.Length);
		
		if(lastFrameIncorrect!=incorrect){
			textDisplay.caretColor=incorrect?caretErrorColor:caretColor;
			lastFrameIncorrect=incorrect;
		}
		
		if(done) return;
		int seconds=Mathf.FloorToInt(totalTestTime%60);
		float accuracy=(float)hitCount/(hitCount+missCount)*100;
		if(hitCount+missCount==0) accuracy=100;
		WPMInfo.text=
			"Accuracy: "+Mathf.RoundToInt(accuracy)+"%"+
			"\nSpeed: "+(totalTestTime==0?"-":Mathf.RoundToInt(loc/totalTestTime*60/5))+" WPM"+
		   "\nTime: "+Mathf.FloorToInt(totalTestTime/60)+':'+(seconds<10?"0":"")+seconds;
	}
	void LateUpdate(){
		incorrect=!text.StartsWith(input);
		switch(lastMaxLength){
			case >= 0 when input.Length<text.Length||incorrect:{
				seekTime+=Time.deltaTime;
				wordTime+=Time.deltaTime;
				totalTestTime+=Time.deltaTime;
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
				if(loc>lastMaxLength)
					lastMaxLength=loc;
				KeyManager.RegisterKeyHit(keyIndex);
				if(loc>0&&input[loc]!=input[loc-1]){
					KeyManager.UpdateSeekTime(keyIndex,seekTime);
					KeyManager.UpdateNextKeySeekTime(KeyManager.GetKeyIndex(input[loc-1]),seekTime);
				}
				// else{
				// 	quoteInfo.text="";
				// }
				if(!KeyManager.IsAlphaNumericIndex(keyIndex)&&length>lastMaxLength||input.Length==text.Length){
					//TODO: Save all word WPMs to an array, show them as a raw speed graph at the end of the game (also track real WPM in the same way)
					//BUG: Can result in infinity WPM and wrong words being registered, if not typing at the end of the field (eg. pressing left arrow key)
					if(loc>0&&input[loc]!=input[loc-1])
						KeyManager.UpdateWordSpeed(KeyManager.ValidateWord(lastInput),wordTime); 
					wordTime=0;
				}

				seekTime=0;
			}else{
				missCount++;
				KeyManager.RegisterKeyMiss(keyIndex);
				incorrect=true;
				break;
			}
		}
		
		incorrect=!text.StartsWith(input);
		//TODO: Autofill tabs/spaces after a newline or another tab/space (for code snippets)
		
		lastLength=input.Length;
		// if(!incorrect&&lastLength>lastMaxLength)
		// 	lastMaxLength=lastLength;
		lastInput=input;
		
		if(incorrect||input.Length<text.Length)	return;
		// KeyManager.RemoveHitsAndMisses(curPracticeIndex);
		done=textDisplay.readOnly=true;
		UpdateCurrentPracticeUI(curPracticeIndex);
		UnfocusInputField();
		KeyManager.Save();
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
}
