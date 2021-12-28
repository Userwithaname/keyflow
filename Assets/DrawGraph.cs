using UnityEngine;
using UnityEngine.UI;

public class DrawGraph:Graphic{
	public float lineWidth=5;
	public bool fillArea;
	public float valueScale=1;
	public float[] values;
	public float timeScale;
	public float[] times;
	public int currentIndex=1;
	public float expandedBlend;
	float width,height;
	// protected override void OnRectTransformDimensionsChange(){
	// 	Rect rect=rectTransform.rect;
	// 	width=rect.width;
	// 	height=rect.height;
	// 	base.OnRectTransformDimensionsChange();
	// }

	protected override void OnPopulateMesh(VertexHelper vh){
		Rect rect=rectTransform.rect;
		width=rect.width;
		height=rect.height;
		
		vh.Clear();
		
		UIVertex vertex=UIVertex.simpleVert;
		if(currentIndex<2||values.Length<=1)
			return;
		for(int i=1;i<values.Length;i++){
			if(times[i]<times[i-1]) return;
			vertex.color=color; //TODO: Maybe color the vertices red where the user makes an error? (right side of current & left side of next)
			
			// Top left		 (0)		//TODO: Top-left is never used except for the first iteration, consider removing it (and account for the different indexes when adding triangles)
			Vector3 topLeft=vertex.position=new Vector3(width*(times[i-1]-times[0])/(timeScale-times[0])*((float)currentIndex/(values.Length-1)),height*values[i-1]/valueScale);
			vh.AddVert(vertex);
			
			// Top right	 (1)
			vertex.position=new Vector3(width*(times[i]-times[0])/(timeScale-times[0])*((float)currentIndex/(values.Length-1)),height*values[i]/valueScale);
			vh.AddVert(vertex);
			
			Vector3 offsetDir=Vector3.Cross(topLeft-vertex.position,Vector3.forward).normalized*lineWidth;
			// vertex.color.a=0;
			
			// Bottom left	 (2)
			vertex.position=new Vector3(width*(times[i-1]-times[0])/(timeScale-times[0])*((float)currentIndex/(values.Length-1)),height*values[i-1]/valueScale)-offsetDir;
			if(fillArea) vertex.position.y=0;
			vh.AddVert(vertex);
			
			// Bottom right (3)
			vertex.position=new Vector3(width*(times[i]-times[0])/(timeScale-times[0])*((float)currentIndex/(values.Length-1)),height*values[i]/valueScale)-offsetDir;
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
		
		//TODO: Draw another curve for the "full-word" speed, but draw it "sharply" (as in, no diagonal lines, just horizontal and vertical)
		/*
		 * To implement the "sharp" lines, create an extra quad (two triangles) at both ends of each line, with the size of 'lineWidth'
		 * Draw each segment as a straight line, and connect it by joining the current start-quad to the previous end-quad's top or bottom vertices (create another quad between them)
		 * (Join top+bottom or bottom+top depending on if the value is greater or lower. Or just have them overlap.)
		 */
		
		//TODO: Draw a tooltip at the cursor's position when hovering over the graph points (possibly in another component?) Also draw a circle (or diamond) over the currently selected point
		
		// Maybe instead of a tooltip, show text where the quote would ordinarily be?
	}
}
