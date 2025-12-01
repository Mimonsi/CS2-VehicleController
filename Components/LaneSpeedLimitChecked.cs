using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Components
{
    /// <summary>
    /// Component to mark lane as checked by Vehicle Controller. New lanes will be checked once and then marked with this component.
    /// </summary>
    public struct LaneSpeedLimitChecked : IComponentData, ISerializable
    {
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            
        }
    }
}