/* /////////////////////////////////////////////////////////////////////////////////////////////////
Utils 2023-11-12 by MacGruber
Collection of various utility functions.
https://www.patreon.com/MacGruber_Laboratory

Licensed under CC-BY. (see https://creativecommons.org/licenses/by/4.0/)
Feel free to incorporate this libary in your releases, but credit is required.

Non triggers related code removed by everlaster, plus minor edits.
Original MacGruber_Utils.cs: https://hub.virtamate.com/resources/macgruber-utils.40744/

///////////////////////////////////////////////////////////////////////////////////////////////// */

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using everlaster;
using SimpleJSON;

namespace MacGruber
{
	// ===========================================================================================

	// TriggerHandler implementation for easier handling of custom triggers.
	// Essentially call this in your plugin init code:
	//     StartCoroutine(SimpleTriggerHandler.LoadAssets());
	//
	// Credit to AcidBubbles for figuring out how to do custom triggers.
	public class SimpleTriggerHandler : TriggerHandler
	{
		public static bool Loaded { get; private set; }

		private static SimpleTriggerHandler myInstance;

		private RectTransform myTriggerActionsPrefab;
        private RectTransform myTriggerActionMiniPrefab;
        private RectTransform myTriggerActionDiscretePrefab;
        private RectTransform myTriggerActionTransitionPrefab;

		public static SimpleTriggerHandler Instance {
			get {
				if (myInstance == null)
					myInstance = new SimpleTriggerHandler();
				return myInstance;
			}
		}

		public static void LoadAssets()
		{
			SuperController.singleton.StartCoroutine(Instance.LoadAssetsInternal());
		}

        private IEnumerator LoadAssetsInternal()
        {
            foreach (var x in LoadAsset("z_ui2", "TriggerActionsPanel", p => myTriggerActionsPrefab = p))
				yield return x;
            foreach (var x in LoadAsset("z_ui2", "TriggerActionMiniPanel", p => myTriggerActionMiniPrefab = p))
				yield return x;
            foreach (var x in LoadAsset("z_ui2", "TriggerActionDiscretePanel", p => myTriggerActionDiscretePrefab = p))
				yield return x;
            foreach (var x in LoadAsset("z_ui2", "TriggerActionTransitionPanel", p => myTriggerActionTransitionPrefab = p))
				yield return x;

			Loaded = true;
        }

        private IEnumerable LoadAsset(string assetBundleName, string assetName, Action<RectTransform> assign)
        {
            AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
            if (request == null)
				throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null request.");
            yield return request;
            GameObject go = request.GetAsset<GameObject>();
            if (go == null)
				throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null GameObject.");
            RectTransform prefab = go.GetComponent<RectTransform>();
            if (prefab == null)
				throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null RectTansform.");
			assign(prefab);
        }


		void TriggerHandler.RemoveTrigger(Trigger t)
        {
            // nothing to do
        }

        void TriggerHandler.DuplicateTrigger(Trigger t)
        {
            throw new NotImplementedException();
        }

        RectTransform TriggerHandler.CreateTriggerActionsUI()
        {
			return UnityEngine.Object.Instantiate(myTriggerActionsPrefab);
        }

        RectTransform TriggerHandler.CreateTriggerActionMiniUI()
        {
            return UnityEngine.Object.Instantiate(myTriggerActionMiniPrefab);
        }

        RectTransform TriggerHandler.CreateTriggerActionDiscreteUI()
        {
            return UnityEngine.Object.Instantiate(myTriggerActionDiscretePrefab);
        }

        RectTransform TriggerHandler.CreateTriggerActionTransitionUI()
        {
			RectTransform rt = UnityEngine.Object.Instantiate(myTriggerActionTransitionPrefab);
			rt.GetComponent<TriggerActionTransitionUI>().startWithCurrentValToggle.gameObject.SetActive(false);
            return rt;
        }

        void TriggerHandler.RemoveTriggerActionUI(RectTransform rt)
        {
            UnityEngine.Object.Destroy(rt?.gameObject);
        }
	}

	// Base class for easier handling of custom triggers.
	public abstract class CustomTrigger : Trigger
	{
		public string Name {
			get { return name; }
			set { name = value; myNeedInit = true; }
		}

		public string SecondaryName {
			get { return secondaryName; }
			set { secondaryName = value; myNeedInit = true; }
		}

		public MVRScript Owner {
			get; private set;
		}

		public List<TriggerActionDiscrete> GetDiscreteActionsStart() => discreteActionsStart;

		private string name;
		private string secondaryName;
		private bool myNeedInit = true;

		public CustomTrigger(MVRScript owner, string name, string secondary = null)
		{
			Name = name;
			SecondaryName = secondary;
			Owner = owner;
			handler = SimpleTriggerHandler.Instance;
			SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;
		}

		public CustomTrigger(CustomTrigger other)
		{
			Name = other.Name;
			SecondaryName = other.SecondaryName;
			Owner = other.Owner;
			handler = SimpleTriggerHandler.Instance;
			SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;

			JSONClass jc = other.GetJSON(Owner.subScenePrefix);
			base.RestoreFromJSON(jc, Owner.subScenePrefix, false);
		}

		private void OnAtomRename(string oldName, string newName) => SyncAtomNames();

		GameObject ownerCloseButton;
		GameObject ownerScrollView;

