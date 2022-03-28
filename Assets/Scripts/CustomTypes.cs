using SteelOfStalin.Assets;
using SteelOfStalin.Assets.Props;
using SteelOfStalin.Assets.Props.Buildings;
using SteelOfStalin.Assets.Props.Tiles;
using SteelOfStalin.Assets.Props.Units;
using SteelOfStalin.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SteelOfStalin.CustomTypes
{
    public enum Direction
    {
        NONE,
        W,
        E,
        D,
        S,
        A,
        Q
    }

    public struct Coordinates : ICloneable, IEquatable<Coordinates>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinates(int x, int y) : this() => (X, Y) = (x, y);
        public Coordinates(Coordinates another) : this() => (X, Y) = (another.X, another.Y);

        public object Clone() => new Coordinates(this);
        public override string ToString() => $"({X},{Y})";
        public override bool Equals(object obj) => Equals((Coordinates)obj);
        public bool Equals(Coordinates other) => this == other;
        public override int GetHashCode() => (X, Y).GetHashCode();

        public static bool operator ==(Coordinates c1, Coordinates c2) => c1.X == c2.X && c1.Y == c2.Y;
        public static bool operator !=(Coordinates c1, Coordinates c2) => !(c1.X == c2.X && c1.Y == c2.Y);
    }

    public struct CubeCoordinates : IEquatable<CubeCoordinates>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public bool IsValid => X + Y + Z == 0;

        public static Dictionary<Direction, (int X, int Y, int Z)> DirectionOffsets => new Dictionary<Direction, (int X, int Y, int Z)>()
        {
            [Direction.NONE] = (0, 0, 0),
            [Direction.W] = (0, -1, 1),
            [Direction.E] = (1, -1, 0),
            [Direction.D] = (1, 0, -1),
            [Direction.S] = (0, 1, -1),
            [Direction.A] = (-1, 1, 0),
            [Direction.Q] = (-1, 0, 1)
        };

        public CubeCoordinates(int x, int y, int z) : this() => (X, Y, Z) = (x, y, z);

        public static int GetDistance(CubeCoordinates c1, CubeCoordinates c2)
            => Mathf.Max(Math.Abs(c1.X - c2.X), Math.Abs(c1.Y - c2.Y), Math.Abs(c1.Z - c2.Z));

        public static decimal GetStraightLineDistance(CubeCoordinates c1, CubeCoordinates c2)
        {
            List<int> diff = new List<int>()
            {
                Math.Abs(c1.X - c2.X),
                Math.Abs(c1.Y - c2.Y),
                Math.Abs(c1.Z - c2.Z)
            };
            diff = diff.OrderByDescending(x => x).ToList();

            // if one of the abs diff is 0, the tiles are on the same x/y/z axis
            // else apply cosine theorem with the abs diffs except the largest one (becuz it is the tile distance, not the sides of the triangle)
            // the angle is always 2 / 3 rad
            return diff.Contains(0)
                ? diff.Max()
                : (decimal)Math.Sqrt(Math.Pow(diff[1], 2) + Math.Pow(diff[2], 2) - 2 * diff[1] * diff[2] * Math.Cos(2 / 3D));
        }

        /// <summary>
        /// Returns an enumerable of negibouring points with distance specified
        /// </summary>
        /// <param name="distance">The distance from the cube coordinates, inclusive</param>
        /// <returns></returns>
        public IEnumerable<CubeCoordinates> GetNeigbours(int distance = 1, bool include_self = false)
        {
            if (include_self)
            {
                yield return new CubeCoordinates(X, Y, Z);
            }
            if (distance == 1)
            {
                foreach ((int X, int Y, int Z) offset in DirectionOffsets.Values)
                {
                    if (!(offset.X == 0 && offset.Y == 0 && offset.Z == 0))
                    {
                        yield return new CubeCoordinates(X + offset.X, Y + offset.Y, Z + offset.Z);
                    }
                }
                yield break;
            }
            for (int x = -distance; x <= distance; x++)
            {
                for (int y = -distance; y <= distance; y++)
                {
                    for (int z = -distance; z <= distance; z++)
                    {
                        if (!(x == 0 && y == 0 && z == 0) && x + y + z == 0)
                        {
                            yield return new CubeCoordinates(X + x, Y + y, Z + z);
                        }
                    }
                }
            }

        }
        public Direction GetDirectionTo(CubeCoordinates c)
        {
            if (GetDistance(this, c) > 1)
            {
                throw new ArgumentException("GetDirection can only be used with direct neighbours or the same tile");
            }

            (int x, int y, int z) diff = c - this;
            return !DirectionOffsets.ContainsValue(diff)
                ? throw new Exception($"Unknown coordinate difference: {diff.x}, {diff.y}, {diff.z}")
                : DirectionOffsets.First(d => d.Value == diff).Key;
        }
        public CubeCoordinates OffsetTo(Direction direction) => this + DirectionOffsets[direction];

        public override string ToString() => $"({X},{Y},{Z})";
        public override bool Equals(object obj) => Equals((CubeCoordinates)obj);
        public bool Equals(CubeCoordinates other) => this == other;
        public override int GetHashCode() => (X, Y, Z).GetHashCode();

        public static CubeCoordinates operator +(CubeCoordinates c, (int X, int Y, int Z) offset) => new CubeCoordinates(c.X + offset.X, c.Y + offset.Y, c.Z + offset.Z);
        public static (int X, int Y, int Z) operator -(CubeCoordinates c1, CubeCoordinates c2) => (c1.X - c2.X, c1.Y - c2.Y, c1.Z - c2.Z);
        public static bool operator ==(CubeCoordinates c1, CubeCoordinates c2) => c1.X == c2.X && c1.Y == c2.Y && c1.Z == c2.Z;
        public static bool operator !=(CubeCoordinates c1, CubeCoordinates c2) => !(c1.X == c2.X && c1.Y == c2.Y && c1.Z == c2.Z);

        public static explicit operator Coordinates(CubeCoordinates c) => new Coordinates(c.X, c.Z + (c.X - c.X % 2) / 2);
        public static explicit operator CubeCoordinates(Coordinates p)
        {
            int z = p.Y - (p.X - p.X % 2) / 2;
            int y = -p.X - z;
            return new CubeCoordinates(p.X, y, z);
        }
    }

    public struct SerializableColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public SerializableColor(byte r, byte g, byte b, byte a) : this() => (R, G, B, A) = (r, g, b, a);

        public static explicit operator SerializableColor(Color color) => new SerializableColor((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
        public static explicit operator Color(SerializableColor color) => new Color(color.R / 255F, color.G / 255F, color.B / 255F, color.A / 255F);
    }

    public class Graph<T>
    {
        public IEnumerable<T> Vertices { get; set; }
        public int[][] AdjMatrix { get; private set; }

        public Graph(IEnumerable<T> verts)
        {
            Vertices = verts;
            AdjMatrix = new int[Vertices.Count()][];
            for (int i = 0; i < Vertices.Count(); i++)
            {
                AdjMatrix[i] = new int[Vertices.Count()];
            }
        }

        // set weight to 0 if removing the edge
        public void SetEdge(T from, T to, int weight = 1, bool directional = false)
        {
            int from_index = Vertices.IndexOf(from);
            int to_index = Vertices.IndexOf(to);

            if (from_index == -1 || to_index == -1)
            {
                throw new ArgumentException("At least one of the specified vertices is not in the graph.");
            }
            AdjMatrix[from_index][to_index] = weight;
            if (!directional)
            {
                AdjMatrix[to_index][from_index] = weight;
            }
        }

        public bool HasEdge(T from, T to)
        {
            int from_index = Vertices.IndexOf(from);
            int to_index = Vertices.IndexOf(to);
            return from_index != -1 && to_index != -1 && AdjMatrix[from_index][to_index] != 0;
        }

        public IEnumerable<T> GetVerticesWithNoInput()
        {
            int[][] transposed = AdjMatrix.Transpose();
            for (int i = 0; i < Vertices.Count(); i++)
            {
                if (transposed[i].All(w => w == 0))
                {
                    yield return Vertices.ElementAt(i);
                }
            }
        }

        public IEnumerable<T> GetVerticesWithNoOutput()
        {
            for (int i = 0; i < Vertices.Count(); i++)
            {
                if (AdjMatrix[i].All(w => w == 0))
                {
                    yield return Vertices.ElementAt(i);
                }
            }
        }

        public IEnumerable<T> GetIsloatedVertices() => GetVerticesWithNoInput().Intersect(GetVerticesWithNoOutput());
    }

    public class AssetListConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(List<>)
            && typeToConvert.GetGenericArguments()[0].IsSubclassOf(typeof(INamedAsset));

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null);
    }

    public class AssetListConverter<T> : JsonConverter<List<T>> where T : INamedAsset
    {
        private readonly Type m_propType;
        private readonly AssetConverter<T> m_propConverter;

        public AssetListConverter()
        {
            m_propType = typeof(T);
            m_propConverter = new AssetConverter<T>();
        }

        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            List<T> prop_list = new List<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Reader's token type is not PropertyName. It is {reader.TokenType} instead.");
                }
                _ = reader.Read();
                prop_list.Add(m_propConverter.Read(ref reader, m_propType, options));
            }
            return prop_list;
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, options);
    }

    public class AssetConverter<T> : JsonConverter<T> where T : INamedAsset
    {
        private readonly IEnumerable<Type> m_Types;

        public AssetConverter()
        {
            m_Types = Assembly.GetExecutingAssembly().GetTypes().Where(t => !string.IsNullOrEmpty(t.Namespace) && t.FullName.StartsWith(typeof(T).Namespace));
            if (!m_Types.Any())
            {
                throw new Exception($"No type name starting with {typeof(T).Namespace}");
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            Dictionary<string, object> prop_properties = new Dictionary<string, object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Reader's token type is not PropertyName. It is {reader.TokenType} instead.");
                }

                string property_name = reader.GetString();
                _ = reader.Read();
                object value = JsonSerializer.Deserialize<object>(ref reader, options);

                prop_properties.Add(property_name, value);
            }

            string prop_name = prop_properties.Keys.Find(k => k == "Name");
            if (string.IsNullOrEmpty(prop_name))
            {
                throw new JsonException("There is no property with name \"Name\"");
            }

            string child_type_name = Utilities.ToPascal(Regex.Replace(prop_properties[prop_name].ToString(), @"_[a-f0-9]{32}", ""));
            Type child_type = m_Types.Find(t => t.Name == child_type_name);
            if (child_type == null)
            {
                throw new JsonException($"There is no type with name {child_type_name}");
            }

            T t = (T)Activator.CreateInstance(child_type);
            foreach (KeyValuePair<string, object> property in prop_properties)
            {
                if (property.Value != null)
                {
                    PropertyInfo info = child_type.GetProperty(property.Key);
                    if (info == null)
                    {
                        throw new JsonException($"There is no properties with name {property.Key} in type {child_type.Name}");
                    }
                    else if (info.GetSetMethod() == null)
                    {
                        Debug.LogWarning($"The property {info.Name} is readonly");
                        continue;
                    }
                    info.SetValue(t, ((JsonElement)property.Value).Deserialize(info.PropertyType, options));
                }
            }
            return t;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            IEnumerable<PropertyInfo> serializing_info = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetSetMethod() != null && p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null);

            foreach (PropertyInfo info in serializing_info)
            {
                writer.WritePropertyName(info.Name);
                object val = info.GetValue(value);
                if (val == null)
                {
                    Debug.Log($"{info.Name} is null");
                    writer.WriteNullValue();
                }
                else
                {
                    JsonSerializer.Serialize(writer, val, val.GetType(), options);
                }
            }
            writer.WriteEndObject();
        }
    }

    public class RoundingJsonConverter : JsonConverter<double>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(double);

        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            decimal d = JsonSerializer.Deserialize<decimal>(ref reader, options);
            return d == decimal.MaxValue ? double.MaxValue : (double)d;
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteRawValue(value == double.MaxValue ? decimal.MaxValue.ToString() : Math.Round((decimal)value, 4).ToString());
    }

    public class StringEnumFlagConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(
                typeof(StringEnumFlagConverter<>).MakeGenericType(typeToConvert),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null);
    }

    public class StringEnumFlagConverter<T> : JsonConverter<T> where T : Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string[] strings = reader.GetString()?.Split(new string[] { " | " }, StringSplitOptions.None);
            if (strings == null || strings.Length == 0)
            {
                throw new JsonException("Cannot split the serailized enum string into string array of names");
            }

            string[] names = Enum.GetNames(typeof(T));
            Dictionary<string, int> enum_pairs = names.Zip(Enum.GetValues(typeof(T))
                                                      .Cast<int>(), (n, v) => new { n, v })
                                                      .ToDictionary(p => p.n, p => p.v);

            int val = 0;
            foreach (string s in strings)
            {
                if (s == "NONE")
                {
                    val = 0;
                    break;
                }
                if (enum_pairs.ContainsKey(s))
                {
                    val |= enum_pairs[s];
                }
            }

            return (T)(object)val; // int needed to be boxed first if the enum type is resolved at runtime
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            string[] names = Enum.GetNames(typeof(T));
            Dictionary<string, int> enum_pairs = names.Zip(Enum.GetValues(typeof(T)) // zip enum names with enum values
                                                      .Cast<int>(), (n, v) => new { n, v })
                                                      .ToDictionary(p => p.n, p => p.v)
                                                      .Where(p => (p.Value != 0) && ((p.Value & (p.Value - 1)) == 0)) // filter those with value not power of 2
                                                      .ToDictionary(p => p.Key, p => p.Value);

            int val = Convert.ToInt32(value);
            if (val == -1)
            {
                writer.WriteStringValue(names[names.Length - 1]);
                return;
            }
            if (val == 0)
            {
                writer.WriteStringValue(names[0]);
                return;
            }
            // value cannot be > 32 bits, i.e. the enum cannot have more than 32 flags
            BitArray bits = new BitArray(new int[] { val });
            List<string> json = bits.Cast<bool>()
                                    .Select((b, i) => new { Bit = b, Index = i })
                                    .Take(enum_pairs.Count())
                                    .Where(a => a.Bit == true)
                                    .Select(a => enum_pairs.Keys.ElementAt(a.Index)).ToList();
            writer.WriteStringValue(string.Join(" | ", json));
        }
    }
}
