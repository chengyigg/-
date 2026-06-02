using Godot;
using System;
using 你的项目.Scripts.管理器;
using System.Collections.Generic;

/// <summary>
/// Main status bar UI that displays player stats, backpack, settings, and inventory.
/// </summary>
public partial class StatusBarUI : CanvasLayer
{
	// 单例实例
	private static StatusBarUI 实例;
	public static StatusBarUI 获取实例() => 实例;
	
	private CanvasLayer 当前按键设置图层 = null;
	
	// ===== BackpackFilter节点引用 =====
	[Export] private BackpackFilter BackpackFilter节点;
	// 详情框相关
	private Control 详情框;
	private Label 详情描述标签;

	// 节点引用（在编辑器中拖入）
	[Export] private Control 动画容器;
	[Export] private AnimationPlayer 界面动画播放器;
	[Export] private 标签管理器 标签管理器节点;
	[Export] private SettingsMenuManager SettingsMenuManager节点;
	[Export] private AnimationPlayer 设备动画播放器;
	[Export] private PackedScene CardUIPrefab;
	[Export] private PackedScene 强化预览面板预制体;
	// ========== 背包相关变量（直接在场景中拖入）==========
	[Export] private Button 卡牌标签Button;
	[Export] private ScrollContainer 背包面板;
	[Export] private GridContainer 格子Container;
	// ===============================================================
	[Export] private Font 详情框字体;
	// 新增：可拖入的详情框背景样式资源
	[Export] private StyleBox 详情框背景样式;   
	// 道具相关UI节点
	[Export] private ScrollContainer 道具面板;
	[Export] private GridContainer 道具格子容器;
	[Export] private Control 道具描述框;
	[Export] private Label 道具描述标签;

	// 可拖入自定义详情框背景样式
	private ConfirmationDialog 使用确认对话框;

	private List<道具槽> 所有道具槽 = new List<道具槽>();
	private int 当前选中行 = 0;
	private int 当前选中列 = 0;
	private bool 道具标签激活 = false;

	// 状态
	private bool 已打开 = false;
	public bool 界面已打开 => 已打开;
	private bool 正在播放离场动画 = false;

	// 记录当前选中的卡牌
	private CardUI 当前选中的卡牌 = null;

	// 背包打开状态标记
	private bool 背包打开中 = false;

	// 输入动作名称
	private const string 状态栏动作 = "打开状态栏";

	// ======================== 生命周期 ========================
	public override void _Ready()
	{
		// 先设置单例
		if (实例 == null)
		{
			实例 = this;
			ProcessMode = ProcessModeEnum.Always;
		}
		else
		{
			QueueFree();
			return;
		}

		// 初始化道具对话框（必须在添加信号前）
		初始化道具对话框();

		// 连接道具管理器信号（先判空）
		if (PlayerInventoryManager.实例 != null)
		{
			PlayerInventoryManager.实例.道具列表已更新 += 刷新道具UI;
			刷新道具UI();
		}

		// 初始隐藏道具面板
		if (道具面板 != null) 道具面板.Visible = false;
		if (BackpackFilter节点 != null) BackpackFilter节点.Visible = false;

		初始化组件();
		初始化背包();
		创建详情框();
		初始隐藏();
		CallDeferred("延迟初始化修正");
	}

