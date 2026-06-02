using Godot;
using System;
using System.Collections.Generic;
using 你的项目.Scripts.资源;

public partial class DialogueUI : CanvasLayer
{
	public event Action 请求下一句;
	public event Action<int> 选项被选择;
	private bool _保持历史 = false;
	
	private 对话回溯面板 _当前回溯面板 = null;
	private bool _回溯面板是否打开 = false;
	[Export] public float 默认打字速度 = 0.05f;  // 单位：秒/字符
	[Export] private Panel 对话面板;
	[Export] private Label 说话人标签;
	[Export] private Panel 头像边框;
	[Export] private TextureRect 头像纹理;
	[Export] private Label 对话文本标签;
	[Export] private VBoxContainer 选项容器;
	[Export] private StyleBoxFlat 面板样式;
	[Export] private StyleBoxFlat 按钮正常样式;
	[Export] private StyleBoxFlat 按钮悬停样式;
	[Export] private StyleBoxFlat 按钮已选样式;
	[Export] private Button 历史按钮;
	private bool _等待空格继续片段 = false;
	private Tween _缩放动画;
	private bool _动画播放中 = false;
	private bool _已播放伸展动画 = false;
	private (string 文本, string 说话人, Texture2D 头像, float? 速度)? _待显示句子 = null;
	
	private List<string> _片段列表 = null;
	private int _当前片段索引 = -1;
	private string _当前片段文本 = "";
	private int _当前片段字符索引 = 0;
	private bool _分段追加模式 = false;
	private const string _分隔符 = "/";
	
	private string _完整文本 = "";
	private int _当前字符索引 = 0;
	private Timer _打字计时器;
	private bool _正在打字 = false;
	private float _打字速度 = 0.05f;
	
	private List<BranchOption> _当前选项列表;
	private HashSet<int> _已选选项索引;

	public override void _Ready()
	{
		Layer = 2000; 
		_打字计时器 = new Timer();
		AddChild(_打字计时器);
		_打字计时器.Timeout += On打字计时器超时;
		_打字计时器.OneShot = true;

		对话面板.Visible = false;
		选项容器.Visible = false;
		应用样式();
		if (历史按钮 != null)
			历史按钮.Pressed += On历史按钮按下;
		
		if (对话面板 != null)
		{
			对话面板.Resized += On对话面板调整大小;
			CallDeferred(nameof(更新对话面板中心点));
		}
	}
	
	private void 更新对话面板中心点()
	{
		if (对话面板 != null && 对话面板.Size != Vector2.Zero)
			对话面板.PivotOffset = 对话面板.Size * 0.5f;
	}
	
	private void On对话面板调整大小() => 更新对话面板中心点();
	
