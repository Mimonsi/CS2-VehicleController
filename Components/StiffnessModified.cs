using Game.Prefabs;
using Unity.Entities;

namespace VehicleController.Components
{
    /// <summary>
    /// Component to display Vehicle Controller has halved the speed limit for a road
    /// </summary>
    public struct StiffnessModified : IComponentData
    {
        public SwayingData VanillaData;
    }
}