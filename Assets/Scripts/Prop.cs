using SteelOfStalin.Attributes;
using SteelOfStalin.Assets.Customizables;
using SteelOfStalin.Assets.Customizables.Modules;
using SteelOfStalin.CustomTypes;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Assets.Props.Units.Land;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SteelOfStalin.Util.Utilities;
using Attribute = SteelOfStalin.Attributes.Attribute;
using Resources = SteelOfStalin.Attributes.Resources;
using SteelOfStalin.Util;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

namespace SteelOfStalin.Assets.Props
{
    [Flags]
    public enum PropConnection
    {
        NONE = 0,
        POS_1 = 1 << 0,
        POS_2 = 1 << 1,
        POS_3 = 1 << 2,
        POS_4 = 1 << 3,
        POS_5 = 1 << 4,
        POS_6 = 1 << 5,
        ALL = ~0
    }

    public abstract class Prop : ICloneable, IEquatable<Prop>, INamedAsset
    {
        public const float HEX_APOTHEM = 18.9F; // apothem of a hex in unity scale

        public static Dictionary<PropConnection, string> UniqueVariants => new Dictionary<PropConnection, string>()
        {
            [PropConnection.POS_1] = "l",
            [PropConnection.POS_1 | PropConnection.POS_2] = "o",
            [PropConnection.POS_1 | PropConnection.POS_3] = "m",
            [PropConnection.POS_1 | PropConnection.POS_4] = "p",
            [PropConnection.POS_1 | PropConnection.POS_2 | PropConnection.POS_3] = "e",
            [PropConnection.POS_1 | PropConnection.POS_2 | PropConnection.POS_4] = "u",
            [PropConnection.POS_1 | PropConnection.POS_3 | PropConnection.POS_4] = "h",
            [PropConnection.POS_2 | PropConnection.POS_4 | PropConnection.POS_6] = "y",
            [PropConnection.POS_1 | PropConnection.POS_2 | PropConnection.POS_3 | PropConnection.POS_4] = "k",
            [PropConnection.POS_1 | PropConnection.POS_2 | PropConnection.POS_4 | PropConnection.POS_6] = "t",
            [PropConnection.POS_2 | PropConnection.POS_3 | PropConnection.POS_5 | PropConnection.POS_6] = "x",
            [PropConnection.POS_1 | PropConnection.POS_2 | PropConnection.POS_3 | PropConnection.POS_5 | PropConnection.POS_6] = "s",
            [PropConnection.ALL] = "a"
        };

        public string Name { get; set; }
        public string MeshName { get; set; } = "";
        public Coordinates CoOrds { get; set; }

        [JsonIgnore] public CubeCoordinates CubeCoOrds => (CubeCoordinates)CoOrds;
        [JsonIgnore] public PropConnection PropConnection { get; set; }
        [JsonIgnore] public virtual GameObject PropObject => GameObject.Find(MeshName);
        [JsonIgnore] public virtual PropObject PropObjectComponent => PropObject.GetComponent<PropObject>();
        [JsonIgnore] public Vector3 OnScreenCoordinates => PropObject.transform.position;

        public Prop() { }
        public Prop(Prop another) => (Name, MeshName, CoOrds) = (another.Name, another.MeshName, new Coordinates(another.CoOrds));

        // use CoOrds.ToString() and CubeCoOrds.ToString() directly for printing coords and cube coords
        public string PrintMembers() => Utilities.PrintMembers(this);

        // TODO FUT. Impl. cache as much as possible here, like 
        public virtual void AddToScene()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != "Game" && scene != "Loading")
            {
                Debug.LogError($"Cannot add gameobject to scene: Current scene ({scene}) is neither Game nor Loading.");
                return;
            }

            // TODO FUT. Impl. handle connected props in a separate method
            string name_with_suffix = Name;
            Quaternion quad = Quaternion.identity;
            if (GetType() == typeof(Boundary))
            {
                string variant = GetVariantString(GetConnection());

                Match match = Regex.Match(variant, @"([lompeuhyktxsa])([+\-=]\d?)?");
                if (!match.Success)
                {
                    Debug.LogError($"Failed to match variant string {variant}");
                }

                name_with_suffix += $"_{match.Groups[1].Value}";
                string rot = match.Groups[2].Value;
                if (!string.IsNullOrEmpty(rot))
                {
                    quad = Quaternion.Euler(0, rot == "=" ? 180 : int.Parse(rot) * 60, 0);
                }
            }

            GameObject g = Game.GameObjects.Find(g => g.name == name_with_suffix);
            if (g == null)
            {
                Debug.LogError($"Cannot find game object with name {name_with_suffix}");
                return;
            }

            GameObject cloned = UnityEngine.Object.Instantiate(g, CalculateOnScreenCoordinates(), quad);
            if (cloned.GetComponent<PropObject>() == null)
            {
                cloned.AddComponent<PropObject>();
            }
            if (string.IsNullOrEmpty(MeshName))
            {
                SetMeshName();
            }
            cloned.name = MeshName;

