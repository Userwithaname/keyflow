using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
	public static float charPracticeDifficulty=.62f;	// 1: Completely random, 0: Only the worst character in either speed or accuracy
	public static float quoteDifficulty=.35f;
	
	void Start(){
		instance=this;
		InitializeKeyDatabase();
		Load();
		
		// GetKeyIndexes("abcAbd".ToLower());
	}
	static void InitializeKeyDatabase(){
		instance.confidenceDatabase=new KeyConfidenceData[trackedKeys.Length];
		for(int i=0;i<trackedKeys.Length;i++){
			instance.confidenceDatabase[i].seekTime=instance.confidenceDatabase[i].nextKeySeekTime=Mathf.Infinity;
			instance.confidenceDatabase[i].keyName=trackedKeys[i];
			#if UNITY_EDITOR
				instance.confidenceDatabase[i].name="Element "+i+": "+(trackedKeys[i].ToString());
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
	}
	
	public static int GetKeyIndex(char key){
		// key=key.ToString().ToLower()[0];
		for(int i=0;i<instance.confidenceDatabase.Length;i++){
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
		// keys=keys.ToLower();
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
		if(index>=lowercaseStart&&index<=lowercaseEnd)	return true;
		if(index>=capitalStart&&index<=capitalEnd)		return true;
		if(index>=numbersStart&&index<=numbersEnd)		return true;
		return false;
	}

	public static void RemoveHitsAndMisses(int keyIndex,int amount=1){
		if(instance.confidenceDatabase[keyIndex].hits<10)
			return;
		instance.confidenceDatabase[keyIndex].hits=Mathf.Max(0,instance.confidenceDatabase[keyIndex].hits-1);
		instance.confidenceDatabase[keyIndex].misses=Mathf.Max(0,instance.confidenceDatabase[keyIndex].misses-1);
	}
	public static void RegisterKeyHit(int keyIndex){
		instance.confidenceDatabase[keyIndex].hits++;
	}
	public static void RegisterKeyMiss(int keyIndex){
		instance.confidenceDatabase[keyIndex].misses++;
	}
	
	public static void UpdateSeekTime(int index,float seekTime){
		if(instance.confidenceDatabase[index].seekTime<10000){
			instance.confidenceDatabase[index].seekTime=Mathf.Lerp(instance.confidenceDatabase[index].seekTime,seekTime,.27f);
		}else{
			instance.confidenceDatabase[index].seekTime=seekTime;
		}
	}
	public static void UpdateNextKeySeekTime(int index,float seekTime){
		if(instance.confidenceDatabase[index].nextKeySeekTime<10000){
			instance.confidenceDatabase[index].nextKeySeekTime=Mathf.Lerp(instance.confidenceDatabase[index].seekTime,seekTime,.27f);
		}else{
			instance.confidenceDatabase[index].nextKeySeekTime=seekTime;
		}
	}
	
	public static void UpdateWordSpeed(string word,float time){
		float wpm=word.Length/time*60/5;
		foreach(int i in GetKeyIndexes(word)){
			if(instance.confidenceDatabase[i].wpm==0){
				instance.confidenceDatabase[i].wpm=wpm;
				continue;
			}
			instance.confidenceDatabase[i].wpm=Mathf.Lerp(instance.confidenceDatabase[i].wpm,wpm,.8f/word.Length);
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

	private void OnApplicationQuit(){
		Save();
	}
	
	public static int GetLowConfidenceCharacter(){
		int highestSeekTimeIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float highestSeekTime=-1;
		int highestNextSeekTimeIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float highestNextSeekTime=-1;
		int lowestAccIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float lowestAcc=2;
		int lowestWPMIndex=Random.Range(lowercaseStart,lowercaseEnd);
		float lowestWPM=10000;
		for(int i=0;i<instance.confidenceDatabase.Length;i++){
			if(!includeUppercase&&i>=capitalStart&&i<=capitalEnd) continue;
			if(!includeNumbers&&i>=numbersStart&&i<=numbersEnd) continue;
			if(!includeSymbols&&!IsAlphaNumericIndex(i)) continue;
			if(!includeWhitespace) switch(instance.confidenceDatabase[i].keyName){
				case ' ':	continue;
				case '\n':	continue;
				case '\t':	continue;
			}
			
			if(instance.confidenceDatabase[i].hits==0){
				if(Random.Range(0f,1f)<.02f){	// Chance to select a key because there is no data for it
					lowestAcc=0;
					lowestAccIndex=i;
				}
				continue;
			}
			float accuracy=(float)instance.confidenceDatabase[i].hits/(instance.confidenceDatabase[i].hits+instance.confidenceDatabase[i].misses);
			if(instance.confidenceDatabase[i].seekTime>=highestSeekTime&&instance.confidenceDatabase[i].seekTime<1000){
				if(Random.Range(0f,1f)>=Mathf.Clamp(accuracy,1F-charPracticeDifficulty*.8f,1F-charPracticeDifficulty)){
					highestSeekTime=instance.confidenceDatabase[i].seekTime;
					highestSeekTimeIndex=i;
				}
			}
			if(instance.confidenceDatabase[i].nextKeySeekTime>=highestNextSeekTime&&instance.confidenceDatabase[i].nextKeySeekTime<1000){
				if(Random.Range(0f,1f)>=Mathf.Clamp(accuracy,1F-charPracticeDifficulty*.8f,1F-charPracticeDifficulty)){
					highestNextSeekTime=instance.confidenceDatabase[i].seekTime;
					highestNextSeekTimeIndex=i;
				}
			}
			if(instance.confidenceDatabase[i].wpm<lowestWPM){
				if(Random.Range(0f,1f)<charPracticeDifficulty&&instance.confidenceDatabase[i].wpm>0){
					lowestWPM=instance.confidenceDatabase[i].wpm;
				}
			}
			if(accuracy<=lowestAcc){
				if(Random.Range(0f,1f)<charPracticeDifficulty){
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
	
	public static string GetQuoteByCharFrequency(int keyIndex){
		string[] quotes=RemoveTrailingNewline(Resources.Load<TextAsset>("CharFreqData/"+keyIndex).text).Split('\n');
		int invalidCount=0;
		while(quotes[0].Length==0){
			const int maxRetries=10;
			Debug.Log("Found no quotes containing '"+instance.confidenceDatabase[keyIndex].keyName+"', skipping...");
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
		float random=quoteDifficulty>=1?0:1f-Mathf.Pow(Random.Range(0f,1f),1f-quoteDifficulty);
		// float random=Random.Range(0f,1f)>Mathf.Pow(1f-difficulty,5)?1f-Mathf.Sqrt(Random.Range(0f,1f)):Random.Range(0f,1f);
		string targetQuote=quotes[Mathf.Min(Mathf.FloorToInt(random*quotes.Length),quotes.Length-1)];
		string newQuote=targetQuote.Split('/')[^1];
		if(newQuote==Typing.quoteTitle){
			targetQuote=quotes[Random.Range(0,quotes.Length)];
		}
		Typing.quoteTitle=targetQuote.Split('/')[^1];
		targetQuote=RemoveTrailingNewline(Resources.Load<TextAsset>(targetQuote).text);	//TODO: Allow skipping categories (eg. ignore 'Content/Code/...') (increase index until a different category is found, loop over if not, select different character if same index is reached) (check if an appropriate file exists in the 'while' loop above)
		return targetQuote;
	}
	
	public static string RemoveTrailingNewline(string text){
		if(text.Length<=0) return text;
		return text[^1]=='\n'?text.Remove(text.Length-1,1):text;
	}
}
