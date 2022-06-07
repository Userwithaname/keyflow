// using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyConfidenceMap:MonoBehaviour{
	public static KeyConfidenceMap instance;
	public GameObject keyPrefab;
	public Transform[] rows;
	float averageScore=.3f;
	
	/*
	 * TODO: Create a UI for viewing key data - maybe draw a keyboard and color each key based on confidence, maybe also add a 'determine layout' (or customize) button so the layout matches what is actually used.
	 *		Additionally, different physical layout options could be added (row-stagger, ortho-linear, split, etc.)
	 *		The 'Remove Key Data' can be renamed to 'Show Key Data', and options to clear it can be added to that menu
	 */
	//TODO: Idea: Add a button to start a practice for the selected key
	//TODO: Show data for the key or press (or hover?)
	
	//TODO: Support other layouts (user-customizable)
	string layout=
		"`~1!2@3#4$5%6^7&8*9(0)-_=+\n"+
      "qQwWeErRtTyYuUiIoOpP[{]}\\|\n"+
      "aAsSdDfFgGhHjJkKlL;:'\"\n"+
      "zZxXcCvVbBnNmM,<.>/?";
		// "[{+4=2*0|1<6#$@`>7&8^5~3%9]}\n"+
  //     "bByYoOuU'(/\\\")lLdDwWvVzZ\n"+
  //     "cCiIeEaA,;  .:hHtTsSnNqQ\n"+
  //     "gGxXjJkK-_?!rRmMfFpP";
  string selectedLayout="Engram - ISO Wide Mod.txt";	//TODO: Save in PlayerPrefs
	
	Transform[] buttons;
	// IEnumerator Start(){
	void OnEnable(){
		// yield return null;
		instance=this;
		string layoutsPath=
			#if UNITY_EDITOR
				Application.dataPath+"/../Keyboard Layouts"
			#else
				Application.dataPath+"/Keyboard Layouts"
			#endif
		;
		if(System.IO.Directory.Exists(layoutsPath)){
			foreach(string file in System.IO.Directory.GetFiles(layoutsPath)){	//TODO: Store list of files, show a dropdown for the user to select their layout, maybe add a shortcut to open the layouts directory (or create a custom layout editor in-game)
				if(System.IO.Path.GetFileName(file)==selectedLayout)
					layout=System.IO.File.ReadAllText(file);
				Debug.Log(System.IO.Path.GetFileName(file));
			}
		}
		CreateLayout();
	}
	Color aboveAverageColor=new Color(.5f,1f,.5f,1);
	public void CreateLayout(){
		Cleanup();
		
		buttons=new Transform[(layout.Length-layout.Split('\n').Length+1)/2];
		
		//TODO: Idea: Get the score based on average values from all tracked keys instead of the global average speed
		averageScore=60f/(KeyManager.averageWPM*5);
		
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
				float score=(Mathf.Max(KeyManager.instance.confidenceDatabase[keyIndex].seekTime,
											  Mathf.Max(KeyManager.instance.confidenceDatabase[keyIndex].previousKeySeekTime,
															  KeyManager.instance.confidenceDatabase[keyIndex].nextKeySeekTime)))-averageScore;
				score*=2f-(((float)KeyManager.instance.confidenceDatabase[keyIndex].hits/(KeyManager.instance.confidenceDatabase[keyIndex].hits+KeyManager.instance.confidenceDatabase[keyIndex].misses)+.05f)*.9f+.1f);
				if(score<99999){
					if(score<=averageScore){
						button.GetComponent<Image>().color=Color.green;
						// float colorBlend=Mathf.Pow((averageScore*2-score)/(averageScore*2),2);
						// button.GetComponent<Image>().color=Color.Lerp(Color.green,aboveAverageColor,colorBlend);
					}else{
						float colorBlend=Mathf.Pow(score/(averageScore*3),2);
						if(colorBlend>.5f)
							button.GetComponent<Image>().color=Color.Lerp(Color.yellow,Color.red,(colorBlend-.5f)*2);
						else
							button.GetComponent<Image>().color=Color.Lerp(Color.green,Color.yellow,colorBlend*2);
					}
				}

				key++;
			}
			buttonIndex++;
		}
	}
	public void UpdateLayout(){
		//TODO: Update the layout without re-instantiating the keys (might still need to instantiate/destroy when the key count differs between layouts (extra key in one row, etc))
	}
	public void Cleanup(){
		if(buttons==null)
			return;
		foreach(Transform button in buttons){
			Destroy(button.gameObject);
		}
	}
	public void UpdateTheme(){
		foreach(Transform t in buttons){
			t.Find("Outline").GetComponent<Image>().color=Typing.instance.themes[Typing.instance.selectedTheme].buttonColor;
		}
	}
}
