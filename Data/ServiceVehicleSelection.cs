using Game.Prefabs;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace VehicleController.Data
{
    /// <summary>
    /// Used to record what the user wanted for their custom mesh color.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ServiceVehicleSelection : IBufferElementData, ISerializable
    {
        /// <summary>
        /// A color set for the custom mesh coloring.
        /// </summary>
        public PrefabRef m_PrefabRef;
        
        public ServiceVehicleSelection(PrefabRef prefabRef)
        {
            m_PrefabRef = prefabRef;
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int _); // version;
            reader.Read(out PrefabRef ref0);
            m_PrefabRef = ref0;
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1); // version;
            writer.Write(m_PrefabRef);
        }
    }
}