//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2015 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Attaching this script to an element of a scroll view will make it possible to center on it by clicking on it.
/// </summary>

public class UICenterTargetOnClick : MonoBehaviour
{
	public GameObject target;

	void OnClick()
	{
		if (null == target)
			return;

		UICenterOnChild center = NGUITools.FindInParents<UICenterOnChild>(target);
		UIPanel panel = NGUITools.FindInParents<UIPanel>(target);

		if (center != null)
		{
			if (center.enabled)
				center.CenterOn(target.transform);
		}
		else if (panel != null && panel.clipping != UIDrawCall.Clipping.None)
		{
			UIScrollView sv = panel.GetComponent<UIScrollView>();
			Vector3 offset = -panel.cachedTransform.InverseTransformPoint(target.transform.position);
			if (!sv.canMoveHorizontally) offset.x = panel.cachedTransform.localPosition.x;
			if (!sv.canMoveVertically) offset.y = panel.cachedTransform.localPosition.y;
			SpringPanel.Begin(panel.cachedGameObject, offset, 6f);
		}
	}
}
