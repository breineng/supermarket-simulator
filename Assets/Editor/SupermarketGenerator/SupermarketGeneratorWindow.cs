using UnityEngine;
using UnityEditor;

/// <summary>
/// An editor window to generate a supermarket structure from basic cubes.
/// </summary>
public class SupermarketGeneratorWindow : EditorWindow
{
    // --- Parameters ---
    private float wallThickness = 0.2f;
    private float supermarketWidth = 20f;
    private float supermarketLength = 30f;
    private float supermarketHeight = 4f;

    private enum DoorWall { Front, Back, Left, Right }
    private DoorWall doorLocation = DoorWall.Front;
    private float doorwayWidth = 3f;
    private float doorHeight = 2.2f;

    private bool createRoof = true;
    private float roofBorderHeight = 0.5f;

    private bool addDoors = true;
    
    private Transform generatedObjectParent;

    [MenuItem("Tools/Supermarket Generator")]
    public static void ShowWindow()
    {
        GetWindow<SupermarketGeneratorWindow>("Supermarket Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Supermarket Dimensions", EditorStyles.boldLabel);
        supermarketWidth = EditorGUILayout.FloatField("Width", supermarketWidth);
        supermarketLength = EditorGUILayout.FloatField("Length", supermarketLength);
        supermarketHeight = EditorGUILayout.FloatField("Height", supermarketHeight);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);

        EditorGUILayout.Space();

        GUILayout.Label("Doorway", EditorStyles.boldLabel);
        doorLocation = (DoorWall)EditorGUILayout.EnumPopup("Door Location", doorLocation);
        doorwayWidth = EditorGUILayout.FloatField("Doorway Width", doorwayWidth);
        doorHeight = EditorGUILayout.FloatField("Doorway Height", doorHeight);

        EditorGUILayout.Space();

        GUILayout.Label("Roof & Doors", EditorStyles.boldLabel);
        createRoof = EditorGUILayout.BeginToggleGroup("Create Roof", createRoof);
        roofBorderHeight = EditorGUILayout.FloatField("Roof Border Height", roofBorderHeight);
        EditorGUILayout.EndToggleGroup();
        
