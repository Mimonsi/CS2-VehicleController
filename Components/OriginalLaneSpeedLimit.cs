using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Components
{
    /// <summary>
    /// Component to display Vehicle Controller has halved the speed limit for a road
    /// </summary>
    public struct OriginalLaneSpeedLimit : IComponentData, ISerializable
    {
        public float VanillaSpeedLimit;
        
        /// <summary>
        /// Writes the prefab name to the save file.
        /// </summary>
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(DataMigrationVersion.InitialVersion); // Version
            writer.Write(VanillaSpeedLimit);
        }

        /// <summary>
        /// Reads the prefab name from the save file and restores the struct.
        /// </summary>
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            Mod.OriginalSpeedLimitCount++;
            // Mod.log.Info("Deserialize 1"); // TODO: Remove
            if (version == DataMigrationVersion.InitialVersion)
            {
                reader.Read(out float vanillaSpeedLimit);
                VanillaSpeedLimit = vanillaSpeedLimit;
                Mod.OriginalSpeedLimitDeserialized++;
            }
            else // Defaults
            {
                // TODO: Find way to delete component if version mismatch
                reader.Read(out float vanillaSpeedLimit);
                if (vanillaSpeedLimit == 0)
                    vanillaSpeedLimit = -1;
                VanillaSpeedLimit = vanillaSpeedLimit;
                Mod.OriginalSpeedLimitDeserialized++;
                //Mod.log.Info("Deserialize 2");
            }
            //Mod.log.Info("Deserialize 3");
            //Mod.log.Warn("Serialization version mismatch in OriginalSpeedLimit.Deserialize. Data has been lost");
        }
    }
}