	private void 播放伸展动画()
	{
		if (对话面板 == null) return;
		if (_已播放伸展动画) return;
		
		if (_缩放动画 != null && _缩放动画.IsValid())
			_缩放动画.Kill();
		if (对话面板.Size == Vector2.Zero)
		{
			CallDeferred(nameof(播放伸展动画));
			return;
		}
		
		_动画播放中 = true;
		_已播放伸展动画 = true;
		对话面板.PivotOffset = 对话面板.Size * 0.5f;
		float 目标Y缩放 = 1.0f;
		float 初始Y缩放 = 0.5f;
		对话面板.Scale = new Vector2(对话面板.Scale.X, 初始Y缩放);
		
		_缩放动画 = CreateTween();
		_缩放动画.TweenProperty(对话面板, "scale:y", 目标Y缩放, 0.4f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		
		_缩放动画.Finished += () => {
			_缩放动画 = null;
			_动画播放中 = false;
			if (_待显示句子 != null)
			{
				var (文本, 说话人, 头像, 速度) = _待显示句子.Value;
				_待显示句子 = null;
				直接显示句子(文本, 说话人, 头像, 速度);
			}
		};
	}
	
	public void 显示对话面板()
	{
		对话文本标签.Text = "";
		说话人标签.Text = "";
		if (头像纹理 != null)
			头像纹理.Texture = null;
		
		if (!对话面板.Visible)
		{
			_已播放伸展动画 = false;
			_动画播放中 = false;
			_待显示句子 = null;
			播放伸展动画();
		}
		对话面板.Visible = true;
		选项容器.Visible = false;
		if (历史按钮 != null)
			历史按钮.Visible = true;
	}

	public void 隐藏对话面板()
	{
		if (_缩放动画 != null && _缩放动画.IsValid())
			_缩放动画.Kill();
		if (对话面板 != null)
			对话面板.Scale = Vector2.One;
		_动画播放中 = false;
		_待显示句子 = null;
		_已播放伸展动画 = false;
		对话面板.Visible = false;
		选项容器.Visible = false;
		if (历史按钮 != null)
			历史按钮.Visible = false;
		if (_正在打字) _打字计时器.Stop();
		对话文本标签.Text = "";
		说话人标签.Text = "";
		if (头像纹理 != null)
			头像纹理.Texture = null;
	}
	
	public void 显示句子(string 文本, string 说话人, Texture2D 头像, float? 自定义速度 = null)
	{
		if (_动画播放中)
		{
			_待显示句子 = (文本, 说话人, 头像, 自定义速度);
			return;
		}
		直接显示句子(文本, 说话人, 头像, 自定义速度);
	}
	
	 private void 直接显示句子(string 文本, string 说话人, Texture2D 头像, float? 自定义速度 = null)
	{
		说话人标签.Text = 说话人;
		说话人标签.Visible = !string.IsNullOrEmpty(说话人);
		头像边框.Visible = 头像 != null;
		if (头像 != null)
			头像纹理.Texture = 头像;

		if (_正在打字) _打字计时器.Stop();
		_正在打字 = false;
		_等待空格继续片段 = false;

		if (文本.Contains(_分隔符))
		{
			_分段追加模式 = true;
			_片段列表 = new List<string>(文本.Split(_分隔符, StringSplitOptions.RemoveEmptyEntries));
			_当前片段索引 = -1;
			对话文本标签.Text = "";
			开始下一片段();
		}
		else
		{
			_分段追加模式 = false;
			_完整文本 = 文本;
			_当前字符索引 = 0;
			对话文本标签.Text = "";
			_正在打字 = true;
			_打字速度 = 自定义速度 ?? 默认打字速度;
			_打字计时器.Start(_打字速度);
		}
	}
	
	private void 开始下一片段()
	{
		if (!_分段追加模式 || _片段列表 == null) return;
		_当前片段索引++;
		if (_当前片段索引 < _片段列表.Count)
		{
			_当前片段文本 = _片段列表[_当前片段索引];
			_当前片段字符索引 = 0;
			_正在打字 = true;
			_打字计时器.Start(_打字速度);
		}
		else
		{
			_分段追加模式 = false;
			_片段列表 = null;
			_当前片段索引 = -1;
			_正在打字 = false;
		}
	}
	
	private void On打字计时器超时()
	{
		if (!_正在打字) return;

		if (_分段追加模式)
		{
			if (_当前片段字符索引 < _当前片段文本.Length)
			{
				对话文本标签.Text += _当前片段文本[_当前片段字符索引];
				_当前片段字符索引++;
				_打字计时器.Start(_打字速度);
			}
			else
			{
				_正在打字 = false;
				if (_当前片段索引 + 1 < _片段列表.Count)
					_等待空格继续片段 = true;
				else
				{
					_等待空格继续片段 = false;
					_分段追加模式 = false;
				}
			}
		}
		else
		{
			if (_当前字符索引 < _完整文本.Length)
			{
				对话文本标签.Text += _完整文本[_当前字符索引];
				_当前字符索引++;
				_打字计时器.Start(_打字速度);
			}
			else
				_正在打字 = false;
		}
	}
	
	public void 显示选项(List<BranchOption> 选项列表, Func<string, string> 文本替换回调, HashSet<int> 已选选项索引 = null)
	{
		_当前选项列表 = 选项列表;
		_已选选项索引 = 已选选项索引 ?? new HashSet<int>();
		
		foreach (Node child in 选项容器.GetChildren())
			child.QueueFree();

		Button 第一个按钮 = null;
		for (int i = 0; i < 选项列表.Count; i++)
		{
			var 选项 = 选项列表[i];
			Button 按钮 = new Button();
			string 显示文本 = 文本替换回调?.Invoke(选项.选项文本) ?? 选项.选项文本;
			按钮.Text = 显示文本;
			按钮.FocusMode = Control.FocusModeEnum.All;
			bool 已选过 = _已选选项索引.Contains(i);
			应用按钮样式(按钮, 已选过);
			int 索引 = i;
			按钮.Pressed += () => 选项被选择?.Invoke(索引);
			选项容器.AddChild(按钮);
			if (第一个按钮 == null) 第一个按钮 = 按钮;
		}
		选项容器.Visible = true;
		if (第一个按钮 != null)
			CallDeferred(nameof(延迟聚焦), 第一个按钮);
	}
	
	private void 应用按钮样式(Button 按钮, bool 已选过)
	{
		if (已选过 && 按钮已选样式 != null)
		{
			按钮.AddThemeStyleboxOverride("normal", 按钮已选样式);
			按钮.Disabled = true;
		}
		else
		{
			if (按钮正常样式 != null)
				按钮.AddThemeStyleboxOverride("normal", 按钮正常样式);
			if (按钮悬停样式 != null)
				按钮.AddThemeStyleboxOverride("hover", 按钮悬停样式);
			按钮.Disabled = false;
		}
	}
	
	private void 延迟聚焦(Control 控件)
	{
		if (IsInstanceValid(控件)) 控件.GrabFocus();
	}
	
	public bool 是否正在打字中() => _正在打字;
	
public override void _Input(InputEvent @event)
{
	if (!对话面板.Visible) return;
	if (_动画播放中) return;
	
	if (选项容器.Visible && GetViewport().GuiGetFocusOwner() is Button) return;

	bool 触发 = @event.IsActionPressed("ui_accept") ||
				(@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed);
	if (!触发) return;

	if (_正在打字) return;

	if (_分段追加模式 && _等待空格继续片段)
	{
		开始下一片段();
		_等待空格继续片段 = false;
		return;
	}

	if (!_正在打字 && !_等待空格继续片段)
	{
		if (_分段追加模式)
		{
			_分段追加模式 = false;
			_片段列表 = null;
		}
		请求下一句?.Invoke();
	}
}
	
	private void 应用样式()
	{
		if (面板样式 != null)
			对话面板.AddThemeStyleboxOverride("panel", 面板样式);
	}
	
	private void On历史按钮按下()
	{
		if (_当前回溯面板 != null && IsInstanceValid(_当前回溯面板))
		{
			_当前回溯面板.关闭();
			_当前回溯面板 = null;
			return;
		}

		var 历史数据 = DialoguePlayer.实例?.获取历史记录();
		if (历史数据 == null || 历史数据.Count == 0) return;

		var 面板场景 = GD.Load<PackedScene>("res://对话系统/场景/对话回溯面板.tscn");
		var 面板 = 面板场景.Instantiate<对话回溯面板>();
		面板.面板关闭 += () => _当前回溯面板 = null;
		GetTree().CurrentScene.AddChild(面板);
		面板.设置历史数据(历史数据);
		_当前回溯面板 = 面板;
	}
}
