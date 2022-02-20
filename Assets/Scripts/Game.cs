using SteelOfStalin.Attributes;
using SteelOfStalin.Commands;
using SteelOfStalin.Customizables;
using SteelOfStalin.Customizables.Modules;
using SteelOfStalin.Flow;
using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using SteelOfStalin.Props.Units.Land;
using SteelOfStalin.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SteelOfStalin.Util.Utilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin
{
    //Contains all information of the game itself
    public class Game : MonoBehaviour
    {
        // Handles scenes (un)loading
        public static SceneManager SceneManager { get; set; } = new SceneManager();
        public static List<Battle> Battles { get; set; } = new List<Battle>();
        public static GameSettings Settings { get; set; } = new GameSettings();
        // TODO add achievements later on

        public void Start()
        {
            // TODO Load all models, sound etc. here
            Settings.Load();
        }
    }

    public class GameSettings
    {
        public bool EnableAnimations { get; set; }
        public byte VolumeMusic { get; set; }
        public byte VolumeSoundFX { get; set; }

        public void Save()
        {

        }
        public void Load()
        {

        }
    }

    public class Battle : MonoBehaviour
    {
        public static Battle Instance { get; private set; }

        public string Name { get; set; }
        public Map Map { get; set; } = new Map();
        public List<Player> Players { get; set; } = new List<Player>();
        public BattleRules Rules { get; set; } = new BattleRules();

        public Stack<Round> Rounds = new Stack<Round>();
        public Round CurrentRound { get; set; }
        public int RoundNumber { get; set; } = 1;

        public bool EnablePlayerInput { get; set; } = true;
        public bool IsEnded { get; set; } = false;

        public void Start()
        {
            Instance = this;
            Load();
            Rules.Load();

            // main game logic loop
            while (!IsEnded)
            {
                CurrentRound = new Round();
                Players.ForEach(p => p.IsReady = false);
                CurrentRound.ResetUnitStatus();
                CurrentRound.ActionPrerequisitesChecking();
                CurrentRound.CommandPrerequisitesChecking();
                EnablePlayerInput = true;

                _ = StartCoroutine(((Planning)CurrentRound.Planning).PhaseLoop(Rules.TimeForEachRound));

                EnablePlayerInput = false;
                CurrentRound.EndPlanning();
                Rounds.Push(CurrentRound);
                RoundNumber++;
            }
        }
        public void Save()
        {

        }
        public void Load()
        {
            Rules.Load();
            Map.Load();
        }
    }

    // Contains different rules of the battle, like how much time is allowed for each round etc.
    public class BattleRules
    {
        public int TimeForEachRound { get; set; } = 120; // in seconds
        public bool IsFogOfWar { get; set; } = true;
        public bool DestroyedUnitsCanBeScavenged { get; set; } = false;
        public bool AllowUniversalQueue { get; set; }

        public BattleRules()
        {

        }

        public void Save()
        {

        }

        public void Load()
        {
            // TODO
        }
    }

    public class Map : ICloneable
    {
        public static Map Instance { get; private set; }

        private Tile[][] Tiles { get; set; }
        private List<Unit> Units { get; set; }
        private List<Building> Buildings { get; set; }
        private int Width { get; set; }
        private int Height { get; set; }

        public Map() => Instance = this;
        public Map(int width, int height) => (Width, Height, Instance) = (width, height, this);

        public void Save()
        {

        }

        public void Load()
        {
            // TODO
        }

        public bool AddUnit(Unit u)
        {
            if (u == null)
            {
                Debug.LogWarning("Cannot add unit to map unit list: unit is null");
                return false;
            }
            if (Units.Contains(u))
            {
                Debug.LogWarning($"Unit {u.Name} ({u.CoOrds.X}, {u.CoOrds.Y}) already in map unit array. Skipping operation.");
                return false;
            }
            Units.Add(u);
            return true;
        }

        public bool RemoveUnit(Unit u)
        {
            if (u == null)
            {
                Debug.LogWarning("Cannot remove unit from map unit list: unit is null");
                return false;
            }
            return Units.Remove(u);
        }

        public void PrintUnitList()
        {
            Debug.Log("Current unit list:");
            foreach (Unit unit in Units)
            {
                Debug.Log($"{unit.name} ({unit.CoOrds.X}, {unit.CoOrds.Y}): {unit.Status}");
            }
        }

        public Tile GetTile(int x, int y) => Tiles[x][y];
        public Tile GetTile(Point p) => Tiles[p.X][p.Y];
        public Tile GetTile(CubeCoordinates c) => Tiles[((Point)c).X][((Point)c).Y];
        public Tile[][] GetTiles() => Tiles;
        public IEnumerable<Tile> GetTiles(TileType type) => Tiles.Flatten().Where(t => t.Type == type);
        public IEnumerable<Tile> GetTiles(Predicate<Tile> predicate) => Tiles.Flatten().Where(t => predicate(t));

        public IEnumerable<Cities> GetCities() => Tiles.Flatten().Where(t => t is Cities).Cast<Cities>();
        public IEnumerable<Cities> GetCities(Player player) => GetCities().Where(c => c.Owner == player);
        public IEnumerable<Cities> GetCities(Predicate<Cities> predicate) => GetCities().Where(c => predicate(c));

        public IEnumerable<Tile> GetNeigbours(CubeCoordinates c, int distance = 1)
        {
            if (distance < 1)
            {
                throw new ArgumentException("Distance must be >= 1.");
            }
            for (int x = -distance; x <= distance; x++)
            {
                for (int y = -distance; y <= distance; y++)
                {
                    for (int z = -distance; z <= distance; z++)
                    {
                        if (c.X + x >= 0 && c.Y + y >= 0 && c.Z + z >= 0)
                        {
                            yield return GetTile((Point)new CubeCoordinates(c.X + x, c.Y + y, c.Z + z));
                        }
                    }
                }
            }
            yield break;
        }
        public IEnumerable<Tile> GetStraightLineNeighbours(CubeCoordinates c, double distance = 1) => distance < 1
                ? throw new ArgumentException("Distance must be >= 1.")
                : GetNeigbours(c, (int)Math.Ceiling(distance)).Where(t => CubeCoordinates.GetStraightLineDistance(c, t.CubeCoOrds) <= distance);
        public bool HasUnoccupiedNeighbours(CubeCoordinates c, int distance = 1) => GetNeigbours(c, distance).Any(t => !t.IsOccupied);

        public IEnumerable<Unit> GetUnits() => Units;
        public IEnumerable<Unit> GetUnits<T>() where T : Unit => Units.Where(u => u is T);
        public IEnumerable<Unit> GetUnits(Point p) => GetUnits(GetTile(p));
        public IEnumerable<Unit> GetUnits(Tile t)
        {
            if (t == null)
            {
                Debug.LogWarning("Error when trying to get unit: input tile is null");
                return null;
            }

            List<Unit> units = Units.Where(u => u.CoOrds.X == t.CoOrds.X && u.CoOrds.Y == t.CoOrds.Y).ToList();
            if (units.Count == 0)
            {
                Debug.Log($"No unit found at {t.PrintCoOrds()}");
            }
            else if (units.Count > 2 ||
                (units.Count == 2 && ((units[0] is Ground && units[1] is Ground) || (units[0] is Naval && units[1] is Naval) || (units[0] is Aerial && units[1] is Aerial))))
            {
                Debug.LogError($"Illegal stacking of units found at {t.PrintCoOrds()}!");
            }
            return units;
        }
        public IEnumerable<Unit> GetUnits(UnitStatus status) => Units.Where(u => u.Status.HasFlag(status));
        public IEnumerable<Unit> GetUnits(Player player) => Units.Where(u => u.Owner == player);
        public IEnumerable<Unit> GetUnits(Predicate<Unit> predicate) => Units.Where(u => predicate(u));

        public IEnumerable<Building> GetBuildings() => Buildings;
        public IEnumerable<Building> GetBuildings<T>() where T : Building => Buildings.Where(b => b is T);
        public IEnumerable<Building> GetBuildings(Tile t)
        {
            if (t == null)
            {
                Debug.LogWarning("Error when trying to get building: input tile is null");
                return null;
            }
            return Buildings.Where(b => b.CoOrds.X == t.CoOrds.X && b.CoOrds.Y == t.CoOrds.Y);
        }
        public IEnumerable<Building> GetBuildings(BuildingStatus status) => Buildings.Where(b => b.Status == status);
        public IEnumerable<Building> GetBuildings(Player player) => Buildings.Where(b => b.Owner == player);
        public IEnumerable<Building> GetBuildings(Predicate<Building> predicate) => Buildings.Where(b => predicate(b));

        public object Clone()
        {
            Map copy = (Map)MemberwiseClone();
            copy.Tiles = Tiles.Select(t => t.ToArray()).ToArray();
            copy.Units = Units.Select(u => (Unit)u.Clone()).ToList();
            return copy;
        }
    }

    public class RandomMap : Map, ICloneable
    {
        public RandomMap() { }

        // generation of a new random map
        public RandomMap(int width, int height) : base(width, height)
        {

        }
    }

    public abstract class Command : ICloneable
    {
        public Point Source { get; set; }
        public Point Destination { get; set; }
        public Unit Unit { get; set; }
        public abstract void Execute();
        public override string ToString() => "";

        public Command() { }
        public Command(Unit u) => Unit = u;
        public Command(Unit u, Point src, Point dest) => (Unit, Source, Destination) = (u, src, dest);
        public Command(Unit u, int srcX, int srcY, int destX, int destY) => (Unit, Source, Destination) = ( u, new Point(srcX, srcY), new Point(destX, destY));

        public object Clone()
        {
            Command copy = (Command)MemberwiseClone();
            copy.Unit = (Unit)Unit.Clone();
            return copy;
        }
    }

    public abstract class Customizable
    {
        public string Name { get; set; }
        public Cost Cost { get; set; } = new Cost();
    }
}

