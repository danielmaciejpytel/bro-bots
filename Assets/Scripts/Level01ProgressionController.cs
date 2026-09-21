using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Level01Stage
{
    Tutorial,
    Room2,
    Room3,
    Room4,
    Final,
    Complete
}

public sealed class Level01ProgressionController : MonoBehaviour
{
    private const string LevelName = "Level_01";

    private TutorialCore tutorial;
    private BodyParts bodyParts;

    private RoomState tutorialRoom;
    private RoomState room2;
    private RoomState room3;
    private RoomState room4;
    private RoomState laserRoom;
    private RoomState currentCombatRoom;

    private readonly List<EnemySpawner> subscribedSpawners = new List<EnemySpawner>();

    public Level01Stage Stage { get; private set; } = Level01Stage.Tutorial;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != LevelName)
        {
            enabled = false;
            return;
        }

        tutorial = FindFirstObjectByType<TutorialCore>();
        bodyParts = FindFirstObjectByType<BodyParts>();

        tutorialRoom = BuildRoom("RoomTutorial");
        room2 = BuildRoom("Room2");
        room3 = BuildRoom("Room3");
        room4 = BuildRoom("Room4");
        laserRoom = BuildRoom("LaserRoom");

        ConfigureGates();
        PrepareInitialState();
    }

    private void OnDestroy()
    {
        if (tutorial != null)
        {
            tutorial.StateChanged -= HandleTutorialStateChanged;
        }

        if (bodyParts != null)
        {
            bodyParts.PartCollected -= HandlePartCollected;
        }

        UnsubscribeCurrentWave();
    }

    private void PrepareInitialState()
    {
        // All progression gates start closed. Rooms themselves stay visible.
        SetWalls(tutorialRoom, true);
        SetWalls(room2, true);
        SetWalls(room3, true);
        SetWalls(room4, true);
        SetWalls(laserRoom, true);

        DisableRoomSpawners(tutorialRoom);
        DisableRoomSpawners(room2);
        DisableRoomSpawners(room3);
        DisableRoomSpawners(room4);

        if (tutorial != null)
        {
            tutorial.StateChanged += HandleTutorialStateChanged;
            if (tutorial.tutorialState == TutorialState.Play)
            {
                BeginRoom2();
            }
        }
        else
        {
            Debug.LogWarning("Level_01: TutorialCore nie został znaleziony. Pomijam tutorial.", this);
            BeginRoom2();
        }

        if (bodyParts != null)
        {
            bodyParts.SetWinEnabled(false);
            bodyParts.PartCollected += HandlePartCollected;
        }

        Debug.Log("[Level01] Progression initialized: Tutorial -> Room2 -> Room3 -> Room4 -> Final.");
    }

    private void HandleTutorialStateChanged(TutorialState state)
    {
        if (Stage == Level01Stage.Tutorial && state == TutorialState.Play)
        {
            BeginRoom2();
        }
    }

    private void BeginRoom2()
    {
        if (Stage != Level01Stage.Tutorial)
        {
            return;
        }

        DisableRoomSpawners(tutorialRoom);
        OpenGate(tutorialRoom?.ExitWall);
        OpenGate(room2?.EntryWall);
        BeginCombatRoom(Level01Stage.Room2, room2);
    }

    private void BeginRoom3()
    {
        OpenGate(room3?.EntryWall);
        BeginCombatRoom(Level01Stage.Room3, room3);
    }

    private void BeginRoom4()
    {
        OpenGate(room4?.EntryWall);
        BeginCombatRoom(Level01Stage.Room4, room4);
    }

    private void BeginFinal()
    {
        UnsubscribeCurrentWave();
        Stage = Level01Stage.Final;
        currentCombatRoom = null;

        OpenGate(laserRoom?.EntryWall);

        if (bodyParts != null)
        {
            if (bodyParts.HasAllParts)
            {
                Stage = Level01Stage.Complete;
            }

            bodyParts.SetWinEnabled(true);
        }

        Debug.Log(
            Stage == Level01Stage.Complete
                ? "[Level01] Room4 cleared. Final opened with 3/3 parts already collected. Level complete."
                : "[Level01] Room4 cleared. Final opened: collect the remaining body parts.");
    }

    private void BeginCombatRoom(Level01Stage stage, RoomState room)
    {
        UnsubscribeCurrentWave();

        Stage = stage;
        currentCombatRoom = room;

        if (room == null || room.Spawners.Length == 0)
        {
            Debug.LogWarning($"[Level01] {stage} nie ma spawnerów. Przechodzę dalej.", this);
            AdvanceAfterCurrentRoom();
            return;
        }

        foreach (EnemySpawner spawner in room.Spawners)
        {
            if (spawner == null)
            {
                continue;
            }

            int waveSize = spawner.ConfiguredWaveSize;
            spawner.ConfigureFiniteWave(waveSize);
            spawner.WaveCleared += HandleSpawnerWaveCleared;
            subscribedSpawners.Add(spawner);
            spawner.enabled = true;
            spawner.gameObject.SetActive(true);
        }

        Debug.Log($"[Level01] {stage} started. Spawners: {room.Spawners.Length}.");
    }

    private void HandleSpawnerWaveCleared(EnemySpawner _)
    {
        if (currentCombatRoom == null)
        {
            return;
        }

        for (int i = 0; i < currentCombatRoom.Spawners.Length; i++)
        {
            EnemySpawner spawner = currentCombatRoom.Spawners[i];
            if (spawner != null && !spawner.IsWaveCleared)
            {
                return;
            }
        }

        DisableRoomSpawners(currentCombatRoom);
        OpenGate(currentCombatRoom.ExitWall);

        Debug.Log($"[Level01] {Stage} cleared.");
        AdvanceAfterCurrentRoom();
    }

    private void AdvanceAfterCurrentRoom()
    {
        switch (Stage)
        {
            case Level01Stage.Room2:
                BeginRoom3();
                break;
            case Level01Stage.Room3:
                BeginRoom4();
                break;
            case Level01Stage.Room4:
                BeginFinal();
                break;
        }
    }

    private void HandlePartCollected(int collected, int total)
    {
        Debug.Log($"[Level01] Final parts: {collected}/{total}.");
        if (Stage == Level01Stage.Final && collected >= total)
        {
            Stage = Level01Stage.Complete;
            Debug.Log("[Level01] Level complete.");
        }
    }

    private static void DisableRoomSpawners(RoomState room)
    {
        if (room == null) return;

        foreach (EnemySpawner spawner in room.Spawners)
        {
            if (spawner != null)
            {
                spawner.enabled = false;
            }
        }
    }

    private static void SetWalls(RoomState room, bool active)
    {
        if (room == null) return;

        foreach (GameObject wall in room.Walls)
        {
            if (wall != null)
            {
                wall.SetActive(active);
            }
        }
    }

    private static void OpenGate(GameObject gate)
    {
        if (gate != null)
        {
            gate.SetActive(false);
        }
    }

    private void ConfigureGates()
    {
        AssignRoomGates(tutorialRoom, null, room2);
        AssignRoomGates(room2, tutorialRoom, room3);
        AssignRoomGates(room3, room2, room4);
        AssignRoomGates(room4, room3, laserRoom);
        AssignRoomGates(laserRoom, room4, null);
    }

    private static void AssignRoomGates(RoomState room, RoomState previous, RoomState next)
    {
        if (room == null || room.Walls.Length == 0)
        {
            return;
        }

        if (previous != null)
        {
            Vector3 previousCenter = GetRoomCenter(previous);
            room.EntryWall = room.Walls
                .OrderBy(wall => (wall.transform.position - previousCenter).sqrMagnitude)
                .FirstOrDefault();
        }

        if (next != null)
        {
            Vector3 nextCenter = GetRoomCenter(next);
            room.ExitWall = room.Walls
                .Where(wall => wall != room.EntryWall || room.Walls.Length == 1)
                .OrderBy(wall => (wall.transform.position - nextCenter).sqrMagnitude)
                .FirstOrDefault();
        }

        if (room.EntryWall == null && previous != null)
        {
            room.EntryWall = room.Walls[0];
        }

        if (room.ExitWall == null && next != null)
        {
            room.ExitWall = room.Walls.FirstOrDefault(wall => wall != room.EntryWall) ?? room.Walls[0];
        }

        // Final room has no next room. Keep the wall opposite its entrance closed
        // so the player remains inside the finale while collecting the three parts.
        if (next == null && room.Walls.Length > 1)
        {
            room.ExitWall = room.Walls.FirstOrDefault(wall => wall != room.EntryWall);
        }

        Debug.Log(
            $"[Level01] Gates {room.Root.name}:" +
            $" entry={DescribeGate(room.EntryWall)}" +
            $" exit={DescribeGate(room.ExitWall)}");
    }

    private static string DescribeGate(GameObject gate)
    {
        return gate != null
            ? $"{gate.name}@{gate.transform.position:F2}"
            : "<none>";
    }

    private static Vector3 GetRoomCenter(RoomState room)
    {
        if (room == null)
        {
            return Vector3.zero;
        }

        if (room.Spawners.Length > 0)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (EnemySpawner spawner in room.Spawners)
            {
                if (spawner == null) continue;
                sum += spawner.transform.position;
                count++;
            }

            if (count > 0)
            {
                return sum / count;
            }
        }

        if (room.Walls.Length > 0)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (GameObject wall in room.Walls)
            {
                if (wall == null) continue;
                sum += wall.transform.position;
                count++;
            }

            if (count > 0)
            {
                return sum / count;
            }
        }

        return room.Root != null ? room.Root.transform.position : Vector3.zero;
    }

    private void UnsubscribeCurrentWave()
    {
        foreach (EnemySpawner spawner in subscribedSpawners)
        {
            if (spawner != null)
            {
                spawner.WaveCleared -= HandleSpawnerWaveCleared;
            }
        }

        subscribedSpawners.Clear();
    }

    private static RoomState BuildRoom(string roomName)
    {
        GameObject room = FindSceneObject(roomName);
        if (room == null)
        {
            Debug.LogWarning($"Level_01: nie znaleziono pokoju '{roomName}'.");
            return null;
        }

        EnemySpawner[] spawners = room
            .GetComponentsInChildren<EnemySpawner>(true);

        GameObject[] walls = room
            .GetComponentsInChildren<Transform>(true)
            .Where(t =>
                t != room.transform &&
                t.name.StartsWith("Laser wall", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.gameObject)
            .ToArray();

        return new RoomState(room, spawners, walls);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (transform.name == objectName)
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }

    private sealed class RoomState
    {
        public RoomState(GameObject root, EnemySpawner[] spawners, GameObject[] walls)
        {
            Root = root;
            Spawners = spawners ?? Array.Empty<EnemySpawner>();
            Walls = walls ?? Array.Empty<GameObject>();
        }

        public GameObject Root { get; }
        public EnemySpawner[] Spawners { get; }
        public GameObject[] Walls { get; }
        public GameObject EntryWall { get; set; }
        public GameObject ExitWall { get; set; }
    }
}

public static class Level01ProgressionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene.name != "Level_01")
        {
            return;
        }

        if (UnityEngine.Object.FindFirstObjectByType<Level01ProgressionController>() != null)
        {
            return;
        }

        var progressionObject = new GameObject("Level01Progression");
        progressionObject.AddComponent<Level01ProgressionController>();
    }
}
