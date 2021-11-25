using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resources = SteelOfStalin.Attributes.Resources;

namespace SteelOfStalin
{
    public abstract class Player : ICloneable
    {
        // TODO add country property later
        public string Name { get; set; }
        public Color Color { get; set; }
        public Resources Resources { get; set; } = new Resources();
        public List<Unit> Units { get; set; } = new List<Unit>();
        public List<Building> Buildings { get; set; } = new List<Building>();

        public bool IsReady { get; set; } = false;
        public List<Unit> UnitsInSight { get; set; } = new List<Unit>();

        public object Clone()
        {
            Player copy = (Player)MemberwiseClone();
            copy.Resources = (Resources)Resources.Clone();
            copy.Units = Units.Select(x => (Unit)x.Clone()).ToList();
            return copy;
        }
    }

    public class AIPlayer : Player
    {
        // AI algo goes here
    }

    public class HumanPlayer : Player
    {

    }
}
