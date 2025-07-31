/*
 * Taken from RoadBuilder-CSII by JadHajjar (T.D.W.)
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using Colossal;
using Colossal.Json;
using Game.SceneFlow;

namespace VehicleController
{
        /// <summary>
        /// Helper for loading embedded localisation dictionaries.
        /// </summary>
        public class LocaleHelper
	{
		private readonly Dictionary<string, Dictionary<string, string>> _locale;

                /// <summary>
                /// Loads all embedded locale resources with the same base name as the provided file.
                /// </summary>
                public LocaleHelper(string dictionaryResourceName)
		{
			var assembly = GetType().Assembly;

			_locale = new Dictionary<string, Dictionary<string, string>>
			{
				[string.Empty] = GetDictionary(dictionaryResourceName)
			};

			foreach (var name in assembly.GetManifestResourceNames())
			{
				if (name == dictionaryResourceName || !name.Contains(Path.GetFileNameWithoutExtension(dictionaryResourceName) + "."))
				{
					continue;
				}

				var key = Path.GetFileNameWithoutExtension(name);

				_locale[key.Substring(key.LastIndexOf('.') + 1)] = GetDictionary(name);
			}

			Dictionary<string, string> GetDictionary(string resourceName)
			{
				using var resourceStream = assembly.GetManifestResourceStream(resourceName);
				if (resourceStream == null)
				{
					return new Dictionary<string, string>();
				}

				using var reader = new StreamReader(resourceStream, Encoding.UTF8);
				JSON.MakeInto<Dictionary<string, string>>(JSON.Load(reader.ReadToEnd()), out var dictionary);

				return dictionary;
			}
		}

                /// <summary>
                /// Looks up a translated string in the currently active dictionary.
                /// </summary>
                public static string? Translate(string? id, string? fallback = null)
		{
			if (id is not null && GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var result))
			{
				return result;
			}

			return fallback ?? id;
		}
		
                /// <summary>
                /// Returns a dictionary source for every embedded language.
                /// </summary>
                public IEnumerable<DictionarySource> GetAvailableLanguages()
		{
			foreach (var item in _locale)
			{
				yield return new DictionarySource(item.Key is "" ? "en-US" : item.Key, item.Value);
			}
		}

                /// <summary>
                /// Simple wrapper exposing a dictionary to the localisation system.
                /// </summary>
                public class DictionarySource : IDictionarySource
		{
			private readonly Dictionary<string, string> _dictionary;

                        /// <summary>
                        /// Creates a new dictionary source for the specified locale.
                        /// </summary>
                        public DictionarySource(string localeId, Dictionary<string, string> dictionary)
			{
				LocaleId = localeId;
				_dictionary = dictionary;
			}

                        /// <summary>
                        /// Identifier of the locale this dictionary provides.
                        /// </summary>
                        public string LocaleId { get; }

                        /// <inheritdoc />
                        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
                        {
                                return _dictionary;
                        }

                        /// <inheritdoc />
                        public void Unload() { }
		}
	}
}
