#define ENV_DEVELOPMENT
using System;
using UnityEngine.UI;

namespace everlaster
{
    class Script : MVRScript
    {
        public const string VERSION = "0.0.0";
        public readonly LogBuilder logBuilder = new LogBuilder();
        protected bool initialized;
        UnityEventsListener _pluginUIEventsListener;
        bool _isUIBuilt;

        public override bool ShouldIgnore() => true;

        public override void InitUI()
        {
            if(ShouldIgnore())
            {
                return;
            }

            base.InitUI();
            if(!UITransform)
            {
                return;
            }

            SetGrayBackground();
            if(!_pluginUIEventsListener)
            {
                _pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
                _pluginUIEventsListener.enabledHandlers += OnUIEnabled;
            }
        }

        void SetGrayBackground()
        {
            var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
            background.color = Colors.backgroundGray;
        }

        void OnUIEnabled()
        {
            try
            {
                if(!initialized)
                {
                    return;
                }

                if(!_isUIBuilt)
                {
                    BuildUI();
                    _isUIBuilt = true;
                }
            }
            catch(Exception e)
            {
                logBuilder.Error("{0}: {1}", nameof(OnUIEnabled), e);
            }
        }

        protected virtual void BuildUI()
        {
        }

        protected virtual void OnDestroy()
        {
            DestroyImmediate(_pluginUIEventsListener);
            _pluginUIEventsListener = null;
        }
    }
}
