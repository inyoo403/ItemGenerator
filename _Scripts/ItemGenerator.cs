using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Data structures for JSON logging
[Serializable]
public class ItemEntry {
    public string name;
    public ItemRarity rarity;
    public string stats;
    public Vector2 worldPos;
}

[Serializable]
public class RoomLog {
    public int roomLevel;
    public List<ItemEntry> items = new List<ItemEntry>();
}

[Serializable]
public class DungeonItemReport {
    public int seed;
    // Parameter metadata for comparison
    public float globalDifficulty;
    public float penaltyIntensity;
    public float tradeOffSpawnChance;
    public List<RoomLog> rooms = new List<RoomLog>();
}

public class ItemGenerator : MonoBehaviour
{
    [Serializable]
    public struct RarityWeight {
        public ItemRarity rarity;
        public int baseWeight; 
        public Sprite sprite; 
    }

    [Header("Prefabs & Scaling")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Vector3 itemScale = new Vector3(2.5f, 2.5f, 1f); 

    [Header("Trade-Off Parameters")]
    [Range(0, 100)]
    [SerializeField] private float tradeOffSpawnChance = 50f;
    [Range(0.5f, 3.0f)]
    [SerializeField] private float penaltyIntensity = 1.0f;
    [Range(0.5f, 2.0f)]
    [SerializeField] private float globalDifficulty = 1.0f;

    [Header("Natural Placement")]
    [SerializeField] private float minSpacing = 2.0f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckRadius = 0.45f;

    [Header("Dependencies")]
    [SerializeField] private RoomFirstDungeonGenerator dungeonGenerator;
    [SerializeField] private Tilemap floorTilemap; 
    [SerializeField] private List<RarityWeight> raritySettings; 

    private DungeonItemReport currentSessionData;

    // Entry point for manual generation
    public void ManualSpawn()
    {
        if (floorTilemap == null) floorTilemap = GameObject.Find("Floor").GetComponent<Tilemap>();
        if (dungeonGenerator == null) dungeonGenerator = FindFirstObjectByType<RoomFirstDungeonGenerator>();
        if (dungeonGenerator == null || dungeonGenerator.currentDungeonData == null) return;

        ExecuteSpawning(dungeonGenerator.currentDungeonData);
    }

    private void ExecuteSpawning(DungeonSaveData data)
    {
        ClearItems();
        
        // Store current parameter values in the report
        currentSessionData = new DungeonItemReport { 
            seed = data.seed,
            globalDifficulty = this.globalDifficulty,
            penaltyIntensity = this.penaltyIntensity,
            tradeOffSpawnChance = this.tradeOffSpawnChance
        };

        GameObject rootFolder = new GameObject("--- [FINAL] Items Root ---");
        rootFolder.transform.SetParent(this.transform);

        var sortedRooms = data.rooms.OrderBy(r => r.roomLevel).ToList();
        foreach (var room in sortedRooms)
        {
            if (room.type == RoomType.Start) continue;
            
            GameObject roomFolder = new GameObject($"Room_{room.roomLevel:D2}_Folder");
            roomFolder.transform.SetParent(rootFolder.transform);

            RoomLog roomLog = new RoomLog { roomLevel = room.roomLevel };
            int bonus = Mathf.FloorToInt(room.roomLevel * 0.2f);
            int count = UnityEngine.Random.Range(1, 4) + bonus;

            SpawnInRoom(room, count, roomFolder.transform, roomLog);
            currentSessionData.rooms.Add(roomLog);
        }
    }

    private void SpawnInRoom(RoomSaveData room, int targetCount, Transform parent, RoomLog log)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = room.min.x + 2; x < room.min.x + room.size.x - 2; x++) {
            for (int y = room.min.y + 2; y < room.min.y + room.size.y - 2; y++) {
                if (floorTilemap.HasTile(new Vector3Int(x, y, 0))) candidates.Add(new Vector2Int(x, y));
            }
        }
        candidates = candidates.OrderBy(t => UnityEngine.Random.value).ToList();

