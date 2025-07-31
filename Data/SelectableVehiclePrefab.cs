using Colossal.UI.Binding;

namespace VehicleController.Data
{
    /// <summary>
    /// Represents a vehicle prefab that can be selected in the UI.
    /// </summary>
    public class SelectableVehiclePrefab : IJsonWritable
    {
        public string prefabName;
        public string? imageUrl;
        public bool selected;
        /// <summary>
        /// Serializes this instance to JSON for the UI binding system.
        /// </summary>
        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin("VehicleController." + nameof(SelectableVehiclePrefab));
            writer.PropertyName("prefabName");
            writer.Write(prefabName);
            writer.PropertyName("imageUrl");
            writer.Write(imageUrl);
            writer.PropertyName("selected");
            writer.Write(selected);
            writer.TypeEnd();
        }
    }
}