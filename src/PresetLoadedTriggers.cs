#define ENV_DEVELOPMENT
using System;
using SimpleJSON;
using UnityEngine;
using MacGruber;
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
                    AddTrigger("Session Plugin Preset", containingAtom.GetComponent<PresetManager>());
                }
                else if(containingAtom.name == "CoreControl")
                {
                    AddTrigger("Scene Plugin Preset", "PluginManagerPresets");
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
                    AddTrigger("Preset", "Preset");
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
            CreateSpacer().height = 85;
            CreateSpacer(true).height = 130;
            var verticalLayoutGroup = leftUIContent.GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.spacing = 0f;

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
                DisableScroll(uiDynamic);
            }

            var exceptSet = new HashSet<TriggerWrapper>();
            if(containingAtom.type == "Person")
            {
                CreateSubHeader("Category1", "Built In Presets");

                for(int i = 0; i < _personPresetManagerNames.Count; i++)
                {
                    string presetManagerName = _personPresetManagerNames[i];
                    var trigger = _triggers.FirstOrDefault(t => t.presetManager.name == presetManagerName);
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

            var remaining = new List<TriggerWrapper>(_triggers.Except(exceptSet));
            remaining.Sort((a, b) => string.Compare(a.eventTrigger.Name, b.eventTrigger.Name, StringComparison.Ordinal));
            for(int i = 0; i < remaining.Count; i++)
            {
                var trigger = remaining[i];
                CreateTriggerButton(trigger);
            }

            CreateToggle(forceExecuteTriggersBool, true).label = "Force execute triggers";
            CreateToggle(enableLoggingBool, true).label = "Enable logging";

            {
                const string infoText =
                    "If a trigger contains any actions where the receiverAtom or receiver isn't found when the event fires," +
                    " none of the actions will execute unless Force execute triggers is enabled." +
                    "\n\nIf logging is enabled, both successful and unsuccessful trigger events will be logged.";
                var uiDynamic = CreateTextField(new JSONStorableString("Info", infoText), true);
                uiDynamic.height = 300;
                uiDynamic.backgroundColor = Color.clear;
                uiDynamic.UItext.alignment = TextAnchor.UpperLeft;
                DisableScroll(uiDynamic);
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
            DisableScroll(uiDynamic);
        }

        void CreateTriggerButton(TriggerWrapper trigger)
        {
            if(trigger == null)
            {
                return;
            }

            var uiDynamic = CreateButton(trigger.eventTrigger.Name);
            uiDynamic.AddListener(trigger.OpenPanel);
            var textComponent = uiDynamic.buttonText;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = 24;
            textComponent.resizeTextMaxSize = 28;
            textComponent.alignment = TextAnchor.MiddleLeft;
            uiDynamic.height = 69;
            var textRect = uiDynamic.transform.Find("Text").GetComponent<RectTransform>();
            var size = textRect.sizeDelta;
            textRect.sizeDelta = new Vector2(size.x - 30f, size.y);

            trigger.button = uiDynamic;
            trigger.UpdateLabel();
        }

        static void DisableScroll(UIDynamicTextField uiDynamic)
        {
            var scrollViewT = uiDynamic.transform.Find("Scroll View");
            scrollViewT.Find("Scrollbar Horizontal").SafeDestroyGameObject();

            var scrollRect = scrollViewT.GetComponent<ScrollRect>();
            var content = scrollRect.content;
            var movementType = scrollRect.movementType;
            float elasticity = scrollRect.elasticity;
            bool inertia = scrollRect.inertia;
            float decelerationRate = scrollRect.decelerationRate;
            float scrollSensitivity = scrollRect.scrollSensitivity;
            var viewport = scrollRect.viewport;
            var horizontalScrollbar = scrollRect.horizontalScrollbar;
            var verticalScrollbar = scrollRect.verticalScrollbar;
            var horizontalScrollbarVisibility = scrollRect.horizontalScrollbarVisibility;
            var verticalScrollbarVisibility = scrollRect.verticalScrollbarVisibility;
            float horizontalScrollbarSpacing = scrollRect.horizontalScrollbarSpacing;
            float verticalScrollbarSpacing = scrollRect.verticalScrollbarSpacing;

            DestroyImmediate(scrollRect);

            var newScroll = scrollViewT.gameObject.AddComponent<PassThroughScroll>();
            newScroll.content = content;
            newScroll.movementType = movementType;
            newScroll.elasticity = elasticity;
            newScroll.inertia = inertia;
            newScroll.decelerationRate = decelerationRate;
            newScroll.scrollSensitivity = scrollSensitivity;
            newScroll.viewport = viewport;
            newScroll.horizontalScrollbar = horizontalScrollbar;
            newScroll.verticalScrollbar = verticalScrollbar;
            newScroll.horizontalScrollbarVisibility = horizontalScrollbarVisibility;
            newScroll.verticalScrollbarVisibility = verticalScrollbarVisibility;
            newScroll.horizontalScrollbarSpacing = horizontalScrollbarSpacing;
            newScroll.verticalScrollbarSpacing = verticalScrollbarSpacing;
            newScroll.vertical = false;
            newScroll.horizontal = false;
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json[JSONKeys.VERSION] = VERSION;
            needsStore = true;
            var triggersJson = new JSONClass();

            for(int i = 0; i < _triggers.Count; i++)
            {
                _triggers[i].StoreToJSON(triggersJson);
            }

            json[JSONKeys.TRIGGERS] = triggersJson;
            return json;
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
        {
            base.LateRestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);
            JSONClass triggersJson;
            if(jc.TryGetValue(JSONKeys.TRIGGERS, out triggersJson))
            {
                for(int i = 0; i < _triggers.Count; i++)
                {
                    _triggers[i].RestoreFromJSON(triggersJson);
                }
            }
            else if(setMissingToDefault)
            {
                for(int i = 0; i < _triggers.Count; i++)
                {
                    _triggers[i].RestoreFromJSON(new JSONClass());
                }
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
