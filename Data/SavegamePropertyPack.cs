using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Data
{
    /// <summary>
    /// Component that saves the active property pack of the savegame on 
    /// </summary>
    public struct SavegamePropertyPack : ISerializable, IComponentData
    {
        public FixedString128Bytes PackName;

        /// <summary>
        /// Writes the pack name to the save file.
        /// </summary>
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(PackName.ToString());
        }

        /// <summary>
        /// Reads the prefab name from the save file and restores the struct.
        /// </summary>
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out string prefabNameStr);
            PackName = new FixedString128Bytes(prefabNameStr);
        }
    }
}