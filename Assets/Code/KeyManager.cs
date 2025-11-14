#define SCALLE_CHAR_DIFFICULTY_WITH_NUM_QUOTES

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class KeyManager : MonoBehaviour {
	public static KeyManager instance;
	public const float DefaultWPM = 34;
	[Serializable]public struct KeyConfidenceData{
		#if UNITY_EDITOR
			[NonSerialized]public string name;
		#endif
		
		public char  keyName;

		public float seekTime,
		             previousKeySeekTime,
		             nextKeySeekTime;

		[FormerlySerializedAs("wpm")] public float wordSpeed;
		public float speedTrend;
		public int   hits, misses;
		public float accuracy => (float) hits / (hits + misses);
	}
	public KeyConfidenceData[] confidenceDatabase; 
	public static readonly char[] trackedKeys = {
		'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
		'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
		'0','1','2','3','4','5','6','7','8','9',
		' ','\n','\t','\\','@','#','$',
		'.',',','\'','`','"','?','!',';',':','_','&',
		'=','~','-','+','%','/','*','^','<','>','|',
		'(',')','[',']','{','}',
	};
	public static int numbersStart, numbersEnd,
	                  capitalStart, capitalEnd,
	                  lowercaseStart, lowercaseEnd;

	public static bool includeUppercase = false,
	                   includeNumbers = false,
	                   includeSymbols = false,
	                   includeWhitespace = false;

	public static float averageAccuracy, averageWPM, topWPM;
	public static float charPracticeDifficulty = .71f; // 1: Only the lowest confidence characters, 0: Completely random
	public static float quoteDifficulty = .85f; // 1: only the quote with most frequent occurrence of the selected character, 0: unbiased
	public static float modeBias = .4f; // 1: multiple keys practice, 0: single key practice
	
	public static bool unsavedData = false;
	public static bool unsavedPrefs = false;

	void Start() {
		instance = this;
		InitializeKeyDatabase();
		Load();
	}
	
	public static void InitializeKeyDatabase() {
		instance.confidenceDatabase = new KeyConfidenceData[trackedKeys.Length];
		List<char> sortedKeys = trackedKeys.ToList<char>();
		sortedKeys.Sort();
		
		for (int i = 0; i < sortedKeys.Count; i++) {
			instance.confidenceDatabase[i].seekTime =
				instance.confidenceDatabase[i].previousKeySeekTime =
				instance.confidenceDatabase[i].nextKeySeekTime =
				Mathf.Infinity;
			instance.confidenceDatabase[i].keyName = sortedKeys[i];
			#if UNITY_EDITOR
				instance.confidenceDatabase[i].name = $"Element {i}: {sortedKeys[i]}".Trim();
			#endif
			
			switch (sortedKeys[i]) {
				case 'a': {
					lowercaseStart = i;
					break;
				}
				case 'z': {
					lowercaseEnd = i;
					break;
				}
				case 'A': {
					capitalStart = i;
					break;
				}
				case 'Z': {
					capitalEnd = i;
					break;
				}
				case '0': {
					numbersStart = i;
					break;
				}
				case '9': {
					numbersEnd = i;
					break;
				}
			}
		}
	}
	public static void Load() {
		averageAccuracy = PlayerPrefs.GetFloat("Average Acc", 0);
		averageWPM = PlayerPrefs.GetFloat("Average WPM", 0);
		topWPM = PlayerPrefs.GetFloat("Top WPM", 0);
		if (!System.IO.File.Exists($"{Application.persistentDataPath}/key-confidence-data")) {
			return;
		}
		string[] data =
			System.IO.File.ReadAllLines($"{Application.persistentDataPath}/key-confidence-data");
		for (int i = 0; i < data.Length; i++) {
			instance.confidenceDatabase[i] = JsonUtility.FromJson<KeyConfidenceData>(data[i]);
			if(instance.confidenceDatabase[i].seekTime <= 0)
				instance.confidenceDatabase[i].seekTime = Mathf.Infinity;
			if(instance.confidenceDatabase[i].previousKeySeekTime <= 0)
				instance.confidenceDatabase[i].previousKeySeekTime = Mathf.Infinity;
			if(instance.confidenceDatabase[i].nextKeySeekTime <= 0)
				instance.confidenceDatabase[i].nextKeySeekTime = Mathf.Infinity;
		}
	}
	public static void Save() {
		if (!unsavedData) {
			if (unsavedPrefs) {
				PlayerPrefs.Save();
				unsavedPrefs = false;
			}
			return;
		}

		Typing.instance.Save();
		
		PlayerPrefs.SetFloat("Average Acc", averageAccuracy);
		PlayerPrefs.SetFloat("Average WPM", averageWPM);
		PlayerPrefs.SetFloat("Top WPM", topWPM);
		
		string fileContents = "";
		foreach(KeyConfidenceData kcd in instance.confidenceDatabase) {
			fileContents += JsonUtility.ToJson(kcd) + "\n";
		}
		System.IO.File.WriteAllText($"{Application.persistentDataPath}/key-confidence-data", fileContents);
		PlayerPrefs.Save();

		unsavedData = false;
	}
	public void RemoveScores() {
		PlayerPrefs.DeleteAll();
		Load();
	}
	public void RemoveKeyData() {
		InitializeKeyDatabase();
	}
	
	public static int GetKeyIndex(char key, int startIndex = 0) {
		// key=key.ToString().ToLower()[0];
		for (int i = startIndex;i < instance.confidenceDatabase.Length; i++) {
			if(instance.confidenceDatabase[i].keyName == key)
				return i;
		}
		Debug.LogError("Invalid (untracked) character: " + key);
		return -1;
	}

	public static int[] GetKeyIndexes(string keys) {
		/*
		 * sort alphabetically
		 * remove duplicates
		 * loop through the 'instance.confidenceDatabase' and look for the next sorted match
		 */
		List<char> sorted = new List<char>();
		for (int i = 0; i < keys.Length; i++) {
			sorted.Add(keys[i]);
		}
		sorted.Sort();
		for(int i = 1; i < sorted.Count; i++) {
			if(sorted[i] == sorted[i - 1])
				sorted.RemoveAt(i);
		}
		
		int[] o = new int[sorted.Count];
		int lookupIndex = 0;
		for(int i = 0; i < instance.confidenceDatabase.Length && lookupIndex < o.Length; i++) {
			if (instance.confidenceDatabase[i].keyName != sorted[lookupIndex]) continue;
			o[lookupIndex]= i;
			lookupIndex++;
		}
		
		return o;
	}

	public static bool IsAlphaNumericIndex(int index) {
		return 
			index >= lowercaseStart && index <= lowercaseEnd ||
			index >= capitalStart && index <= capitalEnd ||
			index >= numbersStart && index <= numbersEnd;
	}
	public static bool IsWhitespaceIndex(int index) {
		return instance.confidenceDatabase[index].keyName switch {
			 ' ' => true,
			'\n' => true,
			'\t' => true,
			   _ => false
		};
	}
	public static bool IsWhitespaceCharacter(char character) {
		return character switch {
			 ' ' => true,
			'\n' => true,
			'\t' => true,
			   _ => false
		};
	}
	public static bool IsAlphanumericCharacter(char character) {
		return character
			is >= 'a' and <= 'z'
			or >= 'A' and <= 'Z'
			or >= '0' and <= '9';
	}
	
	public static float SeekTimeToWPM(float seekTime) {
		return 60f / seekTime / 5;
	}
	public static float WPMToSeekTime(float wpm) {
		return 60f / (wpm * 5);
	}
	public static float WPMFromTime(float timeSeconds, int textLength){
		return textLength / timeSeconds * 60 / 5;
	}

	//TODO: Function for lowering the hit/miss count without changing the ratio (e.g. divide by the same value, ratio might still change a bit because the numbers are integers)
	public static void RemoveHitsAndMisses(int keyIndex, int amount = 1) {
		if (instance.confidenceDatabase[keyIndex].hits < 10)
			return;
		instance.confidenceDatabase[keyIndex].hits = Mathf.Max(
			0, instance.confidenceDatabase[keyIndex].hits - amount
		);
		instance.confidenceDatabase[keyIndex].misses = Mathf.Max(
			0, instance.confidenceDatabase[keyIndex].misses - amount
		);
	}
	public static void RegisterKeyHit(int keyIndex) {
		instance.confidenceDatabase[keyIndex].hits++;
		unsavedData = true;
	}
	public static void RegisterKeyMiss(int keyIndex) {
		instance.confidenceDatabase[keyIndex].misses++;
		unsavedData = true;
	}
	
	/// <summary>
	/// Sets the previous key seek time (the time between the two previous keypress)
	/// </summary>
	public static void UpdatePreviousKeySeekTime(int index, float previousKeySeekTime) {
		float oldSeekTime = instance.confidenceDatabase[index].previousKeySeekTime;
		if (oldSeekTime is <10000 and >0) {
			instance.confidenceDatabase[index].previousKeySeekTime = Mathf.Lerp(
				instance.confidenceDatabase[index].previousKeySeekTime,
				previousKeySeekTime,
				.12f
			);
			instance.confidenceDatabase[index].speedTrend = Misc.ValidateIfNaN(
				Mathf.Lerp(
					instance.confidenceDatabase[index].speedTrend,
					SeekTimeToWPM(instance.confidenceDatabase[index].previousKeySeekTime) -
						SeekTimeToWPM(oldSeekTime),
					.003f
				),
				0
			);
		} else {
			instance.confidenceDatabase[index].previousKeySeekTime = previousKeySeekTime;
		}
	}
	
	/// <summary>
	/// Sets the key seek time (the time between the previous and current keypress)
	/// </summary>
	public static void UpdateKeySeekTime(int index, float seekTime) {
		float oldSeekTime = instance.confidenceDatabase[index].seekTime;
		if (oldSeekTime is <10000 and >0) {
			instance.confidenceDatabase[index].seekTime = Mathf.Lerp(
				instance.confidenceDatabase[index].seekTime,
				seekTime,
				.12f
			);
			instance.confidenceDatabase[index].speedTrend = Misc.ValidateIfNaN(
				Mathf.Lerp(
					instance.confidenceDatabase[index].speedTrend,
					SeekTimeToWPM(instance.confidenceDatabase[index].seekTime) -
						SeekTimeToWPM(oldSeekTime),
					.08f
				),
				0
			);
		} else {
			instance.confidenceDatabase[index].seekTime = seekTime;
		}
	}
	
	/// <summary>
	/// Sets the next key seek time (the time it took to move on from the current key)
	/// </summary>
	public static void UpdateNextKeySeekTime(int index, float nextKeySeekTime) {
		float oldSeekTime = instance.confidenceDatabase[index].nextKeySeekTime;
		if (oldSeekTime is <10000 and >0) {
			instance.confidenceDatabase[index].nextKeySeekTime = Mathf.Lerp(
				instance.confidenceDatabase[index].nextKeySeekTime,
				nextKeySeekTime,
				.12f
			);
			instance.confidenceDatabase[index].speedTrend = Misc.ValidateIfNaN(
				Mathf.Lerp(
					instance.confidenceDatabase[index].speedTrend,
					SeekTimeToWPM(instance.confidenceDatabase[index].nextKeySeekTime) -
						SeekTimeToWPM(oldSeekTime),
					.06f
				),
				0
			);
		} else {
			instance.confidenceDatabase[index].nextKeySeekTime = nextKeySeekTime;
		}
	}
	
	public static float UpdateWordSpeed(string word, float time) {
		float wpm = word.Length / time * 60 / 5;
		foreach (int i in GetKeyIndexes(word)) {
			switch (instance.confidenceDatabase[i].wordSpeed) {
				case 0:
					instance.confidenceDatabase[i].wordSpeed = wpm;
					continue;
				case float.NaN:
					instance.confidenceDatabase[i].wordSpeed = wpm;
					break;
			}

			instance.confidenceDatabase[i].wordSpeed = Mathf.Lerp(
				instance.confidenceDatabase[i].wordSpeed,
				wpm,
				.45f / (Mathf.Max(
					5 - word.Length * 5,
					word.Length
				) / 3f + 1)
			);
		}
		// Debug.Log($"The word \"{word}\" was typed at {wpm} WPM (took {time} seconds)");
		return wpm;
	}
	
	public static int GetWordCount(string text) {
		int wordCount = 0;
		foreach (char c in text) {
			if (!IsAlphaNumericIndex(GetKeyIndex(c))) wordCount++;
		}
		return wordCount;
	}
	
	public static string[] GetWordsInText(string text) {
		List<string> words = new List<string>();
		string word = "";
		foreach (char c in text) {
			word += c;
			if (!IsWhitespaceCharacter(c)) continue;
			words.Add(word);
			word = "";
		}
		if (word != "") {
			words.Add(word);
		}
		return words.ToArray();
	}
	
	public static string GetLastWord(string text) {
		return GetLastWord(text, text.Length - 1);
	}
	
	public static string GetLastWord(string text, int trimIndex) {
		for (int i = trimIndex; i > -1; i--) {
			if (i == trimIndex && IsWhitespaceCharacter(text[i])) i--;
			if (!IsWhitespaceCharacter(text[i])) continue;
			i++;
			string ret = "";
			while (i <= trimIndex) {
				ret += text[i];
				i++;
			}
			return ret;
		}
		return text;
	}
	
	public static bool CharWithinFilters(int index) {
		if (!includeUppercase && index >= capitalStart && index <= capitalEnd) return false;
		if (!includeNumbers && index >= numbersStart && index <= numbersEnd) return false;
		if (!includeSymbols && !IsAlphaNumericIndex(index)) return false;
		if (includeWhitespace) return true;
		return !IsWhitespaceIndex(index);
	}
	
	public static int GetLowConfidenceCharacter() {
		return GetLowConfidenceCharacter(charPracticeDifficulty);
	}
	public static int GetLowConfidenceCharacter(float difficulty) {
		int highestSeekTimeIndex = Random.Range(lowercaseStart, lowercaseEnd);
		float highestSeekTime = -1;
		int highestNextSeekTimeIndex = Random.Range(lowercaseStart, lowercaseEnd);
		float highestNextSeekTime = -1;
		int lowestWPMIndex = Random.Range(lowercaseStart, lowercaseEnd);
		float lowestWPM = 10000;
		int lowestAccIndex = Random.Range(lowercaseStart, lowercaseEnd);
		float lowestAcc = 2;
		int bestImprovementTrendIndex = Random.Range(lowercaseStart, lowercaseEnd);
		float bestImprovementTrend = -10000;
		float averageSeekTime = WPMToSeekTime(averageWPM);
		for(int i = 0; i < instance.confidenceDatabase.Length; i++) {
			if (!CharWithinFilters(i))	continue;
			if (instance.confidenceDatabase[i].hits < 4) {
				// Chance to select a key with insufficient data
				if(Random.Range(0f, 1f) < .125f && GetNumberOfQuotesForKey(i) > 0) {
					lowestAcc = lowestWPM = 0;
					lowestAccIndex = lowestWPMIndex = i;
				}
				continue;
			}
			float accuracy = instance.confidenceDatabase[i].accuracy;
			float contextualSeekTime = Mathf.Max(
				instance.confidenceDatabase[i].nextKeySeekTime,
				instance.confidenceDatabase[i].previousKeySeekTime
			);
			
			if (instance.confidenceDatabase[i].speedTrend > bestImprovementTrend) {
				if (Random.Range(0f, 1f) > Mathf.Sqrt(difficulty) ||
					instance.confidenceDatabase[i].wordSpeed < lowestWPM ||
					instance.confidenceDatabase[i].seekTime > highestSeekTime ||
					contextualSeekTime > highestNextSeekTime
				) {
					bestImprovementTrend = instance.confidenceDatabase[i].speedTrend;
					bestImprovementTrendIndex = i;
				}
			}
			if (instance.confidenceDatabase[i].wordSpeed < lowestWPM){
				lowestWPM = instance.confidenceDatabase[i].wordSpeed;
				lowestWPMIndex = i;
			}
			if (instance.confidenceDatabase[i].seekTime >= highestSeekTime &&
				instance.confidenceDatabase[i].seekTime < 10000) {
				if(Random.Range(0f, 1f) >= Mathf.Clamp(accuracy, 1f - difficulty * .8f, 1f - difficulty) ||
					Random.Range(0f, 1f) < difficulty * 1.4f && highestSeekTime >= averageSeekTime
				) {
						highestSeekTime = instance.confidenceDatabase[i].seekTime;
						highestSeekTimeIndex = i;
				}
			}
			if (contextualSeekTime >= highestNextSeekTime && contextualSeekTime < 10000) {
				if(Random.Range(0f, 1f) >= Mathf.Clamp(accuracy, 1f - difficulty * .8f, 1f - difficulty) ||
					Random.Range(0f, 1f) < difficulty * 1.4f && highestNextSeekTime >= averageSeekTime
				) {
						highestNextSeekTime =
							instance.confidenceDatabase[i].previousKeySeekTime < 10000?
								contextualSeekTime:
								instance.confidenceDatabase[i].nextKeySeekTime;
						highestNextSeekTimeIndex = i;
				}
			}
			if (instance.confidenceDatabase[i].wordSpeed < lowestWPM) {
				if (Random.Range(0f, 1f) < difficulty + .2f && instance.confidenceDatabase[i].wordSpeed > 0) {
					lowestWPM = instance.confidenceDatabase[i].wordSpeed;
				}
			}
			if (accuracy <= lowestAcc) {
				if (Random.Range(0f, 1f) > Mathf.Pow(1f - difficulty, 3) ||
					Random.Range(0f, 1f) < difficulty * 1.4f && lowestAcc >= averageAccuracy / 100
				) {
					lowestAcc = accuracy;
					lowestAccIndex = i;
				}
			}
		}
		return Random.Range(0f, 1f) switch {
			<.2f	=> lowestWPMIndex,
			<.55f	=> bestImprovementTrendIndex,
			<.9f	=> instance.confidenceDatabase[highestNextSeekTimeIndex].speedTrend >
			           instance.confidenceDatabase[highestSeekTimeIndex].speedTrend &&
			           Random.Range(0f, 1f) > difficulty * .45f + .1f?
			                highestNextSeekTimeIndex:
			                highestSeekTimeIndex,
			_		=> lowestAccIndex
		};
	}
	
	public static int GetNumberOfQuotesForKey(int keyIndex) {
		// Key quote data files end with a newline
		return Resources.Load<TextAsset>($"CharFreqData/{keyIndex}").text.Split('\n').Length - 1;
	}
	public static int GetNumberOfQuotesForKey(char key) {
		return GetNumberOfQuotesForKey(GetKeyIndex(key));
	}
	
	public static string[] GetQuoteTitlesForKeyIndex(ref int keyIndex) {
		string[] quotes = RemoveTrailingNewline(
			Resources.Load<TextAsset>($"CharFreqData/{keyIndex}").text
		).Split('\n');
		int invalidCount = 0;
		while (quotes[0].Length == 0) {
			const int maxRetries = 10;
			// Debug.Log("Found no quotes containing '"+instance.confidenceDatabase[keyIndex].keyName+"', skipping...");
			if (invalidCount < maxRetries) {
				keyIndex = GetLowConfidenceCharacter();
			} else {
				keyIndex++;
				keyIndex = Mathf.Max(5, keyIndex % trackedKeys.Length);
			}
			invalidCount++;
			quotes = RemoveTrailingNewline(
				Resources.Load<TextAsset>($"CharFreqData/{keyIndex}").text
			).Split('\n');
		}
		return quotes;
	}
	public static string GetQuoteByCharFrequency(int keyIndex, ref string quoteTitle) {
		return GetQuoteByCharFrequency(ref keyIndex, ref quoteTitle, quoteDifficulty);
	}
	public static string GetQuoteByCharFrequency(ref int keyIndex, ref string quoteTitle) {
		return GetQuoteByCharFrequency(ref keyIndex, ref quoteTitle, quoteDifficulty);
	}
	public static string GetQuoteByCharFrequency(int keyIndex, ref string quoteTitle, float difficulty) {
		return GetQuoteByCharFrequency(ref keyIndex, ref quoteTitle, difficulty);
	}
	public static string GetQuoteByCharFrequency(ref int keyIndex, ref string quoteTitle, float difficulty) {
		string[] quotes = GetQuoteTitlesForKeyIndex(ref keyIndex);
		
		#if SCALLE_CHAR_DIFFICULTY_WITH_NUM_QUOTES
			// The value is used to attenuate difficulty based on the number of quotes.
			// When the number of quotes is below this threshold, difficulty is considered 0.
			// The threshold scales with the difficulty value; when difficulty is 1, the threshold is 0.
			const float quoteNumberThreshold = 2.5f;
			float difficultyThreshold = quoteNumberThreshold / difficulty - quoteNumberThreshold;
			
			// Difficulty scaling based on the number of quotes to reduce repetition.
			// The higher the number of quotes, the closer it gets to the original difficulty.
			difficulty -= difficulty / (Mathf.Pow(
				Mathf.Max(0f, quotes.Length - difficultyThreshold) / difficultyThreshold,
				2
			) + 1);
		#endif
		
		float random = difficulty >= 1 ? 0:
			(1f - Mathf.Pow(Random.Range(0f, 1f), 1f - difficulty)) *
				(1f - Mathf.Pow(Mathf.Max(0, difficulty - .52f), 2));
		if (random > Mathf.Pow(Mathf.Max(0, difficulty - .3f) / (1f - .3f), 2) * .75f) {
			float random2 = difficulty >= 1 ? 0 : 1f - Mathf.Pow(Random.Range(0f, 1f), 1f - difficulty);
			random = random2 < random || Random.Range(0f, 1f) < .1f ? random2 : random;
		}
		string targetQuote = quotes[Mathf.Min(Mathf.FloorToInt(random * quotes.Length), quotes.Length - 1)];
		string newQuote = targetQuote.Split('/')[^1];
		if (newQuote == quoteTitle) {
			targetQuote = quotes[Random.Range(0, quotes.Length)];
		}
		quoteTitle = targetQuote.Split('/')[^1];
		targetQuote = RemoveTrailingNewline(Resources.Load<TextAsset>(targetQuote).text);	
		return targetQuote;
	}
	public static string GetQuoteByOverallScore(ref string quoteTitle, ref KeyConfidenceData quoteConfidenceData) {
		int numCandidates = (int)(182f * quoteDifficulty + 1);
		string[] quoteCandidates = new string[numCandidates];
		string[] quoteCandidateTitles = new string[numCandidates];
		KeyConfidenceData[] averageConfidence = new KeyConfidenceData[numCandidates];
		float charBias = charPracticeDifficulty * charPracticeDifficulty * .0051f;
		const float quoteBias = 0;
		int spacebarIndex = GetKeyIndex(' ');
		for(int i = 0; i < numCandidates; i++) {
			quoteCandidates[i] = GetQuoteByCharFrequency(
				Random.Range(0f, 1f) > charBias ?
					spacebarIndex:
					GetLowConfidenceCharacter(charBias),
				ref quoteCandidateTitles[i],
				quoteBias
			);
			averageConfidence[i] = GetQuoteConfidenceData(quoteCandidates[i]);
		}
		
		float lowestAcc = 2,
		      highestSeekTime = -1,
		      highestNextKeySeekTime = -1,
		      bestTrendingSeekTime = -1,
		      lowestFullWordSpeed = 99999;
		int   lowestAccIndex = Random.Range(0, numCandidates - 1),
		      highestSeekTimeIndex = Random.Range(0, numCandidates - 1),
		      highestNextKeySeekTimeIndex = Random.Range(0, numCandidates - 1),
		      bestTrendingSeekTimeIndex = Random.Range(0, numCandidates),
		      lowestFullWordSpeedIndex = Random.Range(0, numCandidates - 1);
		float newQuoteDifficulty = (quoteDifficulty - .2f) * (quoteDifficulty + .123f);
		for(int i = 0; i < numCandidates; i++) {
			if (quoteCandidateTitles[i] == quoteTitle)	continue;
			if (averageConfidence[i].accuracy < lowestAcc && Random.Range(0f, 1f) <= newQuoteDifficulty) {
				lowestAcc = averageConfidence[i].accuracy;
				lowestAccIndex = i;
			}
			if (averageConfidence[i].seekTime > highestSeekTime && Random.Range(0f, 1f) <= newQuoteDifficulty) {
				highestSeekTime = averageConfidence[i].seekTime;
				highestSeekTimeIndex = i;
			}
			if (averageConfidence[i].nextKeySeekTime > highestNextKeySeekTime &&
				Random.Range(0f, 1f) <= newQuoteDifficulty
			) {
				highestNextKeySeekTime = averageConfidence[i].nextKeySeekTime;
				highestNextKeySeekTimeIndex = i;
			}
			if (averageConfidence[i].speedTrend > bestTrendingSeekTime &&
				Random.Range(0f, 1f) <= newQuoteDifficulty
			) {
				bestTrendingSeekTime = averageConfidence[i].speedTrend;
				bestTrendingSeekTimeIndex = i;
			}
			if (averageConfidence[i].wordSpeed < lowestFullWordSpeed && Random.Range(0f, 1f) <= newQuoteDifficulty) {
				lowestFullWordSpeed = averageConfidence[i].wordSpeed;
				lowestFullWordSpeedIndex = i;
			}
		}
		//TODO: Idea: Function for selecting easy quotes (maybe an option in the settings to insert
		// intermittent easy quotes to help with motivation when fatigue/regression is detected?)

		int finalIndex = Random.Range(0f, 1f) switch {
			<.15f => highestSeekTimeIndex,
			<.3f => highestNextKeySeekTimeIndex,
			<.6f => bestTrendingSeekTimeIndex,
			<.875f => lowestFullWordSpeedIndex,
			_ => lowestAccIndex
		};
		
		quoteTitle = quoteCandidateTitles[finalIndex];
		quoteConfidenceData = averageConfidence[finalIndex];
		
		return quoteCandidates[finalIndex];
	}
	
	public static KeyConfidenceData GetQuoteConfidenceData(string quote) {
		List<char> sortedQuote = quote.ToList();
		sortedQuote.Sort();
		float quoteAccuracyScore = 0;
		KeyConfidenceData averageConfidence = new();
		char lastC = averageConfidence.keyName = '\0';
		int keyIndex = 0;
		int numChars = 0;
		bool skipChar = false;
		foreach(char c in sortedQuote) {
			if (c != lastC) {
				keyIndex = GetKeyIndex(c, keyIndex);
				lastC = c;
				if (CharWithinFilters(keyIndex)) {
					skipChar = false;
				} else {
					skipChar = true;
					continue;
				}
			}
			if (skipChar ||
				instance.confidenceDatabase[keyIndex].hits +
				instance.confidenceDatabase[keyIndex].misses == 0
			) {
				continue;
			}
			
			quoteAccuracyScore += instance.confidenceDatabase[keyIndex].accuracy;
			if (instance.confidenceDatabase[keyIndex].seekTime < 999999)
				averageConfidence.seekTime += instance.confidenceDatabase[keyIndex].seekTime;
			if (instance.confidenceDatabase[keyIndex].previousKeySeekTime < 999999)
				averageConfidence.previousKeySeekTime += instance.confidenceDatabase[keyIndex].previousKeySeekTime;
			if (instance.confidenceDatabase[keyIndex].nextKeySeekTime < 999999) {
				averageConfidence.nextKeySeekTime +=
					instance.confidenceDatabase[keyIndex].previousKeySeekTime < 999999?
						Mathf.Max(
							instance.confidenceDatabase[keyIndex].previousKeySeekTime,
							instance.confidenceDatabase[keyIndex].nextKeySeekTime
						):
						instance.confidenceDatabase[keyIndex].nextKeySeekTime;
			}
			averageConfidence.speedTrend += instance.confidenceDatabase[keyIndex].speedTrend;
			averageConfidence.wordSpeed += instance.confidenceDatabase[keyIndex].wordSpeed;
			numChars++;
		}
		
		averageConfidence.speedTrend /= numChars;
		if (numChars > 0) {
			quoteAccuracyScore /= numChars;
			averageConfidence.seekTime /= numChars;
			averageConfidence.previousKeySeekTime /= numChars;
			averageConfidence.nextKeySeekTime /= numChars;
			averageConfidence.wordSpeed /= numChars;
			const int maxHits = 9999999;
			averageConfidence.hits = (int)(maxHits * quoteAccuracyScore);
			averageConfidence.misses = maxHits-averageConfidence.hits;
		} else {
			averageConfidence.seekTime = Mathf.Infinity;
			averageConfidence.previousKeySeekTime = Mathf.Infinity;
			averageConfidence.nextKeySeekTime = Mathf.Infinity;
		}
		
		return averageConfidence;
	}
	
	public static float GetEstimatedTypingTimeSeconds(string quote) {
		List<char> sortedQuote = quote.ToList();
		sortedQuote.Sort();
		float timeEstimate = 0;
		int keyIndex = 0;
		char lastC = '\t';
		foreach(char c in sortedQuote) {
			if (lastC != c) {
				keyIndex = GetKeyIndex(c, keyIndex);
				lastC = c;
			}
			timeEstimate +=
				instance.confidenceDatabase[keyIndex].seekTime < 1000?
					instance.confidenceDatabase[keyIndex].seekTime:
					WPMToSeekTime(DefaultWPM);
		}
		return timeEstimate;
	}

	public static string RemoveTrailingNewline(string text) {
		if (text.Length <= 0) return text;
		while (text[^1] is '\n' or ' ') {
			text = text.Remove(text.Length - 1, 1);
		}
		return text;
	}
	
	private void OnApplaicationQuit() {
	 	 Save();
	}
}
