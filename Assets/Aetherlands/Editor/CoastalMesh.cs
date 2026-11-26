using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Aetherlands.Coast.Editor
{
    // One mesh per material, with hard normals per face and world-scale repeating UVs.
    public sealed class CoastalMesh
    {
        readonly List<Vector3> vertices=new();
        readonly List<Vector2> uv=new();
        readonly List<Color32> colors=new();
        readonly List<int> triangles=new();
        public void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint,float scale=.55f)
        {
            int first=vertices.Count;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            float u=Vector3.Distance(a,b)*scale,v=Vector3.Distance(a,d)*scale;
            uv.Add(Vector2.zero);uv.Add(new Vector2(u,0));uv.Add(new Vector2(u,v));uv.Add(new Vector2(0,v));
            if(a==d || Mathf.Abs(Vector3.Cross(b-a,d-a).normalized.y)>.97f && scale<0){
                for(int i=0;i<4;i++){var p=vertices[first+i];uv[first+i]=new Vector2(p.x,p.z)*Mathf.Abs(scale);}
            }
            for(int i=0;i<4;i++)colors.Add(tint);
            triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
        }
        public void Beam(Vector3 a,Vector3 b,float width,float depth,Color tint)
        {
            var axis=(b-a).normalized;
            var side=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.forward:Vector3.up).normalized*width*.5f;
            var top=Vector3.Cross(side.normalized,axis)*depth*.5f;
            var corners=new[]{side+top,-side+top,-side-top,side-top};
            for(int i=0;i<4;i++){var j=(i+1)%4;Face(a+corners[i],b+corners[i],b+corners[j],a+corners[j],tint);}
            Face(a+corners[3],a+corners[2],a+corners[1],a+corners[0],tint);Face(b+corners[0],b+corners[1],b+corners[2],b+corners[3],tint);
        }
        public void Tube(Vector3 a,Vector3 b,float r0,float r1,int count,Color tint)
        {
            Vector3 axis=(b-a).normalized, side=Vector3.Cross(axis,Mathf.Abs(axis.y)>.96f?Vector3.forward:Vector3.up).normalized, up=Vector3.Cross(axis,side);
            for(int i=0;i<count;i++){
                float t=i*Mathf.PI*2/count,s=(i+1)*Mathf.PI*2/count;
                Vector3 n=side*Mathf.Cos(t)+up*Mathf.Sin(t),m=side*Mathf.Cos(s)+up*Mathf.Sin(s);
                Face(a+m*r0,b+m*r1,b+n*r1,a+n*r0,tint);
                Face(b,b+n*r1,b+m*r1,b,tint);
            }
        }
        public Mesh Build(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
