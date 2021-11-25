using SteelOfStalin.Attributes;
using SteelOfStalin.Commands;
using SteelOfStalin.Flow;
using SteelOfStalin.Props.Tiles;
using SteelOfStalin.Props.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SteelOfStalin
{
    //Contains all information of the game itself
    public class Game
    {
        // Handles scenes (un)loading
        public SceneManager SceneManager { get; set; } = new SceneManager();
        public static List<Battle> Battles { get; set; } = new List<Battle>();
        public static GameSettings Settings { get; set; } = new GameSettings();
        // TODO add achievements later on
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

    public class Battle
    {
        public string Name { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public BattleRules Rules { get; set; } = new BattleRules();
        // caching at most 10 rounds, make the capacity configurable maybe
        public Stack<Round> Rounds = new Stack<Round>(10);

        public void Start()
        {
            Rounds.Push(new Round(Players));
        }
        public void Save()
        {

        }
        public void Load()
        {

        }
    }

    // Contains different rules of the battle, like how much time is allowed for each round etc.
    public class BattleRules
    {
        public int TimeForEachRound { get; set; } = 120; // in seconds
        public bool IsFogOfWar { get; set; } = true;
        public bool UnitsCanBeScavenged { get; set; }
        public bool AllowUniversalQueue { get; set; }

    }

    public class Map : ICloneable
    {
        public Tile[][] Tiles { get; set; }
        public List<Unit> Units { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public void Save()
        {

        }

        public void Load()
        {

        }

        public Tile GetTile(int x, int y) => Tiles[x][y];
        public Tile GetTile(Point p) => Tiles[p.X][p.Y];
        public Tile GetTile(CubeCoordinates c) => Tiles[((Point)c).X][((Point)c).Y];

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

        public IEnumerable<Tile> GetAccessibleNeigbours(Unit u, int distance = 1)
        {
            // TODO
            return null;
        }

        // TODO: add support for inaccessible tiles corresponding to each type of units (i.e. land units cannot access ocean/river, naval units cannot access land)
        // TODO: add support for aerial units (i.e. weight for each tile is 1 and cost is constant)
        public List<Tile> GetPath(Tile start, Tile end, Unit u, PathfindingOptimization opt = PathfindingOptimization.LEAST_SUPPLIES_COST)
        {
            if (u is Personnel && opt == PathfindingOptimization.LEAST_FUEL_COST)
            {
                throw new ArgumentException("Only units with fuel capacity can be used with fuel cost optimization for pathfinding.");
            }

            WeightedTile w_start = start.ConvertToWeightedTile(u.Consumption, opt, end);
            WeightedTile w_end = end.ConvertToWeightedTile(u.Consumption, opt, end);

            List<WeightedTile> active = new List<WeightedTile>();
            List<WeightedTile> visited = new List<WeightedTile>();
            List<Tile> path = new List<Tile>();
            Func<WeightedTile, double> sort = 
                opt == PathfindingOptimization.LEAST_SUPPLIES_COST 
                ? new Func<WeightedTile, double>(w => w.SuppliesCostDistance) 
                : w => w.FuelCostDistance;

            active.Add(w_start);
            while (active.Count > 0)
            {
                WeightedTile w_check = active.OrderBy(sort).FirstOrDefault();
                if (w_check == w_end 
                    || w_check.SuppliesCostSoFar > u.Carrying.Supplies.ApplyMod()
                    || (!(u is Personnel) && w_check.FuelCostSoFar > u.Carrying.Fuel.ApplyMod())
                    || w_check.DistanceSoFar > u.Maneuverability.Speed.ApplyMod())
                {
                    while (w_check.Parent != null)
                    {
                        path.Add(GetTile(w_check.CubeCoOrds));
                        w_check = w_check.Parent;
                    }
                    return path;
                }
                visited.Add(w_check);
                _ = active.Remove(w_check);

                List<WeightedTile> w_neibours = new List<WeightedTile>();
                GetNeigbours(w_check.CubeCoOrds).ToList().ForEach(t => w_neibours.Add(t.ConvertToWeightedTile(u.Consumption, opt, end, w_check)));

                w_neibours.ForEach(n =>
                {
                    if (visited.Where(v => v == n).Count() == 0)
                    {
                        WeightedTile w_exist = active.Where(a => a == n).FirstOrDefault();
                        if (w_exist != null)
                        {
                            // exist in active list
                            bool moreExpensive =
                                opt == PathfindingOptimization.LEAST_SUPPLIES_COST
                                ? w_exist.SuppliesCostDistance > w_check.SuppliesCostDistance
                                : w_exist.FuelCostDistance > w_check.FuelCostDistance;

                            // remove from active list if it costs more than current (w_check)
                            if (moreExpensive)
                            {
                                _ = active.Remove(w_exist);
                            }
                        }
                        else
                        {
                            // add the neigbour to active list
                            active.Add(n);
                        }
                    }
                });
            }
            return path;
        }

        public object Clone()
        {
            Map copy = (Map)MemberwiseClone();
            copy.Tiles = Tiles.Select(t => t.ToArray()).ToArray();
            copy.Units = Units.Select(u => (Unit)u.Clone()).ToList();
            return copy;
        }
    }

    public abstract class Command : ICloneable
    {
        public Point Source { get; set; }
        public Point Destination { get; set; }
        public Unit Unit { get; set; }
        public abstract void Execute(Map map);
        public override string ToString() => "";

        public object Clone()
        {
            Command copy = (Command)MemberwiseClone();
            copy.Unit = (Unit)Unit.Clone();
            return copy;
        }
    }
}

namespace SteelOfStalin.Flow
{
    public class Round : ICloneable
    {
        public int Number { get; set; }
        public bool AreAllPlayersReady => Players.Count(p => p.IsReady) == Players.Count();

        public Map Map { get; set; }
        public List<Player> Players { get; set; }
        public List<Command> Commands = new List<Command>();
        public List<Phase> Phases = new List<Phase>();
        
        public Round() { }
        public Round(List<Player> players) => Players = players;

        public void EndPlanning()
        {
            Phases.AddRange(new Phase[]
            {
                new Firing(Commands),
                new Moving(Commands),
                new CounterAttacking(Commands),
                new Constructing(Commands),
                new Training(Commands),
                new Spotting(),
                new Signaling(),
                new Misc(Commands)
            });
            Phases.ForEach(p => p.Execute(Map));
            // TODO handle screen cleanup here
        }
        public void Record()
        {

        }
        public void Replay()
        {

        }

        public object Clone()
        {
            Round copy = (Round)MemberwiseClone();
            copy.Map = (Map)Map.Clone();
            copy.Players = Players.Select(p => (Player)p.Clone()).ToList();
            copy.Commands = Commands.Select(c => (Command)c.Clone()).ToList();
            copy.Phases = Phases.Select(p => (Phase)p.Clone()).ToList();
            return copy;
        }
    }

    public abstract class Phase : ICloneable
    {
        public List<Command> CommandsForThisPhase { get; set; } = new List<Command>();
        public virtual void Execute(Map map) => CommandsForThisPhase.ForEach(c => c.Execute(map));

        // empty ctor for (de)serialization
        public Phase() { }
        public Phase(List<Command> commands, params Type[] commandTypes) 
            => Array.ForEach(commandTypes, t => CommandsForThisPhase.AddRange(commands.Where(c => c.GetType() == t)));

        public object Clone()
        {
            Phase copy = (Phase)MemberwiseClone();
            copy.CommandsForThisPhase = CommandsForThisPhase.Select(c => (Command)c.Clone()).ToList();
            return copy;
        }
    }

    public sealed class Moving : Phase
    {
        public Moving() : base() { }
        public Moving(List<Command> commands) 
            : base(commands, typeof(Hold), typeof(Move), typeof(Merge), typeof(Aboard), typeof(Disembark), typeof(Capture)) { }

    }
    public sealed class Firing : Phase
    {
        public Firing() : base() { }
        public Firing(List<Command> commands) : base(commands, typeof(Fire), typeof(Suppress), typeof(Sabotage)) { }

    }
    public sealed class CounterAttacking : Phase
    {
        public CounterAttacking() : base() { }
        public CounterAttacking(List<Command> commands) : base(commands, typeof(Ambush)) { }
    }
    public sealed class Spotting : Phase
    {
        public Spotting() : base() { }

    }
    public sealed class Signaling : Phase
    {
        public Signaling() : base() { }

    }
    public sealed class Constructing : Phase
    {
        public Constructing() : base() { }
        public Constructing(List<Command> commands) : base(commands, typeof(Construct), typeof(Fortify), typeof(Demolish)) { }
    }
    public sealed class Training : Phase
    {
        public Training() : base() { }
        public Training(List<Command> commands) : base(commands, typeof(Train), typeof(Deploy)) { }
    }
    public sealed class Misc : Phase
    {
        public Misc() : base() { }
        public Misc(List<Command> commands) 
            : base(commands, typeof(Scanvenge), typeof(Disassemble), typeof(Assemble)) { }
    }
}

