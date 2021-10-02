using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin.Props.Units
{
    public abstract class Personnel : Unit
    {
        public Personnel() : base()
        {
            
        }
    }

    public abstract class Artillery : Unit
    {
        public Artillery() : base()
        {

        }
    }

    public abstract class Vehicle : Unit
    {
        public Vehicle() : base()
        {

        }
    }

    public abstract class Vessel : Unit
    {
        public Vessel() : base()
        {

        }
    }

    public abstract class Plane : Unit
    {
        public Plane() : base()
        {

        }
    }
}

namespace SteelOfStalin.Props.Units.Land
{
    // different types of land units here, all should inherit Personnel, Artillery or Vehicle
}

namespace SteelOfStalin.Props.Units.Naval
{
    // different types of naval units here, all should inherit Vessel
}

namespace SteelOfStalin.Props.Units.Aerial
{
    // different types of aerial units here, all should inherit Plane
}
