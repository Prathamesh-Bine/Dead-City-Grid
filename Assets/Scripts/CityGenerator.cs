using UnityEngine;
using System.Collections.Generic;

public class CityGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 30;
    public int height = 30;
    public float spacing = 10f;
    
    [Header("Prefabs")]
    public GameObject roadPrefab;
    public GameObject groundPrefab; 
    public GameObject[] commonBuildings; 
    public GameObject uniqueBuildingPrefab; 
    
    [Header("Generation Rules")]
    public int maxUniqueBuildings = 2;
    public float roadChance = 0.3f;
    public float emptyLotChance = 0.1f; 
    public int minBlockSize = 2; 

    [Header("City Shape (Perlin Noise)")]
    public float shapeScale = 0.15f; 
    public float shapeThreshold = 0.4f; 

    [Header("Player Settings")]
    public GameObject player; // Drag the PlayerCapsule here in the Inspector

    private int[,] mapGrid;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        GameObject mapContainer = new GameObject("GeneratedCity");
        mapContainer.transform.SetParent(this.transform);

        mapGrid = new int[width, height];
        
        bool[] isRoadCol = new bool[width];
        bool[] isRoadRow = new bool[height];
        
        int tilesSinceLastRoadX = 0;
        for (int x = 0; x < width; x++) 
        { 
            if (tilesSinceLastRoadX >= minBlockSize && Random.value < roadChance) { isRoadCol[x] = true; tilesSinceLastRoadX = 0; }
            else { tilesSinceLastRoadX++; }
        }

        int tilesSinceLastRoadZ = 0;
        for (int z = 0; z < height; z++) 
        { 
            if (tilesSinceLastRoadZ >= minBlockSize && Random.value < roadChance) { isRoadRow[z] = true; tilesSinceLastRoadZ = 0; }
            else { tilesSinceLastRoadZ++; }
        }

        List<Vector2Int> validPlots = new List<Vector2Int>();
        List<Vector2Int> emptyLots = new List<Vector2Int>(); // Track our empty spawn points
        
        float offsetX = Random.Range(0f, 10000f);
        float offsetZ = Random.Range(0f, 10000f);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float noise = Mathf.PerlinNoise((x + offsetX) * shapeScale, (z + offsetZ) * shapeScale);
                
                if (noise < shapeThreshold) 
                {
                    mapGrid[x, z] = -1; 
                    continue; 
                }

                Vector3 tilePos = new Vector3(x * spacing, 0, z * spacing);
                
                Vector3 groundPos = new Vector3(tilePos.x, -1f, tilePos.z);
                Instantiate(groundPrefab, groundPos, Quaternion.identity, mapContainer.transform);

                if (isRoadCol[x] || isRoadRow[z])
                {
                    mapGrid[x, z] = 1; 
                    Instantiate(roadPrefab, tilePos, Quaternion.identity, mapContainer.transform);
                }
                else
                {
                    if (Random.value < emptyLotChance)
                    {
                        mapGrid[x, z] = 0; 
                        emptyLots.Add(new Vector2Int(x, z)); // Save this coordinate for the player
                    }
                    else
                    {
                        mapGrid[x, z] = 2; 
                        validPlots.Add(new Vector2Int(x, z)); 
                    }
                }
            }
        }

        int uniqueSpawned = 0;
        while (uniqueSpawned < maxUniqueBuildings && validPlots.Count > 0)
        {
            int randomIndex = Random.Range(0, validPlots.Count);
            Vector2Int plot = validPlots[randomIndex];
            
            Vector3 spawnPos = new Vector3(plot.x * spacing, 0, plot.y * spacing);
            GameObject newBuilding = Instantiate(uniqueBuildingPrefab, spawnPos, Quaternion.identity, mapContainer.transform);
            
            newBuilding.transform.position = new Vector3(spawnPos.x, newBuilding.transform.localScale.y / 2f, spawnPos.z);
            
            validPlots.RemoveAt(randomIndex);
            uniqueSpawned++;
        }

        foreach (Vector2Int plot in validPlots)
        {
            Vector3 spawnPos = new Vector3(plot.x * spacing, 0, plot.y * spacing);
            
            int randomIndex = Random.Range(0, commonBuildings.Length);
            GameObject selectedPrefab = commonBuildings[randomIndex];
            
            GameObject newBuilding = Instantiate(selectedPrefab, spawnPos, Quaternion.identity, mapContainer.transform);
            
            newBuilding.transform.position = new Vector3(spawnPos.x, newBuilding.transform.localScale.y / 2f, spawnPos.z);
        }

        // --- Teleport the Player ---
        TeleportPlayerToEmptyLot(emptyLots);
    }

    void TeleportPlayerToEmptyLot(List<Vector2Int> emptyLots)
    {
        if (player != null && emptyLots.Count > 0)
        {
            // Pick a random empty lot
            int randomIndex = Random.Range(0, emptyLots.Count);
            Vector2Int spawnPlot = emptyLots[randomIndex];
            
            // Calculate the exact 3D position (Drop them from Y: 2 to ensure they land cleanly)
            Vector3 spawnPos = new Vector3(spawnPlot.x * spacing, 2f, spawnPlot.y * spacing);

            // Disable the Character Controller temporarily to allow teleportation
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Move the player
            player.transform.position = spawnPos;

            // Re-enable the Character Controller
            if (cc != null) cc.enabled = true;
        }
        else if (emptyLots.Count == 0)
        {
            Debug.LogWarning("No empty lots were generated! Consider increasing Empty Lot Chance or Map Size.");
        }
    }
}