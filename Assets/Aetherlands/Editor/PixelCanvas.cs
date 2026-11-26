using UnityEngine;
namespace Aetherlands.Coast.Editor
{
    public sealed class PixelCanvas
    {
        public readonly int width,height;
        public readonly Color32[] pixels;
        public PixelCanvas(int w,int h){width=w;height=h;pixels=new Color32[w*h];}
        public void Dot(int x,int y,Color32 color){if(x>=0 && x<width && y>=0 && y<height)pixels[x+y*width]=color;}
        public void Box(int x,int y,int w,int h,Color32 color){for(int j=y;j<y+h;j++)for(int i=x;i<x+w;i++)Dot(i,j,color);}
        public void Line(int x,int y,int endX,int endY,Color32 color)
        {
            int steps=Mathf.Max(Mathf.Abs(endX-x),Mathf.Abs(endY-y),1);
            for(int i=0;i<=steps;i++){float t=(float)i/steps;Dot(Mathf.RoundToInt(Mathf.Lerp(x,endX,t)),Mathf.RoundToInt(Mathf.Lerp(y,endY,t)),color);}
        }
        public void Stamp(PixelCanvas layer,int x,int y)
        {
            for(int j=0;j<layer.height;j++)for(int i=0;i<layer.width;i++){var c=layer.pixels[j*layer.width+i];if(c.a>0)Dot(x+i,y+j,c);}
        }
    }
}
