#define ENV_DEVELOPMENT
using System;
using SimpleJSON;
using UnityEngine;
using MacGruber_Utils;
using MeshVR;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace everlaster
{
    sealed class PresetLoadedTriggers : Script
    {
        public override bool ShouldIgnore() => false;
        protected override string className => nameof(PresetLoadedTriggers);

        readonly List<string> _personPresetManagerNames = new List<string>
        {
            "AppearancePresets",
            "PosePresets",
            "MorphPresets",
            "geometry", // = Preset = General Presets
            "ClothingPresets",
            "HairPresets",
            "SkinPresets",
            "PluginPresets",
            "AnimationPresets",
            "FemaleBreastPhysicsPresets",
            "FemaleGlutePhysicsPresets",
        };

        readonly Dictionary<string, TriggerWrapper> _triggers = new Dictionary<string, TriggerWrapper>();
        public JSONStorableBool forceExecuteTriggersBool { get; private set; }
        public JSONStorableBool enableLoggingBool { get; private set; }
        public JSONStorableBool enableAtomFallbackBool { get; set; }
        public JSONStorableBool waitUntilTargetsFoundBool { get; private set; }
        public JSONStorableFloat waitUntilTargetsFoundTimeoutFloat { get; private set; }

        DAZCharacterSelector _geometry;

        public override void Init()
        {
            try
            {
                base.Init();
                if(!IsValidAtomType(AtomType.PERSON))
                {
                    return;
                }

                // if(containingAtom.type == "SessionPluginManager")
                // {
                //     AddTrigger("Session Plugin Preset", containingAtom.GetComponent<PresetManager>());
                // }
                // else if(containingAtom.name == "CoreControl")
                // {
                //     AddTrigger("Scene Plugin Preset", "PluginManagerPresets");
                // }

                SimpleTriggerHandler.LoadAssets();
                var sb = new StringBuilder();
                var ids = containingAtom.GetStorableIDs();
                for(int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    if(id == "geometry")
                    {
                        AddTrigger("General Preset", "geometry");
                    }
                    // else if(id.EndsWith("Preset") || id.EndsWith("Presets")))
                    else if(_personPresetManagerNames.Contains(id))
                    {
                        AddTrigger(ToTriggerName(id, sb), id);
                    }
                }

                // FindGeometry();
                // else
                // {
                //     AddTrigger("Preset", "Preset");
                // }

                forceExecuteTriggersBool = new JSONStorableBool("forceExecuteTriggers", false);
                enableLoggingBool = new JSONStorableBool("enableLogging", false);
                enableAtomFallbackBool = new JSONStorableBool("enableAtomFallback", true);
                waitUntilTargetsFoundBool = new JSONStorableBool("waitUntilTargetsFound", true);
                waitUntilTargetsFoundTimeoutFloat = new JSONStorableFloat("waitUntilTargetsFoundTimeout", 2.00f, 0.00f, 10.00f, false);
                forceExecuteTriggersBool.storeType = JSONStorableParam.StoreType.Any;
                enableLoggingBool.storeType = JSONStorableParam.StoreType.Any;
                enableAtomFallbackBool.storeType = JSONStorableParam.StoreType.Any;
                waitUntilTargetsFoundBool.storeType = JSONStorableParam.StoreType.Any;
                waitUntilTargetsFoundTimeoutFloat.storeType = JSONStorableParam.StoreType.Any;
                RegisterBool(forceExecuteTriggersBool);
                RegisterBool(enableLoggingBool);
                RegisterBool(enableAtomFallbackBool);
                RegisterBool(waitUntilTargetsFoundBool);
                RegisterFloat(waitUntilTargetsFoundTimeoutFloat);

                initialized = true;
            }
            catch(Exception e)
            {
                logBuilder.Exception(e);
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
                logBuilder.Debug($"Storable {storableId} not found");
                return;
            }

            var presetManager = storable.GetComponent<PresetManager>();
            if(presetManager == null)
            {
                logBuilder.Debug($"PresetManager not found on {storableId}");
                return;
            }

            // if(containingAtom.type == "Person")
            // {
            //     AddPersonTrigger(storable, name, presetManager);
            // }
            // else
            // {
            //     AddTrigger(name, presetManager);
            // }

            AddTrigger(name, presetManager);
        }

        void AddPersonTrigger(JSONStorable storable, string name, PresetManager presetManager)
        {
            DynamicItemParams dynamicItemParams = null;
            var parent = storable.transform.parent;
            var clothingItem = parent.GetComponent<DAZClothingItem>();
            if(clothingItem != null)
            {
                dynamicItemParams = new DynamicItemParams("clothing", clothingItem.uid);
            }
            else
            {
                var hairItem = parent.GetComponent<DAZHairGroup>();
                if(hairItem != null)
                {
                    dynamicItemParams = new DynamicItemParams("hair", hairItem.uid);
                }
            }

            AddTrigger(name, presetManager, dynamicItemParams);
        }

        void AddTrigger(string name, PresetManager presetManager, DynamicItemParams dynamicItemParams = null) =>
            _triggers[name] = new TriggerWrapper(this, name, presetManager, dynamicItemParams);

        void FindGeometry()
        {
            if(_geometry != null)
            {
                return;
            }

            _geometry = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
        }

        protected override void CreateUI()
        {
            UITransform.Find("Scroll View").GetComponent<ScrollRect>().vertical = false;

            CreateSpacer().height = 95f;
            CreateSpacer(true).height = 88f;
            // CreateSpacer(true).height = 130;

            leftUIContent.GetComponent<VerticalLayoutGroup>().spacing = 0f;
            rightUIContent.GetComponent<VerticalLayoutGroup>().spacing = 8f;

            {
                var uiDynamic = CreateTextField(new JSONStorableString("Title", "\nPreset Loaded Triggers"));
                uiDynamic.height = 100f;
                uiDynamic.backgroundColor = Color.clear;
                var uiDynamicT = uiDynamic.transform;
                uiDynamicT.SetParent(uiDynamicT.parent.parent, false);
                uiDynamicT.SetAsFirstSibling();
                var rectTransform = uiDynamicT.GetComponent<RectTransform>();
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, 100f);
                var textComponent = uiDynamic.UItext;
                textComponent.fontSize = 36;
                textComponent.fontStyle = FontStyle.Bold;
                textComponent.alignment = TextAnchor.LowerCenter;
                uiDynamic.DisableScroll();
            }

            if(containingAtom.type == "Person")
            {
                for(int i = 0; i < _personPresetManagerNames.Count; i++)
                {
                    string presetManagerName = _personPresetManagerNames[i];
                    var trigger = _triggers.Values.FirstOrDefault(x => x.GetPresetManagerName() == presetManagerName);
                    if(trigger != null)
                    {
                        CreateTriggerButton(trigger);
                    }
                }
            }
            else
            {
                foreach(var pair in _triggers)
                {
                    CreateTriggerButton(pair.Value);
                }
            }

            // CreateAltPersonList();

            CreateToggle(forceExecuteTriggersBool, true).label = "Force execute triggers";
            {
                const string infoText =
                    "If a trigger contains any actions where the Receiver Atom or the Receiver isn't found when the event fires," +
                    " none of the actions will execute unless Force execute triggers is enabled.";
                var uiDynamic = CreateTextField(new JSONStorableString("Info", infoText), true);
                uiDynamic.height = 160f;
                uiDynamic.backgroundColor = Color.clear;
                uiDynamic.UItext.alignment = TextAnchor.UpperLeft;
                uiDynamic.DisableScroll();
            }

            CreateToggle(enableLoggingBool, true).label = "Enable logging";
            {
                const string infoText = "If logging is enabled, both successful and unsuccessful trigger events will be logged.";
                var uiDynamic = CreateTextField(new JSONStorableString("Info", infoText), true);
                uiDynamic.height = 100f;
                uiDynamic.backgroundColor = Color.clear;
                uiDynamic.UItext.alignment = TextAnchor.UpperLeft;
                uiDynamic.DisableScroll();
            }

            CreateToggle(enableAtomFallbackBool, true).label = "Use this atom if missing";
            {
                const string infoText = "When restoring the plugin parameters, use this atom as the" +
                    " Receiver Atom on actions where the atom isn't found by the stored atom name." +
                    " The Receiver Atom is swapped only if the stored Receiver and Receiver Target currently exist on this atom.";
                var uiDynamic = CreateTextField(new JSONStorableString("Info", infoText), true);
                uiDynamic.height = 258f;
                uiDynamic.backgroundColor = Color.clear;
                uiDynamic.UItext.alignment = TextAnchor.UpperLeft;
                uiDynamic.DisableScroll();
            }

            {
                CreateSpacer(true).height = 10f;
                var uiDynamic = CreateToggle(waitUntilTargetsFoundBool, true);
                uiDynamic.label = "Wait until targets found";
                var layoutElement = uiDynamic.GetComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                var toggleRect = uiDynamic.GetComponent<RectTransform>();
                toggleRect.pivot = Vector2.zero;
                toggleRect.anchoredPosition = new Vector2(10f, -840f);
                toggleRect.sizeDelta = new Vector2(-15f, 50f);
                CreateSlider(waitUntilTargetsFoundTimeoutFloat, true).label = "Timeout (s)";
            }

            {
                const string infoText = "Wait until each action's Receiver Target is found on the Receiver" +
                    " before executing the trigger.This can matter e.g. when the Receiver is a plugin that doesn't" +
                    " create its triggerable parameters immediately on startup.";
                var uiDynamic = CreateTextField(new JSONStorableString("Info", infoText), true);
                uiDynamic.height = 230f;
                uiDynamic.backgroundColor = Color.clear;
                uiDynamic.UItext.alignment = TextAnchor.UpperLeft;
                uiDynamic.DisableScroll();
            }

            {
                var t = Instantiate(manager.configurableTextFieldPrefab, UITransform.Find("Scroll View"));
                var rectT = t.GetComponent<RectTransform>();
                rectT.pivot = Vector2.zero;
                rectT.anchoredPosition = new Vector2(901f, -1230f);
                rectT.sizeDelta = new Vector2(-900f, 35f);
                var uiDynamic = t.GetComponent<UIDynamicTextField>();
                uiDynamic.text = $"v{VERSION}";
                uiDynamic.backgroundColor = Color.clear;
                var textComponent = uiDynamic.UItext;
                textComponent.alignment = TextAnchor.LowerRight;
                textComponent.fontSize = 26;
                textComponent.color = new Color(0, 0, 0, 0.80f);
                uiDynamic.DisableScroll();
            }
        }

        void CreateAltPersonList()
        {
            var exceptSet = new HashSet<TriggerWrapper>();
            if(containingAtom.type == "Person")
            {
                CreateSubHeader("Category1", "Built In Presets");

                for(int i = 0; i < _personPresetManagerNames.Count; i++)
                {
                    string presetManagerName = _personPresetManagerNames[i];
                    var trigger = _triggers.Values.FirstOrDefault(x => x.GetPresetManagerName() == presetManagerName);
                    if(trigger != null)
                    {
                        exceptSet.Add(trigger);
                        CreateTriggerButton(trigger);
                    }
                }

                CreateSubHeader("Category2", "\nHair/Clothing Item Presets", 100f);
            }
            else
            {
                CreateSpacer().height = 60f;
            }

            var remaining = new List<TriggerWrapper>(_triggers.Values.Except(exceptSet));
            remaining.Sort((a, b) => string.Compare(a.eventTrigger.name, b.eventTrigger.name, StringComparison.Ordinal));
            for(int i = 0; i < remaining.Count; i++)
            {
                var trigger = remaining[i];
                CreateTriggerButton(trigger);
            }
        }

        void CreateSubHeader(string name, string text, float height = 60f)
        {
            var uiDynamic = CreateTextField(new JSONStorableString(name, text));
            var layoutElement = uiDynamic.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            uiDynamic.height = height;
            uiDynamic.backgroundColor = Color.clear;
            var textComponent = uiDynamic.UItext;
            textComponent.fontSize = 32;
            textComponent.alignment = TextAnchor.LowerLeft;
            var scrollViewRect = uiDynamic.transform.Find("Scroll View").GetComponent<RectTransform>();
            var pos = scrollViewRect.anchoredPosition;
            scrollViewRect.anchoredPosition = new Vector2(pos.x, pos.y - 10f);
            uiDynamic.DisableScroll();
        }

        void CreateTriggerButton(TriggerWrapper trigger)
        {
            if(trigger == null)
            {
                return;
            }

            var uiDynamic = CreateButton(trigger.eventTrigger.name);
            uiDynamic.AddListener(trigger.eventTrigger.OpenPanel);
            var textComponent = uiDynamic.buttonText;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = 24;
            textComponent.resizeTextMaxSize = 28;
            textComponent.alignment = TextAnchor.MiddleLeft;
            uiDynamic.height = 69;
            var textRectT = textComponent.GetComponent<RectTransform>();
            var size = textRectT.sizeDelta;
            textRectT.sizeDelta = new Vector2(size.x - 30f, size.y);

            trigger.button = uiDynamic;
            trigger.UpdateButton();
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jc = base.GetJSON(includePhysical, includeAppearance, forceStore);

            try
            {
                jc[JSONKeys.VERSION] = VERSION;
                needsStore = true;
                var triggersArray = new JSONArray();
                foreach(var pair in _triggers)
                {
                    var triggerJson = pair.Value.GetJSON(_subScenePrefix);
                    if(triggerJson != null)
                    {
                        triggersArray.Add(triggerJson);
                    }
                }

                jc[JSONKeys.TRIGGERS] = triggersArray;
                return jc;
            }
            catch(Exception e)
            {
                logBuilder.Exception(e);
                return jc;
            }
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
        {
            try
            {
                base.LateRestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);

                JSONArray triggersArray;
                if(jc.TryGetValue(JSONKeys.TRIGGERS, out triggersArray))
                {
                    var restoredSet = new HashSet<string>();
                    var missingList = new List<JSONClass>();

                    foreach(JSONClass triggerJson in triggersArray)
                    {
                        string triggerName;
                        if(triggerJson.TryGetValue(JSONKeys.NAME, out triggerName))
                        {
                            TriggerWrapper trigger;
                            if(_triggers.TryGetValue(triggerName, out trigger))
                            {
                                trigger.RestoreFromJSON(triggerJson, _subScenePrefix, mergeRestore, setMissingToDefault);
                                restoredSet.Add(triggerName);
                            }
                            else
                            {
                                // missingList.Add(triggerJson);
                            }
                        }
                    }

                    if(setMissingToDefault)
                    {
                        foreach(var pair in _triggers)
                        {
                            if(!restoredSet.Contains(pair.Key))
                            {
                                pair.Value.RestoreFromJSON(new JSONClass(), _subScenePrefix, mergeRestore);
                            }
                        }
                    }

                    // Should only include dynamic items unless restored on a different atom type
                    if(containingAtom.type == "Person")
                    {
                        for(int i = 0; i < missingList.Count; i++)
                        {
                            var triggerJson = missingList[i];
                            JSONClass dynamicItemParamsJson;
                            if(triggerJson.TryGetValue(JSONKeys.DYNAMIC_ITEM_PARAMS, out dynamicItemParamsJson))
                            {
                                string triggerName = triggerJson[JSONKeys.NAME].Value;
                                var dynamicItemParams = DynamicItemParams.FromJSON(dynamicItemParamsJson);
                                if(dynamicItemParams != null)
                                {
                                    AddTrigger(triggerName, null, dynamicItemParams);
                                    SetupDynamicToggledCallback(dynamicItemParams.boolParamName);
                                }
                            }
                        }
                    }
                }
                else if(setMissingToDefault)
                {
                    foreach(var pair in _triggers)
                    {
                        pair.Value.RestoreFromJSON(new JSONClass(), _subScenePrefix, mergeRestore);
                    }
                }
            }
            catch(Exception e)
            {
                logBuilder.Exception(e);
            }
        }

        readonly List<JSONStorableBool> _boolParamsWithCallbacks = new List<JSONStorableBool>();

        // TODO setup for enabled dynamic items on init
        // TODO listen for dynamic item disabled -> disable trigger
        //      (preset manager probably becomes null because of storable being deregistered)
        // TODO listen for dynamic item enabled -> create trigger
        void SetupDynamicToggledCallback(string boolParamName)
        {
            var geometry = containingAtom.GetStorableByID("geometry");
            if(geometry == null)
            {
                logBuilder.Error("SetupCallback: geometry storable not found");
                return;
            }

            var boolParam = geometry.GetBoolJSONParam(boolParamName);
            if(boolParam == null)
            {
                logBuilder.Message($"Bool param {boolParamName} not found - referenced hair/clothing item referenced is not installed?");
                return;
            }

            boolParam.setJSONCallbackFunction += OnDynamicToggled;
            if(!_boolParamsWithCallbacks.Contains(boolParam))
            {
                _boolParamsWithCallbacks.Add(boolParam);
            }
        }

        void OnDynamicToggled(JSONStorableBool boolParam)
        {
            if(!boolParam.val)
            {
                return;
            }

            var trigger = _triggers.Values.FirstOrDefault(x => x.dynamicItemParams.boolParamName == boolParam.name);
            if(trigger == null)
            {
                return;
            }

            FindGeometry();
            if(_geometry == null)
            {
                return;
            }

            var dynamicItemParams = trigger.dynamicItemParams;
            PresetManager presetManager = null;
            if(dynamicItemParams.type == "clothing")
            {
                var clothingItem = _geometry.GetClothingItem(dynamicItemParams.uid);
                if(clothingItem != null)
                {
                    presetManager = clothingItem.GetComponentInChildren<PresetManager>();
                    Debug.Log($"Found presetManager by uid: {dynamicItemParams.uid}");
                }
            }
            else if(dynamicItemParams.type == "hair")
            {
                var hairItem = _geometry.GetHairItem(dynamicItemParams.uid);
                if(hairItem != null)
                {
                    presetManager = hairItem.GetComponentInChildren<PresetManager>();
                    Debug.Log($"Found presetManager by uid: {dynamicItemParams.uid}");
                }
            }

            if(presetManager != null)
            {
                trigger.InitPresetManager(presetManager);
            }
        }

        protected override void DoDestroy()
        {
            foreach(var pair in _triggers)
            {
                pair.Value.OnDestroy();
            }

            for(int i = 0; i < _boolParamsWithCallbacks.Count; i++)
            {
                var boolParam = _boolParamsWithCallbacks[i];
                boolParam.setJSONCallbackFunction -= OnDynamicToggled;
            }
        }
    }
}
