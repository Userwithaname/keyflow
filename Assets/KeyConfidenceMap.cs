// using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class KeyConfidenceMap:MonoBehaviour{
	public static KeyConfidenceMap instance;
	public GameObject keyPrefab;
	public Transform[] rows;
	float averageSeekTime=.3f;
	public TMP_Dropdown layoutsDropdown;
	
	/*
	 * TODO: Create a UI for viewing key data - maybe draw a keyboard and color each key based on confidence, maybe also add a 'determine layout' (or customize) button so the layout matches what is actually used.
	 *		Additionally, different physical layout options could be added (row-stagger, ortho-linear, split, etc.)
	 *		The 'Remove Key Data' can be renamed to 'Show Key Data', and options to clear it can be added to that menu
	 */
	
	string layout=
		"`~1!2@3#4$5%6^7&8*9(0)-_=+\n"+
      "qQwWeErRtTyYuUiIoOpP[{]}\\|\n"+
      "aAsSdDfFgGhHjJkKlL;:'\"\n"+
      "zZxXcCvVbBnNmM,<.>/?";
		// "[{+4=2*0|1<6#$@`>7&8^5~3%9]}\n"+
  //     "bByYoOuU'(/\\\")lLdDwWvVzZ\n"+
  //     "cCiIeEaA,;  .:hHtTsSnNqQ\n"+
  //     "gGxXjJkK-_?!rRmMfFpP";
  public string selectedLayout="QWERTY.txt";
  // string selectedLayout="QWERTY.txt";
	
	Transform[] buttons;
	// IEnumerator Start(){
	void OnEnable(){
		// yield return null;
		instance=this;
		UpdateLayout();
		CreateLayout();
	}
	
	int lastActiveLayoutIndex=-1;
	void LateUpdate(){
		if(layoutsDropdown.value!=lastActiveLayoutIndex){
			ChangeSelectedLayout();
			lastActiveLayoutIndex=layoutsDropdown.value;
			PlayerPrefs.SetString("keyboardLayoutUI",selectedLayout);
		}
	}
	public void ChangeSelectedLayout(){
		selectedLayout=lastActiveLayoutIndex==-1?PlayerPrefs.GetString("keyboardLayoutUI",selectedLayout):layoutsDropdown.options[layoutsDropdown.value].text+".txt";
		// Debug.Log(selectedLayout);
		UpdateLayout();
		CreateLayout();
	}
	public void UpdateLayout(){
		string layoutsPath=
			#if UNITY_EDITOR
				Application.dataPath+"/../Keyboard Layouts"
			#else
				Application.dataPath+"/Keyboard Layouts"
			#endif
		;
		if(!System.IO.Directory.Exists(layoutsPath))	return;
		
		bool hasValue=false;
		layoutsDropdown.ClearOptions();
		foreach(string file in System.IO.Directory.GetFiles(layoutsPath)){	//TODO: Idea: Maybe add a shortcut to open the layouts directory (or create a custom layout editor in-game)
			string filename=System.IO.Path.GetFileName(file);
			layoutsDropdown.options.Add(new TMP_Dropdown.OptionData(filename.Remove(filename.Length-4,4)));
			if(filename==selectedLayout||(!hasValue&&filename=="QWERTY.txt")){
				layout=System.IO.File.ReadAllText(file);
				layoutsDropdown.value=layoutsDropdown.options.Count;
				hasValue=true;
			}
			// Debug.Log(System.IO.Path.GetFileName(file));
		}
		UpdateTheme();
		#if UNITY_WEBGL
			layoutsDropdown.gameObject.SetActive(false);	// Don't show the layouts dropdown in the web player, because the layouts directory doesn't exist
		#endif
	}
	
	public void CreateLayout(){
		Cleanup();
		
		buttons=new Transform[(layout.Length-layout.Split('\n').Length+1)/2];
		
		// averageScore=60f/(KeyManager.averageWPM*5);
		// float averageContextualSeekTime=0;
		for(int i=KeyManager.lowercaseStart;i<=KeyManager.lowercaseEnd;i++){
			if(KeyManager.instance.confidenceDatabase[i].hits<=3&&
			   KeyManager.instance.confidenceDatabase[i].seekTime+
			   KeyManager.instance.confidenceDatabase[i].previousKeySeekTime+
			   KeyManager.instance.confidenceDatabase[i].nextKeySeekTime>=999999)
				continue;
			// averageSeekTime+=KeyManager.instance.confidenceDatabase[i].seekTime;
			// averageSeekTime+=(KeyManager.instance.confidenceDatabase[i].previousKeySeekTime+
			//                   KeyManager.instance.confidenceDatabase[i].seekTime+
			//                   KeyManager.instance.confidenceDatabase[i].nextKeySeekTime)/3;
			averageSeekTime+=Mathf.Min(KeyManager.instance.confidenceDatabase[i].previousKeySeekTime,
			                           Mathf.Min(KeyManager.instance.confidenceDatabase[i].seekTime,
			                                       KeyManager.instance.confidenceDatabase[i].nextKeySeekTime));
			if(i>KeyManager.lowercaseStart) averageSeekTime/=2;
		}
		// averageSeekTime=Mathf.Min(averageSeekTime,.4f);	// Consider 30 WPM to be the lowest allowed to be colored green
		
		int row=0;
		int buttonIndex=0;
		for(int key=0;key<layout.Length;){
			if(layout[key]=='\n'){
				row++;
				key++;
				continue;
			}
			buttons[buttonIndex]=Instantiate(keyPrefab,rows[row],true).transform;
			buttons[buttonIndex].Find("Outline").GetComponent<Image>().color=Typing.instance.themes[Typing.instance.selectedTheme].buttonColor;
			for(int i=0;i<2;i++){
				Transform button=buttons[buttonIndex].GetChild(i);
				button.GetChild(0).GetComponent<TMP_Text>().text=$"{layout[key]}";
				int keyIndex=KeyManager.GetKeyIndex(layout[key]);
				float score=Mathf.Max(KeyManager.instance.confidenceDatabase[keyIndex].seekTime,
				                      Mathf.Max(KeyManager.instance.confidenceDatabase[keyIndex].previousKeySeekTime,
				                                  KeyManager.instance.confidenceDatabase[keyIndex].nextKeySeekTime));
				if(KeyManager.instance.confidenceDatabase[keyIndex].hits>=3&&score<99999){
					score=(averageSeekTime/score)*.55f;
					switch(score){
						case >1:
							button.GetComponent<Image>().color=Color.Lerp(Color.green,new Color(0,.95f,.25f,1),(score-1)*2);
							break;
						case >.5f:
							button.GetComponent<Image>().color=Color.Lerp(Color.yellow,Color.green,(score-.5f)*2);
							break;
						default:
							button.GetComponent<Image>().color=Color.Lerp(Color.red,Color.yellow,score*2);
							break;
					}
					
					// if(score<=averageScore){
					// 	button.GetComponent<Image>().color=Color.green;
					// 	// float colorBlend=Mathf.Pow((averageScore*2-score)/(averageScore*2),2);
					// 	// button.GetComponent<Image>().color=Color.Lerp(Color.green,aboveAverageColor,colorBlend);
					// }else{
					// 	// float colorBlend=Mathf.Pow(score/(averageScore),2);
					// 	float colorBlend=score;
					// 	if(colorBlend>.5f)
					// 		button.GetComponent<Image>().color=Color.Lerp(Color.yellow,Color.red,(colorBlend-.5f)*2);
					// 	else
					// 		button.GetComponent<Image>().color=Color.Lerp(Color.green,Color.yellow,colorBlend*2);
					// }
				}

				key++;
			}
			buttonIndex++;
		}
	}
	public void Cleanup(){
		if(buttons==null)
			return;
		foreach(Transform button in buttons){
			Destroy(button.gameObject);
		}
	}
	public void UpdateTheme(){
		if(buttons!=null){
			foreach(Transform t in buttons){
				t.Find("Outline").GetComponent<Image>().color=Typing.instance.themes[Typing.instance.selectedTheme].buttonColor;
			}
		}

		foreach(var img in layoutsDropdown.GetComponentsInChildren<Image>(true)){
			switch(img.name){
				case "Arrow":case "Item Checkmark":{
					img.color=Typing.instance.themes[Typing.instance.selectedTheme].textColorUI;
					continue;
				}
				default:{
					img.color=Typing.instance.themes[Typing.instance.selectedTheme].buttonColor;
					continue;
				}
			}
		}

		foreach(var text in layoutsDropdown.GetComponentsInChildren<TMP_Text>(true)){
			text.color=Typing.instance.themes[Typing.instance.selectedTheme].textColorUI;
		}
	}
}
