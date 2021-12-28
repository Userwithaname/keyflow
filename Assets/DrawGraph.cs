using UnityEngine;
using UnityEngine.UI;

public class DrawGraph:Graphic{
	public Color32 diamondColor;
	public float selectionSize=32;
	public float lineWidth=5;
	public bool fillArea;
	public float valueScale=1;
	public float[] values;
	public float timeScale;
	public float[] accuracy; 
	public int[] misses;
	public float[] times;
	[System.NonSerialized]public int currentIndex;
	[System.NonSerialized]public int hoverIndex=-1;
	[System.NonSerialized]public float expandedBlend;
	float width,height;

	protected override void OnPopulateMesh(VertexHelper vh){
		vh.Clear();
		
		Rect rect=rectTransform.rect;
		width=rect.width;
		height=rect.height;

		UIVertex vertex=UIVertex.simpleVert;
		if(currentIndex<2||values.Length<=1)
			return;
		
		float mouseHoverPos=Input.mousePosition.x-rectTransform.position.x;
		float lowestHoverDiff=999;
		hoverIndex=-1;
		float hoverPointX=0;
		for(int i=0;i<values.Length;i++){
			float currentPosX=width*(times[i]-times[0])/(timeScale-times[0]);
			if(expandedBlend>0.00001f){
				float diff=Mathf.Abs(mouseHoverPos-currentPosX);
				if(diff<=lowestHoverDiff){
					hoverPointX=currentPosX;
					lowestHoverDiff=diff;
					hoverIndex=i;	//TODO: Only select a point if the mouse is hovering over the graph (within the RectTransform boundaries)
				}
			}
			if(i==0) continue;
			if(times[i]<times[i-1]) return;
			
			float previousPosX=width*(times[i-1]-times[0])/(timeScale-times[0]);
			float textProgress=(float)currentIndex/(values.Length-1);
			vertex.color=color;
			
			// Top left		 (0)
			Vector3 topLeft=vertex.position=new Vector3(previousPosX*textProgress,height*values[i-1]/valueScale);
			if(i==1){
				vh.AddVert(vertex);
			}
			
			// Top right	 (1) (0) (0)
			vertex.position=new Vector3(currentPosX*textProgress,height*values[i]/valueScale);
			vh.AddVert(vertex);
			
			Vector3 offsetDir=Vector3.Cross(topLeft-vertex.position,Vector3.forward).normalized*lineWidth;
			// vertex.color.a=0;
			
			// Bottom left	 (2) (1)
			if(!fillArea||i==1){
				vertex.position=fillArea?
					new Vector3(previousPosX*textProgress,0):
					topLeft-offsetDir;
				vh.AddVert(vertex);
			}
			
			// Bottom right (3) (2) (1)
			vertex.position=new Vector3(currentPosX*textProgress,height*values[i]/valueScale)-offsetDir;
			if(fillArea) vertex.position.y=0;
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
		
		int vertexCount=vh.currentVertCount;
		for(int i=0;expandedBlend>0.00001f&&i<accuracy.Length;i++){	//BUG: Doesn't work
			if(misses[i]==0) continue;
			
			float currentPosX=width*(times[i]-times[0])/(timeScale-times[0]);
			
			vertex.color=Typing.instance.themes[Typing.instance.selectedTheme].textColorError*new Color(1,1,1,expandedBlend*.5f);
			// vertex.color=Color.red;
			
			float offset=selectionSize*expandedBlend*2;
			Vector3 diagonal=new Vector3(offset/2,offset/2);
			
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)-offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)-offset)+diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)+offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)+offset)+diagonal;
			vh.AddVert(vertex);
			
			vh.AddTriangle(vertexCount+0,vertexCount+1,vertexCount+2);
			vh.AddTriangle(vertexCount+2,vertexCount+3,vertexCount+3);
			
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)-offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX+offset,height*(1f-accuracy[i]/100)-offset)+diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)+offset)-diagonal;
			vh.AddVert(vertex);
			vertex.position=new Vector3(currentPosX-offset,height*(1f-accuracy[i]/100)+offset)+diagonal;
			vh.AddVert(vertex);
			
			vh.AddTriangle(vertexCount+0,vertexCount+1,vertexCount+2);
			vh.AddTriangle(vertexCount+2,vertexCount+3,vertexCount+3);
			
			vertexCount+=8;
		}
		
		if(hoverIndex>-1){
			Color32 inactiveColor=vertex.color;
			inactiveColor.a=0;
			vertex.color=Color32.Lerp(inactiveColor,diamondColor*new Color(1,1,1,.75f),expandedBlend);
			
			float hoverPointY=height*values[hoverIndex]/valueScale;
			
			vertex.position=new Vector3(hoverPointX-(selectionSize*expandedBlend),hoverPointY);
			vh.AddVert(vertex);
			
			vertex.position=new Vector3(hoverPointX,hoverPointY-(selectionSize*expandedBlend));
			vh.AddVert(vertex);
			
			vertex.position=new Vector3(hoverPointX+(selectionSize*expandedBlend),hoverPointY);
			vh.AddVert(vertex);
			
			vertex.position=new Vector3(hoverPointX,hoverPointY+(selectionSize*expandedBlend));
			vh.AddVert(vertex);
			
			int vertCount=vh.currentVertCount;
			vh.AddTriangle(vertCount-1,vertCount-2,vertCount-3);
			vh.AddTriangle(vertCount-3,vertCount-4,vertCount-1);
			
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
		}
		
		//TODO: Draw error rate curve (or do the MonkeyType thing and draw x-es where errors occur (or just a triangle if you are lazy))
		
		//TODO: Draw another curve for the "full-word" speed, but draw it "sharply" (as in, no diagonal lines, just horizontal and vertical)
		/*
		 * To implement the "sharp" lines, create an extra quad (two triangles) at both ends of each line, with the size of 'lineWidth'
		 * Draw each segment as a straight line, and connect it by joining the current start-quad to the previous end-quad's top or bottom vertices (create another quad between them)
		 * (Join top+bottom or bottom+top depending on if the value is greater or lower. Or just have them overlap.)
		 */
		// May also scale the alpha color of that curve by the 'expandedBlend' value, so it doesn't make the mini-graph look too busy
	}
}