namespace SteelOfStalin.Flow
{
    public class Round : ICloneable 
    {
        public static Round Instance { get; private set; }

        public int Number { get; set; }
        public bool AreAllPlayersReady => Players.All(p => p.IsReady);

        [JsonIgnore] public List<Player> Players { get; set; }
        public List<Command> Commands { get; set; } = new List<Command>();
        public List<Phase> Phases { get; set; } = new List<Phase>();
        public Phase Planning { get; set; } = new Planning();
        
        public Round() 
        {
            Players = Battle.Instance.Players;
            Number = Battle.Instance.RoundNumber;
            Instance = this;
        }

        // Remove fired and moved flags from all units
        public void ResetUnitStatus() => Map.Instance.GetUnits().ToList().ForEach(u => u.Status &= ~(UnitStatus.MOVED | UnitStatus.FIRED));

        public void CommandPrerequisitesChecking()
        {
            // all units should be able to hold at round start
            Map.Instance.GetUnits().ToList().ForEach(u => u.AvailableCommands = AvailableCommands.HOLD);

            List<Unit> ActiveUnits = Map.Instance.GetUnits(UnitStatus.ACTIVE).ToList();
            foreach (Unit u in ActiveUnits)
            {
                if (u.CanMove())
                {
                    u.AvailableCommands |= AvailableCommands.MOVE;
                }
                if (u.CanMerge())
                {
                    u.AvailableCommands |= AvailableCommands.MERGE;
                }
                if (u.CanFire())
                {
                    u.AvailableCommands |= AvailableCommands.FIRE;
                }
                if (u.CanSabotage())
                {
                    u.AvailableCommands |= AvailableCommands.SABOTAGE;
                }
                if (u is Ground g)
                {
                    if (g.CanSuppress())
                    {
                        g.AvailableCommands |= AvailableCommands.SUPPRESS;
                    }
                    if (g.CanAmbush())
                    {
                        g.AvailableCommands |= AvailableCommands.AMBUSH;
                    }
                }
                if (u is Personnel p)
                {
                    if (p.CanAboard())
                    {
                        p.AvailableCommands |= AvailableCommands.ABOARD;
                    }
                    if (p.CanCapture())
                    {
                        p.AvailableCommands |= AvailableCommands.CAPTURE;
                    }
                    if (p.CanConstruct())
                    {
                        p.AvailableCommands |= AvailableCommands.CONSTRUCT;
                    }
                    if (p.CanFortify())
                    {
                        p.AvailableCommands |= AvailableCommands.FORTIFY;
                    }
                    if (Battle.Instance.Rules.DestroyedUnitsCanBeScavenged && p.CanScavenge())
                    {
                        p.AvailableCommands |= AvailableCommands.SCAVENGE;
                    }
                }
                if (u is Artillery a)
                {
                    if (a.CanAboard())
                    {
                        a.AvailableCommands |= AvailableCommands.ABOARD;
                    }
                    if (a.CanAssemble())
                    {
                        a.AvailableCommands |= AvailableCommands.ASSEMBLE;
                    }
                    if (a.CanDisassemble())
                    {
                        a.AvailableCommands |= AvailableCommands.DISASSEMBLE;
                    }
                }
            }
        }

