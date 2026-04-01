using System;
using System.Collections.Generic;
using Colossal.Logging;
using Unity.Entities;
using UnityEngine;
using VehicleController.Components;

namespace VehicleController.Systems
{
    /// <summary>
    /// Manages a clipboard of vehicle prefab names that can be copied from
    /// and pasted to service building entities.
    /// </summary>
    public class VehicleClipboard
    {
        private static ILog log => Mod.log;
        private readonly List<string> _entries = new();

        public IReadOnlyList<string> Entries => _entries;
        public int Count => _entries.Count;
        public bool IsEmpty => _entries.Count == 0;

        public string Serialize() => string.Join(",", _entries);

        /// <summary>
        /// Copies allowed vehicle prefab names from the given entity's buffer.
        /// </summary>
        public void CopyFrom(EntityManager em, Entity entity)
        {
            _entries.Clear();
            if (em.HasBuffer<AllowedVehiclePrefab>(entity))
            {
                foreach (var allowed in em.GetBuffer<AllowedVehiclePrefab>(entity))
                {
                    _entries.Add(allowed.PrefabName.ToString());
                }
            }
            log.Info($"Copied {_entries.Count} vehicles to clipboard");
        }

        /// <summary>
        /// Replaces the allowed vehicle buffer on the target entity with the clipboard contents.
        /// </summary>
        public void ApplyTo(EntityManager em, Entity entity)
        {
            if (em.HasBuffer<AllowedVehiclePrefab>(entity))
            {
                em.RemoveComponent<AllowedVehiclePrefab>(entity);
            }
            if (_entries.Count == 0)
                return;
            var buffer = em.AddBuffer<AllowedVehiclePrefab>(entity);
            foreach (var name in _entries)
            {
                buffer.Add(new AllowedVehiclePrefab { PrefabName = name });
            }
        }

        /// <summary>
        /// Exports clipboard contents to the system clipboard.
        /// </summary>
        public void ExportToSystem()
        {
            try
            {
                GUIUtility.systemCopyBuffer = Serialize();
                log.Info("Clipboard exported to system clipboard");
            }
            catch (Exception ex)
            {
                log.Error($"Error exporting clipboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports clipboard contents from the system clipboard.
        /// </summary>
        public void ImportFromSystem()
        {
            try
            {
                string clipboardText = GUIUtility.systemCopyBuffer;
                log.Info($"Importing clipboard data: {clipboardText}");
                _entries.Clear();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    foreach (string entry in clipboardText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _entries.Add(entry.Trim());
                    }
                }
                log.Info($"Imported {_entries.Count} entries from system clipboard");
            }
            catch (Exception ex)
            {
                log.Error($"Error importing clipboard: {ex.Message}");
            }
        }
    }
}
