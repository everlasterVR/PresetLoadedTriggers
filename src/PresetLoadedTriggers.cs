#define ENV_DEVELOPMENT
using System;
using SimpleJSON;
using UnityEngine;
using MacGruber;
using MeshVR;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace everlaster
{
    sealed class PresetLoadedTriggers : Script
    {
        public override bool ShouldIgnore() => false;

        readonly List<TriggerWrapper> _triggers = new List<TriggerWrapper>();
        public JSONStorableBool forceExecuteTriggersBool { get; private set; }
        public JSONStorableBool enableLoggingBool { get; private set; }

        public override void Init()
        {
            try
            {
                SimpleTriggerHandler.LoadAssets();

                if(containingAtom.type == "SessionPluginManager")
                {
                    AddTrigger("On Session Plugin Preset Loaded", containingAtom.GetComponent<PresetManager>());
                }
                else if(containingAtom.name == "CoreControl")
                {
                    AddTrigger("On Scene Plugin Preset Loaded", "PluginManagerPresets");
                }
                else if(containingAtom.type == "Person")
                {
                    var sb = new StringBuilder();
                    var ids = containingAtom.GetStorableIDs();
                    for(int i = 0; i < ids.Count; i++)
                    {
                        string id = ids[i];
                        if(id == "Preset")
                        {
                            AddTrigger("General Preset", "Preset");
                        }
                        else if(id.EndsWith("Preset") || id.EndsWith("Presets"))
                        {
                            AddTrigger(ToTriggerName(id, sb), id);
                        }
                    }
                }
                else
                {
                    AddTrigger("On Preset Loaded", "Preset");
                }

                forceExecuteTriggersBool = new JSONStorableBool("forceExecuteTriggers", false);
                enableLoggingBool = new JSONStorableBool("enableLogging", false);
                RegisterBool(forceExecuteTriggersBool);
                RegisterBool(enableLoggingBool);

                initialized = true;
            }
            catch(Exception e)
            {
                logBuilder.Error("{0}: {1}", nameof(Init), e);
            }
        }

        static string ToTriggerName(string input, StringBuilder sb)
        {
            var pattern = Utils.NewRegex("(?<!^)(?=[A-Z])");
            string separatedString = pattern.Replace(input, " ");
            int spaceIndex = separatedString.LastIndexOf(" ", StringComparison.Ordinal);

            sb.Clear();
            if(spaceIndex >= 0)
            {
                sb.Append(separatedString.Substring(0, spaceIndex));
                sb.Append(" ");
            }

            sb.Append("Preset");
            return sb.ToString();
        }

        void AddTrigger(string name, string storableId)
        {
            var storable = containingAtom.GetStorableByID(storableId);
            if(storable == null)
            {
                return;
            }

            var presetManager = storable.GetComponent<PresetManager>();
            if(presetManager == null)
            {
                return;
            }

            AddTrigger(name, presetManager);
        }

        void AddTrigger(string name, PresetManager presetManager) => _triggers.Add(new TriggerWrapper(this, name, presetManager));

        protected override void BuildUI()
        {
            var title = CreateTextField(new JSONStorableString("Title", "\nPreset Loaded Triggers"));
            title.UItext.fontSize = 32;
            title.UItext.fontStyle = FontStyle.Bold;
            title.UItext.alignment = TextAnchor.LowerCenter;
            title.backgroundColor = Color.clear;
            title.height = 100f;

            // TODO if person, sort always available by predefined, then sort dynamic below alphabetically
            // TODO possible to sort clothing item presets into one category and hair item presets into another?
            foreach(var trigger in _triggers)
            {
                var triggerButton = CreateButton(trigger.GetName());
                triggerButton.AddListener(trigger.OpenPanel);
                var textComponent = triggerButton.buttonText;
                textComponent.resizeTextForBestFit = true;
                textComponent.resizeTextMinSize = 24;
                textComponent.resizeTextMaxSize = 28;
                textComponent.alignment = TextAnchor.MiddleLeft;
                triggerButton.height = 50f;
                var textRect = triggerButton.transform.Find("Text").GetComponent<RectTransform>();
                var size = textRect.sizeDelta;
                textRect.sizeDelta = new Vector2(size.x - 30f, size.y);
            }

            CreateToggle(forceExecuteTriggersBool, true).label = "Force execute triggers";
            CreateToggle(enableLoggingBool, true).label = "Enable logging";

            const string infoText =
                "\n\nIf an event trigger contains any invalid actions when the event fires, none of the actions will execute" +
                " unless Force execute triggers is enabled." +
                " Invalid means that the action's receiverAtom or receiver are null or not found in the scene." +
                "\n\nIf logging is enabled, both successful and unsuccessful trigger events will be logged.";
            var textField = CreateTextField(new JSONStorableString("Info", infoText), true);
            textField.height = 500;
            textField.backgroundColor = Color.clear;
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json[JSONKeys.VERSION] = VERSION;
            needsStore = true;

            for(int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].StoreToJSON(json);
            }

            return json;
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
        {
            base.LateRestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);
            for(int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].RestoreFromJSON(jc);
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                base.OnDestroy();
                foreach(var trigger in _triggers)
                {
                    trigger.OnDestroy();
                }
            }
            catch(Exception e)
            {
                if(initialized)
                {
                    SuperController.LogError($"{nameof(PresetLoadedTriggers)}.{nameof(OnDestroy)}: {e}");
                }
                else
                {
                    Debug.LogError($"{nameof(PresetLoadedTriggers)}.{nameof(OnDestroy)}: {e}");
                }
            }
        }
    }
}
