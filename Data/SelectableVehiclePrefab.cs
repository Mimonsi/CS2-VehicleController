using Colossal.UI.Binding;

namespace VehicleController
{
    public class SelectableVehiclePrefab : IJsonWritable
    {
        public string prefabName;
        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin("VehicleController." + nameof(SelectableVehiclePrefab));
            writer.PropertyName("prefabName");
            writer.Write(prefabName);
            writer.TypeEnd();
        }
    }
}