	public override void _Input(InputEvent 事件)
	{
		if (正在播放离场动画)
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (事件.IsActionPressed(状态栏动作))
		{
			切换显示状态();
			GetViewport().SetInputAsHandled();
		}

		// 道具导航
		if (道具标签激活 && 道具面板.Visible && !正在播放离场动画)
		{
			if (事件.IsActionPressed("ui_up"))
			{
				int 新行 = 当前选中行 - 1;
				if (新行 >= 0)
				{
					当前选中行 = 新行;
					更新选中高亮和描述();
				}
				GetViewport().SetInputAsHandled();
			}
			else if (事件.IsActionPressed("ui_down"))
			{
				int 新行 = 当前选中行 + 1;
				int 新索引 = 新行 * 2 + 当前选中列;
				if (新索引 < 所有道具槽.Count)
				{
					当前选中行 = 新行;
					更新选中高亮和描述();
				}
				GetViewport().SetInputAsHandled();
			}
			else if (事件.IsActionPressed("ui_left"))
			{
				if (当前选中列 == 1)
				{
					当前选中列 = 0;
					更新选中高亮和描述();
				}
				GetViewport().SetInputAsHandled();
			}
			else if (事件.IsActionPressed("ui_right"))
			{
				if (当前选中列 == 0)
				{
					int 新索引 = 当前选中行 * 2 + 1;
					if (新索引 < 所有道具槽.Count)
					{
						当前选中列 = 1;
						更新选中高亮和描述();
					}
				}
				GetViewport().SetInputAsHandled();
			}
			else if (事件.IsActionPressed("ui_accept"))
			{
				使用当前选中的道具();
				GetViewport().SetInputAsHandled();
			}
		}

		// 原有的取消键逻辑
		if (已打开 && Visible && 事件.IsActionPressed("ui_cancel"))
		{
			// 1. 优先关闭强化面板（已有代码）
			var 强化面板 = GetNodeOrNull<强化预览面板>("强化预览面板");
			if (强化面板 != null && 强化面板.Visible)
			{
				if (强化面板.HasMethod("关闭面板")) 强化面板.Call("关闭面板");
				else 强化面板.QueueFree();
				GetViewport().SetInputAsHandled();
				return;
			}
			
			// 2. 其次，如果背包打开，关闭背包（已有代码）
			if (背包面板 != null && 背包面板.Visible)
			{
				关闭背包();
				关闭界面();
				GetViewport().SetInputAsHandled();
				return;
			}

			// 2.5 新增：如果道具标签激活且道具面板可见，则只关闭道具面板
			if (道具标签激活 && 道具面板.Visible)
			{
				道具面板.Visible = false;
				道具标签激活 = false;
				if (道具描述框 != null) 道具描述框.Visible = false;
				标签管理器节点?.获取当前标签()?.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}
			
			if (SettingsMenuManager节点 != null && SettingsMenuManager节点.是否激活中())
			{
				if (SettingsMenuManager节点.音量面板打开) _ = SettingsMenuManager节点.关闭音量面板();
				else 关闭设置菜单();
			}
			else
			{
				关闭界面();
			}
			GetViewport().SetInputAsHandled();
		}
	}

	// ======================== 基础控制方法 ========================
	private void 初始化组件()
	{
		if (标签管理器节点 != null)
		{
			标签管理器节点.标签切换 += (int 索引) => 当标签切换(索引);
			标签管理器节点.打开设置菜单 += () => 当打开设置菜单();
		}

		if (SettingsMenuManager节点 != null)
		{
			SettingsMenuManager节点.返回游戏请求 += 当返回游戏请求;
			SettingsMenuManager节点.打开音量设置 += 当打开音量设置;
			SettingsMenuManager节点.返回主菜单请求 += 当返回主菜单请求;
			SettingsMenuManager节点.打开按键设置 += 当打开按键设置;
		}
	}

	private void 延迟初始化修正()
	{
		动态修正动画轨道路径();
		确保设置菜单节点树可见();
	}

	private void 控制玩家移动(bool 启用)
	{
		if (玩家管理器.实例 != null && 玩家管理器.实例.玩家存在())
		{
			玩家管理器.实例.当前玩家.设置可移动(启用);
		}
	}

	private void 切换显示状态()
	{
		if (正在播放离场动画) return;
		if (已打开)
		{
			关闭界面();
		}
		else
		{
			var 当前场景 = GetTree().CurrentScene;
			if (当前场景 == null || !当前场景.IsInGroup("需要玩家"))
			{
				return;
			}
			if (玩家管理器.实例 == null || !玩家管理器.实例.玩家存在())
			{
				return;
			}
			打开界面();
		}
	}

	private void 打开界面()
	{
		if (正在播放离场动画) return;
		已打开 = true;
		Visible = true;
		标签管理器节点?.隐藏所有标签();
		控制玩家移动(false);
		string 当前场景路径 = GetTree().CurrentScene.SceneFilePath;
		全局背景音乐管理器.实例?.根据场景播放音乐(当前场景路径);
		if (SettingsMenuManager节点 != null) SettingsMenuManager节点.关闭();
		if (界面动画播放器 != null && 界面动画播放器.HasAnimation("打开"))
		{
			界面动画播放器.Play("打开");
			界面动画播放器.AnimationFinished += 当开场动画完成;
		}
		else 当开场动画完成("无动画");
	}

