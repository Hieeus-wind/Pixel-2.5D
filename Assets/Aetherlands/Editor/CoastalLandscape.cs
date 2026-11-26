using System;
using System.Collections.Generic;
using UnityEngine;
using Rng=System.Random;
namespace Aetherlands.Coast.Editor
{
    public sealed class CoastalLandscape
    {
        public readonly Dictionary<string,CoastalMesh> surfaces=new();
        readonly Rng random;
        CoastalMesh wood=>surfaces["Wood"]; CoastalMesh rock=>surfaces["Stone"]; CoastalMesh grass=>surfaces["Grass"]; CoastalMesh detail=>surfaces["Plain"];
        static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        Color Shade(float lo=.82f,float hi=1f){float s=Range(lo,hi);return new Color(s,s,s);}
        public CoastalLandscape(int seed)
        {
            random=new Rng(seed);
            foreach(var name in new[]{"Wood","Stone","Grass","Roof","Sand","Plaster","Plain"})surfaces.Add(name,new CoastalMesh());
            surfaces["Sand"].Face(P(-70,-4,-70),P(-70,-4,70),P(70,-4,70),P(70,-4,-70),Color.white);
            Island(0,8,6.6f,5.6f,0,true);Island(0,-12,7,5.5f,0,false);
            Island(-10,5,2.3f,1.9f,-1.5f,false);Island(11,12,4,3,-.75f,false);
            Island(-12,20,6,4,1,false);Island(13,-9,3.2f,2.7f,-2.25f,false);
            Bridge(); Cottage(); DockFurniture(); Palm(P(-3.9f,0,7),3.45f);Palm(P(4.4f,0,9.3f),3.9f);Palm(P(-3.8f,0,-10),3.6f);
        }
        void Island(float x,float z,float rx,float rz,float top,bool path)
        {
            int steps=44;
            var rings=new Vector3[6,steps];var center=P(x,top,z);
            float[] widths={1.36f,1.32f,1.17f,1.12f,1.01f,1f};
            float[] levels={-4.4f,-2.65f,-2.48f,-1.14f,-1.02f,0};
            for(int i=0;i<steps;i++){
                float a=i*2*Mathf.PI/steps, variation=Range(.93f,1.06f);
                for(int k=0;k<6;k++)rings[k,i]=center+P(Mathf.Cos(a)*rx*widths[k]*variation,levels[k]+(k==5?0:Range(-.12f,.12f)),Mathf.Sin(a)*rz*widths[k]*variation);
            }
            for(int i=0;i<steps;i++){
                int j=(i+1)%steps;
                for(int k=0;k<5;k++)rock.Face(rings[k,i],rings[k+1,i],rings[k+1,j],rings[k,j],Shade(.63f+k*.035f,.83f+k*.03f));
                for(int k=1;k<=3;k+=2){
                    var start=rings[k,i];var end=rings[k,j];
                    var lip=(start-center);lip.y=0;lip=lip.normalized*.035f;
                    rock.Face(start+lip,end+lip,end+lip+Vector3.up*.08f,start+lip+Vector3.up*.14f,C("929d91"));
                }
                grass.Face(center,rings[5,j],rings[5,i],center,Shade(.94f,1));
                var rim=rings[5,i];grass.Face(rim,rings[5,j],rings[5,j]-Vector3.up*.10f,rim-Vector3.up*.16f,Shade());
                Vector3 Shoal(int index,float spread,float level){var p=rings[5,index];return P(x+(p.x-x)*spread,level,z+(p.z-z)*spread);}
                surfaces["Sand"].Face(Shoal(i,1.12f,-2.2f),Shoal(j,1.12f,-2.2f),Shoal(j,1.72f,-3.64f),Shoal(i,1.72f,-3.64f),Shade(.72f,.84f),-.55f);
                // Split basalt columns break the silhouette and catch narrow glints between terraces.
                Vector3 radial=(rim-center).normalized;
                for(int n=0;n<2;n++){
                    var foot=Vector3.Lerp(rim,rings[5,j],n*.49f)+radial*Range(.14f,.42f);
                    rock.Tube(foot-Vector3.up*Range(1.65f,2.2f),foot-Vector3.up*Range(.25f,.8f),Range(.29f,.53f),Range(.23f,.45f),6,Shade(.68f,.95f));
                    if(n==0 && i%3!=0){var edge=(rings[5,j]-rim)*.33f;var lip=rim+radial*.025f;
                        grass.Face(lip+Vector3.up*.008f,lip+edge,lip+edge*.73f-Vector3.up*.28f,lip+edge*.25f-Vector3.up*.16f,C("b9cc8b"));}
                }
            }
            // The sandy path widens around the bridgehead and curves toward the station.
            if(path){for(int i=0;i<13;i++){float zz=4.56f+i*.32f;float xx=.38f*Mathf.Sin(i*.28f);
                surfaces["Sand"].Face(P(xx-1.25f,.012f,zz),P(xx-1.12f,.012f,zz+.34f),P(xx+1.13f,.012f,zz+.34f),P(xx+1.2f,.012f,zz),Shade(.9f,.97f),-.55f);}}
            for(int i=0;i<(int)(rx*rz*9);i++){
                float dx=Range(-1,1),dz=Range(-1,1);if(dx*dx+dz*dz>.88f)continue;
                var root=center+P(dx*rx,.02f,dz*rz);if(Mathf.Abs(root.x)<1.65f)continue;
                for(int j=0;j<4;j++){
                    var a=root+P(Range(-.09f,.09f),0,Range(-.1f,.1f));float ht=Range(.09f,.28f);
                    detail.Face(a,a+P(.055f,0,0),a+P(.09f,ht,.035f),a+P(.048f,ht*.65f,0),j%2==0?C("98bf59"):C("528c52"));
                }
                if(i%13==0){detail.Tube(root,root+Vector3.up*.21f,.018f,.015f,4,C("518550"));detail.Tube(root+Vector3.up*.2f,root+Vector3.up*.26f,.065f,.033f,5,i%2==0?C("e5ab5c"):C("e5d8a1"));}
                if(i%8==0){
                    for(int leaf=0;leaf<8;leaf++){
                        float a=leaf*2.4f+i, reach=Range(.17f,.43f);
                        var dir=P(Mathf.Cos(a),0,Mathf.Sin(a));var wide=Vector3.Cross(dir,Vector3.up)*.075f;
                        var rib=root+dir*reach*.48f+Vector3.up*reach*.74f;var tip=root+dir*reach+Vector3.up*reach*.47f;
                        detail.Face(root,rib-wide,rib,root,C("3c8259"));detail.Face(rib-wide,tip,rib,rib-wide,C("4a9862"));
                        detail.Face(root,rib,rib+wide,root,C("80af55"));detail.Face(rib,tip,rib+wide,rib,C("96ba61"));
                    }
                }
            }
        }
        void Bridge()
        {
            for(int i=0;i<49;i++){
                float z=-8+i*.26f;
                wood.Beam(P(-1.65f+Range(-.06f,.04f),-.057f,z),P(1.65f+Range(-.04f,.06f),-.057f,z+Range(-.018f,.018f)),.251f,.18f,Shade(.75f,1f));
                for(int side=-1;side<=1;side+=2)detail.Tube(P(side*1.36f,.034f,z),P(side*1.36f,.044f,z),.025f,.029f,5,C("614c45"));
            }
            for(int side=-1;side<=1;side+=2){
                float x=side*1.62f;wood.Beam(P(x,-.4f,-8.3f),P(x,-.4f,4.7f),.29f,.42f,Shade());
                for(int i=0;i<=6;i++){
                    float z=-8+i*2.08f;var basePoint=P(x,0,z);
                    wood.Tube(basePoint-Vector3.up*3.9f,basePoint+Vector3.up*.94f,.18f,.145f,9,Shade());
                    wood.Tube(basePoint+Vector3.up*.94f,basePoint+Vector3.up*1.02f,.16f,.115f,9,C("f1d3aa"));
                    for(int b=0;b<4;b++)detail.Tube(basePoint+Vector3.up*(.57f+b*.048f),basePoint+Vector3.up*(.606f+b*.048f),.161f,.163f,10,b%2==0?C("c5a671"):C("937956"));
                    if(i==6)continue;
                    for(int j=0;j<12;j++){
                        float t=j/12f,s=(j+1)/12f;
                        Vector3 Knot(float f)=>basePoint+P(0,.72f-.23f*Mathf.Sin(f*Mathf.PI),2.08f*f);
                        detail.Tube(Knot(t),Knot(s),.038f,.038f,5,j%2==0?C("d0b57f"):C("ac8c65"));
                        if(j%4==0)detail.Tube(Knot(t),basePoint+P(0,.20f,2.08f*(t+.15f)),.02f,.02f,4,C("af946c"));
                    }
                    wood.Beam(basePoint+P(0,-1.2f,0),basePoint+P(0,-.32f,1.1f),.13f,.16f,Shade());
                    wood.Beam(basePoint+P(0,.18f,0),basePoint+P(0,.18f,2.08f),.075f,.09f,Shade());
                    for(int j=0;j<4;j++){
                        float z0=j*.52f;var a=basePoint+P(0,.22f,z0);var b=basePoint+P(0,.48f,z0+.26f);
                        detail.Tube(a,b,.024f,.024f,5,C("b5a079"));detail.Tube(b,basePoint+P(0,.22f,z0+.52f),.024f,.024f,5,C("c0a879"));
                    }
                    if(i%3==1)Lantern(basePoint,side);
                }
            }
        }
        void Lantern(Vector3 root,int side)
        {
            Vector3 pole=root+P(side*.07f,0,0);
            wood.Tube(pole+Vector3.up*.9f,pole+Vector3.up*2.12f,.08f,.058f,7,Shade());
            var hook=pole+P(-side*.38f,2.13f,0);wood.Beam(pole+Vector3.up*2.13f,hook,.07f,.08f,Shade());
            detail.Tube(hook,hook-Vector3.up*.15f,.018f,.018f,5,C("463f43"));
            var top=hook-Vector3.up*.14f;
            detail.Tube(top-Vector3.up*.12f,top,.19f,.055f,6,C("775045"));
            detail.Tube(top-Vector3.up*.40f,top-Vector3.up*.12f,.12f,.145f,6,C("ffc975"));
            detail.Tube(top-Vector3.up*.45f,top-Vector3.up*.39f,.055f,.16f,6,C("705047"));
        }
        void Cottage()
        {
            var plaster=surfaces["Plaster"];var roof=surfaces["Roof"];
            const float x=-.65f,z=9.3f;
            Vector3 L(float a,float b,float c)=>P(x+a,b,z+c);
            plaster.Beam(L(-1.78f,1.1f,0),L(1.78f,1.1f,0),2.95f,2.2f,Color.white);
            plaster.Face(L(-1.78f,2.18f,-1.48f),L(1.78f,2.18f,-1.48f),L(0,3.57f,-1.48f),L(0,3.57f,-1.48f),Color.white);
            for(int side=-1;side<=1;side+=2){
                roof.Face(L(side*2.13f,2,-1.9f),L(side*2.13f,2,1.88f),L(0,3.68f,1.88f),L(0,3.68f,-1.9f),Color.white,1.05f);
                wood.Beam(L(side*2.13f,2,-1.91f),L(0,3.68f,-1.91f),.14f,.15f,Color.white);
                wood.Beam(L(side*1.72f,0,-1.52f),L(side*1.72f,2.18f,-1.52f),.16f,.19f,Shade());
                plaster.Beam(L(side*1.09f,.99f,-1.51f),L(side*1.09f,1.75f,-1.51f),.63f,.13f,C("50828c"));
                for(int k=0;k<4;k++)wood.Beam(L(side*1.09f-.29f,1.05f+k*.16f,-1.61f),L(side*1.09f+.29f,1.05f+k*.16f,-1.61f),.07f,.06f,Shade());
            }
            wood.Beam(L(0,0,-1.55f),L(0,1.72f,-1.55f),.96f,.21f,Shade(.56f,.66f));
            for(int i=0;i<5;i++)wood.Beam(L(-.37f+i*.18f,.04f,-1.67f),L(-.37f+i*.18f,1.66f,-1.67f),.169f,.06f,Shade());
            for(int n=0;n<9;n++){
                float angle=n*Mathf.PI/8f;
                Vector3 Arch(float a)=>L(.51f*Mathf.Cos(a),1.55f+Mathf.Sin(a)*.43f,-1.72f);
                rock.Beam(Arch(angle),Arch(angle+Mathf.PI/8f),.17f,.18f,C("bbc9bc"));
            }
            for(int side=-1;side<=1;side+=2){
                for(int row=0;row<3;row++)wood.Beam(L(side*.53f,.22f+row*.48f,-1.69f),L(side*.53f,.62f+row*.48f,-1.69f),.16f,.16f,C("b1a383"));
                for(int row=0;row<4;row++)plaster.Beam(L(side*.69f,.22f+row*.44f,-1.505f),L(side*1.72f,.22f+row*.44f,-1.505f),.042f,.07f,C("68a7a4"));
                wood.Beam(L(side*1.5f,.01f,-2.05f),L(side*1.5f,1.9f,-2.05f),.13f,.13f,Shade());
                wood.Beam(L(side*1.5f,1.45f,-2.05f),L(side*1.17f,1.92f,-2.05f),.09f,.09f,Shade());
            }
            for(int strip=0;strip<14;strip++){
                float u=-1.68f+strip*.24f;
                var tint=strip%4<2?C("428986"):C("e6b86c");
                detail.Face(L(u,2.13f,-1.52f),L(u+.24f,2.13f,-1.52f),L(u+.24f,1.95f,-2.14f),L(u,1.95f,-2.14f),tint);
                detail.Face(L(u,1.95f,-2.14f),L(u+.24f,1.95f,-2.14f),L(u+.22f,1.83f,-2.15f),L(u+.02f,1.83f,-2.15f),tint);
            }
            for(int i=0;i<16;i++)roof.Tube(L(0,3.69f,-1.94f+i*.24f),L(0,3.69f,-1.71f+i*.24f),.115f,.1f,8,Shade());
            for(int step=0;step<2;step++)rock.Beam(L(-.70f,.055f+step*.065f,-2.02f+step*.23f),L(.70f,.055f+step*.065f,-2.02f+step*.23f),.32f,.13f,Shade());
            wood.Beam(L(-1.8f,2.13f,-1.52f),L(1.8f,2.13f,-1.52f),.15f,.18f,Shade());
            wood.Beam(L(-.48f,1.72f,-1.65f),L(.48f,1.72f,-1.65f),.17f,.12f,Shade());
            detail.Tube(L(.28f,.81f,-1.72f),L(.28f,.81f,-1.78f),.055f,.052f,7,C("e3b865"));
        }
        void DockFurniture()
        {
            for(int n=0;n<6;n++){
                var p=P(n<3?-2.12f-n%2*.69f:2.5f+n%2*.65f,.02f,5.9f+n%3*.65f);
                wood.Tube(p,p+Vector3.up*.36f,.27f,.33f,12,Shade());wood.Tube(p+Vector3.up*.36f,p+Vector3.up*.75f,.33f,.27f,12,Shade());
                foreach(float level in new[]{.14f,.58f})detail.Tube(p+Vector3.up*level,p+Vector3.up*(level+.065f),.312f,.316f,12,C("576065"));
            }
            var left=P(-2.2f,0,4.7f);var right=P(2.2f,0,4.7f);
            foreach(var p in new[]{left,right})wood.Tube(p,p+Vector3.up*2.55f,.105f,.075f,8,Shade());
            for(int side=-1;side<=1;side+=2){
                for(int i=0;i<6;i++){var p=P(side*(2.5f+i*.37f),0,5.04f+i*.17f);
                    wood.Tube(p,p+Vector3.up*(.65f+.08f*(i%3)),.08f,.07f,7,Shade());
                    if(i<5){var next=p+P(side*.37f,0,.17f);wood.Beam(p+Vector3.up*.38f,next+Vector3.up*.38f,.06f,.09f,Shade());}}
            }
            var sign=P(1.84f,0,5.28f);wood.Beam(sign,sign+Vector3.up*1.25f,.12f,.12f,Shade());
            for(int board=0;board<4;board++){var a=sign+P(-.28f,.73f+board*.135f,-.08f);wood.Beam(a,a+Vector3.right*.56f,.09f,.13f,Color.white);}
            for(int i=0;i<3;i++){var a=sign+P(-.14f,.81f+i*.095f,-.14f);detail.Beam(a,a+Vector3.right*(.19f+i*.034f),.018f,.025f,C("e5cea1"));}
            Vector3 Cord(float f)=>Vector3.Lerp(left,right,f)+Vector3.up*(2.5f-.28f*Mathf.Sin(f*Mathf.PI));
            Color[] flags={C("c46a4c"),C("e9bc69"),C("4b9992"),C("9c6264")};
            for(int i=0;i<16;i++){var a=Cord(i/16f);var b=Cord((i+1)/16f);detail.Tube(a,b,.023f,.023f,5,C("a9875c"));
                if(i%2==0)detail.Face(a,b,(a+b)*.5f+P(.035f,-.24f,-.04f),a,flags[(i/2)%4]);}
            for(int i=0;i<130;i++){
                float x=Range(-14,14),z=Range(-17,17);var p=P(x,-3.95f,z);float h=Range(.32f,1.8f),r=Range(.28f,.82f);
                rock.Tube(p,p+P(.13f,h,0),r,r*.7f,7,Shade());rock.Tube(p+P(.13f,h,0),p+P(.14f,h+.14f,0),r*.7f,r*.36f,7,Shade());
            }
        }
        void Palm(Vector3 root,float height)
        {
            Vector3 prev=root;
            for(int i=1;i<=14;i++){float f=i/14f;var next=root+P(.5f*f*f,height*f,.18f*f);
                wood.Tube(prev,next,Mathf.Lerp(.22f,.105f,f),Mathf.Lerp(.19f,.075f,f),8,Shade());prev=next;}
            for(int i=0;i<11;i++){float a=i*2.4f;var forward=P(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(forward,Vector3.up);
                Vector3 Tip(float t)=>prev+forward*t*2+Vector3.up*(1.05f*Mathf.Sin(t*2.9f)-.58f*t);
                for(int j=0;j<13;j++){float f=j/13f;var start=Tip(f);var end=Tip((j+1)/13f);
                    detail.Tube(start,end,.032f,.026f,4,C("a7af4d"));
                    foreach(int sign in new[]{-1,1}){var wing=side*sign*(.10f+.28f*Mathf.Sin(f*Mathf.PI));
                        detail.Face(start,end,end+wing+forward*.23f-Vector3.up*.13f,start+wing*.68f+forward*.12f,sign==1?C("72a650"):C("448752"));}
                }
            }
        }
    }
}
