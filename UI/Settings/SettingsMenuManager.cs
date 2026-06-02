using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// Manages the settings menu UI, including button navigation, volume panel, and key rebinding.
/// </summary>
public partial class SettingsMenuManager : Control
{
	[Export] private Button 返回游戏按钮;
	[Export] private Button 音量按钮;
	[Export] private Button 返回主菜单按钮;
	[Export] private Button 游戏按键按钮;
	
	public bool 音量面板打开 { get; private set; } = false;

	private bool 正在播放音量动画 = false;
	private bool 激活中 = false;

	public override void _Ready()
	{
		Visible = false;
		初始化按钮();
	}

	private void 初始化按钮()
	{
		尝试自动查找按钮();
		设置垂直导航();
		连接按钮事件();
	}

	private void 尝试自动查找按钮()
	{
		var 动画容器 = GetNodeOrNull<Control>("动画容器");
		if (动画容器 != null)
		{
			if (返回游戏按钮 == null)
				返回游戏按钮 = 动画容器.GetNodeOrNull<Button>("返回游戏按钮");
			if (音量按钮 == null)
				音量按钮 = 动画容器.GetNodeOrNull<Button>("音量按钮");
			if (返回主菜单按钮 == null)
				返回主菜单按钮 = 动画容器.GetNodeOrNull<Button>("返回主菜单按钮");
			if (游戏按键按钮 == null)
				游戏按键按钮 = 动画容器.GetNodeOrNull<Button>("游戏按键按钮");
		}
		else
		{
			返回游戏按钮 = GetNodeOrNull<Button>("返回游戏按钮") ?? FindChild("返回游戏", true, false) as Button;
			音量按钮 = GetNodeOrNull<Button>("音量按钮") ?? FindChild("音量", true, false) as Button;
			返回主菜单按钮 = GetNodeOrNull<Button>("返回主菜单按钮") ?? FindChild("返回主菜单", true, false) as Button;
			游戏按键按钮 = GetNodeOrNull<Button>("游戏按键按钮") ?? FindChild("游戏按键", true, false) as Button;
		}
	}

	/// <summary>Opens the volume panel (placeholder for extracted logic).</summary>
	public void 打开音量面板()
	{
		if (音量面板打开) return;
		音量面板打开 = true;
	}

	/// <summary>Closes the volume panel with animation.</summary>
	public async Task 关闭音量面板()
	{
		if (!音量面板打开) return;
		if (正在播放音量动画) return;

		正在播放音量动画 = true;

		Node 父节点 = GetParent();
		Node 爷节点 = 父节点?.GetParent();
		Control 音量面板节点 = 爷节点?.GetNodeOrNull<Control>("VolumeControlPanel");
		if (音量面板节点 != null)
		{
			音量面板节点.Visible = false;
			音量面板节点.MouseFilter = Control.MouseFilterEnum.Ignore;
		}

		AnimationPlayer 音量动画器 = 音量按钮?.GetNodeOrNull<AnimationPlayer>("动画器");
		if (音量动画器 != null && 音量动画器.HasAnimation("音量界面关闭"))
		{
			音量动画器.Play("音量界面关闭");
			await ToSignal(音量动画器, AnimationPlayer.SignalName.AnimationFinished);
		}

		音量面板打开 = false;
		正在播放音量动画 = false;

		if (音量按钮 != null)
			音量按钮.GrabFocus();
	}

	private void 设置垂直导航()
	{
		Button[] 按钮顺序 = new Button[] { 返回主菜单按钮, 音量按钮, 游戏按键按钮, 返回游戏按钮 };
		
		for (int i = 0; i < 按钮顺序.Length; i++)
		{
			Button 当前 = 按钮顺序[i];
			if (当前 == null) continue;
			
			Button 上一个 = 按钮顺序[(i - 1 + 按钮顺序.Length) % 按钮顺序.Length];
			Button 下一个 = 按钮顺序[(i + 1) % 按钮顺序.Length];
			
			if (上一个 != null)
			{
				当前.FocusNeighborTop = 上一个.GetPath();
				当前.FocusNeighborRight = 上一个.GetPath();
			}
			
			if (下一个 != null)
			{
				当前.FocusNeighborBottom = 下一个.GetPath();
				当前.FocusNeighborLeft = 下一个.GetPath();
			}
		}
	}

