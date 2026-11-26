#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
namespace Aetherlands.Coast.Tests
{
    public sealed class CrossingTests
    {
        const string ScenePath="Assets/Aetherlands/Generated/Scenes/CoastalBenchmark.unity";
        TravelerMotor motor; Keyboard keyboard; Scene scene; Scene previous;
        float priorCaptureDelta;
        InputSettings priorSettings,testSettings;
        [UnitySetUp] public IEnumerator SetUp()
        {
            previous=SceneManager.GetActiveScene();
            priorCaptureDelta=Time.captureDeltaTime;
            priorSettings=InputSystem.settings;
            testSettings=Object.Instantiate(priorSettings);
            testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=testSettings;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            scene=SceneManager.GetSceneByPath(ScenePath);SceneManager.SetActiveScene(scene);
            foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent(out motor))break;
            Assert.IsNotNull(motor);keyboard=InputSystem.AddDevice<Keyboard>();yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            Time.captureDeltaTime=priorCaptureDelta;
            InputSystem.settings=priorSettings;
            Object.Destroy(testSettings);
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid() && scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
        }
        IEnumerator Hold(float seconds,params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();
            yield return new WaitForSeconds(seconds);
        }
        CoastalCamera CameraRig(){foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent<CoastalCamera>(out var camera))return camera;throw new System.InvalidOperationException();}
        IEnumerator WalkTo(float z,Key key)
        {
            float direction=Mathf.Sign(z-motor.transform.position.z);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();
            int frames=0;
            while(direction*(z-motor.transform.position.z)>.04f && frames++<240){
                keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();yield return null;
            }
            yield return Hold(.06f);
            Assert.LessOrEqual(direction*(z-motor.transform.position.z),.05f,"Walk stopped at "+motor.transform.position+" toward "+z+" after "+frames+" frames");
        }
        void Capture(string name,string folder="Docs/Captures/M1",int width=1280,int height=720)
        {
            var cam=CameraRig().GetComponent<Camera>();var old=cam.targetTexture;var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());}
            finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Object.Destroy(pixels);}
        }
        [UnityTest] public IEnumerator KeyboardCrossesReturnsAndStops()
        {
            Time.captureDeltaTime=1f/30;
            yield return WalkTo(-7,Key.S);Capture("Entrance");
            Assert.Less(motor.transform.position.z,-6);
            yield return WalkTo(0,Key.W);Capture("Midspan");Assert.That(motor.transform.position.z,Is.InRange(-2f,2f));
            yield return WalkTo(5.5f,Key.UpArrow);Capture("Landing");
            Assert.Greater(motor.transform.position.z,4.65f);Assert.That(motor.transform.position.y,Is.InRange(-.06f,.08f));
            var stopped=motor.transform.position;yield return Hold(.15f);Assert.Less(Vector3.Distance(stopped,motor.transform.position),.02f);
            Assert.Less(Vector3.Distance(CameraRig().transform.position,motor.transform.position+CameraRig().offset),.03f);
            yield return WalkTo(-8.3f,Key.S);Assert.Less(motor.transform.position.z,-7f);
        }
        [UnityTest] public IEnumerator DiagonalMovementAndRails()
        {
            motor.enabled=false;var start=motor.transform.position;
            for(int i=0;i<10;i++)motor.Step(Vector2.one,.02f);var delta=motor.transform.position-start;
            Assert.AreEqual(motor.recipe.speed*.2f,new Vector2(delta.x,delta.z).magnitude,.035f);
            for(int i=0;i<80;i++)motor.Step(Vector2.right,.02f);
            Assert.That(motor.transform.position.x,Is.InRange(1.2f,1.5f));
            for(int i=0;i<100;i++)motor.Step(Vector2.left,.02f);
            Assert.That(motor.transform.position.x,Is.InRange(-1.5f,-1.2f));yield return null;
        }
        [UnityTest] public IEnumerator ProfileWarmCrossingAndRecordMovement()
        {
            motor.enabled=false;
            for(int i=0;i<95;i++){motor.Step(Vector2.down,.015f);yield return null;}
            var frameTimes=new float[90];long bytes=0;
            int textures=Resources.FindObjectsOfTypeAll<Texture2D>().Length,meshes=Resources.FindObjectsOfTypeAll<Mesh>().Length;
            using(var gc=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame")){
                for(int i=0;i<frameTimes.Length;i++){motor.Step(Vector2.up,.015f);yield return null;frameTimes[i]=Time.unscaledDeltaTime*1000;if(gc.Valid)bytes+=gc.LastValue;}
                System.Array.Sort(frameTimes);
                var report=string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Unity {0}; {1}; {2}\n90 Editor frames; median {3:F2} ms; p95 {4:F2} ms; whole-Editor GC bytes/frame {5}\nMesh count {6} -> {7}; Texture2D count {8} -> {9}\n",
                    Application.unityVersion,SystemInfo.processorType,SystemInfo.graphicsDeviceName,frameTimes[45],frameTimes[85],gc.Valid?(bytes/90).ToString():"unavailable",
                    meshes,Resources.FindObjectsOfTypeAll<Mesh>().Length,textures,Resources.FindObjectsOfTypeAll<Texture2D>().Length);
                Directory.CreateDirectory("Docs/Captures/M1");File.WriteAllText("Docs/Captures/M1/EditorProfile.txt",report);
            }
            for(int i=0;i<20;i++){
                motor.Step(new Vector2(.08f,1),.08f);yield return null;
                Capture(i.ToString("D2"),"Temp/M1Motion",640,360);
            }
        }
    }
}
#endif
