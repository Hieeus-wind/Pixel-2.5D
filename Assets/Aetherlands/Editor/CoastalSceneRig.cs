using Aetherlands.Fishing;
using Aetherlands.Networking;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Aetherlands.Coast.Editor
{
    static class CoastalSceneRig
    {
        static void Barrier(Transform parent,string name,Vector3 pos,Vector3 size,float yaw=0)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent);obj.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));obj.AddComponent<BoxCollider>().size=size;
        }
        static void Landing(Transform parent,float center,float radius,float length,bool north)
        {
            Barrier(parent,"Landing floor",new Vector3(0,-.2f,center),new Vector3(radius*2,.4f,length*2));
            for(int i=0;i<40;i++){
                float a=i*2*Mathf.PI/40,b=(i+1)*2*Mathf.PI/40;
                Vector3 At(float t)=>new Vector3(Mathf.Cos(t)*radius,.48f,center+Mathf.Sin(t)*length);
                var start=At(a);var end=At(b);var mid=(start+end)*.5f;
                if(Mathf.Abs(mid.x)<1.64f && (north?mid.z<center:mid.z>center))continue;
                var axis=end-start;Barrier(parent,"Cliff edge",mid,new Vector3(.15f,1,axis.magnitude+.09f),Mathf.Atan2(axis.x,axis.z)*Mathf.Rad2Deg);
            }
        }
        public static void Install(
            CoastalRecipe recipe,
            Sprite[] sprites,
            Sprite footShadow,
            FishingConfig fishingConfig=null,
            CoastalSessionConfig sessionConfig=null,
            NetworkObject travelerNetPrefab=null,
            NetworkObject worldRecordNetPrefab=null)
        {
            var collision=new GameObject("Walkable coast").transform;
            Barrier(collision,"Deck",new Vector3(0,-.16f,-1.7f),new Vector3(3.35f,.32f,13.2f));
            foreach(int side in new[]{-1,1})Barrier(collision,"Rope railing",new Vector3(side*1.64f,.5f,-1.8f),new Vector3(.14f,1,12.5f));
            Landing(collision,8,5.85f,5.05f,true);Landing(collision,-12,6.2f,4.95f,false);
            Barrier(collision,"Station walls",new Vector3(-.65f,1,9.3f),new Vector3(3.62f,2,3.22f));
            for(int n=0;n<6;n++){
                float x=n<3?-2.12f-n%2*.69f:2.5f+n%2*.65f;
                Barrier(collision,"Cargo barrel",new Vector3(x,.38f,5.9f+n%3*.65f),new Vector3(.52f,.76f,.52f));
            }
            Barrier(collision,"Noticeboard",new Vector3(1.84f,.57f,5.28f),new Vector3(.51f,1.14f,.19f));
            foreach(var trunk in new[]{new Vector3(-3.9f,1,7),new Vector3(4.4f,1,9.3f),new Vector3(-3.8f,1,-10)})
                Barrier(collision,"Palm trunk",trunk,new Vector3(.39f,2,.39f));
            var light=new GameObject("Afternoon sun").AddComponent<Light>();light.type=LightType.Directional;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(48,-32,0);light.intensity=1.1f;
            var avatar=new GameObject("Coastal traveler");avatar.transform.position=new Vector3(0,.015f,.9f);
            var controller=avatar.AddComponent<CharacterController>();controller.radius=.22f;controller.height=1.35f;controller.center=Vector3.up*.675f;controller.skinWidth=.025f;controller.minMoveDistance=0;controller.stepOffset=.12f;
            var picture=new GameObject("Traveler sprite");picture.transform.SetParent(avatar.transform,false);
            Quaternion angle=Quaternion.Euler(37,-9,0);picture.transform.rotation=angle;
            var sprite=picture.AddComponent<SpriteRenderer>();sprite.sprite=sprites[0];sprite.sharedMaterial=UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            var shade=new GameObject("Contact shadow");shade.transform.SetParent(avatar.transform,false);
            shade.transform.localPosition=new Vector3(0,.052f,-.19f);shade.transform.localRotation=Quaternion.Euler(90,0,0);shade.transform.localScale=new Vector3(.78f,.64f,1);
            var shadow=shade.AddComponent<SpriteRenderer>();shadow.sprite=footShadow;shadow.sharedMaterial=sprite.sharedMaterial;shadow.sortingOrder=-1;
            var view=avatar.AddComponent<TravelerView>();view.portrait=sprite;view.frames=sprites;
            var motor=avatar.AddComponent<TravelerMotor>();motor.recipe=recipe;motor.view=view;
            var visualsObj=new GameObject("FishingWorldVisuals");visualsObj.transform.SetParent(avatar.transform,false);
            var visuals=visualsObj.AddComponent<FishingWorldVisuals>();
            var fishing=avatar.AddComponent<FishingController>();fishing.config=fishingConfig;fishing.travelerMotor=motor;fishing.worldVisuals=visuals;fishing.enableLocalInput=true;fishing.blockTravelerMotorWhileFishing=true;
            var camera=new GameObject("Coastal camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=recipe.cameraSize;camera.nearClipPlane=.2f;camera.farClipPlane=90;
            var follow=camera.gameObject.AddComponent<CoastalCamera>();follow.target=avatar.transform;follow.offset=angle*(Vector3.back*21)+Vector3.forward*3.5f;
            camera.transform.SetPositionAndRotation(avatar.transform.position+follow.offset,angle);
            camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;camera.gameObject.AddComponent<AudioListener>();
            var sessionObj=new GameObject("MultiplayerSession");
            var runner=sessionObj.AddComponent<CoastalSessionRunner>();
            runner.ConfigureSceneReferences(sessionConfig,travelerNetPrefab,worldRecordNetPrefab,avatar,follow);
            var hud=sessionObj.AddComponent<MultiplayerHud>();
            hud.SetSessionRunner(runner);
            EditorUtility.SetDirty(runner);
            EditorUtility.SetDirty(hud);
        }
    }
}
