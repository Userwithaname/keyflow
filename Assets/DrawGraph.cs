using UnityEngine;
using UnityEngine.UI;

public class DrawGraph:Graphic{
	public Color32 diamondColor;
	public float selectionSize=32;
	public float lineWidth=5;
	public bool fillArea;
	public float[] speedValues;
	public float speedValueScale=1;
	public float timeScale;
	public float[] accuracy; 
	public int[] misses;
	public float[] seekTimes;
	public float[] times;
	public float[] wordSpeedValues;
	public float[] wordTimes;
	public float wordSpeedScale=1;
	[System.NonSerialized]public int currentIndex;
	[System.NonSerialized]public int hoverIndex=-1;
	[System.NonSerialized]public int hoverWordIndex=-1;
	[System.NonSerialized]public float expandedBlend;
	public bool mouseOverGraph;
	float width,height;
	float lastWidth,lastHeight;
	float pixelScale;

	//TODO: Ability to zoom (stretch the bar horizontally using the mouse scroolwheel, keep the selected index at the same screen position, shew a scrollbar)
	
	protected override void OnPopulateMesh(VertexHelper vh){
		vh.Clear();
		
		Rect rect=rectTransform.rect;
		width=rect.width;
		height=rect.height;
		if(lastWidth!=width||lastHeight!=height){
			CanvasScaler scaler=FindObjectOfType<CanvasScaler>();
			pixelScale=Mathf.Lerp(Screen.width/scaler.referenceResolution.x,Screen.height/scaler.referenceResolution.y,scaler.matchWidthOrHeight);
		}

		UIVertex vertex=UIVertex.simpleVert;
		if(currentIndex<2||speedValues.Length<=1)
			return;
		
		Vector3 rtPos=rectTransform.position;
		Vector3 mouseHoverPos=Input.mousePosition;
		mouseHoverPos.x-=rtPos.x;
		mouseHoverPos/=pixelScale;
		float lowestHoverDiff=999;
		float hoverPointX=0;
		hoverIndex=hoverWordIndex=-1;
		mouseOverGraph=mouseHoverPos.x>-10&&mouseHoverPos.x<width+10&&mouseHoverPos.y<rtPos.y+height+10&&mouseHoverPos.y>rtPos.y-10;

		float topWPM=Mathf.Lerp(speedValueScale,Mathf.Max(speedValueScale,wordSpeedScale),expandedBlend);
		
		// Draw WPM graph
		for(int i=0;i<speedValues.Length;i++){
			float textProgress=(float)(currentIndex)/(speedValues.Length-1);
			// float textProgress=(float)((values.Length)-currentIndex+1)/(values.Length);
			float currentPosX=(times[i]-times[0])/(timeScale-times[0])*textProgress*width;
			if(expandedBlend>0.00001f&&mouseOverGraph){
				float diff=mouseHoverPos.x-currentPosX;
				float diffAbs=Mathf.Abs(diff);
				if(diff>0?diffAbs<=lowestHoverDiff:diffAbs<lowestHoverDiff){
					hoverPointX=currentPosX;
					lowestHoverDiff=diffAbs;
					hoverIndex=i;
				}
			}
			if(i==0) continue;
			if(times[i]<times[i-1]) break;
			
			float previousPosX=(times[i-1]-times[0])/(timeScale-times[0])*textProgress*width;
			vertex.color=color;
			
			// Top left		 (0)
			Vector3 topLeft=vertex.position=new Vector3(previousPosX,height*speedValues[i-1]/topWPM);
			if(i==1){
				vh.AddVert(vertex);
			}
			
			// Top right	 (1) (0) (0)
			vertex.position=new Vector3(currentPosX,height*speedValues[i]/topWPM);
			vh.AddVert(vertex);
			
			Vector3 offsetDir=Vector3.Cross(topLeft-vertex.position,Vector3.forward).normalized*lineWidth;
			// vertex.color.a=0;
			
			// Bottom left	 (2) (1)
			if(!fillArea||i==1){
				vertex.position=fillArea?
					new Vector3(previousPosX,0):
					topLeft-offsetDir;
				vh.AddVert(vertex);
			}
			
			// Bottom right (3) (2) (1)
			vertex.position=fillArea?
				new Vector3(currentPosX,0):
				new Vector3(currentPosX,height*speedValues[i]/topWPM)-offsetDir;
			vh.AddVert(vertex);

			switch(i){
				case 1:{
					vh.AddTriangle(1,0,2);
					vh.AddTriangle(2,1,3);
					break;
				}
				case 2:{
					if(fillArea){
						vh.AddTriangle(4,1,3);
						vh.AddTriangle(3,4,5);
					}else{
						vh.AddTriangle(1,4+0,4+1);
						vh.AddTriangle(4+0,4+2,4+1);
						vh.AddTriangle(3,4+1,1);
					}
					break;
				}
				default:{
					if(fillArea){
						vh.AddTriangle((i-1)*2+2+0,(i-2)*2+2+0,(i-2)*2+2+1);
						vh.AddTriangle((i-2)*2+2+1,(i-1)*2+2+1,(i-1)*2+2+0);
					}else{
						vh.AddTriangle((i-2)*3+1+0,(i-1)*3+1+0,(i-1)*3+1+1);
						vh.AddTriangle((i-1)*3+1+0,(i-1)*3+1+2,(i-1)*3+1+1);
						vh.AddTriangle((i-2)*3+1+2,(i-1)*3+1+1,(i-2)*3+1+0);
					}
					break;
				}
			}
		}
		
		if(expandedBlend<0.00001f)
			return;
		int vertCount=vh.currentVertCount;
		
		// Draw a curve for the individual key seek times
		for(int i=0;i<speedValues.Length;i++){
			if(i==0) continue;
			if(times[i]<times[i-1]) break;
			
			float textProgress=(float)(currentIndex)/(seekTimes.Length-1);
			float currentPosX=(times[i]-times[0])/(timeScale-times[0])*textProgress*width;
			float previousPosX=(times[i-1]-times[0])/(timeScale-times[0])*textProgress*width;
			
			// vertex.color=new Color(1,.6f,0,1f);
			// vertex.color=selectedTheme.backgroundColor*new Color(1,1,1,expandedBlend);
			vertex.color=diamondColor*new Color(1,1,1,.75f*expandedBlend*.75f*.5f);
			
			/*
			 * Idea: For non-filled (line) graphs, always 'pivot' around the outer (pointy) end of the line angle
			 *
			 * For example:
			 * If a line goes up and then down, the start of the down line should pivot around the top vertex
			 * If a line geos down and then up, then pivot around the bottom vertex
			 * Doing so would also prevent the need for the extra triangle which currently fills the gap between the two lines
			 * It may also be a good idea to offset both the top and the bottom vertices in such a way that the true value is in the center, and they are equally distant form that value
			 */
			
			Vector3 topLeft=new Vector3(previousPosX,height*seekTimes[i-1]/4);
			Vector3 topRight=new Vector3(currentPosX,height*seekTimes[i]/4);
			Vector3 offsetDir=Vector3.Cross(topLeft-topRight,Vector3.forward).normalized*lineWidth/2*expandedBlend;
			
			// Top left		 (0)
			vertex.position=topLeft+offsetDir;
			// if(i==1){
				vh.AddVert(vertex);
			// }
			
			// Top right	 (1) (0)
			vertex.position=topRight+offsetDir;
			vh.AddVert(vertex);
			
			// vertex.color.a=0;
			
			// Bottom left	 (2) (1)
			vertex.position=topLeft-offsetDir;
			vh.AddVert(vertex);
			
			// Bottom right (3) (2)
			vertex.position=topRight-offsetDir;
			vh.AddVert(vertex);

			vertCount+=4;
			vh.AddTriangle(vertCount+1,vertCount+0,vertCount+2);
			vh.AddTriangle(vertCount+2,vertCount+1,vertCount+3);
		}
		
		// Draw a word speed graph
		for(int i=wordSpeedValues.Length-1;i>-1;i--){
			if(wordTimes[i]==0) continue;
			if(hoverIndex!=-1&&wordTimes[i]>=times[hoverIndex]&&(i==0||wordTimes[i-1]<times[hoverIndex])&&!Typing.instance.settingsOpen){
				hoverWordIndex=i;
			}
			
			float currentPosX=(wordTimes[i]-times[0])/(timeScale-times[0])*width;
			float previousPosX=i>0?(wordTimes[i-1]-times[0])/(timeScale-times[0])*width:0;
			
			 //BUG: First word always highlighted when mouse is over graph, but only the color (right two vertices), not size
			vertex.color=Typing.currentTheme.improvementColor*new Color(1,1,1,(i==hoverWordIndex?.75f:.4f)*expandedBlend);
			
			Vector3 topLeft=new Vector3(previousPosX,height*wordSpeedValues[i]/topWPM);
			Vector3 topRight=new Vector3(currentPosX,height*wordSpeedValues[i]/topWPM);
			Vector3 offsetDir=Vector3.up*lineWidth/2*expandedBlend*(i==hoverWordIndex?3:1);
			
			// Top left		 (0)
			vertex.position=topLeft+offsetDir;
			vh.AddVert(vertex);
			
			// Top right	 (1)
			vertex.position=topRight+offsetDir;
			vh.AddVert(vertex);
			
			// Bottom left	 (2)
			vertex.position=topLeft-offsetDir;
			vh.AddVert(vertex);
			
			// Bottom right (3)
			vertex.position=topRight-offsetDir;
			vh.AddVert(vertex);

			vertCount+=4;
			vh.AddTriangle(vertCount+1,vertCount+0,vertCount+2);
			vh.AddTriangle(vertCount+2,vertCount+1,vertCount+3);
		}
		
		// Draw a diamond and a line at the point nearest to the cursor
		if(hoverIndex>-1&&!Typing.instance.settingsOpen){
			float hoverPointY=height*speedValues[hoverIndex]/topWPM;
			
			//TODO: When drawing diamonds, also draw a line going towards each tooltip (shouldn't be too hard assuming Typing.GraphUpdate() runs before this function)
			/*
			 * tooltip pos - graph pos to get the location in local space, which you can use to draw an extra diamond an da line connecting the two
			 */
			
			// Draw WPM diamond 
			vertex.color=diamondColor*new Color(1,1,1,.75f*expandedBlend);
			
			vertex.position=new Vector3(hoverPointX-(selectionSize*expandedBlend),hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX,hoverPointY-(selectionSize*expandedBlend));
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+(selectionSize*expandedBlend),hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX,hoverPointY+(selectionSize*expandedBlend));
			vh.AddVert(vertex);
			
			vertCount+=4;
			vh.AddTriangle(vertCount-1,vertCount-2,vertCount-3);
			vh.AddTriangle(vertCount-3,vertCount-4,vertCount-1);
			
			// Draw line below WPM graph
			vertex.color*=new Color(1,1,1,.75f);
			
			vertex.position=new Vector3(hoverPointX-selectionSize/4,hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+selectionSize/4,hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+selectionSize/4,0);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX-selectionSize/4,0);
			vh.AddVert(vertex);
			
