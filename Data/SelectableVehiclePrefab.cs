using Colossal.UI.Binding;

namespace VehicleController.Data
{
    public class SelectableVehiclePrefab : IJsonWritable
    {
        public string prefabName;
        public string? imageUrl;
        public bool selected;
        public void Write(IJsonWriter writer)
        {
            //Mod.log.Info($"Writing SelectableVehiclePrefab: {prefabName}, selected: {selected}");
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