        public void ActionPrerequisitesChecking()
        {
            Map.Instance.GetBuildings().ToList().ForEach(b =>
            {
                // TODO
            });
        }

        public void AddAutoCommands()
        {
            // TODO
        }

        public void WinnerChecking()
        {
            // TODO
        }

        public void ScreenUpdate()
        {
            // TODO handle screen update here
            IEnumerable<Unit> destroyed_unit = Map.Instance.GetUnits(UnitStatus.DESTROYED);
            destroyed_unit.ToList().ForEach(u => u.RemoveFromScene());
        }

        public void EndPlanning()
        {
            // add Hold command for any units that aren't assigned command
            Map.Instance.GetUnits(u => (u.Status != UnitStatus.IN_QUEUE || u.Status != UnitStatus.CAN_BE_DEPLOYED) && u.CommandAssigned == CommandAssigned.NONE).ToList().ForEach(u => Commands.Add(new Hold(u)));
            Phases.AddRange(new Phase[]
            {
                new Firing(Commands),
                new Moving(Commands),
                new CounterAttacking(),
                new Constructing(Commands),
                new Training(Commands),
                new Spotting(),
                new Signaling(),
                new Misc(Commands)
            });
            Phases.ForEach(p => p.Execute());
            ScreenUpdate();
            WinnerChecking();
            Commands.Clear();
        }