        addDoors = EditorGUILayout.Toggle("Add Doors", addDoors);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Generate Supermarket"))
        {
            GenerateSupermarket();
        }
    }
    
    private void GenerateSupermarket()
    {
        var root = new GameObject("Generated_Supermarket");
        generatedObjectParent = root.transform;

        // Floor
        CreateCube("Floor", new Vector3(0, wallThickness / 2, 0), new Vector3(supermarketWidth, wallThickness, supermarketLength), generatedObjectParent);

        // Walls
        GenerateWalls(generatedObjectParent);

        // Roof
        if (createRoof)
        {
            GenerateRoof(generatedObjectParent);
        }

        // Doors
        if (addDoors)
        {
            GenerateDoors(generatedObjectParent);
        }
        
        Selection.activeGameObject = root;
    }

    private void GenerateWalls(Transform parent)
    {
        var wallsRoot = new GameObject("Walls").transform;
        wallsRoot.SetParent(parent);

        float wallH = supermarketHeight - wallThickness;
        float wallY = wallThickness + wallH / 2f;

        // Front Wall (-Z)
        if (doorLocation == DoorWall.Front)
            CreateWallWithDoor(wallsRoot, "FrontWall", wallH, wallY, new Vector3(0, 0, -(supermarketLength / 2f) + wallThickness / 2f), supermarketWidth, false);
        else
            CreateFullWall(wallsRoot, "FrontWall", wallH, wallY, new Vector3(0, 0, -(supermarketLength / 2f) + wallThickness / 2f), supermarketWidth, false);

        // Back Wall (+Z)
        if (doorLocation == DoorWall.Back)
            CreateWallWithDoor(wallsRoot, "BackWall", wallH, wallY, new Vector3(0, 0, (supermarketLength / 2f) - wallThickness / 2f), supermarketWidth, false);
        else
            CreateFullWall(wallsRoot, "BackWall", wallH, wallY, new Vector3(0, 0, (supermarketLength / 2f) - wallThickness / 2f), supermarketWidth, false);

        // Left Wall (-X)
        if (doorLocation == DoorWall.Left)
            CreateWallWithDoor(wallsRoot, "LeftWall", wallH, wallY, new Vector3(-(supermarketWidth / 2f) + wallThickness / 2f, 0, 0), supermarketLength - 2 * wallThickness, true);
        else
            CreateFullWall(wallsRoot, "LeftWall", wallH, wallY, new Vector3(-(supermarketWidth / 2f) + wallThickness / 2f, 0, 0), supermarketLength - 2 * wallThickness, true);
        
        // Right Wall (+X)
        if (doorLocation == DoorWall.Right)
            CreateWallWithDoor(wallsRoot, "RightWall", wallH, wallY, new Vector3((supermarketWidth / 2f) - wallThickness / 2f, 0, 0), supermarketLength - 2 * wallThickness, true);
        else
            CreateFullWall(wallsRoot, "RightWall", wallH, wallY, new Vector3((supermarketWidth / 2f) - wallThickness / 2f, 0, 0), supermarketLength - 2 * wallThickness, true);
    }

    private void CreateFullWall(Transform parent, string name, float height, float posY, Vector3 position, float length, bool isVertical)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;

        Vector3 scale = isVertical ? new Vector3(wallThickness, height, length) : new Vector3(length, height, wallThickness);
        CreateCube("WallSegment", new Vector3(0, posY, 0), scale, wall.transform);
    }
    
    private void CreateWallWithDoor(Transform parent, string name, float wallHeight, float wallY, Vector3 position, float wallLength, bool isVertical)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;

        float sideWidth = (wallLength - doorwayWidth) / 2f;

        if (sideWidth < 0) {
            Debug.LogError("Doorway is wider than the wall.");
            return;
        }

        // Left/Bottom segment
        Vector3 pos1 = isVertical ? new Vector3(0, 0, -wallLength / 2f + sideWidth / 2f) : new Vector3(-wallLength / 2f + sideWidth / 2f, 0, 0);
        Vector3 scale1 = isVertical ? new Vector3(wallThickness, wallHeight, sideWidth) : new Vector3(sideWidth, wallHeight, wallThickness);
        CreateCube("Side_A", pos1 + new Vector3(0, wallY, 0), scale1, wall.transform);

        // Right/Top segment
        Vector3 pos2 = isVertical ? new Vector3(0, 0, wallLength / 2f - sideWidth / 2f) : new Vector3(wallLength / 2f - sideWidth / 2f, 0, 0);
        Vector3 scale2 = isVertical ? new Vector3(wallThickness, wallHeight, sideWidth) : new Vector3(sideWidth, wallHeight, wallThickness);
        CreateCube("Side_B", pos2 + new Vector3(0, wallY, 0), scale2, wall.transform);
        
        // Lintel (above door)
        float lintelHeight = wallHeight - doorHeight;
        if (lintelHeight > 0)
        {
            float lintelY = wallThickness + doorHeight + lintelHeight / 2f;
            Vector3 scale3 = isVertical ? new Vector3(wallThickness, lintelHeight, doorwayWidth) : new Vector3(doorwayWidth, lintelHeight, wallThickness);
            CreateCube("Lintel", new Vector3(0, lintelY, 0), scale3, wall.transform);
        }
    }

    private void GenerateRoof(Transform parent)
    {
        var roofRoot = new GameObject("Roof").transform;
        roofRoot.SetParent(parent);
        
        float roofY = supermarketHeight;

        // Roof base
        CreateCube("RoofBase", new Vector3(0, roofY + wallThickness / 2f, 0), new Vector3(supermarketWidth, wallThickness, supermarketLength), roofRoot);

        // Borders
        float borderY = roofY + wallThickness + roofBorderHeight / 2f;
        // Front
        CreateCube("Border_Front", new Vector3(0, borderY, -supermarketLength / 2f + wallThickness / 2f), new Vector3(supermarketWidth, roofBorderHeight, wallThickness), roofRoot);
        // Back
        CreateCube("Border_Back", new Vector3(0, borderY, supermarketLength / 2f - wallThickness / 2f), new Vector3(supermarketWidth, roofBorderHeight, wallThickness), roofRoot);
        // Left
        CreateCube("Border_Left", new Vector3(-supermarketWidth / 2f + wallThickness / 2f, borderY, 0), new Vector3(wallThickness, roofBorderHeight, supermarketLength - 2 * wallThickness), roofRoot);
        // Right
        CreateCube("Border_Right", new Vector3(supermarketWidth / 2f - wallThickness / 2f, borderY, 0), new Vector3(wallThickness, roofBorderHeight, supermarketLength - 2 * wallThickness), roofRoot);
    }

    private void GenerateDoors(Transform parent)
    {
        var doorsRoot = new GameObject("Doors").transform;
        doorsRoot.SetParent(parent);
        
        Vector3 doorPosition = Vector3.zero;
        Quaternion doorRotation = Quaternion.identity;

        switch (doorLocation)
        {
            case DoorWall.Front:
                doorPosition = new Vector3(0, wallThickness + doorHeight / 2f, -supermarketLength / 2f + wallThickness / 2f);
                break;
            case DoorWall.Back:
                doorPosition = new Vector3(0, wallThickness + doorHeight / 2f, supermarketLength / 2f - wallThickness / 2f);
                break;
            case DoorWall.Left:
                doorPosition = new Vector3(-supermarketWidth / 2f + wallThickness / 2f, wallThickness + doorHeight / 2f, 0);
                doorRotation = Quaternion.Euler(0, 90, 0);
                break;
            case DoorWall.Right:
                doorPosition = new Vector3(supermarketWidth / 2f - wallThickness / 2f, wallThickness + doorHeight / 2f, 0);
                doorRotation = Quaternion.Euler(0, 90, 0);
                break;
        }
        
        // Left Door
        Vector3 leftDoorPos = doorPosition - (doorRotation * new Vector3(doorwayWidth / 4f, 0, 0));
        CreateCube("Door_Left", leftDoorPos, new Vector3(doorwayWidth / 2f, doorHeight, wallThickness * 0.4f), doorsRoot, doorRotation);

        // Right Door
        Vector3 rightDoorPos = doorPosition + (doorRotation * new Vector3(doorwayWidth / 4f, 0, 0));
        CreateCube("Door_Right", rightDoorPos, new Vector3(doorwayWidth / 2f, doorHeight, wallThickness * 0.4f), doorsRoot, doorRotation);
    }
    
    private void CreateCube(string name, Vector3 position, Vector3 scale, Transform parent, Quaternion rotation = default)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.localPosition = position;
        if(rotation != default) cube.transform.localRotation = rotation;
        cube.transform.localScale = scale;
    }
} 