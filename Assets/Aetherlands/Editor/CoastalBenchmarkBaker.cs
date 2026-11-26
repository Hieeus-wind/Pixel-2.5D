using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Aetherlands.Fishing;
using Aetherlands.Fishing.Editor;
using Aetherlands.Networking;
using Aetherlands.Networking.Editor;
using Fusion;
using UnityEngine.U2D;
using UnityEditor.U2D;
namespace Aetherlands.Coast.Editor
{
    public static class CoastalBenchmarkBaker
    {
        public const string Root="Assets/Aetherlands/Generated";
        public const string ScenePath=Root+"/Scenes/CoastalBenchmark.unity";
        const string ManifestPath=Root+"/CoastalManifest.json";
        const string RecipePath="Assets/Aetherlands/Recipes/CoastalCrossing.asset";
        [Serializable] sealed class Manifest {public string owner="AetherForge.CoastalCrossing"; public int version,seed; public string digest,rigDigest;public bool complete;public string[] paths;}
        static readonly string[] Kinds={"Wood","Stone","Grass","Roof","Sand","Plaster","Plain"};
        static string PathFor(string name,string ext)=>Root+"/"+name+"."+ext;
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        [MenuItem("Tools/Aetherlands/Bake Coastal Benchmark")]
        public static void Bake()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before baking.");
            Folder("Assets/Aetherlands/Recipes");Folder(Root);Folder(Root+"/Scenes");
            var recipe=AssetDatabase.LoadAssetAtPath<CoastalRecipe>(RecipePath);
            if(!recipe){if(File.Exists(RecipePath))throw new IOException(RecipePath);recipe=ScriptableObject.CreateInstance<CoastalRecipe>();AssetDatabase.CreateAsset(recipe,RecipePath);}
            var paths=new List<string>{ScenePath,PathFor("Water","mat"),PathFor("Water","asset")};
            var atlasPath=PathFor("TravelerAtlas","spriteatlas");
            var shadowPath=PathFor("FootShadow","png");paths.Add(atlasPath);paths.Add(shadowPath);
            foreach(string kind in Kinds)foreach(string ext in new[]{"png","mat","asset"})paths.Add(PathFor(kind,ext));
            for(int i=0;i<12;i++)paths.Add(PathFor("Traveler"+i,"png"));
            var previous=File.Exists(ManifestPath)?JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath)):null;
            if(previous!=null && previous.owner!="AetherForge.CoastalCrossing")throw new IOException("Unexpected manifest owner");
            foreach(var p in paths)if(File.Exists(p) && (previous?.paths==null || !previous.paths.Contains(p)))throw new IOException("Unowned output: "+p);
            var m2AssetPaths=new[]{FishingCatalogBaker.ConfigAssetPath,NetworkPrefabBaker.SessionConfigPath,NetworkPrefabBaker.TravelerPrefabPath,NetworkPrefabBaker.WorldRecordPrefabPath};
            bool hasM2Assets=m2AssetPaths.All(File.Exists);
            var digest=Hash128.Compute(EditorJsonUtility.ToJson(recipe)+string.Join("",Directory.GetFiles("Assets/Aetherlands","*.cs",SearchOption.AllDirectories).Where(p=>!p.Contains("Tests")).OrderBy(p=>p,StringComparer.Ordinal).Select(File.ReadAllText))+string.Join("",Directory.GetFiles("Assets/Aetherlands","*.shader",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).Select(File.ReadAllText))).ToString();
            if(previous!=null && previous.complete && previous.digest==digest && paths.All(File.Exists) && hasM2Assets){Debug.Log("Coastal crossing is up to date.");return;}
            var active=SceneManager.GetActiveScene();
            if(active.path==ScenePath && active.isDirty)throw new InvalidOperationException("Save or discard changes to the benchmark scene first.");
            var manifest=new Manifest{version=CoastalRecipe.Revision,seed=recipe.seed,digest=digest,paths=paths.ToArray()};
            void SaveManifest(){File.WriteAllText(ManifestPath,JsonUtility.ToJson(manifest,true));AssetDatabase.ImportAsset(ManifestPath);}
            string ComputeRigDigest()=>Hash128.Compute(File.ReadAllText("Assets/Aetherlands/Editor/CoastalSceneRig.cs")+recipe.cameraSize.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+string.Join("",paths.Select(AssetDatabase.AssetPathToGUID))+string.Join("",m2AssetPaths.Select(AssetDatabase.AssetPathToGUID))).ToString();
            SaveManifest();
            var landscape=new CoastalLandscape(recipe.seed);
            var meshes=new Dictionary<string,Mesh>();var materials=new Dictionary<string,Material>();
            foreach(var kind in Kinds){
                Texture(PathFor(kind,"png"),CoastalPixels.Surface(kind,recipe.seed,recipe.timber),false);
                var material=Material(kind,"Aetherlands/Coastal Surface");
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(PathFor(kind,"png"));EditorUtility.SetDirty(material);
                materials.Add(kind,material);meshes.Add(kind,SaveMesh(kind,landscape.surfaces[kind]));
            }
            var water=new CoastalMesh();water.Face(new Vector3(-60,-2,-60),new Vector3(-60,-2,60),new Vector3(60,-2,60),new Vector3(60,-2,-60),Color.white);
            meshes.Add("Water",SaveMesh("Water",water));materials.Add("Water",Material("Water","Aetherlands/Tidal Glass"));
            var sprites=new Sprite[12];for(int i=0;i<12;i++){string path=PathFor("Traveler"+i,"png");Texture(path,CoastalPixels.Traveler(i/3,i%3),true);sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);}
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if(!atlas){
                atlas=new SpriteAtlas();var texture=atlas.GetTextureSettings();texture.filterMode=FilterMode.Point;texture.generateMipMaps=false;atlas.SetTextureSettings(texture);
                var packing=atlas.GetPackingSettings();packing.padding=4;packing.enableRotation=false;atlas.SetPackingSettings(packing);
                atlas.Add(sprites);AssetDatabase.CreateAsset(atlas,atlasPath);
            }
            var penumbra=new PixelCanvas(32,20);
            for(int y=0;y<20;y++)for(int x=0;x<32;x++){
                float d=new Vector2((x-15.5f)/15,(y-9.5f)/9).sqrMagnitude;
                if(d<1)penumbra.Dot(x,y,new Color32(43,38,48,(byte)(94*(1-d))));
            }
            Texture(shadowPath,penumbra,true);
            manifest.rigDigest=ComputeRigDigest();
            AssetDatabase.SaveAssets();
            if(previous!=null && previous.complete && manifest.rigDigest==previous.rigDigest && File.Exists(ScenePath) && hasM2Assets){manifest.complete=true;SaveManifest();return;}
            var fishingConfig=AssetDatabase.LoadAssetAtPath<FishingConfig>(FishingCatalogBaker.ConfigAssetPath);
            if(!fishingConfig)fishingConfig=FishingCatalogBaker.Bake();
            var sessionConfig=AssetDatabase.LoadAssetAtPath<CoastalSessionConfig>(NetworkPrefabBaker.SessionConfigPath);
            var travelerNetPrefab=AssetDatabase.LoadAssetAtPath<NetworkObject>(NetworkPrefabBaker.TravelerPrefabPath);
            var worldRecordNetPrefab=AssetDatabase.LoadAssetAtPath<NetworkObject>(NetworkPrefabBaker.WorldRecordPrefabPath);
            if(!sessionConfig || !travelerNetPrefab || !worldRecordNetPrefab){
                NetworkPrefabBaker.BakeAll();
                sessionConfig=AssetDatabase.LoadAssetAtPath<CoastalSessionConfig>(NetworkPrefabBaker.SessionConfigPath);
                travelerNetPrefab=AssetDatabase.LoadAssetAtPath<NetworkObject>(NetworkPrefabBaker.TravelerPrefabPath);
                worldRecordNetPrefab=AssetDatabase.LoadAssetAtPath<NetworkObject>(NetworkPrefabBaker.WorldRecordPrefabPath);
            }
            manifest.rigDigest=ComputeRigDigest();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try {
                foreach(var item in meshes){var obj=new GameObject(item.Key);obj.AddComponent<MeshFilter>().sharedMesh=item.Value;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[item.Key];
                    if(item.Key=="Water")renderer.shadowCastingMode=ShadowCastingMode.Off;}
                CoastalSceneRig.Install(recipe,sprites,AssetDatabase.LoadAssetAtPath<Sprite>(shadowPath),fishingConfig,sessionConfig,travelerNetPrefab,worldRecordNetPrefab);
                if(active.path==ScenePath)EditorSceneManager.CloseScene(active,true);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save "+ScenePath);
                manifest.complete=true;SaveManifest();
            } finally {
                if(active.IsValid() && active.isLoaded){SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
            }
            Debug.Log("Baked coastal crossing at "+ScenePath);
        }
        static void Texture(string path,PixelCanvas canvas,bool sprite)
        {
            var temporary=new Texture2D(canvas.width,canvas.height,TextureFormat.RGBA32,false);
            byte[] bytes;
            try{temporary.SetPixels32(canvas.pixels);temporary.Apply();bytes=temporary.EncodeToPNG();}finally{UnityEngine.Object.DestroyImmediate(temporary);}
            if(File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes))return;
            File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
            if(sprite){importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.BottomCenter;importer.SetTextureSettings(settings);}
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=sprite?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        static Material Material(string name,string shaderName)
        {
            var shader=Shader.Find(shaderName);if(!shader)throw new InvalidOperationException(shaderName);
            string path=PathFor(name,"mat");var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
            return m;
        }
        static Mesh SaveMesh(string name,CoastalMesh source)
        {
            string path=PathFor(name,"asset");var mesh=source.Build(name);var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!existing){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }
    }
}