        // output this round to JSON
        public void Record()
        {

        }

        // load a round from JSON
        public void Replay()
        {

        }

        public object Clone()
        {
            Round copy = (Round)MemberwiseClone();
            copy.Players = Players.Select(p => (Player)p.Clone()).ToList();
            copy.Commands = Commands.Select(c => (Command)c.Clone()).ToList();
            copy.Phases = Phases.Select(p => (Phase)p.Clone()).ToList();
            return copy;
        }
    }

    public abstract class Phase : ICloneable
    {
        public List<Command> CommandsForThisPhase { get; set; } = new List<Command>();
        public virtual void Execute() => CommandsForThisPhase.ForEach(c => c.Execute());

        // empty ctor for (de)serialization
        public Phase()
        {
            
        }
        public Phase(List<Command> commands, params Type[] commandTypes) => Array.ForEach(commandTypes, t => CommandsForThisPhase.AddRange(commands.Where(c => c.GetType() == t)));

        public object Clone()
        {
            Phase copy = (Phase)MemberwiseClone();
            copy.CommandsForThisPhase = CommandsForThisPhase.Select(c => (Command)c.Clone()).ToList();
            return copy;
        }
    }

    public sealed class Planning : Phase
    {
        public Planning() { }
        public IEnumerator PhaseLoop(float wait_time)
        {
            float counter = 0;
            while (counter < wait_time || !Round.Instance.AreAllPlayersReady)
            {
                yield return new WaitForSeconds(1);
                counter += 1;
                Debug.Log($"Time remaining: {wait_time - counter} second(s)");
                yield return null;
            }
        }
    }
    public sealed class Moving : Phase
    {
        public Moving() : base() { }
        public Moving(List<Command> commands) 
            : base(commands, typeof(Hold), typeof(Move), typeof(Merge), typeof(Aboard), typeof(Disembark), typeof(Capture), typeof(Submerge), typeof(Surface), typeof(Landing)) { }

        public override void Execute()
        {
            // TODO resolve move conflicts here

            base.Execute();
        }
    }
    public sealed class Firing : Phase
    {
        public Firing() : base() { }
        public Firing(List<Command> commands) : base(commands, typeof(Fire), typeof(Suppress), typeof(Sabotage), typeof(Ambush)) { }

        public override void Execute()
        {
            IEnumerable<Unit> units = Map.Instance.GetUnits();
            units.Where(u => u.CurrentSuppressionLevel > 0).ToList().ForEach(u => 
            {
                u.CurrentSuppressionLevel -= u.Defense.Suppression.Resilience.ApplyMod();
                if (u.CurrentSuppressionLevel < 0)
                {
                    u.CurrentSuppressionLevel = 0;
                }
                if (u.CurrentSuppressionLevel < u.Defense.Suppression.Threshold)
                {
                    u.Status &= ~UnitStatus.SUPPRESSED;
                }
            });

            // TODO LOS logic here

            IEnumerable<Unit> suppress_targets = CommandsForThisPhase.Where(c => c is Suppress).Select(c => ((Suppress)c).Target);
            // reset cons. sup. round for those units with the following conditions:
            // 1. cons. sup. round > 0 (consecutively suppressed for at least 1 round before), and
            // 2. are not under suppression this round
            units.Where(u => u.ConsecutiveSuppressedRound > 0 && !suppress_targets.Contains(u)).ToList().ForEach(u => u.ConsecutiveSuppressedRound = 0);

            base.Execute();
            suppress_targets.ToList().ForEach(t =>
            {
                if (t.CurrentSuppressionLevel > t.Defense.Suppression.Threshold)
                {
                    t.Status |= UnitStatus.SUPPRESSED;
                }
            });
        }
    }
    public sealed class CounterAttacking : Phase
    {
        public CounterAttacking() : base() { }
        //public CounterAttacking(List<Command> commands) : base(commands, typeof(Ambush)) { }
        public override void Execute()
        {
            // TODO
        }
    }
    public sealed class Spotting : Phase
    {
        public Spotting() : base() { }