            if (this is IOwnableAsset ownable && !string.IsNullOrEmpty(ownable.OwnerName))
            {
                PropObjectComponent.SetColorForAllChildren(ownable.Owner.Color);
            }
        }
        public virtual void RemoveFromScene() => UnityEngine.Object.Destroy(PropObject);
        public Vector3 CalculateOnScreenCoordinates() => new Vector3(2 * HEX_APOTHEM * CoOrds.X * (float)Math.Cos(Math.PI / 6), 0, 2 * HEX_APOTHEM * (CoOrds.Y + (CoOrds.X % 2 == 1 ? 0.5F : 0)));
        public virtual void UpdateOnScreenLocation()
        {
            if (PropObject != null)
            {
                PropObject.transform.position = CalculateOnScreenCoordinates();
            }
        }

        public void SetMeshName()
        {
            if (string.IsNullOrEmpty(MeshName))
            {
                MeshName = $"{Name}_{Guid.NewGuid().ToString().Replace("-", "")}";
            }
        }

        public PropConnection GetConnection()
        {
            PropConnection conn = PropConnection.NONE;
            IEnumerable<CubeCoordinates> connected = GetNeighboursWithSameType().Select(p => p.CubeCoOrds);

            foreach (CubeCoordinates coor in connected)
            {
                Direction direction = CubeCoOrds.GetDirectionTo(coor);
                conn |= direction switch
                {
                    Direction.NONE => 0,
                    Direction.W => PropConnection.POS_1,
                    Direction.E => PropConnection.POS_2,
                    Direction.D => PropConnection.POS_3,
                    Direction.S => PropConnection.POS_4,
                    Direction.A => PropConnection.POS_5,
                    Direction.Q => PropConnection.POS_6,
                    _ => throw new NotImplementedException($"Unknown direction {direction}"),
                };
            }
            return conn;
        }
        public static string GetVariantString(PropConnection connection)
        {
            if (UniqueVariants.ContainsKey(connection))
            {
                return UniqueVariants[connection];
            }

            int num_rotations;
            PropConnection rotate = connection;
            for (num_rotations = 1; num_rotations <= 3; num_rotations++)
            {
                rotate = Rotate(connection, num_rotations);
                if (UniqueVariants.ContainsKey(rotate))
                {
                    // rotate connection clockwise to get the unique variant == rotate unique variant anti-clockwise to get the connection
                    num_rotations = -num_rotations;
                    break;
                }
                rotate = Rotate(connection, -num_rotations);
                if (UniqueVariants.ContainsKey(rotate))
                {
                    break;
                }
            }
            return UniqueVariants[rotate] + (num_rotations == 3 || num_rotations == -3 ? "=" : num_rotations.ToString("+0;-0"));
        }
        public static PropConnection Rotate(PropConnection original, int thirds_of_pi)
        {
            if (Math.Abs(thirds_of_pi) > 3)
            {
                throw new ArgumentOutOfRangeException($"thirds of pi must be with in [-3, 3]");
            }

            PropConnection rotated = original;
            if (thirds_of_pi < 0)
            {
                int cycles = (int)Math.Abs(thirds_of_pi);
                for (int i = 0; i < cycles; i++)
                {
                    bool needs_cyclic = rotated.HasFlag(PropConnection.POS_1);
                    rotated = (PropConnection)((int)rotated >> 1);
                    if (needs_cyclic)
                    {
                        rotated |= PropConnection.POS_6;
                    }
                }

            }
            else if (thirds_of_pi > 0)
            {
                for (int i = 0; i < thirds_of_pi; i++)
                {
                    bool needs_cyclic = rotated.HasFlag(PropConnection.POS_6);
                    rotated = (PropConnection)(((int)rotated << 1) % (1 << 6));
                    if (needs_cyclic)
                    {
                        rotated |= PropConnection.POS_1;
                    }
                }
            }
            return rotated;
        }

        public virtual int GetDistance(Prop prop) => CubeCoordinates.GetDistance(CubeCoOrds, prop.CubeCoOrds);
        public virtual decimal GetStraightLineDistance(Prop prop) => CubeCoordinates.GetStraightLineDistance(CubeCoOrds, prop.CubeCoOrds);
        public virtual IEnumerable<Prop> GetNeighboursWithSameType() => CubeCoOrds.GetNeighbours(include_self: false).SelectMany(c => Map.Instance.GetProps(p => p.CubeCoOrds == c && p.GetType() == GetType()));
        public virtual Tile GetLocatedTile() => Map.Instance.GetTile(CoOrds);

        public virtual object Clone()
        {
            Prop copy = (Prop)MemberwiseClone();
            copy.CoOrds = (Coordinates)CoOrds.Clone();
            return copy;
        }
        public virtual bool Equals(Prop other) => !string.IsNullOrEmpty(MeshName) && MeshName == other.MeshName;
        public override string ToString() => $"{Name} {CoOrds}";
    }

    public class PropObject : MonoBehaviour
    {
        public AudioClip AudioOnPlaced { get; set; }
        public AudioClip AudioOnClicked { get; set; }
        public AudioClip AudioOnDestroy { get; set; }
        public AudioClip AudioOnFire { get; set; }
        public AudioClip AudioOnMove { get; set; }

        public List<Material> RendererMaterials { get; set; }
        public List<Material> SkinRendererMaterials { get; set; }

        public PropEventTrigger Trigger => GetComponent<PropEventTrigger>();

        public string PrintOnScreenCoOrds() => $"({gameObject.transform.position.x},{gameObject.transform.position.y},{gameObject.transform.position.z})";
        public Coordinates GetCoordinates() => Map.Instance.GetProp(gameObject).CoOrds;

        public GameObject GetClickedObject(BaseEventData data) => ((PointerEventData)data).pointerClick;
        public GameObject GetEnteredObject(BaseEventData data) => ((PointerEventData)data).pointerEnter;
        public Prop GetClickedProp(BaseEventData data) => Map.Instance.GetProp(GetClickedObject(data));
        public Prop GetEnteredProp(BaseEventData data) => Map.Instance.GetProp(GetEnteredObject(data));
        public PropObject GetClickedPropObjectComponent(BaseEventData data) => GetClickedObject(data).GetComponent<PropObject>();
        public PropObject GetEnteredPropObjectComponent(BaseEventData data) => GetEnteredObject(data).GetComponent<PropObject>();

        public virtual void Start()
        {
            if (Trigger == null)
            {
                gameObject.AddComponent<PropEventTrigger>();
            }
            //TODO: separate all types of prop and implement the menu navigation
            Trigger.Subscribe("focus", EventTriggerType.PointerClick, (data) => {
                if (!UIUtil.instance.isBlockedByUI()) {
                    Prop p = GetClickedProp(data);
                    if (p is Cities c && c.IsOwn(Battle.Instance.Self))
                    {
                        TrainPanel.instance.SetCity(c);
                    }
                    else if (p is Unit u) {
                        UnitPanel.instance.SetUnit(u);
                        if (u.IsOwn(Battle.Instance.Self)) {
                            CommandPanel.instance.SetUnit(u);
                        }
                    }
                    else
                    {
                        UnitPanel.instance.Hide();
                        DeployPanel.instance.HideAll();
                        CommandPanel.instance.Hide();
                    }
                }
            });
            Trigger.Subscribe("tooltip_show", EventTriggerType.PointerEnter, (data) => {
                if (!UIUtil.instance.isBlockedByUI())
                {
                    Prop p = GetEnteredProp(data);
                    StringBuilder sb = new StringBuilder();
                    if (p is Tiles.Boundary b) {
                        sb.AppendLine(b.Name);
                        sb.AppendLine(b.CoOrds.ToString());
                        sb.Append("Impassable");
                    }
                    else if (p is Tile t)
                    {
                        sb.AppendLine(t.Name);
                        if (t is Cities c &&c.Owner!=null) {
                            sb.AppendLine($"Owner: {c.OwnerName}");
                            sb.AppendLine($"Morale: {c.Morale.ApplyMod()}");
                        }
                        sb.AppendLine(t.CoOrds.ToString());
                        sb.AppendLine($"Concealment Mod: {t.TerrainMod.Concealment.Value}%");
                        sb.AppendLine($"Fuel Mod: {t.TerrainMod.Fuel.Value}%");
                        sb.AppendLine($"Supplies Mod: {t.TerrainMod.Supplies.Value}%");
                        sb.AppendLine($"Mobility Mod: {t.TerrainMod.Mobility.Value}%");
                        sb.Append($"Recon Mod: {t.TerrainMod.Recon.Value}");

                    }
                    else if (p is Unit u)
                    {
                        sb.AppendLine(u.Name);
                        sb.Append(u.OwnerName);
                    }

                    Tooltip.ShowTooltip_Static(sb.ToString());


                }
            });
            Trigger.Subscribe("tooltip_hide", EventTriggerType.PointerExit, (data) => Tooltip.HideTooltip_Static());
            Trigger.Subscribe("deploy_tile", EventTriggerType.PointerClick, (data) => {
                if (!UIUtil.instance.isBlockedByUI())
                {
                    Prop p = GetClickedProp(data);
                    if (p is Tile t)
                    {
                        DeployPanel.instance.DeployUnit(t);
                    }
                }
            },false);
            Trigger.Subscribe("move_tile", EventTriggerType.PointerClick, (data) => {
                if (!UIUtil.instance.isBlockedByUI())
                {
                    Prop p = GetClickedProp(data);
                    if (p is Tile t)
                    {
                        CommandPanel.instance.ExecuteCommand("Move",t);
                    }
                }
            }, false);
            Trigger.Subscribe("fire_unit", EventTriggerType.PointerClick, (data) => {
                if (!UIUtil.instance.isBlockedByUI())
                {
                    Prop p = GetClickedProp(data);
                    if (p is Unit u)
                    {
                        CommandPanel.instance.ExecuteCommand("Fire", u);
                    }
                }
            }, false);
            Trigger.Subscribe("suppress_unit", EventTriggerType.PointerClick, (data) => {
                if (!UIUtil.instance.isBlockedByUI())
                {
                    Prop p = GetClickedProp(data);
                    if (p is Unit u)
                    {
                        CommandPanel.instance.ExecuteCommand("Suppress", u);
                    }
                }
            }, false);
            Trigger.Subscribe("sabotage_building", EventTriggerType.PointerClick, (data) => {
                if (!UIUtil.instance.isBlockedByUI())
                {
                    Prop p = GetClickedProp(data);
                    if (p is Tile t)
                    {
                        CommandPanel.instance.ExecuteCommand("Sabotage", t);
                    }
                }
            }, false);



        }

        public virtual void OnMouseDown()
        {

        }

        public virtual void OnDestroy()
        {

        }

        public void SetColorForChild(Color color, string child_name)
        {
            MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                mr = gameObject.GetComponentInSpecificChild<MeshRenderer>(child_name);
                if (mr == null)
                {
                    Debug.LogError($"Cannot find {child_name} in gameobject {name}");
                    return;
                }
            }
            // note: use _Color if not using HDRP
            mr.material.SetColor("_BaseColor", color);
        }

        public void SetColorForAllChildren(Color color)
        {
            GetComponentsInChildren<MeshRenderer>().ToList().ForEach(mr => mr.material.SetColor("_BaseColor", color));
            GetComponentsInChildren<SkinnedMeshRenderer>().ToList().ForEach(mr => mr.material.SetColor("_BaseColor", color));
        }

        public void Highlight() {
            Color highlightColor = Color.green;
            RendererMaterials = GetComponentsInChildren<MeshRenderer>().ToList().ConvertAll<Material>(mr =>new Material(mr.material));
            SkinRendererMaterials= GetComponentsInChildren<SkinnedMeshRenderer>().ToList().ConvertAll<Material>(mr => new Material(mr.material));
            SetColorForAllChildren(highlightColor);
        }

        public void RestoreHighlight() {
            List<MeshRenderer> renderers = GetComponentsInChildren<MeshRenderer>().ToList();
            if (RendererMaterials == null) return;
            if (RendererMaterials.Count != renderers.Count) return;
            for (int i = 0; i < renderers.Count; i++) {
                renderers[i].material=RendererMaterials[i]; 
            }
            List<SkinnedMeshRenderer> skinRenderers = GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
            if (SkinRendererMaterials == null) return;
            if (SkinRendererMaterials.Count != skinRenderers.Count) return;
            for (int i = 0; i < skinRenderers.Count; i++)
            {
                skinRenderers[i].material = SkinRendererMaterials[i];
            }


        }
    }
}

