using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class ShipSpawner : MonoBehaviour
{
    public FloatersManager floatersManager;
    public LighthouseLight lighthouseLight;
    public GameObject warningPrefab;
    public GameObject shipPrefab;

    public float spawnWarningTime = 1.5f;
    public float spawnRapidTime = 2.5f;
    public float minimumSpawnDistance = 2.25f;
    public float shipRange = 7.0f;

    public uint lastChanceFoodThreshold = 5;
    public float lastChanceTimeThreshold = 30.0f;

    public List<SpawnEvent> mainEventSpawn = new();
    public List<SpawnEvent> backupSpawn = new();

    private List<Path> paths = new();
    private List<SpawnEvent> mainSpawn = new();
    private List<GameObject> warningLights = new();
    private float spawnWarningCounter = 0f;
    private List<Vector2> spawnPosition = new();

    [SerializeField] private List<Ship> ships = new();
    private AudioSource lastChanceSound;
    private AudioSource bellSound;

    [FormerlySerializedAs("isTestSpawnEnabled")] [SerializeField] private bool onlyTestSpawn = false;
    private bool isHalfRapidDone = false;
    private bool isLastChanceActivated = false;
    private SpawnType spawnType = SpawnType.Sequence;

    private const int MAX_SHIPS_SPAWNED = 2;

    public enum SpawnType
    {
        Sequence,
        Immediate,
        Rapid
    }

    [System.Serializable]
    public struct SpawnEvent
    {
        public List<ShipType> shipList;
        public SpawnType spawnType;

        public SpawnEvent(List<ShipType> shipList, SpawnType spawnType = SpawnType.Sequence)
        {
            this.shipList = shipList;
            this.spawnType = spawnType;
        }
    }

    public Ship FindNearestShip(Vector2 position)
    {
        if (ships.Count == 0)
        {
            return null;
        }

        Ship nearestShip = ships[0];
        float nearestDistance = Vector2.Distance(position, Utilities.Convert3DTo2D(nearestShip.transform.position));

        foreach (var ship in ships)
        {
            Vector2 shipPosition = Utilities.Convert3DTo2D(ship.transform.position);
            float distance = Vector2.Distance(position, shipPosition);

            if (nearestDistance > distance)
            {
                nearestDistance = distance;
                nearestShip = ship;
            }
        }

        return nearestShip;
    }

    public Ship FindNearestNonPirateShip(Vector2 position)
    {
        bool foundNonPirateShip = false;
        Ship nearest = null;
        foreach (var ship in ships)
        {
            if (ship.type == ShipType.Pirates)
                continue;

            foundNonPirateShip = true;
            nearest = ship;
            break;
        }

        if (!foundNonPirateShip)
            return null;

        Ship nearestShip = null;
        float nearestDistance = Vector2.Distance(position, Utilities.Convert3DTo2D(nearest.transform.position));

        foreach (var ship in ships)
        {
            if (ship.type == ShipType.Pirates || ship.hasBeenDestroyed)
            {
                continue;
            }

            Vector2 shipPosition = Utilities.Convert3DTo2D(ship.transform.position);
            float distance = Vector2.Distance(position, shipPosition);

            if (nearestDistance > distance)
            {
                nearestDistance = distance;
                nearestShip = ship;
            }
        }

        return nearestShip;
    }

    private void Start()
    {
        if (onlyTestSpawn)
        {
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodSmall }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodMedium }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodBig }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Pirates }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Tool }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodSmall }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodMedium }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodBig }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Pirates }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Tool }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodSmall }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodMedium }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodBig }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Pirates }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Tool }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodSmall }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodMedium }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.FoodBig }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Pirates }, spawnType = SpawnType.Sequence });
            backupSpawn.Add(new SpawnEvent { shipList = new List<ShipType> { ShipType.Tool }, spawnType = SpawnType.Sequence });
        }
        else
        {
            backupSpawn.AddRange(mainEventSpawn);
            mainEventSpawn.Clear();
        }

        for (int i = backupSpawn.Count - 1; i >= 0; --i)
        {
            if (backupSpawn[i].shipList.Count == 0)
            {
                Debug.LogWarning("Removed empty event!");
                backupSpawn.RemoveAt(i);
            }
        }

        CopySpawnList(mainSpawn, backupSpawn);

        if (!LevelController.Instance.IsTutorial)
        {
            mainSpawn.Sort((a, b) => Random.Range(0, 2));
        }

        GetSpawnPaths();
    }

    private void Update()
    {
        if (LevelController.Instance.HasStarted)
        {
            PrepareForSpawn();
        }
    }

    private void CopySpawnList(List<SpawnEvent> destination, List<SpawnEvent> source)
    {
        destination.Clear();

        foreach (var spawnEvent in source)
        {
            List<ShipType> shipListCopy = new();

            foreach (var shipType in spawnEvent.shipList)
            {
                shipListCopy.Add(shipType);
            }

            SpawnEvent spawnEventCopy = new SpawnEvent(shipListCopy, spawnEvent.spawnType);
            destination.Add(spawnEventCopy);
        }
    }

    private void GetSpawnPaths()
    {
        paths.Clear();

        paths.AddRange(GetComponentsInChildren<Path>());
    }

    public bool ShouldDecalBeDrawn()
    {
        if (lighthouseLight != null && lighthouseLight.enabled)
        {
            foreach (var ship in ships)
            {
                if (ship.behavioralState == BehavioralState.Control)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public void BurnOutAllShips(bool value)
    {
        // TODO: Burn out lights?
        return;
    }

    public void SpawnShipAtPosition(ShipType type, Vector2 position, float direction, bool glowing = false)
    {
        // TODO: Spawn ship
        // GameObject shipGO = SceneSerializer.LoadPrefab(GetPrefabName(type));
        // shipGO.transform.position = new Vector3(position.x, 0, position.y);
        // if (GameController.Instance.NextScene == null)
        // {
        //     shipGO.transform.SetParent(GameController.Instance.CurrentScene.transform);
        // }
        // else
        // {
        //     shipGO.transform.SetParent(GameController.Instance.NextScene.transform);
        // }

        // Ship ship = shipGO.GetComponent<Ship>();
        // Floater floater = shipGO.AddComponent<Floater>();
        // floater.Configure(floatersManager.GetSettings(type));

        // ship.Initialize(floatersManager, light, floater, direction, glowing, this);

        // ships.Add(ship);
    }

    public void PopEvent()
    {
        SpawnEvent currentEvent = mainSpawn.Last();
        currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);
    }

    public void ResetEvent()
    {
        mainSpawn = backupSpawn;
    }

    public void SetGlowToLastShip()
    {
        // TODO: Glowing
        // if (ships.Count > 0)
        // {
        //     ships[ships.Count - 1].SetGlowing(true);
        // }
    }

    public bool IsLastChanceActivated() => isLastChanceActivated;

    public int GetNumberOfFoodShips()
    {
        int numberOfFoodShips = 0;

        foreach (var ship in ships)
        {
            if (ship.type is ShipType.FoodSmall || ship.type is ShipType.FoodMedium || ship.type is ShipType.FoodBig)
                numberOfFoodShips++;
        }

        return numberOfFoodShips;
    }

    public static string SpawnTypeToString(SpawnType type)
    {
        return type switch
        {
            SpawnType.Sequence => "Sequence",
            SpawnType.Immediate => "Immediate",
            SpawnType.Rapid => "Rapid",
            _ => "Undefined spawn"
        };
    }

    private void RemoveShip(Ship shipToRemove)
    {
        ships.Remove(shipToRemove);
        shipToRemove.onDestroyed -= RemoveShip;
    }

    private bool IsSpawnPossible()
    {
        int numberOfShips = 0;

        foreach (var ship in ships)
        {
            if (!ship.hasBeenDestroyed)
            {
                numberOfShips += 1;
            }
        }

        if (numberOfShips < LevelController.Instance.ShipsLimit)
        {
            return true;
        }

        return false;
    }

    private void PrepareForSpawn()
    {
        if (!isLastChanceActivated && backupSpawn.Count == 0)
        {
            Debug.LogError("Ship spawn list is empty!");
            return;
        }
        
        if (mainSpawn.Count == 0)
        {
            if (!isLastChanceActivated)
            {
                CopySpawnList(mainSpawn, backupSpawn);
            }

            return;
        }
        
        SpawnEvent currentEvent = mainSpawn.Last();

        if (currentEvent.shipList.Count == 0)
        {
            if (spawnType == SpawnType.Immediate || spawnType == SpawnType.Rapid)
            {
                spawnWarningCounter = spawnWarningTime;
            }

            mainSpawn.RemoveAt(mainSpawn.Count - 1);

            if (mainSpawn.Count == 0)
            {
                return;
            }
        }

        if (paths.Count == 0)
        {
            Debug.LogWarning("No available paths to create ships on!");
            return;
        }

        if (!isLastChanceActivated && IsTimeForLastChance())
            return;

        currentEvent = mainSpawn.Last();
        spawnType = currentEvent.spawnType;

        if (spawnWarningCounter > 0.0f)
        {
            if (spawnType != SpawnType.Rapid)
            {
                spawnWarningCounter -= Time.deltaTime;
                return;
            }

            if (spawnWarningCounter > spawnRapidTime || isHalfRapidDone)
            {
                spawnWarningCounter -= Time.deltaTime;
                return;
            }

            if (warningLights.Count == MAX_SHIPS_SPAWNED)
            {
                if (ships.Count != 0)
                {
                    Assert.IsTrue(spawnPosition.Count > 0);
                    Vector2 nearestShipPosition = Utilities.Convert3DTo2D(FindNearestShip(spawnPosition.Last()).transform.position);

                    if (Vector2.Distance(nearestShipPosition, spawnPosition.Last()) < minimumSpawnDistance)
                    {
                        spawnWarningCounter = spawnWarningTime;
                        return;
                    }
                }

                Destroy(warningLights.Last());
                warningLights.RemoveAt(warningLights.Count - 1);

                SpawnShip(currentEvent);

                spawnPosition.RemoveAt(spawnPosition.Count - 1);

                currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);

                if (currentEvent.shipList.Count > 1)
                {
                    Path randomPath = paths[Random.Range(0, paths.Count)];

                    spawnPosition.Insert(0, randomPath.GetPointAt(Random.Range(0.0f, 1.0f)));

                    AddWarning();
                }
            }
            else
            {
                if (currentEvent.shipList.Count > 1)
                {
                    Path randomPath = paths[Random.Range(0, paths.Count)];

                    spawnPosition.Insert(0, randomPath.GetPointAt(Random.Range(0.0f, 1.0f)));

                    AddWarning();
                }
                else
                {
                    Destroy(warningLights.Last());
                    warningLights.RemoveAt(warningLights.Count - 1);

                    SpawnShip(currentEvent);

                    spawnPosition.RemoveAt(spawnPosition.Count - 1);

                    currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);
                }
            }

            isHalfRapidDone = true;

            spawnWarningCounter -= Time.deltaTime;
            return;
        }

        if (spawnType == SpawnType.Sequence)
        {
            if (!IsSpawnPossible())
            {
                return;
            }

            if (warningLights.Count != 0)
            {
                if (ships.Count != 0)
                {
                    Assert.IsTrue(spawnPosition.Count > 0);
                    Vector2 nearestShipPosition = Utilities.Convert3DTo2D(FindNearestShip(spawnPosition.Last()).transform.position);

                    if (Vector2.Distance(nearestShipPosition, spawnPosition.Last()) < minimumSpawnDistance)
                    {
                        // There is no room near the spawning point, delay until next spawn time
                        spawnWarningCounter = spawnWarningTime;
                        return;
                    }
                }

                Destroy(warningLights.Last());
                warningLights.RemoveAt(warningLights.Count - 1);

                SpawnShip(currentEvent);

                spawnPosition.RemoveAt(spawnPosition.Count - 1);

                if (!LevelController.Instance.IsTutorial)
                {
                    currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);
                }

                return;
            }

            Path randomPath = paths[Random.Range(0, paths.Count)];
            spawnPosition.Add(randomPath.GetPointAt(Random.Range(0.0f, 1.0f)));

            AddWarning();

            spawnWarningCounter = spawnWarningTime;
        }
        else if (spawnType == SpawnType.Immediate)
        {
            if (warningLights.Count != 0)
            {
                for (int i = warningLights.Count - 1; i >= 0; i--)
                {
                    Destroy(warningLights[i]);
                    warningLights.RemoveAt(i);

                    SpawnShip(currentEvent);

                    spawnPosition.RemoveAt(spawnPosition.Count - 1);

                    currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);
                }
            }
            else
            {
                foreach (var ship in currentEvent.shipList)
                {
                    Vector2 potentialSpawnPoint = Vector2.zero;

                    // NOTE: 100 is an arbitrary number of tries that we perform to find a suitable spawn point.
                    //       If this number is reached we just don't spawn any more ships from this event.
                    for (int i = 0; i < 100; i++)
                    {
                        Path randomPath = paths[Random.Range(0, paths.Count)];
                        potentialSpawnPoint = randomPath.GetPointAt(Random.Range(0.0f, 1.0f));

                        Ship nearestShip = FindNearestShip(potentialSpawnPoint);
                        if (nearestShip == null || Vector2.Distance(Utilities.Convert3DTo2D(nearestShip.transform.position), potentialSpawnPoint) >= minimumSpawnDistance)
                        {
                            break;
                        }
                    }

                    spawnPosition.Add(potentialSpawnPoint);

                    AddWarning();
                }

                spawnWarningCounter = spawnWarningTime;
            }
        }
        else if (spawnType == SpawnType.Rapid)
        {
            if (warningLights.Count == MAX_SHIPS_SPAWNED)
            {
                if (ships.Count != 0)
                {
                    Assert.IsTrue(spawnPosition.Count > 0);
                    Vector2 nearestShipPosition = Utilities.Convert3DTo2D(FindNearestShip(spawnPosition.Last()).transform.position);

                    if (Vector2.Distance(nearestShipPosition, spawnPosition.Last()) < minimumSpawnDistance)
                    {
                        // There is no room near the spawning point, delay until next spawn time.
                        spawnWarningCounter = spawnWarningTime;
                        return;
                    }
                }

                if (warningLights.Count == 0)
                {
                    Debug.LogError("There is no warning but one should be destroyed!");
                    return;
                }

                Destroy(warningLights.Last());
                warningLights.RemoveAt(warningLights.Count - 1);

                SpawnShip(currentEvent);

                spawnPosition.RemoveAt(spawnPosition.Count - 1);

                currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);

                if (currentEvent.shipList.Count > 1)
                {
                    Path randomPath = paths[Random.Range(0, paths.Count)];
                    spawnPosition.Insert(0, randomPath.GetPointAt(Random.Range(0.0f, 1.0f)));

                    AddWarning();
                }
            }
            else
            {
                if (currentEvent.shipList.Count > 1)
                {
                    Path randomPath = paths[Random.Range(0, paths.Count)];
                    spawnPosition.Insert(0, randomPath.GetPointAt(Random.Range(0.0f, 1.0f)));

                    AddWarning();
                }
                else
                {
                    if (warningLights.Count == 0)
                    {
                        Debug.LogError("There is no warning but one should be destroyed!");
                        return;
                    }

                    Destroy(warningLights.Last());
                    warningLights.RemoveAt(warningLights.Count - 1);

                    SpawnShip(currentEvent);

                    spawnPosition.RemoveAt(spawnPosition.Count - 1);

                    currentEvent.shipList.RemoveAt(currentEvent.shipList.Count - 1);
                }
            }

            isHalfRapidDone = false;

            spawnWarningCounter = spawnWarningTime;
        }
    }

    private void AddWarning()
    {
        GameObject warning = Instantiate(warningPrefab);

        float x = Mathf.Abs(spawnPosition[0].x);
        float y = Mathf.Abs(spawnPosition[0].y);

        warning.transform.position = new Vector3(x, 0.0f, y);

        if (spawnType == SpawnType.Rapid)
        {
            warningLights.Insert(0, warning);
        }
        else
        {
            warningLights.Add(warning);
        }
    }

    private bool IsTimeForLastChance()
    {
        if (LevelController.Instance.Time > lastChanceTimeThreshold)
        {
            return false;
        }

        int foodShortage = LevelController.Instance.MapFood - Player.Instance.Food;

        if (foodShortage <= 0 || foodShortage > lastChanceFoodThreshold)
        {
            return false;
        }

        backupSpawn.Clear();
        mainSpawn.Clear();

        SpawnEvent lastChance = new SpawnEvent { spawnType = SpawnType.Rapid };

        while (foodShortage > 0)
        {
            if (foodShortage >= 5)
            {
                lastChance.shipList.Add(ShipType.FoodBig);
                foodShortage -= 5;
            }
            else if (foodShortage >= 3)
            {
                lastChance.shipList.Add(ShipType.FoodMedium);
                foodShortage -= 3;
            }
            else
            {
                lastChance.shipList.Add(ShipType.FoodSmall);
                foodShortage -= 1;
            }
        }

        mainSpawn.Add(lastChance);
        isLastChanceActivated = true;

        Debug.Log("LAST CHANCE!");

        // TODO: Last chance sound

        return true;
    }

    private void SpawnShip(SpawnEvent currentEvent)
    {
        GameObject ship = Instantiate(shipPrefab);
        Debug.Log("SPAWN");
        FloatersManager.FloaterSettings spawnedBoatSettings = new FloatersManager.FloaterSettings();

        if (!currentEvent.shipList.Any())
            return;
        
        ShipType spawnedShipType = currentEvent.shipList.Last();   

        if (spawnedShipType == ShipType.FoodSmall)
        {
            // ship = SceneSerializer::load_prefab("ShipSmall");
            spawnedBoatSettings = floatersManager.smallBoatSettings;
        }
        else if (spawnedShipType == ShipType.FoodMedium)
        {
            // ship = SceneSerializer::load_prefab("ShipMedium");
            spawnedBoatSettings = floatersManager.mediumBoatSettings;
        }
        else if (spawnedShipType == ShipType.FoodBig)
        {
            // ship = SceneSerializer::load_prefab("ShipBig");
            spawnedBoatSettings = floatersManager.bigBoatSettings;
        }
        else if (spawnedShipType == ShipType.Pirates)
        {
            // ship = SceneSerializer::load_prefab("ShipPirates");
            spawnedBoatSettings = floatersManager.pirateBoatSettings;
        }
        else if (spawnedShipType == ShipType.Tool)
        {
            // ship = SceneSerializer::load_prefab("ShipTool");
            spawnedBoatSettings = floatersManager.toolBoatSettings;
        }

        // TODO: Add floater, set position, set parent, add component, set direction etc.

        var spawnPos = spawnPosition.Last();
        ship.transform.position = new Vector3(spawnPos.x, 0.0f, spawnPos.y);

        var shipComp = ship.GetComponent<Ship>();
        shipComp.onDestroyed += RemoveShip;
        shipComp.maximumSpeed = LevelController.Instance.ShipsSpeed;
        shipComp.shipSpawner = this;
        shipComp.lighthouseLight = lighthouseLight;

        shipComp.SetStartDirection();

        ships.Add(shipComp);
    }
}
