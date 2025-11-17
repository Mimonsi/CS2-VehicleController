using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Data
{
    /// <summary>
    /// Component to display Vehicle Controller has halved the speed limit for a road
    /// </summary>
    public struct SpeedLimitModified : IComponentData//, ISerializable
    {
        public float VanillaSpeedLimit;
        
        /*/// <summary>
        /// Writes the prefab name to the save file.
        /// </summary>
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(VanillaSpeedLimit);
        }

        /// <summary>
        /// Reads the prefab name from the save file and restores the struct.
        /// </summary>
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out float vanillaSpeedLimit);
            VanillaSpeedLimit = vanillaSpeedLimit;
        }*/
    }
}