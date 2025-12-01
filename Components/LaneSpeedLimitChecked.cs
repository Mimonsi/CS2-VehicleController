using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Components
{
    /// <summary>
    /// Component to mark lane as checked by Vehicle Controller. New lanes will be checked once and then marked with this component.
    /// </summary>
    public struct LaneSpeedLimitChecked : IComponentData, IEmptySerializable
    {
        // No data needed, presence of component is enough
    }
}