			vertCount+=4;
			vh.AddTriangle(vertCount-1,vertCount-2,vertCount-3);
			vh.AddTriangle(vertCount-3,vertCount-4,vertCount-1);
			
			// Draw line above WPM graph
			vertex.color*=new Color(1,1,1,.5f);
			
			vertex.position=new Vector3(hoverPointX-selectionSize/8,hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+selectionSize/8,hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+selectionSize/8,height);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX-selectionSize/8,height);
			vh.AddVert(vertex);
			
			vertCount+=4;
			vh.AddTriangle(vertCount-1,vertCount-2,vertCount-3);
			vh.AddTriangle(vertCount-3,vertCount-4,vertCount-1);
			
			// Draw seek time diamond
			vertex.color=diamondColor*new Color(1,1,1,.67f*expandedBlend);
			
			hoverPointY=height*seekTimes[hoverIndex]/4;
			
			vertex.position=new Vector3(hoverPointX-(selectionSize*expandedBlend),hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX,hoverPointY-(selectionSize*expandedBlend));
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+(selectionSize*expandedBlend),hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX,hoverPointY+(selectionSize*expandedBlend));
			vh.AddVert(vertex);
			
			vertCount+=4;
			vh.AddTriangle(vertCount-1,vertCount-2,vertCount-3);
			vh.AddTriangle(vertCount-3,vertCount-4,vertCount-1);
			
			// Draw word speed diamond
			vertex.color=Typing.currentTheme.improvementColor*new Color(1,1,1,expandedBlend);
			
			hoverPointY=height*wordSpeedValues[hoverWordIndex]/topWPM;
			
			vertex.position=new Vector3(hoverPointX-(selectionSize*expandedBlend/2),hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX,hoverPointY-(selectionSize*expandedBlend/2));
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX+(selectionSize*expandedBlend/2),hoverPointY);
			vh.AddVert(vertex);
			vertex.position=new Vector3(hoverPointX,hoverPointY+(selectionSize*expandedBlend/2));
			vh.AddVert(vertex);
			
			vertCount+=4;
			vh.AddTriangle(vertCount-1,vertCount-2,vertCount-3);
			vh.AddTriangle(vertCount-3,vertCount-4,vertCount-1);
		}
		
		// this is only here to work around an index error which otherwise occurs when there were no mistakes and mouse is not hovering on the graph
		vertCount=vh.currentVertCount;
		vertex.position=new Vector3(-50000,-50000);
		vh.AddVert(vertex);
		vertex.position=new Vector3(-50000,-50000);
		vh.AddVert(vertex);
		vertex.position=new Vector3(-50000,-50000);
		vh.AddVert(vertex);
		vertex.position=new Vector3(-50000,-50000);
		vh.AddVert(vertex);
		vh.AddTriangle(vertCount+1,vertCount+0,vertCount+2);
		vh.AddTriangle(vertCount+2,vertCount+1,vertCount+3);
		vertCount+=4;
		
		// Draw errors
		for(int i=0;expandedBlend>0.00001f&&i<accuracy.Length;i++){
			if(misses[i]==0) continue;
			
			float currentPosX=width*(times[i]-times[0])/(timeScale-times[0]);
			
			vertex.color=Typing.currentTheme.textColorError;
			// vertex.color=Color.red;
			
			float offset=selectionSize*expandedBlend/(i==hoverIndex?1.25f:1.5f);
			Vector3 diagonal=i==hoverIndex?new Vector3(-offset/3,offset/3):new Vector3(-offset/4,offset/4);
			
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)-offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)-offset)+diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)+offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)+offset)+diagonal;
			vh.AddVert(vertex);
			
			vh.AddTriangle(vertCount+1,vertCount+0,vertCount+2);
			vh.AddTriangle(vertCount+2,vertCount+1,vertCount+3);
			vertCount+=4;
			
			diagonal.x=-diagonal.x;
			
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)-offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)-offset)+diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)+offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)+offset)+diagonal;
			vh.AddVert(vertex);
			
			vh.AddTriangle(vertCount+1,vertCount+0,vertCount+2);
			vh.AddTriangle(vertCount+2,vertCount+1,vertCount+3);
			vertCount+=4;
		}
	}
}