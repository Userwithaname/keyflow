using TMPro;
using UnityEngine;

public class SelectPracticeButton:MonoBehaviour{
	public void LoadPractice(){
		Typing.instance.SelectPracticeByCharacter(GetComponentInChildren<TMP_Text>().text[0]);
	}
}
