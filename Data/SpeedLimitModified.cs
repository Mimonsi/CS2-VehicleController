using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace VehicleController.Data
{
    /// <summary>
    /// Component to display Vehicle Controller has halved the speed limit for a road
    /// </summary>
    public struct SpeedLimitModified : IComponentData
    {
        public float VanillaSpeedLimit;
    }
}