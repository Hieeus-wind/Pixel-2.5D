using UnityEngine;
using System;
using Rng=System.Random;
namespace Aetherlands.Coast.Editor
{
    public static class CoastalPixels
    {
        static Color32 Ink(string value){ColorUtility.TryParseHtmlString("#"+value,out Color c);return c;}
        public static PixelCanvas Surface(string kind, int seed, Color timber)
        {
            var r=new Rng(seed); var p=new PixelCanvas(64,64);
            Color baseColor=kind switch {"Wood"=>timber,"Stone"=>Ink("818787"),"Grass"=>Ink("72a64f"),"Roof"=>Ink("ad5946"),"Sand"=>Ink("cbc494"),"Plaster"=>Ink("eed8a0"),_=>Color.white};
            p.Box(0,0,64,64,baseColor);
            if(kind=="Plain")return p;
            for(int i=0;i<1200;i++){
                int x=r.Next(64),y=r.Next(64);
                float brightness=(float)(.87+r.NextDouble()*.24);
                Color c=baseColor*brightness;c.a=1;
                int length=kind=="Wood"?r.Next(2,17):r.Next(1,6);
                p.Box(x,y,length,1,c);if(x+length>64)p.Box(x-64,y,length,1,c);
                if(kind=="Stone" && i%4==0)p.Box(x+1,y+1,length,2,c);
                if(kind=="Grass" && i%6==0)p.Line(x,y,x+2,y+3,c);
            }
            if(kind=="Wood"){
                for(int i=0;i<8;i++){int x=r.Next(60),y=r.Next(60);p.Line(x,y,x+6,y+1,baseColor*.65f);p.Line(x+3,y+2,x+9,y+2,baseColor*1.12f);}
            }
            if(kind=="Roof"){
                for(int row=0;row<8;row++)for(int col=-1;col<8;col++){
                    int x=col*10+(row%2)*5,y=row*8;
                    p.Line(x,y+2,x+3,y,Ink("743e3a"));p.Line(x+3,y,x+9,y+1,Ink("80433b"));
                    p.Line(x+2,y+2,x+7,y+2,Ink("cd8057"));p.Line(x+9,y+1,x+10,y+7,Ink("8b493d"));
                }
            }
            return p;
        }
        public static PixelCanvas Traveler(int facing,int pose)
        {
            if(facing==3){
                var source=Traveler(2,pose);var mirrored=new PixelCanvas(32,48);
                for(int y=0;y<48;y++)for(int x=0;x<32;x++)mirrored.Dot(31-x,y,source.pixels[y*32+x]);
                return mirrored;
            }
            var p=new PixelCanvas(32,48);
            var ink=Ink("343448");var navy=Ink("344d63");var teal=Ink("348b89");var mint=Ink("83c7aa");
            var red=Ink("bf5b43");var gold=Ink("f0bb66");var hair=Ink("674543");var skin=Ink("dd946e");var light=Ink("ffcd95");
            var brown=Ink("865944");var leather=Ink("b58052");int bob=pose==2?1:0;
            // Silhouette coordinates are measured from the top of the common frame.
            void Span(int row,int x,int end,Color32 c)=>p.Box(x,47-row+bob,end-x+1,1,c);
            void Shape(Color32 c,params int[] points){
                int count=points.Length/2;
                for(int row=0;row<48;row++){int left=32,right=-1;
                    for(int i=0;i<count;i++){int j=(i+1)%count,x=points[2*i],y=points[2*i+1],xx=points[2*j],yy=points[2*j+1];
                        if(y==yy || row<Mathf.Min(y,yy) || row>=Mathf.Max(y,yy))continue;
                        float u=(row-y)/(float)(yy-y);float cut=Mathf.Lerp(x,xx,u);left=Mathf.Min(left,Mathf.CeilToInt(cut));right=Mathf.Max(right,Mathf.FloorToInt(cut));}
                    if(right>=left)Span(row,left,right,c);
                }
            }
            for(int leg=0;leg<2;leg++){
                int x=(leg==0?10:17)+(facing==2 && pose!=0?(leg==pose-1?2:-2):0);
                int lift=pose==leg+1?2:0;
                Shape(ink,x,32,x+6,33,x+5,42-lift,x+6,44-lift,x+5,46-lift,x-2,46-lift,x-2,43-lift,x,41-lift);
                Shape(navy,x+1,33,x+5,33,x+4,40-lift,x+1,40-lift);
                Span(40-lift,x,x+4,leather);Span(42-lift,x,x+3,brown);Span(44-lift,x-1,x+3,leather);
            }
            Shape(ink,11,22,20,21,24,25,25,32,23,35,18,34,12,36,6,35,8,30,8,25);
            Shape(navy,11,24,21,23,23,31,21,34,13,33,8,34);
            Shape(teal,11,22,21,23,23,28,21,30,17,29,13,31,8,32,9,27);
            Shape(mint,11,23,15,23,13,26,9,29);
            Shape(ink,9,26,11,28,9,32,6,32,6,30);Span(30,7,8,skin);
            Shape(ink,21,25,24,26,25,30,24,33,21,33,20,30);Span(30,22,24,light);Span(31,22,23,skin);
            Span(32,12,20,brown);Span(32,15,17,gold);Span(34,10,12,gold);
            Shape(ink,9,12,19,10,24,14,24,19,22,22,18,25,12,24,8,20);
            Shape(hair,9,14,22,13,23,18,21,23,12,23,9,20);
            if(facing==1){
                Shape(brown,10,15,21,14,21,18,19,20,19,17,16,21,14,18,13,21,10,19);
                Span(22,13,18,red);
            } else {
                int shift=facing==2?2:0;
                Shape(skin,11+shift,15,21+shift,14,22+shift,19,20+shift,22,15+shift,23,12+shift,20);
                Shape(light,13+shift,16,21+shift,15,21+shift,20,18+shift,22,14+shift,20);
                Span(17,14+shift,15+shift,ink);if(facing==0)Span(17,20,21,ink);
                Span(20,17+shift,18+shift,skin);Span(15,12,15,hair);Span(16,11,13,hair);Span(18,10,12,hair);
            }
            Shape(ink,5,14,8,12,10,8,14,6,20,6,23,9,23,12,27,13,26,15,19,16,11,15,6,16);
            Shape(teal,7,14,10,12,11,9,15,7,20,7,22,10,21,12,25,14,18,15,12,14);
            Shape(mint,11,10,15,7,19,7,17,9,13,10);
            Span(12,10,21,leather);Span(12,15,19,gold);Span(14,7,12,mint);
            Shape(red,19,11,19,7,22,4,26,2,25,5,23,8);Span(5,22,24,gold);
            // Scarf and satchel use the same shoulder and belt anchors in every direction.
            Shape(red,10,22,15,24,21,22,21,24,17,26,12,25,8,28,5,28,8,24);
            Span(23,12,15,gold);
            if(facing==1){
                Shape(ink,13,25,19,24,22,27,21,32,12,32,11,28);
                Shape(brown,13,26,19,25,21,27,20,31,13,31);Span(27,13,20,leather);Span(28,16,17,gold);
            }else{
                p.Line(20,47-25+bob,12,47-32+bob,leather);Span(30,11,14,gold);Span(31,10,14,brown);
            }
            return p;
        }
    }
}