namespace SteelOfStalin.Assets.Props.Units
{
    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum UnitStatus
    {
        NONE = 0,
        IN_QUEUE = 1 << 0,
        CAN_BE_DEPLOYED = 1 << 1,
        ACTIVE = 1 << 2,
        MOVED = 1 << 3,
        FIRED = 1 << 4,
        SUPPRESSED = 1 << 5,
        AMBUSHING = 1 << 6,
        CONSTRUCTING = 1 << 7,
        DISCONNECTED = 1 << 8,
        WRECKED = 1 << 9,
        DESTROYED = 1 << 10,
        IN_FIELD = ~IN_QUEUE & ~CAN_BE_DEPLOYED & ~WRECKED & ~DESTROYED & ~NONE,
        IMMOBILE = SUPPRESSED | AMBUSHING | CONSTRUCTING | DISCONNECTED,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableMovementCommands
    {
        NONE = 0,
        HOLD = 1 << 0,
        MOVE = 1 << 1,
        MERGE = 1 << 2,
        SUBMERGE = 1 << 3,
        SURFACE = 1 << 4,
        LAND = 1 << 5,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableFiringCommands
    {
        NONE = 0,
        FIRE = 1 << 0,
        SUPPRESS = 1 << 1,
        SABOTAGE = 1 << 2,
        AMBUSH = 1 << 3,
        BOMBARD = 1 << 4,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableLogisticsCommands
    {
        NONE = 0,
        ABOARD = 1 << 0,
        DISEMBARK = 1 << 1,
        LOAD = 1 << 2,
        UNLOAD = 1 << 3,
        RESUPPLY = 1 << 4,
        REPAIR = 1 << 5,
        RECONSTRUCT = 1 << 6,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableConstructionCommands
    {
        NONE = 0,
        FORTIFY = 1 << 0,
        CONSTRUCT = 1 << 1,
        DEMOLISH = 1 << 2,
        ALL = ~0
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AvailableMiscCommands
    {
        NONE = 0,
        CAPTURE = 1 << 0,
        SCAVENGE = 1 << 1,
        ASSEMBLE = 1 << 2,
        DISASSEMBLE = 1 << 3,
        ALL = ~0
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommandAssigned
    {
        NONE,
        HOLD,
        MOVE,
        MERGE,
        SUBMERGE,
        SURFACE,
        LAND,
        FIRE,
        SUPPRESS,
        SABOTAGE,
        AMBUSH,
        BOMBARD,
        ABOARD,
        DISEMBARK,
        LOAD,
        UNLOAD,
        RESUPPLY,
        REPAIR,
        RECONSTRUCT,
        FORTIFY,
        CONSTRUCT,
        DEMOLISH,
        CAPTURE,
        SCAVENGE,
        ASSEMBLE,
        DISASSEMBLE,
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum AutoCommands
    {
        NONE = 0,
        MOVE = 1 << 0,
        FIRE = 1 << 1,
        RESUPPLY = 1 << 2,
    }

    [JsonConverter(typeof(AssetConverter<Unit>))]
    public abstract class Unit : Prop, IOwnableAsset, ICloneable, IEquatable<Unit>
    {
        public UnitStatus Status { get; set; }
        [JsonIgnore] public Player Owner { get; set; }
        public string OwnerName { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Maneuverability Maneuverability { get; set; } = new Maneuverability();
        public Defense Defense { get; set; } = new Defense();
        public Resources Consumption { get; set; } = new Resources();
        public Resources Carrying { get; set; } = new Resources();
        public Resources Capacity { get; set; } = new Resources();
        public Scouting Scouting { get; set; } = new Scouting();
        public Attribute Morale { get; set; } = new Attribute(100);

        public List<Unit> UnitsInSight { get; set; } = new List<Unit>();
        public List<Unit> UnitsUnknown { get; set; } = new List<Unit>();
        public List<Building> BuildingsInSight { get; set; } = new List<Building>();

        public CommandAssigned CommandAssigned { get; set; } = CommandAssigned.NONE;
        public AvailableMovementCommands AvailableMovementCommands { get; set; } = AvailableMovementCommands.HOLD;
        public AvailableFiringCommands AvailableFiringCommands { get; set; } = AvailableFiringCommands.NONE;
        public AvailableLogisticsCommands AvailableLogisticsCommands { get; set; } = AvailableLogisticsCommands.NONE;
        public AvailableConstructionCommands AvailableConstructionCommands { get; set; } = AvailableConstructionCommands.NONE;
        public AvailableMiscCommands AvailableMiscCommands { get; set; } = AvailableMiscCommands.NONE;
        public AutoCommands AutoCommands { get; set; } = AutoCommands.NONE;
        public List<Tile> AutoNavigationPath { get; set; } = new List<Tile>();

        public decimal CurrentSuppressionLevel { get; set; } = 0;
        public int ConsecutiveSuppressedRound { get; set; } = 0;
        public decimal TrainingTimeRemaining { get; set; } = 0;

        [JsonIgnore] public bool IsInField => Status.HasAnyOfFlags(UnitStatus.IN_FIELD);
        [JsonIgnore] public bool IsSuppressed => Status.HasFlag(UnitStatus.SUPPRESSED);
        [JsonIgnore] public bool IsConstructing => Status.HasFlag(UnitStatus.CONSTRUCTING);
        [JsonIgnore] public bool CarryingIsFull => Carrying == Capacity;

        public bool IsOwn(Player p) => p != null && Owner == p;
        public bool IsAlly(Player p) => Owner?.Allies.Any(a => a == p) ?? false;
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();
        public bool IsSameSubclassOf<T>(Unit another) where T : Unit => GetType().IsSubclassOf(typeof(T)) && another.GetType().IsSubclassOf(typeof(T));
        public bool IsOfSameCategory(Unit another) => IsSameSubclassOf<Ground>(another) || IsSameSubclassOf<Naval>(another) || IsSameSubclassOf<Aerial>(another);

        // used for spotting phase only, reset to null at round start, need not to be saved (serialized)
        [JsonIgnore] public IOffensiveCustomizable WeaponFired { get; set; }

        // Parameterless constructors are used for (de)serialization
        public Unit() : base() { }
        public Unit(Unit another) : base(another)
            => (Owner, OwnerName, Status, Cost, Maneuverability, Defense, Consumption, Carrying, Capacity, Scouting, Morale, UnitsInSight, UnitsUnknown, BuildingsInSight, 
                CommandAssigned, AvailableMovementCommands, AvailableFiringCommands, AvailableLogisticsCommands, AvailableConstructionCommands, AvailableMiscCommands, AutoCommands, 
                AutoNavigationPath, CurrentSuppressionLevel, ConsecutiveSuppressedRound, TrainingTimeRemaining)
            = (another.Owner,
               another.OwnerName,
               another.Status,
               (Cost)another.Cost.Clone(),
               (Maneuverability)another.Maneuverability.Clone(),
               (Defense)another.Defense.Clone(),
               (Resources)another.Consumption.Clone(),
               (Resources)another.Carrying.Clone(),
               (Resources)another.Capacity.Clone(),
               (Scouting)another.Scouting.Clone(),
               (Attribute)another.Morale.Clone(),
                new List<Unit>(another.UnitsInSight),
                new List<Unit>(another.UnitsUnknown),
                new List<Building>(another.BuildingsInSight),
                another.CommandAssigned,
                another.AvailableMovementCommands,
                another.AvailableFiringCommands,
                another.AvailableLogisticsCommands,
                another.AvailableConstructionCommands,
                another.AvailableMiscCommands,
                another.AutoCommands,
                new List<Tile>(another.AutoNavigationPath),
                another.CurrentSuppressionLevel,
                another.ConsecutiveSuppressedRound,
                another.TrainingTimeRemaining);

        public virtual void Initialize(Player owner, Coordinates coordinates, UnitStatus status)
        {
            SetOwner(owner);
            CoOrds = coordinates;
            Status = status;
            SetMeshName();
            Carrying = (Resources)Cost.Base.Clone();
        }
        public void SetOwner(Player player)
        {
            Owner = player;
            OwnerName = player?.Name ?? "";
        }
        // called after deserialization
        public void SetOwnerFromName()
        {
            if (string.IsNullOrEmpty(OwnerName))
            {
                this.LogWarning("OwnerName is null or empty");
                return;
            }
            // called from test if Battle.Instance is null
            Player owner = Battle.Instance?.GetPlayer(OwnerName) ?? Map.Instance.Players.Find(p => p.Name == OwnerName);
            if (owner == null)
            {
                this.LogWarning($"Cannot find player {OwnerName}");
                return;
            }
            Owner = owner;
        }
        public void SetWeapons(params IOffensiveCustomizable[] weapons) => SetWeapons(new List<IOffensiveCustomizable>(weapons));
        public abstract void SetWeapons(IEnumerable<IOffensiveCustomizable> weapons);

        // TODO FUT Impl. handle same type but different altitude (e.g. planes at and above airfield)
        public virtual bool CanAccessTile(Tile t)
        {
            IEnumerable<Unit> units = Map.Instance.GetUnits(t);
            // either the tile does not have any unit on it, or none is of the same category as this unit
            // TODO FUT Impl. consider altitude of the units as well
            return !units.Any() || !units.Any(u => IsOfSameCategory(u));
        }
        public virtual bool CanMove() => IsInField && GetMoveRange().Any();
        public virtual bool CanMerge()
        {
            // TODO FUT Impl. 
            return false;
        }
        public virtual bool CanFire() => IsInField && GetWeapons().Any(w => HasHostileUnitsInFiringRange(w) && HasEnoughAmmo(w));
        public virtual bool CanSabotage() => IsInField && GetWeapons().Any(w => HasHostileBuildingsInFiringRange(w) && HasEnoughAmmo(w));
        // can be fired upon = can be suppressed
        public virtual bool CanSuppress() => IsInField && GetWeapons().Any(w => HasHostileUnitsInFiringRange(w) && HasEnoughAmmo(w, false));
        public virtual bool CanAmbush() => IsInField && !Status.HasFlag(UnitStatus.AMBUSHING) && GetWeapons().Any(w => HasEnoughAmmo(w));
        public virtual bool CanCommunicateWith(Prop p) => IsInField && (p is Unit u ? CanCommunicateWith(u) : (p is Cities c && CanCommunicateWith(c)));
        public virtual bool CanCommunicateWith(Unit communicatee) => IsInField && this != communicatee && GetStraightLineDistance(communicatee) <= Scouting.Communication + communicatee.Scouting.Communication;
        public virtual bool CanCommunicateWith(Cities cities) => IsInField && GetStraightLineDistance(cities) <= Scouting.Communication + cities.Communication;
        public abstract bool CanBeTrainedIn(UnitBuilding ub);

        // has any tile in range that has hostile units
        public bool HasHostileUnitsInFiringRange(IOffensiveCustomizable weapon) => GetFiringRange(weapon).Where(t => t.HasUnit).Any(t => Map.Instance.GetUnits(t).Any(u => !u.IsFriendly(Owner) && HasSpotted(u)));
        // has any buildings in range that has hostile buildings
        public bool HasHostileBuildingsInFiringRange(IOffensiveCustomizable weapon) => GetFiringRange(weapon).Where(t => t.HasBuilding).Any(t => Map.Instance.GetBuildings(t).Any(b => !b.IsFriendly(Owner) && HasSpotted(b)));
        // true for normal, false for suppress
        public bool HasEnoughAmmo(IOffensiveCustomizable weapon, bool normal = true)
        {
            if (weapon == null)
            {
                this.LogError("Weapon is null.");
                return false;
            }
            return Carrying.HasEnoughResources(normal ? weapon.ConsumptionNormal : weapon.ConsumptionSuppress);
        }

        public bool HasSpotted(Unit observee) => Owner.GetAllUnitsInSight().Contains(observee);
        public bool HasSpotted(Building building) => Owner.GetAllBuildingsInSight().Contains(building);

        public IEnumerable<Tile> GetAccessibleNeigbours(int distance = 1) => Map.Instance.GetNeighbours(CubeCoOrds, distance).Where(n => CanAccessTile(n));
        public IEnumerable<Tile> GetAccessibleNeigbours(CubeCoordinates c, int distance = 1) => Map.Instance.GetNeighbours(c, distance).Where(n => CanAccessTile(n));

        public IEnumerable<Tile> GetMoveRange() => GetAccessibleNeigbours((int)Maneuverability.Speed.ApplyMod());
        public IEnumerable<Tile> GetFiringRange(IOffensiveCustomizable weapon)
        {
            if (weapon == null)
            {
                //this.LogError("weapon is null");
                return Enumerable.Empty<Tile>();
            }

            IEnumerable<Tile> range = Map.Instance.GetStraightLineNeighbours(CubeCoOrds, weapon.Offense.MaxRange.ApplyMod());
            if (weapon.Offense.MinRange > 0)
            {
                // exclude tiles within weapon's min range
                range = range.Except(Map.Instance.GetStraightLineNeighbours(CubeCoOrds, weapon.Offense.MinRange.ApplyMod()));
            }
            return range;
        }
        public virtual IEnumerable<Tile> GetReconRange() => Map.Instance.GetStraightLineNeighbours(CubeCoOrds, Scouting.Reconnaissance.ApplyMod());

        /* TODO FUT. Impl. think of a way to detect accessible (tile itself can be accessed by the unit) but unreachable (no valid path) goal tile
        *  e.g. a flat-land tile surrounded by mountains on all 6 sides within speed of the unit
        *  should be very rare in random gen maps, left for FUT. Impl. as map editor would be available later
        */
        public IEnumerable<Tile> GetPath(Tile start, Tile end, PathfindingOptimization opt = PathfindingOptimization.LEAST_SUPPLIES_COST)
        {
            if (this is Personnel && opt == PathfindingOptimization.LEAST_FUEL_COST)
            {
                this.LogError("Only units with fuel capacity can be used with fuel cost optimization for pathfinding.");
                return Enumerable.Empty<Tile>();
            }
            if (!CanAccessTile(end))
            {
                this.LogError($"This unit cannot access the destination {end}");
                return Enumerable.Empty<Tile>();
            }

            bool is_aerial = this is Aerial;
            WeightedTile w_start = start.ConvertToWeightedTile(Consumption, opt, end, is_aerial);
            WeightedTile w_end = end.ConvertToWeightedTile(Consumption, opt, end, is_aerial);

            List<WeightedTile> active = new List<WeightedTile>();
            List<WeightedTile> visited = new List<WeightedTile>();
            List<Tile> path = new List<Tile>();
            Func<WeightedTile, decimal> sort =
                opt == PathfindingOptimization.LEAST_SUPPLIES_COST
                ? new Func<WeightedTile, decimal>(w => w.SuppliesCostDistance)
                : w => w.FuelCostDistance;

            active.Add(w_start);
            while (active.Any())
            {
                WeightedTile w_check = active.OrderBy(sort).FirstOrDefault();
                if (w_check == w_end
                    || w_check.SuppliesCostSoFar > Carrying.Supplies
                    || (Consumption.Fuel > 0 && w_check.FuelCostSoFar > Carrying.Fuel)
                    || w_check.DistanceSoFar > Maneuverability.Speed)
                {
                    while (w_check.Parent != null)
                    {
                        path.Add(Map.Instance.GetTile(w_check.CubeCoOrds));
                        w_check = w_check.Parent;
                    }
                    return path.Reverse<Tile>();
                }
                visited.Add(w_check);
                _ = active.Remove(w_check);

                List<WeightedTile> w_neighbours = new List<WeightedTile>();
                GetAccessibleNeigbours(w_check.CubeCoOrds).ToList().ForEach(t => w_neighbours.Add(t.ConvertToWeightedTile(Consumption, opt, end, is_aerial, w_check)));

                w_neighbours.ForEach(n =>
                {
                    if (!visited.Where(v => v == n).Any())
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

        public abstract IEnumerable<IOffensiveCustomizable> GetWeapons();
        public abstract IEnumerable<Module> GetModules();
        public IEnumerable<T> GetModules<T>() where T : Module => GetModules()?.OfType<T>();
        public virtual IEnumerable<Module> GetRepairableModules() => GetModules().Where(m => m.Integrity < Game.CustomizableData.Modules[m.Name].Integrity);
        public abstract void SetModules(params Module[] modules);

        public abstract Modifier GetConcealmentPenaltyMove();
        public Modifier GetConcealmentPenaltyFire()
        {
            if (WeaponFired == null)
            {
                this.LogWarning("WeaponFired is not set.");
                return null;
            }
            return WeaponFired.ConcealmentPenaltyFire;
        }

        public List<Unit> GetAvailableMergeTargets()
        {
            // TODO FUT Impl. 
            return null;
        }

        public IEnumerable<Unit> GetUnitsInRange(IEnumerable<Tile> range) => range.Where(t => t.HasUnit).SelectMany(t => Map.Instance.GetUnits(t));
        public IEnumerable<Building> GetBuildingsInRange(IEnumerable<Tile> range) => range.Where(t => t.HasBuilding).SelectMany(t => Map.Instance.GetBuildings(t));

        // TODO FUT. Impl. read game rules to decide whether targets of serveral commands can be ally units (e.g. help allies repair, re-capture allies cities)
        public IEnumerable<Unit> GetOwnUnitsInRange(IEnumerable<Tile> range) => GetUnitsInRange(range).Where(u => u.IsOwn(Owner));
        public IEnumerable<Building> GetOwnBuildingsInRange(IEnumerable<Tile> range) => GetBuildingsInRange(range).Where(b => b.IsOwn(Owner));

        public IEnumerable<Unit> GetFriendlyUnitsInRange(IEnumerable<Tile> range) => GetUnitsInRange(range).Where(u => u.IsFriendly(Owner));
        public IEnumerable<Building> GetFriendlyBuildingsInRange(IEnumerable<Tile> range) => GetBuildingsInRange(range).Where(b => b.IsFriendly(Owner));

        // include all enemies, be it spotted or not
        public IEnumerable<Unit> GetHostileUnitsInRange(IEnumerable<Tile> range) => GetUnitsInRange(range).Where(u => !u.IsFriendly(Owner));
        public IEnumerable<Building> GetHostileBuildingsInRange(IEnumerable<Tile> range) => GetBuildingsInRange(range).Where(b => !b.IsFriendly(Owner));

        // use these for selection of fire/sabotage/suppress targets in UI
        public IEnumerable<Unit> GetHostileUnitsInFiringRange(IOffensiveCustomizable weapon) => GetHostileUnitsInRange(GetFiringRange(weapon)).Where(u => HasSpotted(u));
        public IEnumerable<Building> GetHostileBuildingsInFiringRange(IOffensiveCustomizable weapon) => GetHostileBuildingsInRange(GetFiringRange(weapon)).Where(b => HasSpotted(b));
        public IEnumerable<Cities> GetHostileCitiesInRange(IOffensiveCustomizable weapon) => Map.Instance.GetCities(c => c.IsHostile(Owner) && GetFiringRange(weapon).Contains<Tile>(c));

        public IEnumerable<Unit> GetHostileUnitsInReconRange() => GetHostileUnitsInRange(GetReconRange());
        public IEnumerable<Building> GetHostileBuildingsInReconRange() => GetHostileBuildingsInRange(GetReconRange());

        public decimal GetSuppliesRequired(Tile t) => t.TerrainMod.Supplies.ApplyTo(Consumption.Supplies.ApplyMod());
        public decimal GetSuppliesRequired(IEnumerable<Tile> path) => path.Last().CoOrds == CoOrds ? 0 : path.Select(t => GetSuppliesRequired(t)).Sum(); // if last tile of path is where the unit at, no supplies or fuel is consumed (i.e. cannot move due to move conflict)
        public decimal GetFuelRequired(Tile t) => t.TerrainMod.Fuel.ApplyTo(Consumption.Fuel.ApplyMod());
        public decimal GetFuelRequired(IEnumerable<Tile> path) => path.Last().CoOrds == CoOrds ? 0 : path.Select(t => GetFuelRequired(t)).Sum();

        public string GetResourcesChangeRecord(string res, decimal change) => res switch
        {
            "Money" => $" m:{change:+0.##;-0.##}=>{Carrying.Money.ApplyMod()}/{Capacity.Money.ApplyMod()}",
            "Steel'" => $" t:{change:+0.##;-0.##}=>{Carrying.Steel.ApplyMod()}/{Capacity.Steel.ApplyMod()}",
            "Supplies" => $" s:{change:+0.##;-0.##}=>{Carrying.Supplies.ApplyMod()}/{Capacity.Supplies.ApplyMod()}",
            "Cartridges" => $" c:{change:+0.##;-0.##}=>{Carrying.Cartridges.ApplyMod()}/{Capacity.Cartridges.ApplyMod()}",
            "Shells" => $" h:{change:+0.##;-0.##}=>{Carrying.Shells.ApplyMod()}/{Capacity.Shells.ApplyMod()}",
            "Fuel" => $" f:{change:+0.##;-0.##}=>{Carrying.Fuel.ApplyMod()}/{Capacity.Fuel.ApplyMod()}",
            "RareMetal" => $" r:{change:+0.##;-0.##}=>{Carrying.RareMetal.ApplyMod()}/{Capacity.RareMetal.ApplyMod()}",
            _ => throw new ArgumentException($"Unknown resources symbol {res}")
        };
        public string GetResourcesChangeRecord(Resources consume)
        {
            if (consume.IsZero)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            if (consume.Money > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Money", -consume.Money));
            }
            if (consume.Steel > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Steel", -consume.Steel));
            }
            if (consume.Supplies > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Supplies", -consume.Supplies));
            }
            if (consume.Cartridges > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Cartridges", -consume.Cartridges));
            }
            if (consume.Shells > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Shells", -consume.Shells));
            }
            if (consume.Fuel > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("Fuel", -consume.Fuel));
            }
            if (consume.RareMetal > 0)
            {
                _ = sb.Append(GetResourcesChangeRecord("RareMetal", -consume.RareMetal));
            }
            return sb.ToString();
        }
        public string GetStrengthChangeRecord(decimal change) => $" hp:{change:+0.##;-0.##}=>{Defense.Strength.ApplyMod():0.##}/{Game.UnitData[Name].Defense.Strength.ApplyMod():0.##}";
        public string GetSuppressionChangeRecord(decimal change) => $" sup:{change:+0.####;-0.####}=>{CurrentSuppressionLevel:0.####}";

        public abstract override object Clone();
        public bool Equals(Unit other) => base.Equals(other);
    }
}

namespace SteelOfStalin.Assets.Props.Buildings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BuildingStatus
    {
        NONE,
        UNDER_CONSTRUCTION,
        ACTIVE,
        DESTROYED
    }

    // TODO FUT Impl. add toggle for accessibility to allies of buildings (e.g. allow ally planes land on own airfield etc.)
    [JsonConverter(typeof(AssetConverter<Building>))]
    public abstract class Building : Prop, IOwnableAsset, ICloneable
    {
        [JsonIgnore] public Player Owner { get; set; }
        public string OwnerName { get; set; }
        public Coordinates BuilderLocation { get; set; }
        public BuildingStatus Status { get; set; } = BuildingStatus.NONE;
        public byte Level { get; set; }
        public byte MaxLevel { get; set; }
        public decimal Size { get; set; }
        public Cost Cost { get; set; } = new Cost();
        public Attribute Durability { get; set; } = new Attribute();
        public Scouting Scouting { get; set; } = new Scouting();
        public bool DestroyTerrainOnBuilt { get; set; } = true;
        public decimal ConstructionTimeRemaining { get; set; }

        [JsonIgnore] public bool IsFortifying => Status == BuildingStatus.UNDER_CONSTRUCTION && Level > 0;

        public Building() : base() { }
        public Building(Building another) : base(another)
            => (Owner, OwnerName, Status, Level, MaxLevel, Size, Cost, Durability, Scouting, DestroyTerrainOnBuilt, ConstructionTimeRemaining)
            = (another.Owner, 
               another.OwnerName, 
               another.Status, 
               another.Level, 
               another.MaxLevel, 
               another.Size, 
               (Cost)another.Cost.Clone(), 
               (Attribute)another.Durability.Clone(), 
               (Scouting)another.Scouting.Clone(), 
               another.DestroyTerrainOnBuilt, 
               another.ConstructionTimeRemaining);

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner != null && Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        // public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p); /*&& !IsNeutral();*/

        public virtual void Initialize(Player owner, Coordinates coordinates, BuildingStatus status = BuildingStatus.ACTIVE)
        {
            SetOwner(owner);
            CoOrds = coordinates;
            Status = status;
            // TODO FUT. Impl. consider time cost reduction for diff units
            ConstructionTimeRemaining = Cost.Base.Time.ApplyMod();
            SetMeshName();
        }
        public void SetOwner(Player player)
        {
            Owner = player;
            OwnerName = player?.Name ?? "";
        }
        // called after deserialization
        public void SetOwnerFromName()
        {
            if (string.IsNullOrEmpty(OwnerName))
            {
                this.LogWarning("OwnerName is null or empty");
                return;
            }
            // called from test if Battle.Instance is null
            Owner = Battle.Instance?.GetPlayer(OwnerName) ?? Map.Instance.Players.Find(p => p.Name == OwnerName);
        }

        public bool CanBeFortified() => Level < MaxLevel && Status == BuildingStatus.ACTIVE;
        public bool CanBeDemolished() => Level > 0 && Status == BuildingStatus.ACTIVE;

        public string GetDurabilityChangeRecord(decimal change) => $" d:{change:+0.##;-0.##}=>{Durability.ApplyMod()}";

        public abstract override object Clone();
    }
}

namespace SteelOfStalin.Assets.Props.Tiles
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TileType
    {
        BOUNDARY,
        PLAINS,
        GRASSLAND,
        FOREST,
        JUNGLE,
        STREAM,
        RIVER,
        OCEAN,
        SWAMP,
        DESERT,
        HILLOCK,
        HILLS,
        MOUNTAINS,
        ROCKS,
        SUBURB,
        CITY,
        METROPOLIS
    }

    [Flags]
    [JsonConverter(typeof(StringEnumFlagConverterFactory))]
    public enum Accessibility
    {
        NONE = 0,
        PERSONNEL = 1 << 0,
        ARTILLERY = 1 << 1,
        VEHICLE = 1 << 2,
        VESSEL = 1 << 3,
        PLANE = 1 << 4,
        GROUND = PERSONNEL | ARTILLERY | VEHICLE,
        ALL = ~0
    }

    public abstract class Cities : Tile, IOwnableAsset
    {
        [JsonIgnore] public Player Owner { get; set; }
        public string OwnerName { get; set; }
        public decimal Population { get; set; }
        public Attribute ConstructionRange { get; set; } = new Attribute();
        public Attribute Communication { get; set; } = new Attribute();
        public Resources Production { get; set; } = new Resources();
        public Attribute Durability { get; set; } = new Attribute();
        public Attribute Morale { get; set; } = new Attribute();

        [JsonIgnore] public bool IsDestroyed => Durability <= 0;

        public Cities() : base() { }
        public Cities(Cities another) : base(another)
            => (Owner, OwnerName, Population, ConstructionRange, Communication, Production, Durability, Morale)
            = (another.Owner, 
               another.OwnerName, 
               another.Population, 
               (Attribute)another.ConstructionRange.Clone(), 
               (Attribute)another.Communication,
               (Resources)another.Production.Clone(), 
               (Attribute)another.Durability.Clone(), 
               (Attribute)another.Morale.Clone());

        public bool IsOwn(Player p) => Owner == p;
        public bool IsAlly(Player p) => Owner != null && Owner.Allies.Any(a => a == p);
        public bool IsFriendly(Player p) => IsAlly(p) || IsOwn(p);
        public bool IsNeutral() => Owner == null;
        public bool IsHostile(Player p) => !IsFriendly(p) && !IsNeutral();

        public void SetOwner(Player player)
        {
            Owner = player;
            OwnerName = player?.Name ?? "";
        }
        // called after deserialization
        public void SetOwnerFromName()
        {
            if (string.IsNullOrEmpty(OwnerName))
            {
                this.LogWarning("OwnerName is null or empty");
                return;
            }
            // called from test if Battle.Instance is null
            Owner = Battle.Instance?.GetPlayer(OwnerName) ?? Map.Instance.Players.Find(p => p.Name == OwnerName);
        }
        public void ChangeOwner(Player newOwner)
        {
            if (newOwner == null)
            {
                this.LogError("New owner is null");
                return;
            }
            SetOwner(newOwner);
            Map.Instance.GetBuildings<UnitBuilding>(CoOrds).ToList().ForEach(ub =>
            {
                ub.TrainingQueue.Clear();
                ub.ReadyToDeploy.Clear();
                ub.SetOwner(newOwner);
            });
        }

        public bool CanCommunicateWith(Prop p) => p is Unit u ? CanCommunicateWith(u) : (p is Cities c && CanCommunicateWith(c));
        public bool CanCommunicateWith(Unit u) => GetStraightLineDistance(u) <= Communication + u.Scouting.Communication;
        public bool CanCommunicateWith(Cities c) => this != c && GetStraightLineDistance(c) <= Communication + c.Communication;

        public string GetMoraleChangeRecord(decimal change) => $" m:{change:+0.##;-0.##}=>{Morale.ApplyMod()}/{((Cities)Game.TileData[Name]).Morale.ApplyMod()}";

        public abstract override object Clone();
    }

    [JsonConverter(typeof(AssetConverter<Tile>))]
    public abstract class Tile : Prop
    {
        public TileType Type { get; set; }
        public Accessibility Accessibility { get; set; }
        public TerrainModifier TerrainMod { get; set; } = new TerrainModifier();
        public decimal Obstruction { get; set; }
        public bool AllowConstruction { get; set; }
        public decimal Height { get; set; }
        public char Symbol { get; set; }

        [JsonIgnore] public bool IsWater => Type == TileType.STREAM || Type == TileType.RIVER || Type == TileType.OCEAN || Type == TileType.SWAMP;
        [JsonIgnore] public bool IsHill => Type is TileType.HILLOCK || Type is TileType.HILLS || Type is TileType.MOUNTAINS;
        [JsonIgnore] public bool IsFlatLand => !IsWater && !IsHill && Type != TileType.BOUNDARY;
        [JsonIgnore] public bool IsCity => Type is TileType.SUBURB || Type is TileType.CITY || Type is TileType.METROPOLIS;
        [JsonIgnore] public bool HasUnit => Map.Instance.GetUnits(this).Any();
        [JsonIgnore] public bool HasBuilding => Map.Instance.GetBuildings(this).Any();
        [JsonIgnore] public bool IsOccupied => HasUnit || HasBuilding;

        public Tile() : base() { }
        public Tile(Tile another) : base(another)
            => (Type, Accessibility, TerrainMod, Obstruction, AllowConstruction, Height, Symbol)
            = (another.Type, 
               another.Accessibility, 
               (TerrainModifier)another.TerrainMod.Clone(),
               another.Obstruction, 
               another.AllowConstruction, 
               another.Height, 
               another.Symbol);

        public WeightedTile ConvertToWeightedTile(Resources consumption, PathfindingOptimization opt, Tile end, bool IsAerial, WeightedTile parent = null) => new WeightedTile()
        {
            Parent = parent,
            CubeCoOrds = CubeCoOrds,
            BaseCost = opt == PathfindingOptimization.LEAST_SUPPLIES_COST ? consumption.Supplies.ApplyMod() : consumption.Fuel.ApplyMod(),
            Weight = IsAerial
                        ? 1
                        : opt == PathfindingOptimization.LEAST_SUPPLIES_COST
                            ? TerrainMod.Supplies.ApplyTo()
                            : TerrainMod.Fuel.ApplyTo(),
            SuppliesCostSoFar = (parent == null ? 0 : parent.SuppliesCostSoFar) + TerrainMod.Supplies.ApplyTo(consumption.Supplies.ApplyMod()),
            FuelCostSoFar = (parent == null ? 0 : parent.FuelCostSoFar) + TerrainMod.Fuel.ApplyTo(consumption.Fuel.ApplyMod()),
            DistanceSoFar = parent == null ? 0 : parent.DistanceSoFar + 1,
            DistanceToGoal = CubeCoordinates.GetDistance(CubeCoOrds, end.CubeCoOrds)
        };

        public IEnumerable<Tile> GetNeighbours(int distance = 1, bool include_self = false)
            => CubeCoOrds.GetNeighbours(distance, include_self).Select(c => Map.Instance.GetTile(c));
        public override Tile GetLocatedTile() => this;

        public override bool Equals(object other) => this == (Tile)other;
        public override int GetHashCode() => base.GetHashCode();
        public abstract override object Clone();

        public Tile ToList()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(Tile t1, Tile t2) => t1?.CoOrds.X == t2?.CoOrds.X && t2?.CoOrds.Y == t2?.CoOrds.Y;
        public static bool operator !=(Tile t1, Tile t2) => !(t1?.CoOrds.X == t2?.CoOrds.X && t2?.CoOrds.Y == t2?.CoOrds.Y);
    }

    public class WeightedTile
    {
        public WeightedTile Parent { get; set; }
        public CubeCoordinates CubeCoOrds { get; set; }
        public decimal BaseCost { get; set; }
        public decimal Weight { get; set; }
        // use 2 for tile pathfinding
        public decimal MaxWeight { get; set; } = 2;
        public decimal SuppliesCostSoFar { get; set; }
        public decimal FuelCostSoFar { get; set; }
        public int DistanceSoFar { get; set; }
        public int DistanceToGoal { get; set; }
        public decimal SuppliesCostDistance => SuppliesCostSoFar + DistanceToGoal * BaseCost * MaxWeight;
        public decimal FuelCostDistance => FuelCostSoFar + DistanceToGoal * BaseCost * MaxWeight;

        public override bool Equals(object obj) => this == (WeightedTile)obj;
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(WeightedTile w1, WeightedTile w2) => w1?.CubeCoOrds == w2?.CubeCoOrds;
        public static bool operator !=(WeightedTile w1, WeightedTile w2) => !(w1?.CubeCoOrds == w2?.CubeCoOrds);
    }

    public sealed class Boundary : Tile
    {
        public Boundary() : base() { }
        public Boundary(Boundary another) : base(another) { }
        public override object Clone() => new Boundary(this);
    }
    public sealed class Plains : Tile
    {
        public Plains() : base() { }
        public Plains(Plains another) : base(another) { }
        public override object Clone() => new Plains(this);
    }
    public sealed class Grassland : Tile
    {
        public Grassland() : base() { }
        public Grassland(Grassland another) : base(another) { }
        public override object Clone() => new Grassland(this);
    }
    public sealed class Forest : Tile
    {
        public Forest() : base() { }
        public Forest(Forest another) : base(another) { }
        public override object Clone() => new Forest(this);
    }
    public sealed class Jungle : Tile
    {
        public Jungle() : base() { }
        public Jungle(Jungle another) : base(another) { }
        public override object Clone() => new Jungle(this);
    }
    public sealed class Stream : Tile
    {
        public Stream() : base() { }
        public Stream(Stream another) : base(another) { }
        public override object Clone() => new Stream(this);
    }
    public sealed class River : Tile
    {
        public River() : base() { }
        public River(River another) : base(another) { }
        public override object Clone() => new River(this);
    }
    public sealed class Ocean : Tile
    {
        public Ocean() : base() { }
        public Ocean(Ocean another) : base(another) { }
        public override object Clone() => new Ocean(this);
    }
    public sealed class Swamp : Tile
    {
        public Swamp() : base() { }
        public Swamp(Swamp another) : base(another) { }
        public override object Clone() => new Swamp(this);
    }
    public sealed class Desert : Tile
    {
        public Desert() : base() { }
        public Desert(Desert another) : base(another) { }
        public override object Clone() => new Desert(this);
    }
    public sealed class Hillock : Tile
    {
        public Hillock() : base() { }
        public Hillock(Hillock another) : base(another) { }
        public override object Clone() => new Hillock(this);
    }
    public sealed class Hills : Tile
    {
        public Hills() : base() { }
        public Hills(Hills another) : base(another) { }
        public override object Clone() => new Hills(this);
    }
    public sealed class Mountains : Tile
    {
        public Mountains() : base() { }
        public Mountains(Mountains another) : base(another) { }
        public override object Clone() => new Mountains(this);
    }
    public sealed class Rocks : Tile
    {
        public Rocks() : base() { }
        public Rocks(Rocks another) : base(another) { }
        public override object Clone() => new Rocks(this);
    }
    public sealed class Suburb : Cities
    {
        public Suburb() : base() { }
        public Suburb(Suburb another) : base(another) { }
        public override object Clone() => new Suburb(this);
    }
    public sealed class City : Cities
    {
        public City() : base() { }
        public City(City another) : base(another) { }
        public override object Clone() => new City(this);
    }
    public sealed class Metropolis : Cities
    {
        public Metropolis() : base() { }
        public Metropolis(Metropolis another) : base(another) { }
        public override object Clone() => new Metropolis(this);
    }
}