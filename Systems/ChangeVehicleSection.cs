using System.Collections.Generic;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI.InGame;
using Unity.Entities;

namespace VehicleController.Systems
{
    public partial class ChangeVehicleSection : InfoSectionBase
    {
        private List<string> availableVehicles;
        private ILog Logger = LogManager.GetLogger($"{nameof(VehicleController)}.{nameof(ChangeVehicleSection)}")
            .SetShowsErrorsInUI(false);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            
            availableVehicles = new List<string>()
            {
                "NA_PoliceVehicle01",
                "NA_PoliceVehicle02",
                "EU_PoliceVehicle01",
                "EU_PoliceVehicle02",
            };
            
            
            Logger.Info("ChangeVehicleSection created.");
        }
        
        protected override void Reset()
        {
            
        }

        protected override void OnUpdate()
        {
            visible = Visible();
        }

        private bool Visible()
        {
            return true;
        }

        protected override void OnProcess()
        {
            
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("availableVehicles");
            writer.ArrayBegin(4);
            new SelectableVehiclePrefab(){ prefabName = "NA_PoliceVehicle01" }.Write(writer);
            new SelectableVehiclePrefab(){ prefabName = "NA_PoliceVehicle02" }.Write(writer);
            new SelectableVehiclePrefab(){ prefabName = "EU_PoliceVehicle01" }.Write(writer);
            new SelectableVehiclePrefab(){ prefabName = "EU_PoliceVehicle02" }.Write(writer);
            writer.ArrayEnd();
            writer.PropertyName("serviceType");
            writer.Write("Anything");
        }

        protected override string group => nameof(ChangeVehicleSection);
    }
}