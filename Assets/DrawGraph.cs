using UnityEngine;
using UnityEngine.UI;

public class DrawGraph:Graphic{
	public Color32 selectionColor;
	public float selectionSize=32;
	public float lineWidth=5;
	public bool fillArea;
	public float valueScale=1;
	public float[] values;
	public float timeScale;
	public float[] times;
	[HideInInspector]public int currentIndex;
	[HideInInspector]public int hoverIndex=-1;
	[HideInInspector]public float expandedBlend;
	float width,height;

	protected override void OnPopulateMesh(VertexHelper vh){
		Rect rect=rectTransform.rect;
		width=rect.width;
		height=rect.height;
		
		vh.Clear();
		
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
				if(diff<lowestHoverDiff){
					hoverPointX=currentPosX;
					lowestHoverDiff=diff;
					hoverIndex=i;	//TODO: Only select a point if the mouse is hovering over the graph (within the RectTransform boundaries)
				}
			}
			if(i==0) continue;
			if(times[i]<times[i-1]) return;
			vertex.color=color; //TODO: Maybe color the vertices red where the user makes an error? (right side of current & left side of next)
			
			float previousPosX=width*(times[i-1]-times[0])/(timeScale-times[0]);
			
			// Top left		 (0)		//TODO: Top-left is never used except for the first iteration, consider removing it (and account for the different indexes when adding triangles)
			Vector3 topLeft=vertex.position=new Vector3(previousPosX*((float)currentIndex/(values.Length-1)),height*values[i-1]/valueScale);
			vh.AddVert(vertex);
			
			// Top right	 (1)
			vertex.position=new Vector3(currentPosX*((float)currentIndex/(values.Length-1)),height*values[i]/valueScale);
			vh.AddVert(vertex);
			
			Vector3 offsetDir=Vector3.Cross(topLeft-vertex.position,Vector3.forward).normalized*lineWidth;
			// vertex.color.a=0;
			
			// Bottom left	 (2)
			vertex.position=new Vector3(previousPosX*((float)currentIndex/(values.Length-1)),height*values[i-1]/valueScale)-offsetDir;
			if(fillArea) vertex.position.y=0;
			vh.AddVert(vertex);
			
			// Bottom right (3)
			vertex.position=new Vector3(currentPosX*((float)currentIndex/(values.Length-1)),height*values[i]/valueScale)-offsetDir;
			if(fillArea) vertex.position.y=0;
			vh.AddVert(vertex);

			if(i==1){
				vh.AddTriangle((i-1)*4+0,(i-1)*4+1,(i-1)*4+2);
				vh.AddTriangle((i-1)*4+1,(i-1)*4+3,(i-1)*4+2);
			}else{
				// if(values[i]>values[i-1]-values[i-2]-values[i-1]){
					vh.AddTriangle((i-2)*4+1,(i-1)*4+1,(i-1)*4+2);
					vh.AddTriangle((i-1)*4+1,(i-1)*4+3,(i-1)*4+2);
					vh.AddTriangle((i-2)*4+3,(i-1)*4+2,(i-2)*4+1);
				// }else{
				// 	vh.AddTriangle((i-1)*4+0,(i-1)*4+1,(i-1)*4+2);
				// 	vh.AddTriangle((i-1)*4+1,(i-1)*4+3,(i-1)*4+2);
				// }
			}
		}
		
		if(hoverIndex>-1){
			Color32 inactiveColor=vertex.color;
			inactiveColor.a=0;
			vertex.color=Color32.Lerp(inactiveColor,selectionColor,expandedBlend);
			
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
		}

		
		//TODO: Draw a tooltip at the cursor's position when hovering over the graph points (possibly in another component?) Also draw a circle (or diamond) over the currently selected point
		
		// Maybe instead of a tooltip, show text where the quote would ordinarily be?
		
		//TODO: Draw error rate curve
		
		//TODO: Draw another curve for the "full-word" speed, but draw it "sharply" (as in, no diagonal lines, just horizontal and vertical)
		/*
		 * To implement the "sharp" lines, create an extra quad (two triangles) at both ends of each line, with the size of 'lineWidth'
		 * Draw each segment as a straight line, and connect it by joining the current start-quad to the previous end-quad's top or bottom vertices (create another quad between them)
		 * (Join top+bottom or bottom+top depending on if the value is greater or lower. Or just have them overlap.)
		 */
		// May also scale the alpha color of that curve by the 'expandedBlend' value, so it doesn't make the mini-graph look too busy
	}
}
