using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class KeyManager:MonoBehaviour{
	public static KeyManager instance;
	[Serializable]public struct KeyConfidenceData{
		#if UNITY_EDITOR
			[NonSerialized]public string name;
		#endif
		
		public char		keyName;
		public float	seekTime,
		               nextKeySeekTime,
		               wpm;
		public int		hits, misses;	// Hits and misses are tracked to calculate a confidence ratio
		public float	accuracy;

	}
	public KeyConfidenceData[] confidenceDatabase; 
	public static readonly char[] trackedKeys={
		'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
		'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
		'0','1','2','3','4','5','6','7','8','9',
		' ','\n','\t','\\','@','#','$',
		'.',',','\'','`','"','?','!',';',':','_','&',
		'=','~','-','+','%','/','*','^','<','>','|',
		'(',')','[',']','{','}',
	};
	static int numbersStart,numbersEnd,
	           capitalStart,capitalEnd,
	           lowercaseStart,lowercaseEnd;
	public static bool includeUppercase=false,
	                   includeNumbers=false,
	                   includeSymbols=false,
	                   includeWhitespace=false;
	[Serializable]public struct DailyData{	//TODO: Save the averages for each day
		public int year,
		           month,
		           day;
		public float topSpeed,
		             averageSpeed,
		             averageAcc;
	}
	List<DailyData> dailyData=new List<DailyData>();
	public static float averageAccuracy,averageWPM,topWPM;
	public static float charPracticeDifficulty=.7f;	// 1: Completely random, 0: Only the worst character in either speed or accuracy
	public static float quoteDifficulty=.45f;			// 1: only the quote with most frequent occurrence of the selected character, 0: unbiased
	public static float modeBias=.4f;					// 1: multiple keys practice, 0: single key practice
	
	void Start(){
		instance=this;
		InitializeKeyDatabase();
		Load();
		
		// GetKeyIndexes("abcAbd".ToLower());
	}
	public static void InitializeKeyDatabase(){
		instance.confidenceDatabase=new KeyConfidenceData[trackedKeys.Length];
		for(int i=0;i<trackedKeys.Length;i++){
			instance.confidenceDatabase[i].seekTime=instance.confidenceDatabase[i].nextKeySeekTime=Mathf.Infinity;
			instance.confidenceDatabase[i].keyName=trackedKeys[i];
			#if UNITY_EDITOR
				instance.confidenceDatabase[i].name="Element "+i+": "+trackedKeys[i];
			#endif
		}
		
		List<char> sortedKeys=trackedKeys.ToList<char>();
		sortedKeys.Sort();
		for(int i=0;i<sortedKeys.Count;i++){
			instance.confidenceDatabase[i].keyName=sortedKeys[i];
			#if UNITY_EDITOR
				instance.confidenceDatabase[i].name="Element "+i+": "+sortedKeys[i];
			#endif
			
			switch(sortedKeys[i]){
				case 'a':{
					lowercaseStart=i;
					break;
				}
				case 'z':{
					lowercaseEnd=i;
					break;
				}
				case 'A':{
					capitalStart=i;
					break;
				}
				case 'Z':{
					capitalEnd=i;
					break;
				}
				case '0':{
					numbersStart=i;
					break;
				}
				case '9':{
					numbersEnd=i;
					break;
				}
			}
		}
	}
	public static void Load(){
		averageAccuracy=PlayerPrefs.GetFloat("Average Acc",0);
		averageWPM=PlayerPrefs.GetFloat("Average WPM",0);
		topWPM=PlayerPrefs.GetFloat("Top WPM",0);
		if(!System.IO.File.Exists(Application.persistentDataPath+"/key-confidence-data")){
			return;
		}
		string[] data=System.IO.File.ReadAllLines(Application.persistentDataPath+"/key-confidence-data");
		for(int i=0;i<data.Length;i++){
			instance.confidenceDatabase[i]=JsonUtility.FromJson<KeyConfidenceData>(data[i]);
		}
	}
	public static void Save(){
		Typing.instance.Save();
		
		PlayerPrefs.SetFloat("Average Acc",averageAccuracy);
		PlayerPrefs.SetFloat("Average WPM",averageWPM);
		PlayerPrefs.SetFloat("Top WPM",topWPM);
		
		string fileContents="";
		foreach(KeyConfidenceData kcd in instance.confidenceDatabase){
			fileContents+=JsonUtility.ToJson(kcd)+"\n";
		}
		System.IO.File.WriteAllText(Application.persistentDataPath+"/key-confidence-data",fileContents);
	}
	
	public static int GetKeyIndex(char key,int startIndex=0){
		// key=key.ToString().ToLower()[0];
		for(int i=startIndex;i<instance.confidenceDatabase.Length;i++){
			if(instance.confidenceDatabase[i].keyName==key)
				return i;
		}
		Debug.LogError("Invalid (untracked) character: "+key);
		return -1;
	}
	
	public static int[] GetKeyIndexes(string keys){
		/*
		 * sort alphabetically
		 * remove duplicates
		 * loop through the 'instance.confidenceDatabase' and look for the next sorted match
		 */
		List<char> sorted=new List<char>();
		for(int i=0;i<keys.Length;i++){
			sorted.Add(keys[i]);
		}
		sorted.Sort();
		for(int i=1;i<sorted.Count;i++){
			if(sorted[i]==sorted[i-1])
				sorted.RemoveAt(i);
		}
		
		int[] o=new int[sorted.Count];
		int lookupIndex=0;
		for(int i=0;i<instance.confidenceDatabase.Length&&lookupIndex<o.Length;i++){
			if (instance.confidenceDatabase[i].keyName != sorted[lookupIndex]) continue;
			o[lookupIndex]=i;
			lookupIndex++;
		}
		
		return o;
	}

	public static bool IsAlphaNumericIndex(int index){
		return 
			index>=lowercaseStart&&index<=lowercaseEnd||
			index>=capitalStart&&index<=capitalEnd||
			index>=numbersStart&&index<=numbersEnd;
	}
	public static bool IsWhitespaceIndex(int index){
		return instance.confidenceDatabase[index].keyName switch{
			' ' => true,
			'\n' => true,
			'\t' => true,
			_ => false
		};
	}
	public static bool IsWhitespaceCharacter(char character){
		return character switch{
			' ' => true,
			'\n' => true,
			'\t' => true,
			_ => false
		};
	}

	//TODO: Function for lowering the hit/miss count without changing the ratio (e.g. divide by the same value, ratio might still change a bit because the numbers are integers)
	public static void RemoveHitsAndMisses(int keyIndex,int amount=1){
		if(instance.confidenceDatabase[keyIndex].hits<10)
			return;
		instance.confidenceDatabase[keyIndex].hits=Mathf.Max(0,instance.confidenceDatabase[keyIndex].hits-amount);
		instance.confidenceDatabase[keyIndex].misses=Mathf.Max(0,instance.confidenceDatabase[keyIndex].misses-amount);
	}
	public static void RegisterKeyHit(int keyIndex){
		instance.confidenceDatabase[keyIndex].hits++;
	}
	public static void RegisterKeyMiss(int keyIndex){
		instance.confidenceDatabase[keyIndex].misses++;
	}
	
	public static void UpdateSeekTime(int index,float seekTime){
		if(instance.confidenceDatabase[index].seekTime<10000){
			instance.confidenceDatabase[index].seekTime=Mathf.Lerp(instance.confidenceDatabase[index].seekTime,seekTime,.12f);
		}else{
			instance.confidenceDatabase[index].seekTime=seekTime;
		}
	}
	public static void UpdateNextKeySeekTime(int index,float seekTime){
		if(instance.confidenceDatabase[index].nextKeySeekTime<10000){
			instance.confidenceDatabase[index].nextKeySeekTime=Mathf.Lerp(instance.confidenceDatabase[index].seekTime,seekTime,.12f);
		}else{
			instance.confidenceDatabase[index].nextKeySeekTime=seekTime;
		}
	}
	
	public static float UpdateWordSpeed(string word,float time){
		float wpm=word.Length/time*60/5;
		foreach(int i in GetKeyIndexes(word)){
			switch (instance.confidenceDatabase[i].wpm){
				case 0:
					instance.confidenceDatabase[i].wpm=wpm;
					continue;
				case float.NaN:
					instance.confidenceDatabase[i].wpm=wpm;
					break;
			}

			instance.confidenceDatabase[i].wpm=Mathf.Lerp(instance.confidenceDatabase[i].wpm,wpm,.45f/(Mathf.Max(5-word.Length*5,word.Length)/3+1));
		}
		// Debug.Log($"The word \"{word}\" was typed at {wpm} WPM (took {time} seconds)");
		//TODO: Track top-speed(?)
		return wpm;
	}

	public static void UpdateAccuracy(char actual, char pressed){
		
	}
	
	public static int GetWordCount(string text){
		int wordCount=0;
		foreach(char c in text){
			if(!IsAlphaNumericIndex(GetKeyIndex(c))) wordCount++;
		}
		return wordCount;
	}
	
	public static string[] GetWordsInText(string text){
		List<string> words=new List<string>();
		string word="";
		foreach(char c in text){
			word+=c;
			// if(IsAlphaNumericIndex(GetKeyIndex(c))) continue;
			if(!IsWhitespaceCharacter(c)) continue;
			words.Add(word);
			word="";
		}
		if(word!=""){
			words.Add(word);
		}
		return words.ToArray();
	}
	
	public static string GetLastWord(string text){
		return GetLastWord(text,text.Length-1);
	}
	
	public static string GetLastWord(string text,int trimIndex){
		for(int i=trimIndex;i>-1;i--){
			// if(i==trimIndex&&!IsAlphaNumericIndex(GetKeyIndex(text[i]))) i--;
			// if(IsAlphaNumericIndex(GetKeyIndex(text[i]))) continue;
			if(i==trimIndex&&IsWhitespaceCharacter(text[i])) i--;
			if(!IsWhitespaceCharacter(text[i])) continue;
			i++;
			string ret="";
			while(i<=trimIndex){
				ret+=text[i];
				i++;
			}
			return ret;
		}
		return text;
	}
	
	public static bool CharWithinFilters(int index){
		if(!includeUppercase&&index>=capitalStart&&index<=capitalEnd) return false;
		if(!includeNumbers&&index>=numbersStart&&index<=numbersEnd) return false;
		if(!includeSymbols&&!IsAlphaNumericIndex(index)) return false;
		if(includeWhitespace) return true;
		return !IsWhitespaceIndex(index);
	}
	
	public static int GetLowConfidenceCharacter(){
		return GetLowConfidenceCharacter(charPracticeDifficulty);
	}
	public static int GetLowConfidenceCharacter(float difficulty){
		int highestSeekTimeIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float highestSeekTime=-1;
		int highestNextSeekTimeIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float highestNextSeekTime=-1;
		int lowestAccIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float lowestAcc=2;
		int lowestWPMIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float lowestWPM=10000;
		for(int i=0;i<instance.confidenceDatabase.Length;i++){
			if(!CharWithinFilters(i))	continue;
			if(instance.confidenceDatabase[i].hits==0){
				if(Random.Range(0f,1f)<.02f){	// Chance to select a key because there is no data for it
					lowestAcc=0;
					lowestAccIndex=i;
				}
				continue;
			}
			float accuracy=(float)instance.confidenceDatabase[i].hits/(instance.confidenceDatabase[i].hits+instance.confidenceDatabase[i].misses);
			if(instance.confidenceDatabase[i].seekTime>=highestSeekTime&&instance.confidenceDatabase[i].seekTime<1000){
				if(Random.Range(0f,1f)>=Mathf.Clamp(accuracy,1F-difficulty*.8f,1F-difficulty)){
					highestSeekTime=instance.confidenceDatabase[i].seekTime;
					highestSeekTimeIndex=i;
				}
			}
			if(instance.confidenceDatabase[i].nextKeySeekTime>=highestNextSeekTime&&instance.confidenceDatabase[i].nextKeySeekTime<1000){
				if(Random.Range(0f,1f)>=Mathf.Clamp(accuracy,1F-difficulty*.8f,1F-difficulty)){
					highestNextSeekTime=instance.confidenceDatabase[i].seekTime;
					highestNextSeekTimeIndex=i;
				}
			}
			if(instance.confidenceDatabase[i].wpm<lowestWPM){
				if(Random.Range(0f,1f)<difficulty&&instance.confidenceDatabase[i].wpm>0){
					lowestWPM=instance.confidenceDatabase[i].wpm;
				}
			}
			if(accuracy<=lowestAcc){
				if(Random.Range(0f,1f)<difficulty){
					lowestAcc=accuracy;
					lowestAccIndex=i;
				}
			}
		}
		if(Random.Range(0,1)>.5f&&lowestWPM>0){
			if(highestSeekTime>highestNextSeekTime)
				highestNextSeekTimeIndex=lowestWPMIndex;
			else
				highestSeekTimeIndex=lowestWPMIndex;
		}
		return Random.Range(0f,1f)>.5f?(Random.Range(0f,1f)>.45f?highestSeekTimeIndex:highestNextSeekTimeIndex):lowestAccIndex;
	}
	
	public static string[] GetQuoteTitlesForKeyIndex(ref int keyIndex){
		string[] quotes=RemoveTrailingNewline(Resources.Load<TextAsset>("CharFreqData/"+keyIndex).text).Split('\n');
		int invalidCount=0;
		while(quotes[0].Length==0){
			const int maxRetries=10;
			// Debug.Log("Found no quotes containing '"+instance.confidenceDatabase[keyIndex].keyName+"', skipping...");
			if(invalidCount<maxRetries){
				keyIndex=GetLowConfidenceCharacter();
			}else{
				keyIndex++;
				keyIndex=Mathf.Max(5,keyIndex%trackedKeys.Length);
			}
			invalidCount++;
			quotes=RemoveTrailingNewline(Resources.Load<TextAsset>("CharFreqData/"+keyIndex).text).Split('\n');
		}
		return quotes;
	}
	public static string GetQuoteByCharFrequency(int keyIndex,ref string quoteTitle){
		return GetQuoteByCharFrequency(ref keyIndex,ref quoteTitle,quoteDifficulty);
	}
	public static string GetQuoteByCharFrequency(ref int keyIndex,ref string quoteTitle){
		return GetQuoteByCharFrequency(ref keyIndex,ref quoteTitle,quoteDifficulty);
	}
	public static string GetQuoteByCharFrequency(int keyIndex,ref string quoteTitle,float difficulty){
		return GetQuoteByCharFrequency(ref keyIndex,ref quoteTitle,difficulty);
	}
	public static string GetQuoteByCharFrequency(ref int keyIndex,ref string quoteTitle,float difficulty){
		string[] quotes=GetQuoteTitlesForKeyIndex(ref keyIndex);
		float random=difficulty>=1?0:1f-Mathf.Pow(Random.Range(0f,1f),1f-difficulty);
		if(random>Mathf.Pow(Mathf.Max(0,difficulty-.3f)/(1f-.3f),2)*.75f){
			float random2=difficulty>=1?0:1f-Mathf.Pow(Random.Range(0f,1f),1f-difficulty);
			random=random2<random||Random.Range(0f,1f)<.1f?random2:random;
		}
		// float random=Random.Range(0f,1f)>Mathf.Pow(1f-difficulty,5)?1f-Mathf.Sqrt(Random.Range(0f,1f)):Random.Range(0f,1f);
		string targetQuote=quotes[Mathf.Min(Mathf.FloorToInt(random*quotes.Length),quotes.Length-1)];
		string newQuote=targetQuote.Split('/')[^1];
		if(newQuote==quoteTitle){
			targetQuote=quotes[Random.Range(0,quotes.Length)];
		}
		quoteTitle=targetQuote.Split('/')[^1];
		targetQuote=RemoveTrailingNewline(Resources.Load<TextAsset>(targetQuote).text);	//TODO: Allow skipping categories (eg. ignore 'Content/Code/...') (increase index until a different category is found, loop over if not, select different character if same index is reached) (check if an appropriate file exists in the 'while' loop above)
		return targetQuote;
	}
	public static string GetQuoteByOverallScore(ref string quoteTitle,ref KeyConfidenceData quoteConfidenceData){
		int numCandidates=(int)(127*quoteDifficulty+1);
		string[] quoteCandidates=new string[numCandidates];
		string[] quoteCandidateTitles=new string[numCandidates];
		KeyConfidenceData[] averageConfidence=new KeyConfidenceData[numCandidates];
		float charBias=(charPracticeDifficulty-.1f)*.035f-.05f,
		      quoteBias=0;//quoteDifficulty*.5f*quoteDifficulty*quoteDifficulty;
		for(int i=0;i<numCandidates;i++){
			quoteCandidates[i]=GetQuoteByCharFrequency(GetLowConfidenceCharacter(charBias),ref quoteCandidateTitles[i],quoteBias);
			averageConfidence[i]=GetQuoteConfidenceData(quoteCandidates[i]);
		}
		
		float lowestAcc=2,
		      highestSeekTime=-1,
		      highestNextKeySeekTime=-1,
		      lowestFullWordSpeed=99999;
		int   lowestAccIndex=Random.Range(0,numCandidates-1),
		      highestSeekTimeIndex=Random.Range(0,numCandidates-1),
		      highestNextKeySeekTimeIndex=Random.Range(0,numCandidates-1),
		      lowestFullWordSpeedIndex=Random.Range(0,numCandidates-1);
		float newQuoteDifficulty=(quoteDifficulty-.2f)*(quoteDifficulty+.123f);
		for(int i=0;i<numCandidates;i++){
			if(quoteCandidateTitles[i]==quoteTitle)	continue;
			if(averageConfidence[i].accuracy<lowestAcc&&Random.Range(0f,1f)<=newQuoteDifficulty){
				lowestAcc=averageConfidence[i].accuracy;
				lowestAccIndex=i;
			}
			if(averageConfidence[i].seekTime>highestSeekTime&&Random.Range(0f,1f)<=newQuoteDifficulty){
				highestSeekTime=averageConfidence[i].seekTime;
				highestSeekTimeIndex=i;
			}
			if(averageConfidence[i].nextKeySeekTime>highestNextKeySeekTime&&Random.Range(0f,1f)<=newQuoteDifficulty){
				highestNextKeySeekTime=averageConfidence[i].nextKeySeekTime;
				highestNextKeySeekTimeIndex=i;
			}
			if(averageConfidence[i].wpm<lowestFullWordSpeed&&Random.Range(0f,1f)<=newQuoteDifficulty){
				lowestFullWordSpeed=averageConfidence[i].wpm;
				lowestFullWordSpeedIndex=i;
			}
		}

		int finalIndex=lowestAccIndex;
		switch(Random.Range(0f,1f)){
			case <.3f:
				finalIndex=highestSeekTimeIndex;
			break;
			case <.55f:
				finalIndex=highestNextKeySeekTimeIndex;
			break;
			case <.72f:
				finalIndex=lowestFullWordSpeedIndex;
			break;
		}
		// if(Random.Range(0f,1f)>.125f)	finalIndex=lowestFullWordSpeedIndex;
		// if(Random.Range(0f,1f)>.25f)	finalIndex=highestNextKeySeekTimeIndex;
		// if(Random.Range(0f,1f)>.333f)	finalIndex=highestSeekTimeIndex;
		
		quoteTitle=quoteCandidateTitles[finalIndex];
		quoteConfidenceData=averageConfidence[finalIndex];
		
		return quoteCandidates[finalIndex];
	}
	
	public static KeyConfidenceData GetQuoteConfidenceData(string quote){
		List<char> sortedQuote=quote.ToList();
		sortedQuote.Sort();
		float quoteAccuracyScore=0;
		KeyConfidenceData averageConfidence=new();
		char lastC=averageConfidence.keyName='\0';
		int keyIndex=0;
		int numChars=0;
		bool skipChar=false;
		foreach(char c in sortedQuote){
			if(c!=lastC){
				keyIndex=GetKeyIndex(c,keyIndex);
				lastC=c;
				if(CharWithinFilters(keyIndex)){
					skipChar=false;
				}else{
					skipChar=true;
					continue;
				}
			}
			if(skipChar||instance.confidenceDatabase[keyIndex].hits+instance.confidenceDatabase[keyIndex].misses==0)	continue;
			
			quoteAccuracyScore+=(float)instance.confidenceDatabase[keyIndex].hits/(instance.confidenceDatabase[keyIndex].hits+instance.confidenceDatabase[keyIndex].misses);
			if(instance.confidenceDatabase[keyIndex].seekTime<999999)
				averageConfidence.seekTime+=instance.confidenceDatabase[keyIndex].seekTime;
			if(instance.confidenceDatabase[keyIndex].nextKeySeekTime<999999)
				averageConfidence.nextKeySeekTime+=instance.confidenceDatabase[keyIndex].nextKeySeekTime;
			averageConfidence.wpm+=instance.confidenceDatabase[keyIndex].wpm;
			numChars++;
		}
		
		if(numChars>0){
			quoteAccuracyScore/=numChars;
			averageConfidence.seekTime/=numChars;
			averageConfidence.nextKeySeekTime/=numChars;
			averageConfidence.wpm/=numChars;
			const int maxHits=9999999;
			averageConfidence.hits=(int)(maxHits*quoteAccuracyScore);
			averageConfidence.misses=maxHits-averageConfidence.hits;
		}else{
			averageConfidence.seekTime=Mathf.Infinity;
			averageConfidence.nextKeySeekTime=Mathf.Infinity;
		}
		
		return averageConfidence;
	}
	
	/*
	 * TODO: If code snippets are to be implemented, the following could work for online selection (or they could be added offline):
	 * 
	 *		1: Download an X number of random code snippets (like SpeedTyper.dev, but multiple at once)
	 *		2: Sort alphabetically (in a new variable), check each character and remember the overall average seek time/accuracy/etc and index for each snippet
	 *		3: Pick the hardest of the bunch (using any of the following: )
	 */

	public static string RemoveTrailingNewline(string text){
		if(text.Length<=0) return text;
		return text[^1]=='\n'?text.Remove(text.Length-1,1):text;
	}
	
	private void OnApplicationQuit(){
		Save();
	}
}
