using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

//TODO: Make an asset post-processor to warn when a text file includes a character that isn't tracked
public class QuoteDataGenerator:EditorWindow{
	[MenuItem("Window/Quote Data Generator")]
	static void Init(){
		QuoteDataGenerator window=(QuoteDataGenerator)GetWindow(typeof(QuoteDataGenerator));
		window.Repaint();
	}
	void OnGUI(){
		if(GUILayout.Button("Generate Data")){
			GenerateData();
		}
	}
	[MenuItem("Assets/Generate Quote Data")]
	public static void GenerateData(){
		List<char> trackedKeys=KeyManager.trackedKeys.ToList();
		trackedKeys.Sort();

		// Get all quotes, change the names so they include a valid path
		List<string> quoteFiles=new();
		List<string> sortedQuoteContents=new();
		foreach(string path in new[]{
				// "Content/Movies & Shows",
				// "Content/Games",
				// "Content/Songs",
				// "Content/Code",
				// "Content/Quotes",
				"Content/Wikipedia",
				"Content/Wikipedia - Checked Links",
				// "Content/Non-English",
			}){
			foreach(TextAsset t in Resources.LoadAll<TextAsset>(path)){
				quoteFiles.Add(path+"/"+t.name);
				char[] text=t.text.ToCharArray();
				if(text.Length>0)
					text[0]=' ';	// Ignore the first character, as it isn't tracked
				Array.Sort(text);
				sortedQuoteContents.Add(new string(text.ToArray()));
			}
		}
		
		// Use as such: keyFrequencyInfo[characterIndex][quoteNameIndex] 
		List<string>[] quoteKeyFrequencyInfo=new List<string>[trackedKeys.Count];
		int[] charProgress=new int[quoteFiles.Count];
		
		for(int i=0;i<trackedKeys.Count;i++){
			quoteKeyFrequencyInfo[i]=new List<string>();
			for(int j=0;j<quoteFiles.Count;j++){
				int charCount=0;
				for(;charProgress[j]<sortedQuoteContents[j].Length&&sortedQuoteContents[j][charProgress[j]]==trackedKeys[i];charProgress[j]++){
					charCount++;
				}
				if(charCount==0) continue;
				
				// string charCountPrefix=charCount.ToString();
				string charCountPrefix=(Mathf.CeilToInt((float)(charCount+1)/sortedQuoteContents[j].Length*10000000)).ToString();
				char[] prefix={'0','0','0','0','0','0','0','0',};	// 8 characters
				for(int c=1;c<=charCountPrefix.Length;c++){
					prefix[^c]=charCountPrefix[^c];
				}
				quoteKeyFrequencyInfo[i].Add(new string(prefix)+quoteFiles[j]);
			}
			// Reverse-sort it, which will put highest frequency quotes for that character to the top of the list
			quoteKeyFrequencyInfo[i].Sort();
			quoteKeyFrequencyInfo[i].Reverse();
			// Then you can remove those first [x] characters of each element to get the actual filenames
			for(int j=0;j<quoteKeyFrequencyInfo[i].Count;j++){
				quoteKeyFrequencyInfo[i][j]=quoteKeyFrequencyInfo[i][j].Remove(0,8);
			}
			// Finally, write the contents to a file named by the current index + .txt
			File.WriteAllLines("Assets/Resources/CharFreqData/"+i+".txt",quoteKeyFrequencyInfo[i]);
		}
		
		// Warn if there are untracked characters in the file
		for(int i=0;i<charProgress.Length;i++){
			if(charProgress[i]==sortedQuoteContents[i].Length)
				continue;
			for(int j=charProgress[i];j<sortedQuoteContents[i].Length;j++){
				Debug.LogWarning(quoteFiles[i]+": Invalid character: "+sortedQuoteContents[i][j],Resources.Load(quoteFiles[i]));
			}
		}
	}
}
