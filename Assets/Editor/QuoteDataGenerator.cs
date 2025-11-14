using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/*
 *	Ideas:
 *		- Expose this code outside to the build as well, which would allow things like:
 *			- Indexing to memory instead of disk (generate the per-key quote data and store it to an array somewhere rather than files on disk)
 *			- Allow multiple sources for quotes to be accessed from (for example, Resources folder, user-provided quotes in the build directory, quotes obtained from the web (and likely cached to disk), etc)
 *			- Re-indexing at any time (opens doors to user-provided content, content from the web, etc)
 */

public class QuoteDataGenerator : EditorWindow {
	[MenuItem("Window/Quote Data Generator")]
	static void Init() {
		QuoteDataGenerator window = (QuoteDataGenerator)GetWindow(typeof(QuoteDataGenerator));
		window.Repaint();
	}
	void OnGUI() {
		if (GUILayout.Button("Generate Data")) {
			GenerateData();
		}
	}
	[MenuItem("Assets/Generate Quote Data")]
	public static void GenerateData() {
		List<char> trackedKeys = KeyManager.trackedKeys.ToList();
		trackedKeys.Sort();

		// Get all quotes, change the names so they include a valid path
		List<string> quoteFiles = new();
		List<string> sortedQuoteContents = new();
		string[] quotePaths = {
				"Content/Wikipedia",
		};
		foreach (string path in quotePaths) {
			foreach (TextAsset t in Resources.LoadAll<TextAsset>(path)) {
				quoteFiles.Add(path + "/" + t.name);
				char[] text = t.text.Trim().ToCharArray();

				if (text.Length == 0) {
					Debug.LogWarning($"{t.name}: Empty file", t);
					continue;
				}

				text[0] = ' ';	// Ignore the first character, as it isn't tracked

				// Warn if the quote would take too long to type
				const float MaxTime = 120; // Maximum typing time at default WPM
				float quoteTypingTime = text.Length * KeyManager.WPMToSeekTime(KeyManager.DefaultWPM);
				if (quoteTypingTime > MaxTime) {
					Debug.LogWarning(
						$"'{t.name}' would take {Typing.TimeFormattedString(quoteTypingTime, true)}" +
						$" to type at {KeyManager.DefaultWPM} WPM" +
						$" (expected {Typing.TimeFormattedString(MaxTime, true)} or less)"
					);
				}

				Array.Sort(text);
				sortedQuoteContents.Add(new string(text.ToArray()));
			}
		}

		// Use as such: keyFrequencyInfo[characterIndex][quoteNameIndex]
		List<string>[] quoteKeyFrequencyInfo = new List<string>[trackedKeys.Count];
		int[] charProgress = new int[quoteFiles.Count];

		for (int i = 0; i < trackedKeys.Count; i++) {
			quoteKeyFrequencyInfo[i] = new List<string>();
			for (int j = 0; j < quoteFiles.Count; j++) {
				int charCount = 0;
				for (;
					charProgress[j] < sortedQuoteContents[j].Length &&
						sortedQuoteContents[j][charProgress[j]] == trackedKeys[i];
					charProgress[j]++
				) {
					charCount++;
				}
				if (charCount == 0) continue;

				char[] prefix = { '0', '0', '0', '0', '0', '0', '0', '0' };	// 8 characters
				string charCountPrefix = Mathf.CeilToInt(
					(float)(charCount + 1) / sortedQuoteContents[j].Length * 99999999
				).ToString();
				for (int c = 1; c <= charCountPrefix.Length; c++) {
					prefix[^c] = charCountPrefix[^c];
				}
				quoteKeyFrequencyInfo[i].Add(new string(prefix) + quoteFiles[j]);
			}

			// Reverse-sort it, so quotes with more occurrences of that character are higher in the list
			quoteKeyFrequencyInfo[i].Sort();
			quoteKeyFrequencyInfo[i].Reverse();

			// Remove the prefix characters from the filenames
			for (int j = 0; j < quoteKeyFrequencyInfo[i].Count; j++) {
				quoteKeyFrequencyInfo[i][j] = quoteKeyFrequencyInfo[i][j].Remove(0, 8);
			}

			// Write the list to a file named by the current index
			File.WriteAllLines($"Assets/Resources/CharFreqData/{i}.txt", quoteKeyFrequencyInfo[i]);
		}

		// Warn if there are untracked characters in the file
		for (int i = 0; i < charProgress.Length; i++) {
			if (charProgress[i] == sortedQuoteContents[i].Length)
				continue;
			for (int j = charProgress[i]; j < sortedQuoteContents[i].Length; j++) {
				Debug.LogWarning(
					quoteFiles[i] + ": Invalid character: " + sortedQuoteContents[i][j],
					Resources.Load(quoteFiles[i])
				);
			}
		}

		Debug.Log("Quote character frequency data updated");
	}
}
