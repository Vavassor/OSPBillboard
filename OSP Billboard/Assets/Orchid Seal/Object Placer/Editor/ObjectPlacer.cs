using UnityEditor;
using UnityEngine;

namespace OrchidSeal.ObjectPlacer
{
    public class ObjectPlacer : EditorWindow
    {
        enum Formation
        {
            Block,
            Random,
            Circle,
        }

        enum RotationRandomization
        {
            RandomXZ,
            RandomXYZ,
            NoRotation,
        }

        private Vector3Int blockObjectCount = new (10, 1, 10);
        private Vector3 blockObjectSpacing = new (0.5f, 0.5f, 0.5f);
        private int circleCount = 10;
        private float circleSpacing = 2.0f;
        private float circleInnerRadius = 4.0f;
        private float circleFacingAngle = 180.0f;
        private Formation formation;
        private int maxCount = 1000;
        private const int maxCountLimit = 50000;
        private int placeCount;
        private Transform placementRoot;
        private const string placementRootName = "OSP Object Placement Root";
        private Transform placementTransform;
        private GameObject prefab;
        private bool randomInXZPlane;
        private Vector3 randomVolume = new (10.0f, 10.0f, 10.0f);
        private RotationRandomization rotationRandomization = RotationRandomization.RandomXYZ;
        private bool shouldClearExistingObjects = true;

        private static class Styles
        {
            public static readonly GUIContent blockObjectCountFieldLabel = new ("Count");
            public static readonly GUIContent blockObjectSpacingFieldLabel = new ("Spacing");
            public static readonly GUIContent circleCountFieldLabel = new ("Rings");
            public static readonly GUIContent circleInnerRadiusFieldLabel = new ("Inner Radius");
            public static readonly GUIContent circleSpacingFieldLabel = new ("Ring Spacing");
            public static readonly GUIContent circleFacingAngleFieldLabel = new ("Facing Angle");
            public static readonly GUIContent clearExistingObjectsToggleLabel = new ("Clear existing objects");
            public static readonly GUIContent formationFieldLabel = new ("Formation");
            public static readonly GUIContent invalidPrefabError = new("Please enter a valid prefab.");
            public static readonly GUIContent maxCountFieldLabel = new ("Max Count");
            public static readonly GUIContent parentTransformFieldLabel = new ("Parent Transform");
            public static readonly GUIContent placeButtonLabel = new ("Place");
            public static readonly GUIContent prefabFieldLabel = new ("Prefab");
            public static readonly GUIContent randomInXZPlaneToggleLabel = new("XZ Plane");
            public static readonly GUIContent randomRotationFieldLabel = new("Rotation");
            public static readonly GUIContent randomVolumeFieldLabel = new("Volume");
            public static readonly GUIContent removePlacedObjectsButtonLabel = new ("Clear Objects");
            public static readonly GUIContent windowTitle = new ("Object Placer");

            public const int sectionMargin = 12;

            public static readonly GUIStyle formError = new ()
            {
                margin = new RectOffset(4, 4, 4, 16),
                normal =
                {
                    textColor = new Color(1.0f, 0.3f, 0.3f),
                }
            };
        }

