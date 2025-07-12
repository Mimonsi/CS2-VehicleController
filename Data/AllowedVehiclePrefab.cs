using System;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Collections;

namespace VehicleController.Data
{
    using Unity.Entities;

    [InternalBufferCapacity(0)] // Initial buffer size is 0, it will grow as needed
    // IBufferElementData is used for dynamic buffers, so we can have multiple AllowedVehiclePrefabs on an entity
    // IEquatable is used for comparing AllowedVehiclePrefab instances
    // ISerializable allows to make the component persistent in save files
    public struct AllowedVehiclePrefab : IBufferElementData, IEquatable<AllowedVehiclePrefab>, ISerializable
    {
        //public Entity Prefab;
        public FixedString128Bytes PrefabName;

        public bool Equals(AllowedVehiclePrefab other)
        {
            return PrefabName.Equals(other.PrefabName);
        }

        public override bool Equals(object? obj)
        {
            return obj is AllowedVehiclePrefab other && Equals(other);
        }

        public override int GetHashCode()
        {
            return PrefabName.GetHashCode();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            Mod.log.Info($"Serializing AllowedVehiclePrefab: {PrefabName}");
            writer.Write(PrefabName.ToString());
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out string prefabNameStr);
            PrefabName = new FixedString128Bytes(prefabNameStr);
            Mod.log.Info($"Deserialized AllowedVehiclePrefab: {PrefabName}");
        }
    }
}