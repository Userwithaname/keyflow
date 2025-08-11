using TMPro;
using UnityEngine;

public class SelectPracticeButton : MonoBehaviour {
	public void LoadPractice() {
		char c = GetComponentInChildren<TMP_Text>().text[0];
		if (KeyManager.GetNumberOfQuotesForKey(c) > 0) {
			Typing.instance.SelectPracticeByCharacter(c);
		}
	}
}
