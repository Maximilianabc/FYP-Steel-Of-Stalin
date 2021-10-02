using SteelOfStalin.Props.Buildings;
using SteelOfStalin.Props.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin
{
    public abstract class Player
    {
        // TODO add country property later
        public string Name { get; set; }
        public Color Color { get; set; }
        public Attributes.Resources Resources { get; set; } = new Attributes.Resources();
        public List<Unit> Units { get; set; } = new List<Unit>();
        public List<Building> Buildings = new List<Building>();
    }

    public class AIPlayer : Player
    {
        // AI algo goes here
    }

    public class HumanPlayer : Player
    {

    }
}
