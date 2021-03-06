using SteelOfStalin.Attributes;
using SteelOfStalin.CustomTypes;
using System;
using System.Text.Json.Serialization;

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

    public interface IOwnableAsset
    {
        public Player Owner { get; set; }
        public string OwnerName { get; set; }

        public void SetOwner(Player owner);
        public void SetOwnerFromName();
    }
}
