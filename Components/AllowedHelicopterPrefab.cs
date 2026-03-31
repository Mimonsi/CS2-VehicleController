using System;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;
using ISerializable = Colossal.Serialization.Entities.ISerializable;

namespace VehicleController.Components
{
    /// <summary>
    /// Buffer element for storing allowed helicopter prefab names on a building entity.
    /// Works identically to <see cref="AllowedVehiclePrefab"/> but for helicopters,
    /// ensuring car and helicopter selections remain separate.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct AllowedHelicopterPrefab : IBufferElementData, IEquatable<AllowedHelicopterPrefab>, ISerializable
    {
        public FixedString128Bytes PrefabName;

        public bool Equals(AllowedHelicopterPrefab other)
        {
            return PrefabName.Equals(other.PrefabName);
        }

        public override bool Equals(object? obj)
        {
            return obj is AllowedHelicopterPrefab other && Equals(other);
        }

        public override int GetHashCode()
        {
            return PrefabName.GetHashCode();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(DataMigrationVersion.InitialVersion);
            writer.Write(PrefabName.ToString());
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version == DataMigrationVersion.InitialVersion)
            {
                reader.Read(out string prefabNameStr);
                PrefabName = new FixedString128Bytes(prefabNameStr);
            }
        }
    }
}
