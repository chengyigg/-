using Godot;
using System;

/// <summary>
/// StatusBarUI partial class for settings menu animations and device panel.
/// </summary>
public partial class StatusBarUI
{
	private bool _正在关闭设置菜单 = false;
	
	// ======================== 设置菜单动画 ========================
	private async void 播放设备动画_修正版()
	{
		if (设备动画播放器 == null) return;
		
		设备动画播放器.Play("RESET");
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		if (设备动画播放器.HasAnimation("设备界面"))
			设备动画播放器.Play("设备界面");
		
		if (SettingsMenuManager节点 != null)
			SettingsMenuManager节点.激活();
	}

	private void 检查动画状态()
	{
		// 调试方法保留但无输出
	}

	private Control 获取动画容器节点()
	{
		if (设备动画播放器 == null) return null;
		var 设置内选择节点 = 设备动画播放器.GetNodeOrNull<Control>("设置内选择");
		if (设置内选择节点 == null)
		{
			foreach (Node 子节点 in 设备动画播放器.GetChildren())
			{
				if (子节点 is Control 控制节点)
				{
					var 可能的动画容器 = 控制节点.GetNodeOrNull<Control>("动画容器");
					if (可能的动画容器 != null) return 可能的动画容器;
				}
			}
			return null;
		}
		var 动画容器节点 = 设置内选择节点.GetNodeOrNull<Control>("动画容器");
		if (动画容器节点 != null) return 动画容器节点;
		string[] 可能名称 = { "动画容器", "按钮容器", "旋转容器", "content", "buttons" };
		foreach (string 名称 in 可能名称)
		{
			var 节点 = 设置内选择节点.GetNodeOrNull<Control>(名称);
			if (节点 != null) return 节点;
		}
		return null;
	}

	private void 动态修正动画轨道路径()
	{
		if (设备动画播放器 == null || !设备动画播放器.HasAnimation("出现")) return;
		var 动画容器节点 = 获取动画容器节点();
		if (动画容器节点 == null) return;
		string 正确路径 = 动画容器节点.GetPath();
		var 原始动画 = 设备动画播放器.GetAnimation("出现");
		var 新动画 = new Animation();
		新动画.Length = 原始动画.Length;
		新动画.LoopMode = 原始动画.LoopMode;
		for (int i = 0; i < 原始动画.GetTrackCount(); i++)
		{
			Animation.TrackType 轨道类型 = 原始动画.TrackGetType(i);
			string 轨道路径 = 原始动画.TrackGetPath(i).ToString();
			if (轨道路径.Contains("rotation") && 轨道路径.Contains("动画容器") && !轨道路径.Contains(正确路径))
				轨道路径 = 正确路径 + ":rotation";
			int 新轨道索引 = 新动画.AddTrack(轨道类型);
			新动画.TrackSetPath(新轨道索引, 轨道路径);
			for (int j = 0; j < 原始动画.TrackGetKeyCount(i); j++)
			{
				double 时间 = 原始动画.TrackGetKeyTime(i, j);
				var 值 = 原始动画.TrackGetKeyValue(i, j);
				float 过渡 = 原始动画.TrackGetKeyTransition(i, j);
				新动画.TrackInsertKey(新轨道索引, 时间, 值, 过渡);
			}
		}
		var 动画库名称列表 = 设备动画播放器.GetAnimationLibraryList();
		if (动画库名称列表.Count > 0)
		{
			var 动画库 = 设备动画播放器.GetAnimationLibrary(动画库名称列表[0]);
			if (动画库 != null)
			{
				if (动画库.HasAnimation("出现")) 动画库.RemoveAnimation("出现");
				动画库.AddAnimation("出现", 新动画);
			}
		}
	}

	private void 确保设置菜单节点树可见()
	{
		var 动画容器节点 = 获取动画容器节点();
		if (动画容器节点 != null)
		{
			动画容器节点.Visible = true;
			var 父节点 = 动画容器节点.GetParent();
			if (父节点 != null && 父节点 is Control 父控制节点) 父控制节点.Visible = true;
			foreach (Node 子节点 in 动画容器节点.GetChildren())
				if (子节点 is Control 控制节点) 控制节点.Visible = true;
		}
	}

	private async void 关闭设置菜单()
	{
		if (_正在关闭设置菜单) return;
		_正在关闭设置菜单 = true;
		
		if (SettingsMenuManager节点 != null && SettingsMenuManager节点.音量面板打开)
			await SettingsMenuManager节点.关闭音量面板();
		
		if (设备动画播放器 != null && 设备动画播放器.HasAnimation("消失"))
		{
			设备动画播放器.Play("消失");
			await ToSignal(设备动画播放器, AnimationPlayer.SignalName.AnimationFinished);
		}
		
		SettingsMenuManager节点?.关闭();
		标签管理器节点?.获取当前标签()?.GrabFocus();
		_正在关闭设置菜单 = false;
	}

	private void 当设置关闭动画完成(StringName 动画名称)
	{
		if (设备动画播放器 != null)
		{
			设备动画播放器.AnimationFinished -= 当设置关闭动画完成;
			设备动画播放器.SpeedScale = 1;
			设备动画播放器.Stop();
		}
		SettingsMenuManager节点?.关闭();
		标签管理器节点?.获取当前标签()?.GrabFocus();
	}
}
