using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class KeyConfidenceMap : MonoBehaviour {
	public static KeyConfidenceMap instance;
	public GameObject keyPrefab;
	public Transform[] rows;
	float averageSeekTime = .3f;
	public TMP_Dropdown layoutsDropdown;
	
	/*
	 * TODO: Maybe also add a 'determine layout' (or customize) button so the layout matches what is actually used.
	 *       Additionally, different physical layout options could be added (row-stagger/ortholinear)
	 */
	
	string layout =
		"`~1!2@3#4$5%6^7&8*9(0)-_=+\n" +
		"qQwWeErRtTyYuUiIoOpP[{]}\\|\n" +
		"aAsSdDfFgGhHjJkKlL;:'\"\n" +
		"zZxXcCvVbBnNmM,<.>/?";
	public string selectedLayout = "QWERTY.txt";
	
	Transform[] buttons;
	void OnEnable() {
		instance = this;
		if (layoutsDropdown.value != lastActiveLayoutIndex) {
			LateUpdate();
		} else{
			UpdateLayout();
			CreateLayout();
		}
	}
	
	int lastActiveLayoutIndex = -1;
	void LateUpdate() {
		if (layoutsDropdown.value != lastActiveLayoutIndex) {
			ChangeSelectedLayout();
			lastActiveLayoutIndex = layoutsDropdown.value;
			PlayerPrefs.SetString("keyboardLayoutUI", selectedLayout);
		}
	}
	public void ChangeSelectedLayout() {
		selectedLayout = lastActiveLayoutIndex == -1 ?
			PlayerPrefs.GetString("keyboardLayoutUI", selectedLayout):
			layoutsDropdown.options[layoutsDropdown. value].text + ".txt";
		UpdateLayout();
		CreateLayout();
	}
	public void UpdateLayout() {
		string layoutsPath =
			#if UNITY_EDITOR
				Application.dataPath + "/../Keyboard Layouts"
			#else
				Application.dataPath + "/Keyboard Layouts"
			#endif
		;
		if (!System.IO.Directory.Exists(layoutsPath))	return;
		
		bool hasValue = false;
		layoutsDropdown.ClearOptions();
		// TODO: Idea: Maybe add a shortcut to open the layouts directory
		// (or create a custom layout editor in-game)
		foreach (string file in System.IO.Directory.GetFiles(layoutsPath)) {
			string filename=System.IO.Path.GetFileName(file);
			layoutsDropdown.options.Add(
				new TMP_Dropdown.OptionData(filename.Remove(filename.Length - 4, 4))
			);
			if (filename == selectedLayout || (!hasValue && filename == "QWERTY.txt")) {
				layout = System.IO.File.ReadAllText(file);
				layoutsDropdown.value = layoutsDropdown.options.Count;
				hasValue = true;
			}
		}
		UpdateTheme();
		#if UNITY_WEBGL
			// Don't show the layouts dropdown in the web player,
			// because the layouts directory doesn't exist there
			layoutsDropdown.gameObject.SetActive(false);
		#endif
	}
	
	public void CreateLayout() {
		Cleanup();
		
		buttons = new Transform[(layout.Length - layout.Split('\n').Length + 1) / 2];
		
		const int minHitCount = 3;
		int numKeys = 0;
		
		for (int i = KeyManager.lowercaseStart; i <= KeyManager.lowercaseEnd; i++) {
			if (KeyManager.instance.confidenceDatabase[i].hits < minHitCount ||
				KeyManager.instance.confidenceDatabase[i].seekTime +
					KeyManager.instance.confidenceDatabase[i].previousKeySeekTime +
					KeyManager.instance.confidenceDatabase[i].nextKeySeekTime
				>= 999999
			) {
				continue;
			}
			averageSeekTime += Mathf.Min(
				KeyManager.instance.confidenceDatabase[i].previousKeySeekTime,
				Mathf.Min(
					KeyManager.instance.confidenceDatabase[i].seekTime,
					KeyManager.instance.confidenceDatabase[i].nextKeySeekTime
				)
			);
			if (i > KeyManager.lowercaseStart) numKeys++;
		}
		
		averageSeekTime /= numKeys;
		
		int row = 0;
		int buttonIndex = 0;
		for (int key = 0; key < layout.Length;) {
			if (layout[key] == '\n') {
				row++;
				key++;
				continue;
			}
			buttons[buttonIndex] = Instantiate(keyPrefab,rows[row],true).transform;
			buttons[buttonIndex].Find("Outline").GetComponent<Image>().color =
				Typing.instance.themes[Typing.instance.selectedTheme].buttonColor;
			for (int i = 0; i < 2; i++) {
				Transform button=buttons[buttonIndex].GetChild(i);
				button.GetChild(0).GetComponent<TMP_Text>().text = $"{layout[key]}";
				int keyIndex = KeyManager.GetKeyIndex(layout[key]);
				float score = (
					KeyManager.instance.confidenceDatabase[keyIndex].seekTime+
						KeyManager.instance.confidenceDatabase[keyIndex].previousKeySeekTime+
						KeyManager.instance.confidenceDatabase[keyIndex].nextKeySeekTime+
						Mathf.Max(
							KeyManager.instance.confidenceDatabase[keyIndex].seekTime,
							Mathf.Max(
								KeyManager.instance.confidenceDatabase[keyIndex].previousKeySeekTime,
								KeyManager.instance.confidenceDatabase[keyIndex].nextKeySeekTime
							)
						)
				) / 4;
				if (score < 99999) {
					score = averageSeekTime / score;
					if (score < 1) {
						score *= score;
					} else {
						score = (score - 1) * .75f;
						score = Mathf.Lerp(score, score * score, .5f) + 1;
					}
					Color keyColor = score switch{
						< .5f => Color.Lerp(
							Color.red,
							Color.yellow,
							Mathf.Sqrt(score * 2)
						),
						< 1   => Color.Lerp(
							Color.yellow,
							Color.green,
							Mathf.Pow((score - .5f) * 2, 2)
						),
						_     => Color.Lerp(
							Color.green,
							new Color(.2f, .8f, .6f, 1),
							(score - 1)
						),
					};
					
					if (KeyManager.instance.confidenceDatabase[keyIndex].hits < minHitCount) {
						keyColor = Color.Lerp(
							Color.grey,
							keyColor,
							(float)(KeyManager.instance.confidenceDatabase[keyIndex].hits + 1) / 16
						);
					}
					
					button.GetComponent<Image>().color = keyColor;
				}

				key++;
			}
			buttonIndex++;
		}
	}
	public void Cleanup() {
		if (buttons == null) return;
		foreach (Transform button in buttons) {
			Destroy(button.gameObject);
		}
	}
	public void UpdateTheme() {
		if (buttons == null) return;
		foreach (Transform t in buttons) {
			t.Find("Outline").GetComponent<Image>().color =
				Typing.instance.themes[Typing.instance.selectedTheme].buttonColor;
		}
	}
}