		public void OpenPanel()
		{
			if (!SimpleTriggerHandler.Loaded)
			{
				SuperController.LogError("CustomTrigger: You need to call SimpleTriggerHandler.LoadAssets() before use.");
				return;
			}

			triggerActionsParent = Owner.UITransform;
			ownerCloseButton = triggerActionsParent.Find("CloseButton").gameObject;
			ownerScrollView = triggerActionsParent.Find("Scroll View").gameObject;
            InitTriggerUI();
            OpenTriggerActionsPanel();
			if (myNeedInit)
			{
				Transform panel = triggerActionsPanel.Find("Panel");
				panel.Find("Header Text").GetComponent<Text>().text = Name;
				Transform secondaryHeader = panel.Find("Trigger Name Text");
				secondaryHeader.gameObject.SetActive(!string.IsNullOrEmpty(SecondaryName));
				secondaryHeader.GetComponent<Text>().text = SecondaryName;

				InitPanel();
				myNeedInit = false;
			}
		}

		public override void OpenTriggerActionsPanel()
		{
			ownerCloseButton.SetActive(false);
			ownerScrollView.SetActive(false);
			base.OpenTriggerActionsPanel();
			Transform contentT = triggerActionsPanel.Find("Content");
			if (contentT != null)
			{
				foreach (Transform childT in contentT)
				{
					if (!childT.name.Contains("Tab1"))
					{
						childT.SafeDestroyGameObject();
					}
				}
			}
		}

		public override void CloseTriggerActionsPanel()
		{
			base.CloseTriggerActionsPanel();
			if (ownerCloseButton != null)
			{
				ownerCloseButton.SetActive(true);
			}
			if (ownerScrollView != null)
			{
				ownerScrollView.SetActive(true);
			}
		}

		protected abstract void InitPanel();

		public void RestoreFromJSON(JSONClass jc, string subScenePrefix, bool isMerge, bool setMissingToDefault)
		{
			if (jc.HasKey(Name))
			{
				JSONClass tc = jc[Name].AsObject;
				if (tc != null)
					base.RestoreFromJSON(tc, subScenePrefix, isMerge);
			}
			else if (setMissingToDefault)
			{
				base.RestoreFromJSON(new JSONClass());
			}
		}

		public void OnRemove() => SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRename;
	}

	// Wrapper for easier handling of custom event triggers.
	public class EventTrigger : CustomTrigger
	{
		private JSONStorableFloat delayFloat;

		public EventTrigger(MVRScript owner, string name, JSONStorableFloat delayFloat, string secondary = null)
			: base(owner, name, secondary)
		{
			this.delayFloat = delayFloat;
		}

		public EventTrigger(EventTrigger other)
			: base(other)
		{
		}

		protected override void InitPanel()
		{
			Transform panel = triggerActionsPanel.Find("Panel");
			panel.Find("Header Text").GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 50f);

			Transform sliderT = UnityEngine.Object.Instantiate(Owner.manager.configurableSliderPrefab, triggerActionsPanel);
			var sliderRect = sliderT.GetComponent<RectTransform>();
			sliderRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 50, 120f);
			sliderRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 20f, 545f);
			var image = sliderT.Find("Panel").GetComponent<Image>();
			image.color = Color.clear;
			var valueInputFieldText = sliderT.Find("ValueInputField/Text").GetComponent<Text>();
			valueInputFieldText.color = Color.white;
			var uiDynamic = sliderT.GetComponent<UIDynamicSlider>();
			uiDynamic.labelText.color = Color.white;
			delayFloat.slider = uiDynamic.slider;
			uiDynamic.label = "Delay (s)";
			uiDynamic.valueFormat = "F2";

			Transform content = triggerActionsPanel.Find("Content");
			content.Find("Tab1/Label").GetComponent<Text>().text = "Event Actions";
			content.Find("Tab2").gameObject.SetActive(false);
			content.Find("Tab3").gameObject.SetActive(false);
		}

		public void Trigger()
		{
			active = true;
			active = false;
		}

		public void Trigger(List<TriggerActionDiscrete> actionsNeedingUpdateOut)
		{
			Trigger();
			for (int i=0; i<discreteActionsStart.Count; ++i)
			{
				if (discreteActionsStart[i].timerActive)
					actionsNeedingUpdateOut.Add(discreteActionsStart[i]);
			}
		}
	}

	// Wrapper for easier handling of custom float triggers.
	public class FloatTrigger : CustomTrigger
	{
		public FloatTrigger(MVRScript owner, string name, string secondary = null)
			: base(owner, name, secondary)
		{
		}

		public FloatTrigger(FloatTrigger other)
			: base(other)
		{
		}

		protected override void InitPanel()
		{
			Transform content = triggerActionsPanel.Find("Content");
			content.Find("Tab2/Label").GetComponent<Text>().text = "Value Actions";
			content.Find("Tab3/Label").GetComponent<Text>().text = "Event Actions";
			content.Find("Tab2").GetComponent<Toggle>().isOn = true;
			content.Find("Tab1").gameObject.SetActive(false);
		}

		public void Trigger(float v)
		{
			_transitionInterpValue = Mathf.Clamp01(v);
			if (transitionInterpValueSlider != null)
				transitionInterpValueSlider.value = _transitionInterpValue;
			for (int i=0; i<transitionActions.Count; ++i)
				transitionActions[i].TriggerInterp(_transitionInterpValue, true);
			for (int i=0; i<discreteActionsEnd.Count; ++i)
				discreteActionsEnd[i].Trigger();
		}

		public void Trigger(float v, List<TriggerActionDiscrete> actionsNeedingUpdateOut)
		{
			Trigger(v);
			for (int i=0; i<discreteActionsEnd.Count; ++i)
			{
				if (discreteActionsEnd[i].timerActive)
					actionsNeedingUpdateOut.Add(discreteActionsEnd[i]);
			}
		}
	}
}