	private void 关闭界面()
	{
		if (!Visible || 正在播放离场动画) return;
		正在播放离场动画 = true;
		标签管理器节点?.隐藏所有标签();
		if (界面动画播放器 != null && 界面动画播放器.HasAnimation("离开"))
		{
			界面动画播放器.Play("离开");
			界面动画播放器.AnimationFinished += 当离场动画完成;
		}
		else 当离场动画完成("无动画");
	}

	private void 初始隐藏()
	{
		已打开 = false;
		Visible = false;
		正在播放离场动画 = false;
	}

	private void 当开场动画完成(StringName 动画名称)
	{
		if (界面动画播放器 != null) 界面动画播放器.AnimationFinished -= 当开场动画完成;
		标签管理器节点?.显示所有标签();
		if (标签管理器节点 != null)
		{
			标签管理器节点.设置当前选中索引(0);
			标签管理器节点.获取标签(0)?.GrabFocus();
		}
	}

	private void 当离场动画完成(StringName 动画名称)
	{
		if (界面动画播放器 != null) 界面动画播放器.AnimationFinished -= 当离场动画完成;
		已打开 = false;
		Visible = false;
		正在播放离场动画 = false;
		控制玩家移动(true);
	}

	// ======================== 标签切换 ========================
	private void 当标签切换(int 索引)
	{
		if (背包打开中) 关闭背包();

		// 道具标签索引假设为 1
		if (索引 == 0)
		{
			道具面板.Visible = true;
			道具标签激活 = true;
			刷新道具UI();
			道具面板.ZIndex = 100;
			背包面板.Visible = false;
			
			if (道具描述框 != null)
				道具描述框.Visible = true;
		}
		else
		{
			道具面板.Visible = false;
			道具标签激活 = false;
			
			if (道具描述框 != null)
				道具描述框.Visible = false;
		}

		if (索引 == 3) 播放设备动画_修正版();
	}

	private void 调试打印动画容器状态(string 阶段)
	{
		// 调试方法保留但无输出
	}

	private void 调试打印动画关键帧(string 动画名)
	{
		// 调试方法保留但无输出
	}

	private void 当打开设置菜单()
	{
		播放设备动画_修正版();
	}

	private void 当返回游戏请求()
	{
		if (SettingsMenuManager节点 != null && SettingsMenuManager节点.是否激活中()) 关闭设置菜单();
		else 关闭界面();
	}

	private void 当打开音量设置() { }

	private void 当返回主菜单请求()
	{
		FollowerManager.实例?.隐藏所有跟随者();
		
		if (SettingsMenuManager节点 != null && SettingsMenuManager节点.是否激活中())
			关闭设置菜单();
		
		GetTree().Paused = false;
		
		if (界面已打开)
			关闭界面();
		
		if (TransitionManager.实例 != null)
		{
			TransitionManager.实例.开始转场(
				场景加载器.实例.开始场景路径,
				null,
				GetTree().CurrentScene.SceneFilePath
			);
		}
		else
		{
			场景加载器.实例.加载开始菜单();
		}
	}

	private void 当打开按键设置()
	{
		if (当前按键设置图层 != null && IsInstanceValid(当前按键设置图层))
		{
			当前按键设置图层.QueueFree();
			当前按键设置图层 = null;
		}
		
		SettingsMenuManager节点?.关闭();
		
		var 重绑定场景 = GD.Load<PackedScene>("res://状态栏系统（主要ui）/KeyRebindInterface.tscn");
		if (重绑定场景 == null) return;
		
		var 新图层 = new CanvasLayer();
		新图层.Layer = 9;
		AddChild(新图层);
		当前按键设置图层 = 新图层;
		
		var 重绑定界面 = 重绑定场景.Instantiate<Control>();
		新图层.AddChild(重绑定界面);
		
		GetTree().Paused = true;
		
		重绑定界面.TreeExited += () => 
		{
			GetTree().Paused = false;
			if (当前按键设置图层 == 新图层)
				当前按键设置图层 = null;
			新图层.QueueFree();
			SettingsMenuManager节点?.强制重新激活();
		};
	}
}
