using Godot;
using System;
using 你的项目.Scripts.管理器;

/// <summary>
/// StatusBarUI partial class for backpack (card collection) functionality.
/// </summary>
public partial class StatusBarUI
{
	// ======================== 卡牌背包相关 ========================
	private void 初始化背包()
	{
		try
		{
			if (卡牌标签Button == null) return;
			if (背包面板 == null) return;
			if (格子Container == null) return;

			格子Container.UpdateMinimumSize();
			背包面板.UpdateMinimumSize();
			
			背包面板.Visible = false;
			刷新背包按类型(null);
			卡牌标签Button.Pressed += 切换背包显示;
		}
		catch (Exception e) { }
	}

	private void 切换背包显示()
	{
		if (背包面板 == null) return;
		bool 当前可见 = 背包面板.Visible;
		if (当前可见) 关闭背包();
		else
		{
			背包面板.Visible = true;
			背包打开中 = true;
			if (BackpackFilter节点 != null) BackpackFilter节点.Visible = true;

			刷新背包按类型(null);
			BackpackFilter节点?.重置选中();
		}
	}

	private void 关闭背包()
	{
		if (背包面板 != null && 背包面板.Visible)
		{
			背包面板.Visible = false;
			背包打开中 = false;
			if (BackpackFilter节点 != null) BackpackFilter节点.Visible = false;
			隐藏详情框();
			if (当前选中的卡牌 != null)
			{
				当前选中的卡牌.取消选中();
				当前选中的卡牌 = null;
			}
		}
	}

	/// <summary>Refreshes the backpack display, optionally filtered by card type.</summary>
	public void 刷新背包按类型(CardData.卡牌类型? 类型 = null)
	{
		foreach (Node child in 格子Container.GetChildren()) child.QueueFree();
		var 玩家卡组 = 玩家卡组管理器.实例?.玩家初始卡组;
		if (玩家卡组 == null) return;
		foreach (var CardData in 玩家卡组)
		{
			if (CardData == null) continue;
			if (类型 == null || CardData.类型 == 类型) 创建CardUI(CardData);
		}
		格子Container.QueueSort();
		隐藏详情框();
		if (当前选中的卡牌 != null)
		{
			当前选中的卡牌.取消选中();
			当前选中的卡牌 = null;
		}
		播放所有卡牌摸动画();

		格子Container.UpdateMinimumSize();
		格子Container.ForceUpdateTransform();
		背包面板.UpdateMinimumSize();
		背包面板.ForceUpdateTransform();

		var 原模式 = 背包面板.VerticalScrollMode;
		背包面板.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
		背包面板.VerticalScrollMode = 原模式;

		CallDeferred(nameof(延迟刷新滚动条));
	}

	private void 延迟刷新滚动条()
	{
		if (背包面板 == null) return;
		var 原模式 = 背包面板.VerticalScrollMode;
		背包面板.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
		背包面板.VerticalScrollMode = 原模式;
		背包面板.UpdateMinimumSize();
	}

	private void 创建CardUI(CardData CardData)
	{
		if (CardUIPrefab == null || 格子Container == null) return;
		var CardUI控件 = CardUIPrefab.Instantiate<CardUI>();
		if (CardUI控件 == null) return;
		CardUI控件.设置为查看模式(CardData);
		CardUI控件.鼠标进入卡牌 += (卡牌) => 当鼠标进入卡牌(卡牌);
		CardUI控件.鼠标离开卡牌 += (卡牌) => 当鼠标离开卡牌(卡牌);
		CardUI控件.卡牌被点击 += (godotObject) =>
		{
			var 卡牌 = godotObject as CardUI;
			if (卡牌 != null) 当卡牌被点击(卡牌);
		};
		格子Container.AddChild(CardUI控件);
		CardUI控件.应用初始缩放();
		CardUI控件.CallDeferred(nameof(CardUI.应用初始缩放));
	}

	private void 播放所有卡牌摸动画()
	{
		if (格子Container == null) return;
		foreach (Node child in 格子Container.GetChildren())
		{
			if (child is CardUI 卡牌)
			{
				var tween = 卡牌.CreateTween();
				tween.SetParallel(false);
				tween.TweenProperty(卡牌, "scale", 卡牌.获取原始缩放() * 1.2f, 0.1f);
				tween.TweenProperty(卡牌, "scale", 卡牌.获取原始缩放(), 0.1f);
			}
		}
	}

	/// <summary>Called when mouse enters a card UI element.</summary>
	public void 当鼠标进入卡牌(CardUI 卡牌)
	{
		bool 在强化预览面板内 = false;
		Node 当前节点 = 卡牌.GetParent();
		while (当前节点 != null)
		{
			if (当前节点 is 强化预览面板) { 在强化预览面板内 = true; break; }
			当前节点 = 当前节点.GetParent();
		}
		if (在强化预览面板内)
		{
			显示详情框(卡牌, 卡牌.获取卡牌描述());
			return;
		}
		if (当前选中的卡牌 != null && 当前选中的卡牌 != 卡牌)
		{
			当前选中的卡牌.取消选中();
			隐藏详情框();
		}
		卡牌.设置选中(true);
		当前选中的卡牌 = 卡牌;
		显示详情框(卡牌, 卡牌.获取卡牌描述());
	}

	/// <summary>Called when mouse leaves a card UI element.</summary>
	public void 当鼠标离开卡牌(CardUI 卡牌)
	{
		bool 在强化预览面板内 = false;
		Node 当前节点 = 卡牌.GetParent();
		while (当前节点 != null)
		{
			if (当前节点 is 强化预览面板) { 在强化预览面板内 = true; break; }
			当前节点 = 当前节点.GetParent();
		}
		if (在强化预览面板内) { 隐藏详情框(); return; }
		if (当前选中的卡牌 == 卡牌)
		{
			卡牌.取消选中();
			当前选中的卡牌 = null;
			隐藏详情框();
		}
	}

	private void 当卡牌被点击(CardUI 卡牌)
	{
		if (强化预览面板预制体 == null) return;
		var CardData = 卡牌.当前查看CardData;
		if (CardData == null) return;
		try
		{
			var 面板实例 = 强化预览面板预制体.Instantiate<强化预览面板>();
			if (面板实例 == null) return;
			面板实例.Name = "强化预览面板";
			AddChild(面板实例);
			面板实例.AnchorLeft = 0;
			面板实例.AnchorTop = 0;
			面板实例.AnchorRight = 1;
			面板实例.AnchorBottom = 1;
			面板实例.Size = Vector2.Zero;
			面板实例.显示强化预览(CardData);
		}
		catch (Exception e) { }
	}


}
