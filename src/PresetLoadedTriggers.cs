#define ENV_DEVELOPMENT
using System;
using SimpleJSON;
using UnityEngine;
using MacGruber;
using MeshVR;
using System.Collections.Generic;

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
                    // var sessionPluginsPresetManager = containingAtom.GetComponent<PresetManager>();
                    // _triggers.Add(new TriggerWrapper(this, "SessionPluginPresets"));
                }
                else if(containingAtom.name == "CoreControl")
                {
                    // var scenePluginsPresetManager = containingAtom.GetStorableByID("PluginManagerPresets").GetComponent<PresetManager>();
                    // _triggers.Add(new TriggerWrapper(this, "ScenePluginPresets"));
                }
                else if(containingAtom.type == "Person")
                {
                    var appearancePresetManager = containingAtom.GetStorableByID("AppearancePresets").GetComponent<PresetManager>();
                    const string triggerName = "On Appearance Preset Loaded";
                    var trigger = new TriggerWrapper(this, triggerName, appearancePresetManager);
                    _triggers.Add(trigger);

                    // var animationPresetManager = containingAtom.GetStorableByID("AnimationPresets").GetComponent<PresetManager>();
                    // var pluginPresetManager = containingAtom.GetStorableByID("PluginPresets").GetComponent<PresetManager>();
                    // ...
                }
                else
                {
                    // var presetManager = containingAtom.GetStorableByID("Preset").GetComponent<PresetManager>();
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

        protected override void BuildUI()
        {
            foreach(var trigger in _triggers)
            {
                var triggerButton = CreateButton(trigger.GetName());
                triggerButton.AddListener(trigger.OpenPanel);
                triggerButton.height = 50f;
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
