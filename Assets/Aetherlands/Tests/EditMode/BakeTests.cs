using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Aetherlands.Coast.Editor;
namespace Aetherlands.Coast.Tests
{
    public sealed class BakeTests
    {
        [Test] public void SeedAndPaletteControlPixelOutput()
        {
            var a=CoastalPixels.Surface("Wood",18,Color.gray).pixels;
            CollectionAssert.AreEqual(a,CoastalPixels.Surface("Wood",18,Color.gray).pixels);
            CollectionAssert.AreNotEqual(a,CoastalPixels.Surface("Wood",19,Color.gray).pixels);
            CollectionAssert.AreNotEqual(a,CoastalPixels.Surface("Wood",18,Color.red).pixels);
            CollectionAssert.AreEqual(CoastalPixels.Surface("Stone",18,Color.gray).pixels,CoastalPixels.Surface("Stone",18,Color.red).pixels);
        }
        [Test] public void CliffGeometryRepeatsForTheSameSeed()
        {
            var a=new CoastalLandscape(7319).surfaces["Stone"].Build("a");var b=new CoastalLandscape(7319).surfaces["Stone"].Build("b");
            try{CollectionAssert.AreEqual(a.vertices,b.vertices);CollectionAssert.AreEqual(a.triangles,b.triangles);}
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
        [Test] public void RepeatedBakePreservesAllOutputBytesAndGuids()
        {
            string Snapshot()=>string.Join("|",Directory.GetFiles(CoastalBenchmarkBaker.Root,"*",SearchOption.AllDirectories).OrderBy(p=>p,System.StringComparer.Ordinal).Select(p=>p+":"+Digest(p)));
            var before=Snapshot();CoastalBenchmarkBaker.Bake();Assert.AreEqual(before,Snapshot());
        }
        static string Digest(string path)
        {
            using(var sha=System.Security.Cryptography.SHA256.Create())
                return System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
        [Test] public void TimberRebakeChangesOnlyWoodPixelsAndManifest()
        {
            var recipe=AssetDatabase.LoadAssetAtPath<CoastalRecipe>("Assets/Aetherlands/Recipes/CoastalCrossing.asset");
            var original=recipe.timber;
            var snapshot=Directory.GetFiles(CoastalBenchmarkBaker.Root,"*",SearchOption.AllDirectories).ToDictionary(p=>p,Digest);
            try{
                recipe.timber=original*.8f;CoastalBenchmarkBaker.Bake();
                var changed=snapshot.Keys.Where(p=>Digest(p)!=snapshot[p]).Select(Path.GetFileName);
                CollectionAssert.AreEquivalent(new[]{"Wood.png","CoastalManifest.json"},changed);
            } finally {recipe.timber=original;CoastalBenchmarkBaker.Bake();}
        }
        [TestCase("Aetherlands/Tidal Glass")][TestCase("Aetherlands/Coastal Surface")]
        public void CoastalShadersCompile(string name){var shader=Shader.Find(name);Assert.IsNotNull(shader);Assert.IsFalse(ShaderUtil.ShaderHasError(shader));}
    }
}
