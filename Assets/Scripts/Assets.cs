using SteelOfStalin.Attributes;
using SteelOfStalin.CustomTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SteelOfStalin.Assets
{
    [JsonConverter(typeof(AssetConverter<Customizable>))]
    public abstract class Customizable : ICloneable, INamedAsset
    {
        public string Name { get; set; }
        public Cost Cost { get; set; } = new Cost();

        public Customizable() { }
        public Customizable(Customizable another) => (Name, Cost) = (another.Name, (Cost)another.Cost.Clone());

        public abstract object Clone();
    }

    [JsonConverter(typeof(AssetConverter<INamedAsset>))]
    public interface INamedAsset
    {
        public string Name { get; set; }
    }
}
