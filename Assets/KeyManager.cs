using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class KeyManager:MonoBehaviour{
	public static KeyManager instance;
	[System.Serializable]public struct KeyConfidenceData{
		#if UNITY_EDITOR
			[System.NonSerialized]public string name;
		#endif
		
		public char		keyName;
		public float	seekTime,
		               nextKeySeekTime,
		               wpm;
		public int		hits, misses;	// Hits and misses are tracked to calculate a confidence ratio

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
	[System.Serializable]public struct DailyData{	//TODO: Save the averages for each day
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
	public static float quoteDifficulty=.45f;
	
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
		if(!System.IO.File.Exists(Application.persistentDataPath+"/key-confidence-data")){
			return;
		}
		string[] data=System.IO.File.ReadAllLines(Application.persistentDataPath+"/key-confidence-data");
		for(int i=0;i<data.Length;i++){
			instance.confidenceDatabase[i]=JsonUtility.FromJson<KeyConfidenceData>(data[i]);
		}
		averageAccuracy=PlayerPrefs.GetFloat("Average Acc",0);
		averageWPM=PlayerPrefs.GetFloat("Average WPM",0);
		topWPM=PlayerPrefs.GetFloat("Top WPM",0);
	}
	public static void Save(){
		string fileContents="";
		foreach(KeyConfidenceData kcd in instance.confidenceDatabase){
			fileContents+=JsonUtility.ToJson(kcd)+"\n";
		}
		System.IO.File.WriteAllText(Application.persistentDataPath+"/key-confidence-data",fileContents);
		
		PlayerPrefs.SetFloat("Average Acc",averageAccuracy);
		PlayerPrefs.SetFloat("Average WPM",averageWPM);
		PlayerPrefs.SetFloat("Top WPM",topWPM);
		
		Typing.instance.Save();
	}
	
	public static int GetKeyIndex(char key,int startIndex=0){
		// key=key.ToString().ToLower()[0];
		for(int i=startIndex;i<instance.confidenceDatabase.Length;i++){
			if(instance.confidenceDatabase[i].keyName==key)
				return i;
		}
		//TODO: If an unsupported key is pressed, create a new entry for it in the confidenceDatabase, re-sort, increase numbersStart, numbersEnd, etc. if the index is <= to that number
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
	
	public static void UpdateWordSpeed(string word,float time){
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

			instance.confidenceDatabase[i].wpm=Mathf.Lerp(instance.confidenceDatabase[i].wpm,wpm,.45f/((float)word.Length/3+1));
		}
		// Debug.Log($"The word \"{word}\" was typed at {wpm} WPM (took {time} seconds)");
		//TODO: Track top-speed
	}

	public static void UpoateAccuracy(char actual, char pressed){
		
	}
	
	public static string ValidateWord(string word){
		for(int i=word.Length-1;i>-1;i--){
			if(IsAlphaNumericIndex(GetKeyIndex(word[i]))) continue;
			string ret="";
			while(i<word.Length){
				ret+=word[i];
				i++;
			}
			return ret;
		}
		return word;
	}
	
	public static bool CharWithinFilters(int index){
		if(!includeUppercase&&index>=capitalStart&&index<=capitalEnd) return false;
		if(!includeNumbers&&index>=numbersStart&&index<=numbersEnd) return false;
		if(!includeSymbols&&!IsAlphaNumericIndex(index)) return false;
		if(includeWhitespace) return true;
		switch(instance.confidenceDatabase[index].keyName){
			case ' ':	return false;
			case '\n':	return false;
			case '\t':	return false;
		}
		return true;
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
	
	public static string[] GetQuoteTitlesForKeyIndex(int keyIndex){
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
			Typing.curPracticeIndex=keyIndex;
		}
		return quotes;
	}
	public static string GetQuoteByCharFrequency(int keyIndex,ref string quoteTitle){
		return GetQuoteByCharFrequency(keyIndex,ref quoteTitle,quoteDifficulty);
	}
	public static string GetQuoteByCharFrequency(int keyIndex,ref string quoteTitle,float difficulty){
		string[] quotes=GetQuoteTitlesForKeyIndex(keyIndex);
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
	public static string GetQuoteByOverallScore(ref string quoteTitle){
		//TODO: Add UI for "Current Practice: multiple keys", select this mode at random
		
		// And display the overall average stats where the usual char practice stats are shown
		// This mode could maybe be signified with the 'Typing.curPracticeIndex' being equal to -2 (or a different negative value)
		// Maybe also add another slider to the settings menu to control the 'individual characters' vs 'overall score' practice bias (to affect how frequently this mode is chosen)
		/*
		 * The possible names could also include:
		 *		Current Practice:
		 *			- Frequently Missed (Characters)
		 *			- High Seek Time (Characters)
		 *			- Low Full-Word Speed
		 *			(or alternatively)
		 *			- Accuracy
		 *			- Speed
		 *			(or)
		 *			- multiple keys
		 */
		
		const int numCandidates=100;
		string[] quoteCandidates=new string[numCandidates];
		string[] quoteCandidateTitles=new string[numCandidates];
		float[] averageAcc=new float[numCandidates],
		        averageSeekTime=new float[numCandidates],
		        averageNextKeySeekTime=new float[numCandidates],
		        averageFullWordSpeed=new float[numCandidates];
		float charBias=charPracticeDifficulty*.05f*charPracticeDifficulty,
		      quoteBias=quoteDifficulty*.15f*quoteDifficulty;
		for(int i=0;i<numCandidates;i++){
			quoteCandidates[i]=GetQuoteByCharFrequency(GetLowConfidenceCharacter(charBias),ref quoteCandidateTitles[i],quoteBias);
			
			List<char> sortedQuote=quoteCandidates[i].ToList();
			sortedQuote.Sort();
			char lastC='—';
			int keyIndex=0;
			// int lastValidIndex=0;
			int numChars=0;
			bool skipChar=false;
			foreach(char c in sortedQuote){
				if(c!=lastC){
					keyIndex=GetKeyIndex(c,keyIndex);
					lastC=c;
					if(CharWithinFilters(keyIndex)/*&&keyIndex>=0*/){
						// lastValidIndex=keyIndex;
						skipChar=false;
					}else{
						skipChar=true;
						continue;
					}
				}
				if(skipChar) continue;

				averageAcc[i]+=(float)instance.confidenceDatabase[keyIndex].hits/(instance.confidenceDatabase[keyIndex].hits+instance.confidenceDatabase[keyIndex].misses);
				averageSeekTime[i]+=instance.confidenceDatabase[keyIndex].seekTime;
				averageNextKeySeekTime[i]+=instance.confidenceDatabase[keyIndex].nextKeySeekTime;
				averageFullWordSpeed[i]+=instance.confidenceDatabase[keyIndex].wpm;
				numChars++;
			}
			averageAcc[i]/=numChars;
			averageSeekTime[i]/=numChars;
			averageNextKeySeekTime[i]/=numChars;
			averageFullWordSpeed[i]/=numChars;		//TODO: Turn into a function taking the quote string as input, set these averages as 'ref' parameters
		}
		
		double lowestAcc=2,
		       highestSeekTime=-1,
		       highestNextKeySeekTime=-1,
		       lowestFullWordSpeed=99999;
		int lowestAccIndex=Random.Range(0,numCandidates-1),
		    highestSeekTimeIndex=Random.Range(0,numCandidates-1),
		    highestNextKeySeekTimeIndex=Random.Range(0,numCandidates-1),
		    lowestFullWordSpeedIndex=Random.Range(0,numCandidates-1);
		//TODO: Implement a bias here as well (similarly to 'GetLowConfidenceCharacter()' but with 'quoteDifficulty')
		for(int i=0;i<numCandidates;i++){
			if(averageAcc[i]<lowestAcc&&Random.Range(0f,1f)<=.75f){
				lowestAcc=averageAcc[i];
				lowestAccIndex=i;
			}
			if(averageSeekTime[i]>highestSeekTime&&Random.Range(0f,1f)<=.75f){
				highestSeekTime=averageSeekTime[i];
				highestSeekTimeIndex=i;
			}
			if(averageNextKeySeekTime[i]>highestNextKeySeekTime&&Random.Range(0f,1f)<=.75f){
				highestNextKeySeekTime=averageNextKeySeekTime[i];
				highestNextKeySeekTimeIndex=i;
			}
			if(averageFullWordSpeed[i]<lowestFullWordSpeed&&Random.Range(0f,1f)<=.75f){
				lowestFullWordSpeed=averageFullWordSpeed[i];
				lowestFullWordSpeedIndex=i;
			}
		}

		int finalIndex=lowestAccIndex;
		if(Random.Range(0f,1f)>.25f)	finalIndex=highestSeekTimeIndex;
		if(Random.Range(0f,1f)>.25f)	finalIndex=highestNextKeySeekTimeIndex;
		if(Random.Range(0f,1f)>.25f)	finalIndex=lowestFullWordSpeedIndex;
		
		Debug.Log(averageAcc[finalIndex]);
		Debug.Log(averageSeekTime[finalIndex]);
		Debug.Log(averageNextKeySeekTime[finalIndex]);
		Debug.Log(averageFullWordSpeed[finalIndex]);
		
		quoteTitle=quoteCandidateTitles[finalIndex];
		
		return quoteCandidates[finalIndex];
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
