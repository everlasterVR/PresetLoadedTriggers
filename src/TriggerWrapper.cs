using System;
using MacGruber;
using MeshVR;
using SimpleJSON;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    class TriggerWrapper
    {
        readonly PresetLoadedTriggers _script;
        public readonly EventTrigger eventTrigger;
        public readonly PresetManager presetManager;
        UnityEventsListener _panelEventsListener;

        bool _opened;
        string _label;

        UIDynamicButton _button;
        public UIDynamicButton button
        {
            set
            {
                _button = value;
                _label = _button.label;
            }
        }

        public TriggerWrapper(PresetLoadedTriggers script, string name, PresetManager presetManager)
        {
            _script = script;
            eventTrigger = new EventTrigger(script, name);
            this.presetManager = presetManager;
            this.presetManager.postLoadEvent.AddListener(Trigger); // TODO add only after first trigger is created
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
                    _panelEventsListener.disabledHandlers += UpdateLabel;
                }

                _opened = true;
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", eventTrigger.Name, nameof(OpenPanel), e);
            }
        }

        public void UpdateLabel()
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
                    if(_script.enableLoggingBool.val)
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

        public void StoreToJSON(JSONClass jc)
        {
            var eventJson = eventTrigger.GetJSON();
            if(eventJson.HasKey("transitionActions"))
            {
                eventJson.Remove("transitionActions");
            }

            if(eventJson.HasKey("endActions"))
            {
                eventJson.Remove("endActions");
            }

            if(eventJson.HasKey("startActions"))
            {
                var startActions = eventJson["startActions"].AsArray;
                if(startActions.Count > 0)
                {
                    jc[eventTrigger.Name] = eventJson;
                }
            }
        }

        public void RestoreFromJSON(JSONClass jc, bool setMissingToDefault = true)
        {
            try
            {
                JSONClass triggerJson;
                if(jc.TryGetValue(eventTrigger.Name, out triggerJson))
                {
                    eventTrigger.RestoreFromJSON(triggerJson);
                }
                else if(setMissingToDefault)
                {
                    eventTrigger.RestoreFromJSON(new JSONClass());
                }

                UpdateLabel();
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", eventTrigger.Name, nameof(RestoreFromJSON), e);
            }
        }

        public void OnDestroy()
        {
            eventTrigger.OnRemove();

            if(presetManager != null)
            {
                presetManager.postLoadEvent.RemoveListener(Trigger);
            }

            if(_panelEventsListener != null)
            {
                UnityEngine.Object.DestroyImmediate(_panelEventsListener);
            }
        }
    }
}