	private void 连接按钮事件()
	{
		if (返回游戏按钮 != null)
		{
			返回游戏按钮.Pressed += () => EmitSignal(nameof(返回游戏请求));
		}

		if (音量按钮 != null)
		{
			音量按钮.Pressed += 当音量按钮按下_异步版本;
		}

		if (返回主菜单按钮 != null)
		{
			返回主菜单按钮.Pressed += () => EmitSignal(nameof(返回主菜单请求));
		}

		if (游戏按键按钮 != null)
		{
			游戏按键按钮.Pressed += () => EmitSignal(nameof(打开按键设置));
		}
	}

	private async void 当音量按钮按下_异步版本()
	{
		if (正在播放音量动画) return;
		正在播放音量动画 = true;
		音量按钮.Disabled = true;

		AnimationPlayer 音量动画器 = 音量按钮?.GetNodeOrNull<AnimationPlayer>("动画器");
		if (音量动画器 == null)
		{
			音量按钮.Disabled = false;
			正在播放音量动画 = false;
			EmitSignal(nameof(打开音量设置));
			return;
		}

		if (音量动画器.HasAnimation("点击音量后"))
		{
			音量动画器.Play("点击音量后");
			await ToSignal(音量动画器, AnimationPlayer.SignalName.AnimationFinished);
		}

		if (音量动画器.HasAnimation("音量界面打开"))
		{
			音量动画器.Play("音量界面打开");
			await ToSignal(音量动画器, AnimationPlayer.SignalName.AnimationFinished);
		}

		淡入VolumeControlPanel();
		激活();

		音量按钮.Disabled = false;
		正在播放音量动画 = false;

		EmitSignal(nameof(打开音量设置));
	}

	private void 淡入VolumeControlPanel()
	{
		Node 父节点 = GetParent();
		if (父节点 == null) return;

		Node 爷节点 = 父节点.GetParent();
		if (爷节点 == null) return;

		Control 音量面板 = 爷节点.GetNodeOrNull<Control>("VolumeControlPanel");
		if (音量面板 == null) return;

		音量面板打开 = true;

		音量面板.Modulate = Colors.Transparent;
		音量面板.MouseFilter = Control.MouseFilterEnum.Ignore;
		音量面板.Visible = true;

		Tween 渐显动画 = CreateTween();
		渐显动画.SetParallel(true);
		渐显动画.TweenProperty(音量面板, "modulate", Colors.White, 0.4f)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Sine);
		渐显动画.TweenProperty(音量面板, "scale", Vector2.One, 0.3f)
				.From(new Vector2(0.9f, 0.9f))
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
		渐显动画.TweenProperty(音量面板, "mouse_filter", (int)Control.MouseFilterEnum.Stop, 0.4f)
				.From((int)Control.MouseFilterEnum.Ignore);

		渐显动画.Play();

		渐显动画.Finished += () =>
		{
			if (音量面板 != null)
			{
				音量面板.MouseFilter = Control.MouseFilterEnum.Stop;
				if (音量面板 is VolumeControlPanel 音量控制脚本)
				{
					音量控制脚本.设置默认焦点();
				}
				else
				{
					var 滑块 = 音量面板.GetNodeOrNull<HSlider>("排列/背景音乐滑块");
					滑块?.GrabFocus();
				}
			}
		};
	}

	/// <summary>Activates the settings menu and focuses the "Game Keys" button.</summary>
	public void 激活()
	{
		if (激活中) return;

		激活中 = true;
		Visible = true;

		if (返回游戏按钮 != null) 返回游戏按钮.Visible = true;
		if (音量按钮 != null) 音量按钮.Visible = true;
		if (返回主菜单按钮 != null) 返回主菜单按钮.Visible = true;
		if (游戏按键按钮 != null)
		{
			游戏按键按钮.Visible = true;
			Callable.From(() => 
			{
				if (游戏按键按钮 != null)
					游戏按键按钮.GrabFocus();
			}).CallDeferred();
		}
	}
	
	/// <summary>Forces reactivation of the settings menu.</summary>
	public void 强制重新激活()
	{
		if (激活中)
		{
			激活中 = false;
			Visible = false;
		}
		激活();
	}

	/// <summary>Closes the settings menu.</summary>
	public void 关闭()
	{
		if (!激活中) return;
		if (音量面板打开)
			关闭音量面板();
		激活中 = false;
		Visible = false;
	}

	/// <summary>Returns whether the settings menu is active.</summary>
	public bool 是否激活中() => 激活中;

	[Signal] public delegate void 返回游戏请求EventHandler();
	[Signal] public delegate void 打开音量设置EventHandler();
	[Signal] public delegate void 返回主菜单请求EventHandler();
	[Signal] public delegate void 打开按键设置EventHandler();
}