        List<Vector2> spawnedPos = new List<Vector2>();
        int spawned = 0;

        foreach (var pos in candidates)
        {
            if (spawned >= targetCount) break;
            Vector3 worldPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, -1f);

            // Wall and spacing collision checks
            if (Physics2D.OverlapCircle(worldPos, wallCheckRadius, wallLayer) != null) continue;
            if (spawnedPos.Any(p => Vector2.Distance(p, (Vector2)worldPos) < minSpacing)) continue;

            ItemRarity rarity = GetPureLevelRarity(room.roomLevel);
            bool rollTradeOff = UnityEngine.Random.Range(0f, 100f) < tradeOffSpawnChance;

            Equipment equip = new Equipment(rarity, room.roomLevel, globalDifficulty, penaltyIntensity, rollTradeOff);
            
            CreateItemObject(worldPos, equip, parent);
            log.items.Add(new ItemEntry {
                name = equip.itemName,
                rarity = equip.rarity,
                stats = equip.GetStatString(),
                worldPos = new Vector2(worldPos.x, worldPos.y)
            });

            spawnedPos.Add(worldPos);
            spawned++;
        }
    }

    private ItemRarity GetPureLevelRarity(int level)
    {
        float total = 0;
        List<float> weights = new List<float>();
        for (int i = 0; i < raritySettings.Count; i++) {
            float boost = Mathf.Pow(i + 1, level * 0.15f * 1.8f);
            float suppression = (level > 10 && i < 2) ? 1f / (level - 9) : 1f;
            float finalW = raritySettings[i].baseWeight * boost * suppression;
            weights.Add(finalW);
            total += finalW;
        }

        float roll = UnityEngine.Random.Range(0, total);
        float cursor = 0;
        for (int i = 0; i < weights.Count; i++) {
            cursor += weights[i];
            if (roll <= cursor) return raritySettings[i].rarity;
        }
        return ItemRarity.Normal;
    }

    private void CreateItemObject(Vector3 pos, Equipment data, Transform parent)
    {
        GameObject obj = Instantiate(itemPrefab, pos, Quaternion.identity, parent);
        obj.transform.localScale = itemScale;
        obj.tag = "Item";
        data.itemSprite = raritySettings.Find(s => s.rarity == data.rarity).sprite;
        if (obj.TryGetComponent(out ItemObject itemObj)) itemObj.Setup(data);
        obj.name = data.itemName;
    }

    // Incremental file saving to prevent overwriting
    public void SaveLogToJSON()
    {
        if (currentSessionData == null) return;
        
        string dir = Path.Combine(Application.dataPath, "_Scripts/Data");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string fileName = "Item_Log";
        string extension = ".json";
        string fullPath = Path.Combine(dir, fileName + extension);

        // Logic to increment filename if it already exists
        int counter = 2;
        while (File.Exists(fullPath))
        {
            fullPath = Path.Combine(dir, $"{fileName}_{counter}{extension}");
            counter++;
        }

        File.WriteAllText(fullPath, JsonUtility.ToJson(currentSessionData, true));
        Debug.Log($"Log saved to: {Path.GetFileName(fullPath)}");
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    public void ClearItems()
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform t in transform) toDestroy.Add(t.gameObject);
        foreach (GameObject g in toDestroy) {
            if (Application.isPlaying) Destroy(g);
            else DestroyImmediate(g);
        }
        foreach (var s in GameObject.FindGameObjectsWithTag("Item")) {
            if (Application.isPlaying) Destroy(s);
            else DestroyImmediate(s);
        }
    }
}

// Editor interface
#if UNITY_EDITOR
[CustomEditor(typeof(ItemGenerator))]
public class ItemGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        ItemGenerator gen = (ItemGenerator)target;
        GUILayout.Space(15);
        if (GUILayout.Button("1. Spawn Trade-Off Items")) gen.ManualSpawn();
        if (GUILayout.Button("2. Save Data to JSON")) gen.SaveLogToJSON();
        if (GUILayout.Button("Clear All Items")) gen.ClearItems();
    }
}
#endif