        [MenuItem ("Window/Orchid Seal/Object Placer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(ObjectPlacer));
        }

        private void OnEnable()
        {
            titleContent = Styles.windowTitle;
        }

        private void OnGUI()
        {
            var hasFormError = false;

            prefab = (GameObject) EditorGUILayout.ObjectField(Styles.prefabFieldLabel, prefab, typeof(GameObject), false);
            if (!prefab)
            {
                FormError(Styles.invalidPrefabError);
                hasFormError = true;
            }

            placementTransform = (Transform) EditorGUILayout.ObjectField(Styles.parentTransformFieldLabel, placementTransform, typeof(Transform), true);
            maxCount = Mathf.Clamp(EditorGUILayout.IntField(Styles.maxCountFieldLabel, maxCount), 0, maxCountLimit);

            GUILayout.Space(Styles.sectionMargin);
            formation = (Formation) EditorGUILayout.EnumPopup(Styles.formationFieldLabel, formation);

            switch (formation)
            {
                case Formation.Block:
                {
                    blockObjectCount = EditorGUILayout.Vector3IntField(Styles.blockObjectCountFieldLabel, blockObjectCount);
                    blockObjectSpacing = EditorGUILayout.Vector3Field(Styles.blockObjectSpacingFieldLabel, blockObjectSpacing);
                    break;
                }
                case Formation.Circle:
                {
                    circleCount = EditorGUILayout.IntField(Styles.circleCountFieldLabel, circleCount);
                    circleInnerRadius = EditorGUILayout.FloatField(Styles.circleInnerRadiusFieldLabel, circleInnerRadius);
                    circleSpacing = EditorGUILayout.FloatField(Styles.circleSpacingFieldLabel, circleSpacing);
                    circleFacingAngle = EditorGUILayout.Slider(Styles.circleFacingAngleFieldLabel, circleFacingAngle, 0.0f, 360.0f);
                    break;
                }
                case Formation.Random:
                {
                    randomVolume = EditorGUILayout.Vector3Field(Styles.randomVolumeFieldLabel, randomVolume);
                    randomInXZPlane = EditorGUILayout.Toggle(Styles.randomInXZPlaneToggleLabel, randomInXZPlane);
                    rotationRandomization = (RotationRandomization) EditorGUILayout.EnumPopup(Styles.randomRotationFieldLabel, rotationRandomization);
                    break;
                }
            }

            GUILayout.Space(Styles.sectionMargin);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(Styles.placeButtonLabel) && !hasFormError)
            {
                if (shouldClearExistingObjects) ClearObjects();

                placementRoot = FindOrCreateObject(placementTransform, placementRootName).transform;
                placeCount = 0;

                switch (formation)
                {
                    case Formation.Block:
                        PlaceBlock();
                        break;
                    case Formation.Circle:
                        PlaceCircle();
                        break;
                    case Formation.Random:
                        PlaceRandom();
                        break;
                }
            }

            bool canRemove = placementRoot;
            EditorGUI.BeginDisabledGroup(!canRemove);

            if (GUILayout.Button(Styles.removePlacedObjectsButtonLabel, GUILayout.ExpandWidth(false)) && canRemove)
            {
                ClearObjects();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            shouldClearExistingObjects = EditorGUILayout.Toggle(Styles.clearExistingObjectsToggleLabel, shouldClearExistingObjects);
        }

        private static GameObject FindOrCreateObject(Transform parent, string objectName)
        {
            if (parent)
            {
                var foundTransform = parent.Find(objectName);
                if (foundTransform) return foundTransform.gameObject;
            }
            else
            {
                var foundObject = GameObject.Find("/" + objectName);
                if (foundObject) return foundObject;
            }
            var newObject = new GameObject(objectName);
            newObject.transform.SetParent(parent, false);
            return newObject;
        }

        private void ClearObjects()
        {
            if (!placementRoot) return;
            // for(var i = placementRoot.childCount - 1; i >= 0; i--)
            // {
            //     var gameObj = placementRoot.GetChild(i).gameObject;
            //     var objectPrefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObj);
            //     if (objectPrefab == prefab) Undo.DestroyObjectImmediate(gameObj);
            // }
            Undo.DestroyObjectImmediate(placementRoot.gameObject);
            placementRoot = null;
        }

        private static void FormError(GUIContent content)
        {
            GUILayout.Label(content, Styles.formError);
        }

        private void PlaceBlock()
        {
            for (var z = 0; z < blockObjectCount.z; z++)
            {
                for (var y = 0; y < blockObjectCount.y; y++)
                {
                    for (var x = 0; x < blockObjectCount.x; x++)
                    {
                        if (placeCount >= maxCount) return;
                        var localPosition = Vector3.Scale(blockObjectSpacing, new Vector3(x, y, z));
                        PlaceObject(prefab, localPosition, Quaternion.identity, placementRoot);
                    }
                }
            }
        }

        private void PlaceCircle()
        {
            var ringEnd = circleInnerRadius + circleSpacing * circleCount;
            var radiusSum = SumArithmetic(circleInnerRadius, circleCount, circleSpacing);
            var metersPerImposter = (2.0f * Mathf.PI * radiusSum) / maxCount;

            for (var r = circleInnerRadius; r < ringEnd; r += circleSpacing)
            {
                var theta = Mathf.Rad2Deg * metersPerImposter / r;
                var turnRotation = Quaternion.AngleAxis(theta, Vector3.up);
                var position = r * Vector3.right;
                var rotation = Quaternion.AngleAxis(circleFacingAngle, Vector3.up);

                for (var i = 0.0f; i < 360.0f; i += theta)
                {
                    PlaceObject(prefab, position, rotation, placementRoot);
                    position = turnRotation * position;
                    rotation = turnRotation * rotation;
                }
            }
        }

        private void PlaceRandom()
        {
            var points = BlueNoiseGenerator.GeneratePoints(maxCount, randomVolume, randomInXZPlane);
            foreach (var t in points)
            {
                PlaceObject(prefab, t, RandomRotation(rotationRandomization), placementRoot);
            }
        }

        private static Quaternion RandomRotation(RotationRandomization randomization)
        {
            switch (randomization)
            {
                default:
                case RotationRandomization.NoRotation:
                    return Quaternion.identity;
                case RotationRandomization.RandomXYZ:
                    return Random.rotation;
                case RotationRandomization.RandomXZ:
                    return Quaternion.AngleAxis(360.0f * Random.value, Vector3.up);
            }
        }

        private void PlaceObject(GameObject gameObj, Vector3 localPosition, Quaternion localRotation, Transform transformParent)
        {
            GameObject newObject;

            if (PrefabUtility.IsPartOfPrefabAsset(gameObj))
            {
                newObject = (GameObject) PrefabUtility.InstantiatePrefab(gameObj);
                newObject.transform.SetParent(transformParent, false);
                newObject.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            }
            else
            {
                newObject = Instantiate(gameObj, localPosition, localRotation, transformParent);
            }

            newObject.name += " " + placeCount;
            placeCount++;
        }

        private static float SumArithmetic(float term0, float termCount, float difference)
        {
            return 0.5f * termCount * (2.0f * term0 + (termCount - 1.0f) * difference);
        }

        private static class BlueNoiseGenerator
        {
            private class SpashCell
            {
                public int count;
                public int[] indices = new int[6];
            }

            public static Vector3[] GeneratePoints(int samples, Vector3 side, bool inXZPlane)
            {
                const float factor = 1.0f;

                var pointsCount = 0;
                var points = new Vector3[samples];

                var gridSide = (int) (Mathf.Sqrt(samples) / 3);
                gridSide *= gridSide;
                // TODO: Improve hash grid sizing for non-cubic grids. The grid should have more divisions on
                // long sides and fewer on short sides.
                var cellCount = new Vector3Int(gridSide, gridSide, gridSide);
                var grid = new SpashCell[cellCount.x * cellCount.y * cellCount.z];
                for (var i = 0; i < grid.Length; i++) grid[i] = new SpashCell();

                var strideZ = cellCount.x * cellCount.y;

                for(var i = 0; i < samples; i++, pointsCount++)
                {
                    // Generate some number of candidate points and choose the sample
                    // furthest from all the existing points.
                    var candidates = (int) (factor * pointsCount) + 1;
                    var bestCandidate = Vector3.zero;
                    var bestDistance = 0.0f;
                    var bestCellPosition = Vector3Int.zero;
                    for(var j = 0; j < candidates; ++j)
                    {
                        var x = Random.value * side.x;
                        var y = inXZPlane ? 0.0f : Random.value * side.y;
                        var z = Random.value * side.z;
                        var candidate = new Vector3(x, y, z);

                        // Search the grid cell where the candidate is for potentially
                        // close points.
                        var gx = (int) ((cellCount.x - 1) * (x / side.x));
                        var gy = inXZPlane ? 0 : (int) ((cellCount.y - 1) * (y / side.y));
                        var gz = (int) ((cellCount.z - 1) * (z / side.z));
                        var min = float.MaxValue;
                        var cell = grid[strideZ * gz + cellCount.x * gy + gx];
                        for(var k = 0; k < cell.count; ++k)
                        {
                            var close = points[cell.indices[k]];
                            var d = ToroidalDistanceSquared(candidate, close, side);
                            min = Mathf.Min(min, d);
                        }

                        // If the closest point to this candidate is further than any prior
                        // candidate, then it's the new best.
                        if (!(min > bestDistance)) continue;
                        bestDistance = min;
                        bestCandidate = candidate;
                        bestCellPosition = new Vector3Int(gx, gy, gz);
                    }

                    // Add the picked index to the containing grid cell and its neighbors.
                    for(var j = -1; j <= 1; j++)
                    {
                        for(var k = -1; k <= 1; k++)
                        {
                            for (var l = -1; l <= 1; l++)
                            {
                                var x = ModInt(bestCellPosition.x + k, cellCount.x);
                                var y = ModInt(bestCellPosition.y + j, cellCount.y);
                                var z = ModInt(bestCellPosition.z + l, cellCount.z);
                                var cell = grid[strideZ * z + cellCount.x * y + x];
                                cell.indices[cell.count] = i;
                                cell.count = Mathf.Min(cell.count + 1, cell.indices.Length - 1);
                            }
                        }
                    }

                    points[i] = bestCandidate;
                }

                return points;
            }

            private static float ToroidalDistanceSquared(Vector3 v0, Vector3 v1, Vector3 circumference)
            {
                var dx = Mathf.Abs(v1.x - v0.x);
                var dy = Mathf.Abs(v1.y - v0.y);
                var dz = Mathf.Abs(v1.z - v0.z);
                if(dx > circumference.x / 2.0f)
                {
                    dx = circumference.x - dx;
                }
                if(dy > circumference.y / 2.0f)
                {
                    dy = circumference.y - dy;
                }
                if(dz > circumference.z / 2.0f)
                {
                    dz = circumference.z - dz;
                }
                return (dx * dx) + (dy * dy) + (dz * dz);
            }

            private static int ModInt(int x, int n)
            {
                return (x % n + n) % n;
            }
        }
    }
}
