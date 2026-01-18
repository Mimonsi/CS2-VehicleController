using System;
using System.Runtime.Serialization;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Collections;
using ISerializable = Colossal.Serialization.Entities.ISerializable;

namespace VehicleController.Components
{
    using Unity.Entities;

    // Initial buffer capacity is set to 0 because entries are usually added at runtime.
    // Implements IBufferElementData so multiple entries can be attached to one entity and
    // ISerializable so the buffer contents are persisted in save games.
    [InternalBufferCapacity(0)]
    public struct AllowedVehiclePrefab : IBufferElementData, IEquatable<AllowedVehiclePrefab>, ISerializable
    {
        //public Entity Prefab;
        public FixedString128Bytes PrefabName;

        /// <summary>
        /// Checks equality based on the stored prefab name.
        /// </summary>
        /// <param name="other">Other struct instance to compare with.</param>
        /// <returns>True if both entries reference the same prefab.</returns>
        public bool Equals(AllowedVehiclePrefab other)
        {
            return PrefabName.Equals(other.PrefabName);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is AllowedVehiclePrefab other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return PrefabName.GetHashCode();
        }

        /// <summary>
        /// Writes the prefab name to the save file.
        /// </summary>
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            //Mod.Logger.Info($"Serializing AllowedVehiclePrefab: {PrefabName}");
            writer.Write(DataMigrationVersion.InitialVersion); // Version number
            writer.Write(PrefabName.ToString());
        }

        /// <summary>
        /// Reads the prefab name from the save file and restores the struct.
        /// </summary>
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version == DataMigrationVersion.InitialVersion)
            {
                reader.Read(out string prefabNameStr);
                PrefabName = new FixedString128Bytes(prefabNameStr);
            }
            //Mod.log.Warn("Serialization version mismatch in AllowedVehiclePrefab.Deserialize. Data has been lost");

            //Mod.log.Info($"Deserialized AllowedVehiclePrefab: {PrefabName}");
        }
    }
}