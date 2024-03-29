using System;
using MacGruber;
using MeshVR;
using SimpleJSON;
using UnityEngine;

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

    class TriggerWrapper
    {
        readonly PresetLoadedTriggers _script;
        public readonly EventTrigger eventTrigger;
        PresetManager _presetManager;
        public readonly DynamicItemParams dynamicItemParams;
        UnityEventsListener _panelEventsListener;

        bool _opened;
        bool _inactive;
        string _label;
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
            eventTrigger = new EventTrigger(script, name);

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

        // TODO init only after first trigger is created
        public void InitPresetManager(PresetManager presetManager)
        {
            _presetManager = presetManager;
            _presetManager.postLoadEvent.AddListener(Trigger);
            UpdateButton();
            _inactive = false;
        }

        public void OpenPanel()
        {
            try
            {
                eventTrigger.OpenPanel();

                if(!_opened)
                {
                    if(eventTrigger.triggerActionsPanel == null)
                    {
                        throw new Exception("TriggerActionsPanel is null");
                    }

                    _panelEventsListener = eventTrigger.triggerActionsPanel.gameObject.AddComponent<UnityEventsListener>();
                    _panelEventsListener.disabledHandlers += UpdateButton;
                }

                _opened = true;
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", eventTrigger.Name, nameof(OpenPanel), e);
            }
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
                label += $" ({count})".Bold();
            }

            _button.label = label;
            _button.textColor = _inactive ? Colors.inactive : Color.black;
        }

        public void Trigger()
        {
            try
            {
                if(!_script.enabled)
                {
                    return;
                }

                if(ValidateTrigger(eventTrigger) || _script.forceExecuteTriggersBool.val)
                {
                    if(_script.enableLoggingBool.val && eventTrigger.GetDiscreteActionsStart().Count > 0)
                    {
                        JSONArray startActions;
                        string startActionsString = "";
                        if(eventTrigger.GetJSON().TryGetValue("startActions", out startActions))
                        {
                            startActionsString = JSONUtils.Prettify(startActions);
                        }

                        _script.logBuilder.Message("Trigger {0} OK:\n{1}", eventTrigger.Name, startActionsString);
                    }

                    eventTrigger.Trigger();
                }
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", eventTrigger.Name, nameof(Trigger), e);
            }
        }

        bool ValidateTrigger(EventTrigger trigger)
        {
            bool enableLogging = _script.enableLoggingBool.val;
            foreach(var action in trigger.GetDiscreteActionsStart())
            {
                if(!action.receiverAtom)
                {
                    if(enableLogging)
                    {
                        _script.logBuilder.ErrorNoReport($"{trigger.Name}: Action '{action.name}' receiverAtom was null");
                    }

                    return false;
                }

                var atom = SuperController.singleton.GetAtomByUid(action.receiverAtom.uid);
                if(!atom)
                {
                    if(enableLogging)
                    {
                        _script.logBuilder.ErrorNoReport($"{trigger.Name}: Action '{action.name}' receiverAtom '{action.receiverAtom.uid}' not found in scene");
                    }

                    return false;
                }

                if(!action.receiver)
                {
                    if(enableLogging)
                    {
                        _script.logBuilder.ErrorNoReport($"{trigger.Name}: Action '{action.name}' receiver was null");
                    }

                    return false;
                }

                var storable = atom.GetStorableByID(action.receiver.storeId);
                if(!storable)
                {
                    if(enableLogging)
                    {
                        _script.logBuilder.ErrorNoReport($"{trigger.Name}: Action '{action.name}' receiver '{action.receiver.storeId}' not found on atom '{atom.name}'");
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
                        [JSONKeys.NAME] = eventTrigger.Name,
                        [JSONKeys.TRIGGER] = triggerJson,
                    };

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
                    if(_script.enableAtomFallbackBool.val)
                    {
                        ReceiverAtomFallback(triggerJson);
                    }
                    eventTrigger.RestoreFromJSON(triggerJson, subscenePrefix, mergeRestore);
                }
                else if(setMissingToDefault)
                {
                    eventTrigger.RestoreFromJSON(new JSONClass());
                }

                UpdateButton();
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", eventTrigger.Name, nameof(RestoreFromJSON), e);
            }
        }

        void ReceiverAtomFallback(JSONClass triggerJson)
        {
            JSONArray startActions;
            if(!triggerJson.TryGetValue("startActions", out startActions))
            {
                return;
            }

            var atom = _script.containingAtom;
            foreach(JSONClass actionJson in startActions)
            {
                string receiverAtomUid;
                string receiverStoreId;
                string receiverTargetName;
                if(
                    !actionJson.TryGetValue("receiverAtom", out receiverAtomUid) ||
                    !actionJson.TryGetValue("receiver", out receiverStoreId) ||
                    !actionJson.TryGetValue("receiverTargetName", out receiverTargetName)
                )
                {
                    continue;
                }

                var receiverAtom = SuperController.singleton.GetAtomByUid(receiverAtomUid);
                if(receiverAtom == null)
                {
                    var storable = atom.GetStorableByID(receiverStoreId);
                    if(storable != null && storable.GetAllParamAndActionNames().Contains(receiverTargetName))
                    {
                        actionJson["receiverAtom"] = atom.uid;
                        _script.logBuilder.Message("{0}: Receiver Atom not found by name {1}, using containing atom as fallback.", eventTrigger.Name, receiverAtomUid);
                    }
                }
            }
        }

        public void OnDestroy()
        {
            eventTrigger.OnRemove();

            if(_presetManager != null)
            {
                _presetManager.postLoadEvent.RemoveListener(Trigger);
            }

            if(_panelEventsListener != null)
            {
                UnityEngine.Object.DestroyImmediate(_panelEventsListener);
            }
        }
    }
}
