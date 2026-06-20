#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GreySkies
{
    public static class PropPrefabGenerator
    {
        private const string PrefabFolder = "Assets/GreySkies/Prefabs/Props";
        private const string MaterialFolder = "Assets/GreySkies/Materials/Props";

        [MenuItem("Grey Skies/Generate Prop Prefabs")]
        public static void GeneratePrefabs()
        {
            // Ensure directories exist
            EnsureDirectoryExists(PrefabFolder);
            EnsureDirectoryExists(MaterialFolder);

            // Shaders
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                litShader = Shader.Find("Standard");
            }

            // Create Materials
            Material timberMat = CreateMaterial("TudorTimber", "#52362C", litShader);
            Material plasterMat = CreateMaterial("CreamPlaster", "#C2B29A", litShader);
            Material mossMat = CreateMaterial("Moss", "#3A4138", litShader);
            Material greenMat = CreateMaterial("RacingGreen", "#424E43", litShader);
            Material rustMat = CreateMaterial("Rust", "#7A664B", litShader);
            Material redMat = CreateMaterial("FadedRed", "#A53F2B", litShader);
            Material ivyMat = CreateMaterial("Ivy", "#3A4138", litShader);
            Material brambleMat = CreateMaterial("BrambleGreen", "#2F332C", litShader);
            Material barkMat = CreateMaterial("DarkBark", "#4E4237", litShader);

            // 1. KentishTudorPub (15m x 12m x 7.5m)
            CreateTudorPubPrefab(plasterMat, timberMat, mossMat);

            // 2. RustedRoverMini (3.1m x 1.4m x 1.3m)
            CreateRoverMiniPrefab(greenMat, rustMat);

            // 3. K6RedTelephoneBox (1m x 1m x 2.4m)
            CreateTelephoneBoxPrefab(redMat, ivyMat);

            // 4. OvergrownHawthornHedgerow (3m x 1.2m x 2.5m)
            CreateHedgerowPrefab(brambleMat, barkMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PropPrefabGenerator] All prop prefabs successfully generated!");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static Material CreateMaterial(string name, string hexColor, Shader shader)
        {
            string matPath = $"{MaterialFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Color color;
            if (ColorUtility.TryParseHtmlString(hexColor, out color))
            {
                mat.color = color;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateTudorPubPrefab(Material plaster, Material timber, Material moss)
        {
            GameObject root = new GameObject("KentishTudorPub");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(15f, 7.5f, 12f);
            col.center = new Vector3(0f, 3.75f, 0f);

            // Plaster body
            GameObject plasterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plasterObj.name = "PlasterBody";
            plasterObj.transform.SetParent(root.transform);
            plasterObj.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            plasterObj.transform.localScale = new Vector3(15f, 7f, 12f);
            plasterObj.GetComponent<Renderer>().sharedMaterial = plaster;

            // Moss base
            GameObject mossObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mossObj.name = "MossyBase";
            mossObj.transform.SetParent(root.transform);
            mossObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            mossObj.transform.localScale = new Vector3(15.2f, 0.5f, 12.2f);
            mossObj.GetComponent<Renderer>().sharedMaterial = moss;

            // Timber bottom beam
            GameObject bBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bBeam.name = "TimberBottomBeam";
            bBeam.transform.SetParent(root.transform);
            bBeam.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            bBeam.transform.localScale = new Vector3(15.1f, 0.3f, 12.1f);
            bBeam.GetComponent<Renderer>().sharedMaterial = timber;

            // Timber top beam
            GameObject tBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tBeam.name = "TimberTopBeam";
            tBeam.transform.SetParent(root.transform);
            tBeam.transform.localPosition = new Vector3(0f, 6.85f, 0f);
            tBeam.transform.localScale = new Vector3(15.1f, 0.3f, 12.1f);
            tBeam.GetComponent<Renderer>().sharedMaterial = timber;

            // Vertical timber pillars
            Vector3[] pillars = new Vector3[]
            {
                new Vector3(-7.4f, 3.75f, -5.9f),
                new Vector3(7.4f, 3.75f, -5.9f),
                new Vector3(-7.4f, 3.75f, 5.9f),
                new Vector3(7.4f, 3.75f, 5.9f)
            };

            for (int i = 0; i < pillars.Length; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"TimberPillar_{i}";
                pillar.transform.SetParent(root.transform);
                pillar.transform.localPosition = pillars[i];
                pillar.transform.localScale = new Vector3(0.3f, 6f, 0.3f);
                pillar.GetComponent<Renderer>().sharedMaterial = timber;
            }

            SaveAndDestroy(root, "KentishTudorPub");
        }

        private static void CreateRoverMiniPrefab(Material green, Material rust)
        {
            GameObject root = new GameObject("RustedRoverMini");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.3f, 3.1f); // Width, Height, Length
            col.center = new Vector3(0f, 0.65f, 0f);

            // Car Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CarBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(1.4f, 0.6f, 3.1f);
            body.GetComponent<Renderer>().sharedMaterial = green;

            // Cabin
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(root.transform);
            cabin.transform.localPosition = new Vector3(0f, 1.1f, -0.2f);
            cabin.transform.localScale = new Vector3(1.2f, 0.5f, 1.8f);
            cabin.GetComponent<Renderer>().sharedMaterial = green;

            // Wheels
            Vector3[] wheelPositions = new Vector3[]
            {
                new Vector3(-0.65f, 0.25f, 0.9f),
                new Vector3(0.65f, 0.25f, 0.9f),
                new Vector3(-0.65f, 0.25f, -0.9f),
                new Vector3(0.65f, 0.25f, -0.9f)
            };

            for (int i = 0; i < wheelPositions.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i}";
                wheel.transform.SetParent(root.transform);
                wheel.transform.localPosition = wheelPositions[i];
                wheel.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.GetComponent<Renderer>().sharedMaterial = rust;
            }

            SaveAndDestroy(root, "RustedRoverMini");
        }

        private static void CreateTelephoneBoxPrefab(Material red, Material ivy)
        {
            GameObject root = new GameObject("K6RedTelephoneBox");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 2.4f, 1f);
            col.center = new Vector3(0f, 1.2f, 0f);

            // Red body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "TelephoneBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            body.transform.localScale = new Vector3(0.95f, 2.3f, 0.95f);
            body.GetComponent<Renderer>().sharedMaterial = red;

            // Dome top
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dome.name = "TelephoneDome";
            dome.transform.SetParent(root.transform);
            dome.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            dome.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);
            dome.GetComponent<Renderer>().sharedMaterial = red;

            // Ivy crawl
            GameObject ivyCrawl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ivyCrawl.name = "IvyCrawl";
            ivyCrawl.transform.SetParent(root.transform);
            ivyCrawl.transform.localPosition = new Vector3(0.4f, 0.9f, 0.4f);
            ivyCrawl.transform.localScale = new Vector3(0.2f, 1.8f, 0.2f);
            ivyCrawl.GetComponent<Renderer>().sharedMaterial = ivy;

            SaveAndDestroy(root, "K6RedTelephoneBox");
        }

        private static void CreateHedgerowPrefab(Material bramble, Material bark)
        {
            GameObject root = new GameObject("OvergrownHawthornHedgerow");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(3f, 2.5f, 1.2f);
            col.center = new Vector3(0f, 1.25f, 0f);

            // Foliage
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foliage.name = "Foliage";
            foliage.transform.SetParent(root.transform);
            foliage.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            foliage.transform.localScale = new Vector3(3f, 2f, 1.2f);
            foliage.GetComponent<Renderer>().sharedMaterial = bramble;

            // Main trunk
            GameObject trunk1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk1.name = "Trunk_Main";
            trunk1.transform.SetParent(root.transform);
            trunk1.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            trunk1.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
            trunk1.GetComponent<Renderer>().sharedMaterial = bark;

            // Side trunk L
            GameObject trunk2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk2.name = "Trunk_Left";
            trunk2.transform.SetParent(root.transform);
            trunk2.transform.localPosition = new Vector3(-1f, 0.3f, 0f);
            trunk2.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
            trunk2.GetComponent<Renderer>().sharedMaterial = bark;

            // Side trunk R
            GameObject trunk3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk3.name = "Trunk_Right";
            trunk3.transform.SetParent(root.transform);
            trunk3.transform.localPosition = new Vector3(1f, 0.3f, 0f);
            trunk3.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
            trunk3.GetComponent<Renderer>().sharedMaterial = bark;

            SaveAndDestroy(root, "OvergrownHawthornHedgerow");
        }

        private static void SaveAndDestroy(GameObject obj, string name)
        {
            // Clear colliders on children to only have the main root collider
            Collider[] childColliders = obj.GetComponentsInChildren<Collider>();
            foreach (var childCol in childColliders)
            {
                if (childCol.gameObject != obj)
                {
                    Object.DestroyImmediate(childCol);
                }
            }

            string prefabPath = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
        }
    }
}
#endif