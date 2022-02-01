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
		// "[{<6*0=2+4&8#$@`%9^5~3|1>7]}\n"+
  //     "bByYoOuU'(/\\\")lLdDwWvVzZ\n"+
  //     "cCiIeEaA,;  .:hHtTsSnNqQ\n"+
  //     "gGxXjJkK-_?!rRmMfFpP";
	
	Transform[] buttons;
	void OnEnable(){
		instance=this;
		CreateLayout();
	}
	Color aboveAverageColor=new Color(0.85f,1,0,1);
	public void CreateLayout(){
		Cleanup();
		
		buttons=new Transform[(layout.Length-layout.Split('\n').Length+1)/2];
		
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
				if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),2));

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
