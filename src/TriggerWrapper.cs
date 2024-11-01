using System;
using System.Collections;
using MacGruber_Utils;
using MeshVR;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace everlaster
{
    class DynamicItemParams
    {
        public readonly string type;
        public readonly string uid;
        public readonly string boolParamName;

        public DynamicItemParams(string type, string uid)
        {
            this.type = type;
            this.uid = uid;
            boolParamName = $"{type}:{uid}";
        }

        public JSONClass GetJSON()
        {
            return new JSONClass
            {
                ["type"] = type,
                ["uid"] = uid,
            };
        }

        public static DynamicItemParams FromJSON(JSONClass jc)
        {
            string type;
            if(!jc.TryGetValue("type", out type))
            {
                return null;
            }

            string uid;
            if(!jc.TryGetValue("uid", out uid))
            {
                return null;
            }

            return new DynamicItemParams(type, uid);
        }
    }

    sealed class TriggerWrapper
    {
        readonly PresetLoadedTriggers _script;
        LogBuilder logBuilder => _script.logBuilder;
        public readonly EventTrigger eventTrigger;
        PresetManager _presetManager;
        public readonly DynamicItemParams dynamicItemParams;
        readonly JSONStorableFloat _delayFloat;

        bool _inactive;
        string _label;
        bool _restoringFromJson;
        readonly Stack<Coroutine> _triggerCoroutines = new Stack<Coroutine>();
        Coroutine _restoreCoroutine;

        public string GetPresetManagerName() => _presetManager != null ? _presetManager.name : null;

        UIDynamicButton _button;
        public UIDynamicButton button
        {
            set
            {
                _button = value;
                _label = _button.label;
            }
        }

        public TriggerWrapper(PresetLoadedTriggers script, string name, PresetManager presetManager, DynamicItemParams dynamicItemParams)
        {
            _script = script;
            _delayFloat = new JSONStorableFloat("Delay", 0.00f, 0.00f, 10.00f, false);
            eventTrigger = new EventTrigger(script, name);
            eventTrigger.panelDisabledHandlers += UpdateButton;
            eventTrigger.onInitPanel += OnInitPanel;

            if(presetManager != null)
            {
                InitPresetManager(presetManager);
            }
            else if(dynamicItemParams != null)
            {
                _inactive = true;
            }
            else
            {
                throw new Exception("PresetManager and DynamicItemParams cannot both be null");
            }

            this.dynamicItemParams = dynamicItemParams;
        }

        void OnInitPanel(Transform triggerActionsPanel)
        {
            var sliderT = UnityEngine.Object.Instantiate(_script.manager.configurableSliderPrefab, triggerActionsPanel);
            var sliderRect = sliderT.GetComponent<RectTransform>();
            sliderRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 50, 120f);
            sliderRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 20f, 545f);
            var image = sliderT.Find("Panel").GetComponent<Image>();
            image.color = Color.clear;
            var valueInputFieldText = sliderT.Find("ValueInputField/Text").GetComponent<Text>();
            valueInputFieldText.color = Color.white;
            var uiDynamic = sliderT.GetComponent<UIDynamicSlider>();
            uiDynamic.labelText.color = Color.white;
            _delayFloat.slider = uiDynamic.slider;
            uiDynamic.label = "Delay (s)";
            uiDynamic.valueFormat = "F2";
        }

        // TODO init only after first trigger is created
        public void InitPresetManager(PresetManager presetManager)
        {
            _presetManager = presetManager;
            _presetManager.postLoadEvent.AddListener(Trigger);
            UpdateButton();
            _inactive = false;
        }

        public void UpdateButton()
        {
            if(_button == null)
            {
                return;
            }

            string label = _label;
            int count = eventTrigger.GetDiscreteActionsStart().Count;
            if(count > 0)
            {
                label += $" ({count})";
            }

            _button.label = label;
            _button.textColor = _inactive ? Colors.inactive : Color.black;
        }

        void Trigger()
        {
            try
            {
                if(!_script.enabled)
                {
                    return;
                }

                _triggerCoroutines.Push(_script.StartCoroutine(TriggerCo()));
            }
            catch(Exception e)
            {
                logBuilder.Exception($"Triggering {eventTrigger.name} failed", e);
            }
        }

        IEnumerator TriggerCo()
        {
            while(_restoringFromJson)
            {
                yield return null;
            }

            var skipActions = new HashSet<TriggerActionDiscrete>();
            if(_script.waitUntilTargetsFoundBool.val)
            {
                float timeout = Time.unscaledDeltaTime + _script.waitUntilTargetsFoundTimeoutFloat.val;
                while(!FindReceiverActions(skipActions) && Time.unscaledDeltaTime < timeout)
                {
                    yield return null;
                }
            }

            if(_delayFloat.val > 0)
            {
                yield return new WaitForSeconds(_delayFloat.val);
            }

            TriggerImmediate();

            yield return null;
            _triggerCoroutines.Pop();
        }

        bool FindReceiverActions(HashSet<TriggerActionDiscrete> skipActions)
        {
            foreach(var action in eventTrigger.GetDiscreteActionsStart())
            {
                if(skipActions.Contains(action) || action == null)
                {
                    continue;
                }

                if(action.receiverAtom == null)
                {
                    skipActions.Add(action);
                }

                var atom = SuperController.singleton.GetAtomByUid(action.receiverAtom.uid);
                if(atom == null)
                {
                    skipActions.Add(action);
                }

                if(action.receiver == null)
                {
                    skipActions.Add(action);
                }

                var storable = atom.GetStorableByID(action.receiver.storeId);
                if(storable == null)
                {
                    skipActions.Add(action);
                }

                if(!storable.GetAllParamAndActionNames().Contains(action.receiverTargetName))
                {
                    return false;
                }
            }

            return true;
        }

        void TriggerImmediate()
        {
            try
            {
                if(ValidateTrigger(eventTrigger) || _script.forceExecuteTriggersBool.val)
                {
                    if(_script.enableLoggingBool.val && eventTrigger.GetDiscreteActionsStart().Count > 0)
                    {
                        JSONArray startActions;
                        string startActionsString = "";
                        if(eventTrigger.GetJSON(_script.subScenePrefix).TryGetValue("startActions", out startActions))
                        {
                            startActionsString = startActions.ToPrettyString();
                        }

                        logBuilder.Message($"Trigger {eventTrigger.name} OK:\n{startActionsString}");
                    }

                    eventTrigger.Trigger();
                }
            }
            catch(Exception e)
            {
                logBuilder.Exception($"Triggering {eventTrigger.name} failed", e);
            }
        }

        bool ValidateTrigger(EventTrigger trigger)
        {
            bool enableLogging = _script.enableLoggingBool.val;
            foreach(var action in trigger.GetDiscreteActionsStart())
            {
                if(action.receiverAtom == null)
                {
                    if(enableLogging)
                    {
                        logBuilder.Error($"{trigger.name}: Action '{action.name}' receiverAtom was null");
                    }

                    return false;
                }

                var atom = SuperController.singleton.GetAtomByUid(action.receiverAtom.uid);
                if(atom == null)
                {
                    if(enableLogging)
                    {
                        logBuilder.Error($"{trigger.name}: Action '{action.name}' receiverAtom '{action.receiverAtom.uid}' not found in scene");
                    }

                    return false;
                }

                if(action.receiver == null)
                {
                    if(enableLogging)
                    {
                        logBuilder.Error($"{trigger.name}: Action '{action.name}' receiver was null");
                    }

                    return false;
                }

                var storable = atom.GetStorableByID(action.receiver.storeId);
                if(storable == null)
                {
                    if(enableLogging)
                    {
                        logBuilder.Error($"{trigger.name}: Action '{action.name}' receiver '{action.receiver.storeId}' not found on atom '{atom.name}'");
                    }

                    return false;
                }
            }

            return true;
        }

        public JSONClass GetJSON(string subscenePrefix)
        {
            var triggerJson = eventTrigger.GetJSON(subscenePrefix);
            if(triggerJson != null && triggerJson.HasKey("startActions"))
            {
                if(triggerJson.HasKey("transitionActions"))
                {
                    triggerJson.Remove("transitionActions");
                }

                if(triggerJson.HasKey("endActions"))
                {
                    triggerJson.Remove("endActions");
                }

                var startActions = triggerJson["startActions"].AsArray;
                if(startActions.Count > 0)
                {
                    var jc = new JSONClass
                    {
                        [JSONKeys.NAME] = eventTrigger.name,
                    };

                    _delayFloat.StoreJSON(jc);
                    jc[JSONKeys.TRIGGER] = triggerJson;

                    if(dynamicItemParams != null)
                    {
                        jc[JSONKeys.DYNAMIC_ITEM_PARAMS] = dynamicItemParams.GetJSON();
                    }

                    return jc;
                }
            }

            return null;
        }

        public void RestoreFromJSON(JSONClass jc, string subscenePrefix, bool mergeRestore, bool setMissingToDefault = true)
        {
            try
            {
                JSONClass triggerJson;
                if(jc.TryGetValue(JSONKeys.TRIGGER, out triggerJson))
                {
                    if(_script.enableAtomFallbackBool.val && triggerJson.HasKey("startActions"))
                    {
                        if(_restoreCoroutine != null)
                        {
                            _script.StopCoroutine(_restoreCoroutine);
                        }

                        _restoreCoroutine = _script.StartCoroutine(RestoreWithReceiverAtomFallbackCo(triggerJson, subscenePrefix, mergeRestore));
                    }
                    else
                    {
                        eventTrigger.RestoreFromJSON(triggerJson, subscenePrefix, mergeRestore);
                    }

                }
                else if(setMissingToDefault)
                {
                    eventTrigger.RestoreFromJSON(new JSONClass());
                }

                _delayFloat.RestoreFromJSON(jc, true, true, setMissingToDefault);
                UpdateButton();
            }
            catch(Exception e)
            {
                logBuilder.Exception($"Restoring {eventTrigger.name} failed", e);
            }
        }

        IEnumerator RestoreWithReceiverAtomFallbackCo(JSONClass triggerJson, string subscenePrefix, bool mergeRestore)
        {
            _restoringFromJson = true;
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            var skipActions = new HashSet<JSONClass>();
            var startActions = new List<JSONClass>();
            foreach(JSONClass action in triggerJson["startActions"].AsArray)
            {
                startActions.Add(action);
            }

            if(_script.waitUntilTargetsFoundBool.val)
            {
                float timeout = Time.unscaledDeltaTime + _script.waitUntilTargetsFoundTimeoutFloat.val;
                while(!FindReceiverActionsInJsonWithAtomFallback(startActions, skipActions) && Time.unscaledDeltaTime < timeout)
                {
                    yield return null;
                }
            }
            else
            {
                FindReceiverActionsInJsonWithAtomFallback(startActions, skipActions);
            }

            eventTrigger.RestoreFromJSON(triggerJson, subscenePrefix, mergeRestore);
            _restoringFromJson = false;
            _restoreCoroutine = null;
        }

        bool FindReceiverActionsInJsonWithAtomFallback(List<JSONClass> startActions, HashSet<JSONClass> skipActions)
        {
            for(int i = 0; i < startActions.Count; i++)
            {
                var actionJson = startActions[i];
                if(skipActions.Contains(actionJson))
                {
                    continue;
                }

                string receiverAtomUid;
                string receiverStoreId;
                string receiverTargetName;
                if(
                    !actionJson.TryGetValue("receiverAtom", out receiverAtomUid) ||
                    !actionJson.TryGetValue("receiver", out receiverStoreId) ||
                    !actionJson.TryGetValue("receiverTargetName", out receiverTargetName)
                )
                {
                    skipActions.Add(actionJson);
                    continue;
                }

                var receiverAtom = SuperController.singleton.GetAtomByUid(receiverAtomUid);
                if(receiverAtom != null)
                {
                    skipActions.Add(actionJson);
                }
                else
                {
                    var storable = _script.containingAtom.GetStorableByID(receiverStoreId);
                    if(storable == null)
                    {
                        skipActions.Add(actionJson);
                        continue;
                    }

                    if(storable.GetAllParamAndActionNames().Contains(receiverTargetName))
                    {
                        actionJson["receiverAtom"] = _script.containingAtom.uid;
                        logBuilder.Message($"{eventTrigger.name}: Receiver Atom not found by name {receiverAtomUid}, using containing atom as fallback.");
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void OnDestroy()
        {
            if(_restoreCoroutine != null)
            {
                _script.StopCoroutine(_restoreCoroutine);
                _restoreCoroutine = null;
            }

            while(_triggerCoroutines.Count > 0)
            {
                _script.StopCoroutine(_triggerCoroutines.Pop());
            }

            eventTrigger.OnRemove();

            if(_presetManager != null)
            {
                _presetManager.postLoadEvent.RemoveListener(Trigger);
            }
        }
    }
}
