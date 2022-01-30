using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyConfidenceMap:MonoBehaviour{
	public GameObject keyPrefab;
	public Transform numberRow,
	                 topRow,
	                 middleRow,
	                 bottomRow;
	float averageScore=.3f;
	[System.NonSerialized]
	
	/*
	 * TODO: Create a UI for viewing key data - maybe draw a keyboard and color each key based on confidence, maybe also add a 'determine layout' (or customize) button so the layout matches what is actually used.
	 *		Additionally, different physical layout options could be added (row-stagger, ortho-linear, split, etc.)
	 *		The 'Remove Key Data' can be renamed to 'Show Key Data', and options to clear it can be added to that menu
	 */
	//TODO: Idea: Add a button to start a practice for the selected key
	//TODO: Show data for the key or press (or hover?)
	
	//TODO: Support other layouts
	string[] layout={
		         "`1234567890-=",
		         "qwertyuiop[]\\",
		         "asdfghjkl;'",
		         "zxcvbnm,./"
	         },
	         layoutShift={
		         "~!@#$%^&*()_+",
		         "QWERTYUIOP{}|",
		         "ASDFGHJKL:\"",
		         "ZXCVBNM<>?"
	};
	// string[] layout={
	//          "[<*=+&#@%^~|>]",
	//          "byou'/\"ldwvz",
	//          "ciea, .htsnq",
	//          "gxjk-?rmfp"
 //         },
 //         layoutShift={
	//          "{60248$`95317}",
	//          "BYOU(\\)LDWVZ",
	//          "CIEA; :HTSNQ",
	//          "GXJK_!RMFP"
 //         };
	
	Transform[] buttons;
	void OnEnable(){
		CreateLayout();
	}
	Color aboveAverageColor=new Color(0.85f,1,0,1);
	public void CreateLayout(){
		Cleanup();
		averageScore=60f/(KeyManager.averageWPM*5);	// WPM = time to type 5 characters * 60 (so 1f/WPM*5?)
		int buttonIndex=0;
		buttons=new Transform[layout[0].Length+layout[1].Length+layout[2].Length+layout[3].Length];
		
		// Number Row
		for(int i=0;i<layout[0].Length;i++){
			buttons[buttonIndex]=Instantiate(keyPrefab,numberRow,true).transform;
			Transform button=buttons[buttonIndex].GetChild(0);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layoutShift[0][i]}";
			float score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[0][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[0][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[0][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			          button=buttons[buttonIndex].GetChild(1);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layout[0][i]}";
			      score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[0][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[0][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[0][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			buttonIndex++;
		}
		
		// Top Row
		for(int i=0;i<layout[1].Length;i++){
			buttons[buttonIndex]=Instantiate(keyPrefab,topRow,true).transform;
			Transform button=buttons[buttonIndex].GetChild(0);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layoutShift[1][i]}";
			float score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[1][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[1][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[1][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			          button=buttons[buttonIndex].GetChild(1);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layout[1][i]}";
			      score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[1][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[1][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[1][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			buttonIndex++;
		}
		
		// Middle Row
		for(int i=0;i<layout[2].Length;i++){
			buttons[buttonIndex]=Instantiate(keyPrefab,middleRow,true).transform;
			Transform button=buttons[buttonIndex].GetChild(0);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layoutShift[2][i]}";
			float score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[2][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[2][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[2][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			          button=buttons[buttonIndex].GetChild(1);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layout[2][i]}";
			      score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[2][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[2][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[2][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			buttonIndex++;
		}
		
		// Bottom Row
		for(int i=0;i<layout[3].Length;i++){
			buttons[buttonIndex]=Instantiate(keyPrefab,bottomRow,true).transform;
			Transform button=buttons[buttonIndex].GetChild(0);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layoutShift[3][i]}";
			float score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[3][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[3][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layoutShift[3][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			          button=buttons[buttonIndex].GetChild(1);
			button.GetChild(0).GetComponent<TMP_Text>().text=$"{layout[3][i]}";
			      score=(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[3][i])].seekTime+
			             Mathf.Max(KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[3][i])].previousKeySeekTime,
			                       KeyManager.instance.confidenceDatabase[KeyManager.GetKeyIndex(layout[3][i])].nextKeySeekTime))/2-averageScore;
			if(score<99999) button.GetComponent<Image>().color=Color.Lerp(Color.green,score<=averageScore?aboveAverageColor:Color.red,Mathf.Pow((score<=averageScore?(averageScore-score)/averageScore:score/(averageScore*3)),3));
			buttonIndex++;
		}
	}
	public void UpdateLayout(){
		//TODO
	}
	public void Cleanup(){
		if(buttons==null)
			return;
		foreach(Transform button in buttons){
			Destroy(button.gameObject);
		}
	}
}
