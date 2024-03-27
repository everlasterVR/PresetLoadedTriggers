using System;
using MacGruber;
using MeshVR;
using SimpleJSON;
using UnityEngine;

namespace everlaster
{
    class TriggerWrapper
    {
        readonly PresetLoadedTriggers _script;
        readonly EventTrigger _eventTrigger;
        bool _opened;

        public string GetName() => _eventTrigger.Name;

        public TriggerWrapper(PresetLoadedTriggers script, string name, PresetManager presetManager)
        {
            _script = script;
            _eventTrigger = new EventTrigger(script, name);
            presetManager.postLoadEvent.AddListener(Trigger); // TODO add only after first trigger is created
        }

        public void OpenPanel()
        {
            try
            {
                _eventTrigger.OpenPanel();

                if(!_opened)
                {
                    if(_eventTrigger.triggerActionsPanel == null)
                    {
                        throw new Exception("TriggerActionsPanel is null");
                    }
                }

                _opened = true;
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", _eventTrigger.Name, nameof(OpenPanel), e);
            }
        }

        public void Trigger()
        {
            try
            {
                if(!_script.enabled)
                {
                    return;
                }

                if(ValidateTrigger(_eventTrigger) || _script.forceExecuteTriggersBool.val)
                {
                    if(_script.enableLoggingBool.val)
                    {
                        JSONArray startActions;
                        string startActionsString = "";
                        if(_eventTrigger.GetJSON().TryGetValue("startActions", out startActions))
                        {
                            startActionsString = JSONUtils.Prettify(startActions);
                        }

                        _script.logBuilder.Message("Trigger {0} OK:\n{1}", _eventTrigger.Name, startActionsString);
                    }

                    _eventTrigger.Trigger();
                }
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", _eventTrigger.Name, nameof(Trigger), e);
            }
        }

        bool ValidateTrigger(EventTrigger trigger)
        {
            bool enableLogging = _script.enableLoggingBool.val;
            foreach(var action in trigger.DiscreteActionsStart)
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
            jc[_eventTrigger.Name] = _eventTrigger.GetJSON();
        }

        public void RestoreFromJSON(JSONClass jc)
        {
            try
            {
                JSONClass triggerJson;
                if(jc.TryGetValue(_eventTrigger.Name, out triggerJson))
                {
                    _eventTrigger.RestoreFromJSON(triggerJson);
                }
            }
            catch(Exception e)
            {
                _script.logBuilder.Error("{0}.{1}: {2}", _eventTrigger.Name, nameof(RestoreFromJSON), e);
            }
        }
    }
}
