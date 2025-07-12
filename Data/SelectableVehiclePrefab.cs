using Colossal.UI.Binding;

namespace VehicleController
{
    public class SelectableVehiclePrefab : IJsonWritable
    {
        public string prefabName;
        public bool selected;
        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin("VehicleController." + nameof(SelectableVehiclePrefab));
            writer.PropertyName("prefabName");
            writer.Write(prefabName);
            writer.PropertyName("selected");
            writer.Write(selected);
            writer.TypeEnd();
        }
    }
}