        public override void Execute()
        {
            // TODO add LOS logic
            Map.Instance.GetUnits().ToList().ForEach(u =>
            {
                u.UnitsInSight.Clear();
                u.BuildingsInSight.Clear();
                u.GetHostileUnitsInReconRange().ToList().ForEach(h =>
                {
                    double st_line_distance = CubeCoordinates.GetStraightLineDistance(u.CubeCoOrds, h.CubeCoOrds);
                    if (Formula.VisualSpotting((u, h, st_line_distance)))
                    {
                        u.UnitsInSight.Add(h);
                    }
                });
                u.GetHostileBuildingsInReconRange().ToList().ForEach(b =>
                {
                    double st_line_distance = CubeCoordinates.GetStraightLineDistance(u.CubeCoOrds, b.CubeCoOrds);
                    if (Formula.VisualSpottingForBuildings((u, b, st_line_distance)))
                    {
                        u.BuildingsInSight.Add(b);
                    }
                });
                // TODO add acousting ranging logic
            });
        }
    }
    public sealed class Signaling : Phase
    {
        public Signaling() : base() { }

        public override void Execute()
        {
            // TODO
        }
    }
    public sealed class Constructing : Phase
    {
        public Constructing() : base() { }
        public Constructing(List<Command> commands) : base(commands, typeof(Construct), typeof(Fortify), typeof(Demolish)) { }

        public override void Execute()
        {
            // TODO
            base.Execute();
        }
    }
    public sealed class Training : Phase
    {
        public Training() : base() { }
        public Training(List<Command> commands) : base(commands, typeof(Train), typeof(Deploy), typeof(Rearm)) { }

        public override void Execute()
        {
            Map.Instance.GetBuildings<UnitBuilding>().Where(ub => ub.Status == BuildingStatus.ACTIVE).ToList().ForEach(ub =>
            {
                UnitBuilding b = (UnitBuilding)ub;
                while (b.TrainingQueue.Count > 0)
                {
                    // retrieve the first unit in queue without removing it from the queue
                    Unit queueing = b.TrainingQueue.Peek();
                    queueing.TrainingTimeRemaining -= 1;

                    if (queueing.TrainingTimeRemaining <= 0)
                    {
                        queueing.TrainingTimeRemaining = 0;

                        // if capacity is reached, no more units will be ready to be deployed
                        if (b.ReadyToDeploy.Count < b.QueueCapacity)
                        {
                            // retrieve the first unit in queue and remove it from the queue
                            Unit ready = b.TrainingQueue.Dequeue();

                            // change its status
                            ready.Status &= ~UnitStatus.IN_QUEUE;
                            ready.Status |= UnitStatus.CAN_BE_DEPLOYED;
                            // add it to deploy list
                            b.ReadyToDeploy.Add(ready);
                        }
                        continue;
                    }
                    break;
                }
            });
            base.Execute();
        }
    }
    public sealed class Misc : Phase
    {
        public Misc() : base() { }
        public Misc(List<Command> commands) 
            : base(commands, typeof(Scavenge), typeof(Disassemble), typeof(Assemble)) { }

        public override void Execute()
        {
            base.Execute();
            CalculateMorale();
            AddDestroyedFlags();
            Battle.Instance.Players.ForEach(p => p.ProduceResources());
        }

        public void AddDestroyedFlags()
        {
            // strength <= 0 or no fuel
            Predicate<Unit> unit_destroyed = u => u.Defense.Strength <= 0 || (u is Aerial && u.Carrying.Fuel <= 0);
            Predicate<Building> building_destroyed = b => b.Durability <= 0;

            Map.Instance.GetUnits(unit_destroyed).ToList().ForEach(u => u.Status = UnitStatus.DESTROYED);
            Map.Instance.GetBuildings(building_destroyed).ToList().ForEach(b => b.Status = BuildingStatus.DESTROYED);
        }

        public void CalculateMorale()
        {
            Map.Instance.GetUnits(u => u.Carrying.Supplies == 0).ToList().ForEach(u =>
            {
                // TODO add some variations to the penalty
                u.Morale.MinusEquals(10);
                if (u.Morale < 0)
                {
                    u.Morale.Value = 0;
                    // TODO add some variations to the chance
                    if (new System.Random().NextDouble() < 0.1)
                    {
                        // TODO add some chance for surrender effect: enemy can capture this unit
                        u.Status = UnitStatus.DESTROYED;
                    }
                }
            });
        }
    }
}

