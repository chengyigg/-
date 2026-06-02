using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Interface for rebinding keyboard, mouse, and gamepad keys for game actions.
/// </summary>
public partial class KeyRebindInterface : Control
{
	// ---------- 动作定义 ----------
	private class 动作配置
	{
		public string 动作名中文;
		public string 动作名英文;
		public Godot.Collections.Array<InputEvent> 默认按键事件;
	}

	private List<动作配置> 所有动作 = new List<动作配置>();
	private Dictionary<string, PanelContainer> 动作行映射 = new Dictionary<string, PanelContainer>();
	private Dictionary<string, Label> 绑定键标签映射 = new Dictionary<string, Label>();

	private Control 当前选中的动作行;
	private int 当前选中索引 = -1;
	private Control 恢复默认按钮行;
	private bool 正在等待按键 = false;
	private string 待重绑定的动作名;

	[Export] private Panel 自定义弹窗;
	[Export] private Label 弹窗文字;
	[Export] private ScrollContainer 列表容器;
	[Export] private VBoxContainer 动作列表;

	private const string 配置文件路径 = "user://键位设置.cfg";
	private const string 配置节 = "Keybinds";

	private Dictionary<string, InputEvent> 默认键位预设 = new Dictionary<string, InputEvent>
	{
		{ "上", 创建按键事件(Key.Up) },
		{ "下", 创建按键事件(Key.Down) },
		{ "左", 创建按键事件(Key.Left) },
		{ "右", 创建按键事件(Key.Right) },
		{ "打开状态栏", 创建按键事件(Key.P) },
		{ "交互", 创建按键事件(Key.Z) },
	};

	public override void _Ready()
	{
		if (自定义弹窗 == null || 弹窗文字 == null || 列表容器 == null || 动作列表 == null) return;

		初始化动作列表();
		构建UI();
		加载保存的键位();
		刷新所有绑定显示();
		连接信号();
		设置焦点导航();
	}

	private void 初始化动作列表()
	{
		string[] 动作顺序 = { "上", "下", "左", "右", "打开状态栏", "交互" };
		foreach (string 动作中文 in 动作顺序)
		{
			string 动作英文 = 动作中文;
			var 默认事件 = new Godot.Collections.Array<InputEvent>();
			if (默认键位预设.ContainsKey(动作中文))
				默认事件.Add(默认键位预设[动作中文]);
			所有动作.Add(new 动作配置
			{
				动作名中文 = 动作中文,
				动作名英文 = 动作英文,
				默认按键事件 = 默认事件
			});
		}
	}

	private static InputEventKey 创建按键事件(Key 键码)
	{
		var ev = new InputEventKey();
		ev.Keycode = 键码;
		return ev;
	}

	private void 构建UI()
	{
		foreach (var child in 动作列表.GetChildren())
			child.QueueFree();

		const int 左列宽度 = 240;
		const int 右列宽度 = 200;
		const int 列间距 = 200;

		// 标题行
		var 标题行 = new PanelContainer();
		标题行.Name = "标题行";
		标题行.SizeFlagsHorizontal = SizeFlags.Expand;
		标题行.FocusMode = FocusModeEnum.None;
		var 标题样式 = new StyleBoxFlat();
		标题样式.BgColor = new Color(0.15f, 0.15f, 0.25f);
		标题样式.SetCornerRadiusAll(5);
		标题样式.SetBorderWidthAll(1);
		标题样式.BorderColor = new Color(0.3f, 0.3f, 0.5f);
		标题行.AddThemeStyleboxOverride("panel", 标题样式);

		var 标题HBox = new HBoxContainer();
		标题HBox.SizeFlagsHorizontal = SizeFlags.Expand;
		标题HBox.AddThemeConstantOverride("separation", 0);

		var 左标题格 = new PanelContainer();
		左标题格.CustomMinimumSize = new Vector2(左列宽度, 0);
		左标题格.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		左标题格.FocusMode = FocusModeEnum.None;
		var 左标题样式 = new StyleBoxFlat();
		左标题样式.BgColor = new Color(0.15f, 0.15f, 0.25f);
		左标题样式.SetCornerRadiusAll(5);
		左标题格.AddThemeStyleboxOverride("panel", 左标题样式);
		var 左标题 = new Label();
		左标题.Text = "动作名称";
		左标题.HorizontalAlignment = HorizontalAlignment.Center;
		左标题.AddThemeFontSizeOverride("font_size", 18);
		左标题.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.7f));
		左标题格.AddChild(左标题);

		var 间隔 = new Control();
		间隔.CustomMinimumSize = new Vector2(列间距, 0);
		间隔.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

		var 右标题格 = new PanelContainer();
		右标题格.CustomMinimumSize = new Vector2(右列宽度, 0);
		右标题格.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		右标题格.FocusMode = FocusModeEnum.None;
		var 右标题样式 = new StyleBoxFlat();
		右标题样式.BgColor = new Color(0.15f, 0.15f, 0.25f);
		右标题样式.SetCornerRadiusAll(5);
		右标题格.AddThemeStyleboxOverride("panel", 右标题样式);
		var 右标题 = new Label();
		右标题.Text = "按键绑定";
		右标题.HorizontalAlignment = HorizontalAlignment.Center;
		右标题.AddThemeFontSizeOverride("font_size", 18);
		右标题.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.7f));
		右标题格.AddChild(右标题);

		标题HBox.AddChild(左标题格);
		标题HBox.AddChild(间隔);
		标题HBox.AddChild(右标题格);
		标题行.AddChild(标题HBox);
		动作列表.AddChild(标题行);

		// 动作行
		for (int i = 0; i < 所有动作.Count; i++)
		{
			var 动作 = 所有动作[i];
			var 行容器 = new HBoxContainer();
			行容器.Name = $"行容器_{动作.动作名中文}";
			行容器.SizeFlagsHorizontal = SizeFlags.Expand;
			行容器.AddThemeConstantOverride("separation", 0);

			var 左侧格子 = new PanelContainer();
			左侧格子.CustomMinimumSize = new Vector2(左列宽度, 0);
			左侧格子.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
			左侧格子.FocusMode = FocusModeEnum.None;
			var 左侧样式 = new StyleBoxFlat();
			左侧样式.BgColor = new Color(0.2f, 0.2f, 0.3f);
			左侧样式.SetCornerRadiusAll(5);
			左侧样式.SetBorderWidth(Side.Bottom, 1);
			左侧样式.BorderColor = new Color(0.3f, 0.3f, 0.5f);
			左侧格子.AddThemeStyleboxOverride("panel", 左侧样式);

			var 动作名标签 = new Label();
			动作名标签.Text = 动作.动作名中文;
			动作名标签.HorizontalAlignment = HorizontalAlignment.Center;
			动作名标签.AddThemeFontSizeOverride("font_size", 20);
			动作名标签.FocusMode = FocusModeEnum.None;
			左侧格子.AddChild(动作名标签);

			var 间隔控件 = new Control();
			间隔控件.CustomMinimumSize = new Vector2(列间距, 0);
			间隔控件.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

			var 右侧格子 = new PanelContainer();
			右侧格子.Name = $"键位格子_{动作.动作名中文}";
			右侧格子.CustomMinimumSize = new Vector2(右列宽度, 0);
			右侧格子.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
			右侧格子.FocusMode = FocusModeEnum.All;

			var 右侧默认样式 = new StyleBoxFlat();
			右侧默认样式.BgColor = new Color(0.1f, 0.1f, 0.2f);
			右侧默认样式.SetCornerRadiusAll(6);
			右侧默认样式.SetBorderWidthAll(1);
			右侧默认样式.BorderColor = new Color(0.5f, 0.5f, 0.7f);
			右侧默认样式.ContentMarginLeft = 8;
			右侧默认样式.ContentMarginRight = 8;
			右侧默认样式.ContentMarginTop = 4;
			右侧默认样式.ContentMarginBottom = 4;

			var 右侧高亮样式 = (StyleBoxFlat)右侧默认样式.Duplicate();
			右侧高亮样式.BgColor = new Color(0.4f, 0.6f, 0.8f);
			右侧高亮样式.BorderColor = Colors.Yellow;
			右侧高亮样式.SetBorderWidthAll(2);

			右侧格子.AddThemeStyleboxOverride("panel", 右侧默认样式);

			int 当前索引 = i;
			右侧格子.FocusEntered += () => {
				右侧格子.AddThemeStyleboxOverride("panel", 右侧高亮样式);
				当前选中的动作行 = 右侧格子;
				当前选中索引 = 当前索引;
				UpdateScrollToSelected();
			};
			右侧格子.FocusExited += () => {
				右侧格子.AddThemeStyleboxOverride("panel", 右侧默认样式);
			};

			var 键位标签 = new Label();
			键位标签.Text = "未绑定";
			键位标签.HorizontalAlignment = HorizontalAlignment.Center;
			键位标签.AddThemeFontSizeOverride("font_size", 18);
			键位标签.AddThemeColorOverride("font_color", Colors.Yellow);
			键位标签.AddThemeConstantOverride("outline_size", 1);
			键位标签.FocusMode = FocusModeEnum.None;
			右侧格子.AddChild(键位标签);

			动作行映射[动作.动作名英文] = 右侧格子;
			绑定键标签映射[动作.动作名英文] = 键位标签;

			行容器.AddChild(左侧格子);
			行容器.AddChild(间隔控件);
			行容器.AddChild(右侧格子);
			动作列表.AddChild(行容器);
		}

		// 恢复默认按钮行
		var 按钮行容器 = new HBoxContainer();
		按钮行容器.SizeFlagsHorizontal = SizeFlags.Expand;
		按钮行容器.AddThemeConstantOverride("separation", 0);

		var 左侧占位 = new Control();
		左侧占位.CustomMinimumSize = new Vector2(左列宽度, 0);
		左侧占位.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		左侧占位.FocusMode = FocusModeEnum.None;

		var 间隔占位 = new Control();
		间隔占位.CustomMinimumSize = new Vector2(列间距, 0);
		间隔占位.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		间隔占位.FocusMode = FocusModeEnum.None;

		var 右侧按钮容器 = new PanelContainer();
		右侧按钮容器.CustomMinimumSize = new Vector2(右列宽度, 0);
		右侧按钮容器.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		右侧按钮容器.FocusMode = FocusModeEnum.None;
		var 透明样式 = new StyleBoxFlat();
		透明样式.BgColor = Colors.Transparent;
		右侧按钮容器.AddThemeStyleboxOverride("panel", 透明样式);

		var 按钮 = new Button();
		按钮.Text = "恢复默认";
		按钮.Alignment = HorizontalAlignment.Center;
		按钮.Flat = false;
		按钮.FocusMode = FocusModeEnum.All;
		按钮.Pressed += 恢复默认按键;

		var 按钮默认样式 = new StyleBoxFlat();
		按钮默认样式.BgColor = new Color(0.1f, 0.1f, 0.2f);
		按钮默认样式.SetCornerRadiusAll(6);
		按钮默认样式.SetBorderWidthAll(1);
		按钮默认样式.BorderColor = new Color(0.5f, 0.5f, 0.7f);
		按钮默认样式.ContentMarginLeft = 8;
		按钮默认样式.ContentMarginRight = 8;
		按钮默认样式.ContentMarginTop = 4;
		按钮默认样式.ContentMarginBottom = 4;
		按钮.AddThemeStyleboxOverride("normal", 按钮默认样式);

		var 按钮高亮样式 = (StyleBoxFlat)按钮默认样式.Duplicate();
		按钮高亮样式.BgColor = new Color(0.4f, 0.6f, 0.8f);
		按钮高亮样式.BorderColor = Colors.Yellow;
		按钮高亮样式.SetBorderWidthAll(2);
		按钮.AddThemeStyleboxOverride("focus", 按钮高亮样式);
		按钮.AddThemeStyleboxOverride("hover", 按钮默认样式);
		按钮.AddThemeStyleboxOverride("pressed", 按钮默认样式);
		按钮.AddThemeStyleboxOverride("disabled", 按钮默认样式);

		按钮.FocusEntered += () => {
			按钮.AddThemeStyleboxOverride("normal", 按钮高亮样式);
			当前选中的动作行 = 按钮;
			当前选中索引 = 所有动作.Count;
			UpdateScrollToSelected();
		};
		按钮.FocusExited += () => {
			按钮.AddThemeStyleboxOverride("normal", 按钮默认样式);
		};

		var 居中容器 = new CenterContainer();
		居中容器.SizeFlagsHorizontal = SizeFlags.Expand;
		居中容器.AddChild(按钮);
		右侧按钮容器.AddChild(居中容器);

		按钮行容器.AddChild(左侧占位);
		按钮行容器.AddChild(间隔占位);
		按钮行容器.AddChild(右侧按钮容器);
		动作列表.AddChild(按钮行容器);
		恢复默认按钮行 = 按钮;
	}

	private void 刷新所有绑定显示()
	{
		foreach (var 动作 in 所有动作)
		{
			var 事件列表 = InputMap.ActionGetEvents(动作.动作名英文);
			string 显示文本 = 获取按键显示文本(事件列表);
			if (绑定键标签映射.ContainsKey(动作.动作名英文))
				绑定键标签映射[动作.动作名英文].Text = 显示文本;
		}
	}

	private string 获取按键显示文本(Godot.Collections.Array<InputEvent> 事件列表)
	{
		if (事件列表.Count == 0) return "无";
		var 第一个事件 = 事件列表[0];
		if (第一个事件 is InputEventKey keyEvent)
			return OS.GetKeycodeString(keyEvent.Keycode);
		if (第一个事件 is InputEventMouseButton mouseEvent)
			return 获取鼠标按钮名称(mouseEvent.ButtonIndex);
		if (第一个事件 is InputEventJoypadButton joyEvent)
			return 获取手柄按钮名称((int)joyEvent.ButtonIndex);
		return "未知";
	}

	private string 获取鼠标按钮名称(MouseButton 按钮) => 按钮 switch
	{
		MouseButton.Left => "鼠标左键",
		MouseButton.Right => "鼠标右键",
		MouseButton.Middle => "鼠标中键",
		MouseButton.WheelUp => "滚轮上",
		MouseButton.WheelDown => "滚轮下",
		_ => $"鼠标{按钮}"
	};

	private string 获取手柄按钮名称(int 按钮索引)
	{
		var 映射 = new Dictionary<int, string>
		{
			{ 0, "手柄A" }, { 1, "手柄B" }, { 2, "手柄X" }, { 3, "手柄Y" },
			{ 4, "左肩键" }, { 5, "右肩键" }, { 6, "左扳机" }, { 7, "右扳机" },
			{ 8, "选择键" }, { 9, "开始键" }, { 10, "左摇杆按下" }, { 11, "右摇杆按下" },
			{ 12, "方向上" }, { 13, "方向下" }, { 14, "方向左" }, { 15, "方向右" }
		};
		return 映射.ContainsKey(按钮索引) ? 映射[按钮索引] : $"手柄按钮{按钮索引}";
	}

	private void 设置焦点导航()
	{
		var 所有控件 = new List<Control>();
		foreach (var 动作 in 所有动作)
			所有控件.Add(动作行映射[动作.动作名英文]);
		所有控件.Add(恢复默认按钮行);

		for (int i = 0; i < 所有控件.Count; i++)
		{
			var 当前 = 所有控件[i];
			var 上一个 = 所有控件[(i - 1 + 所有控件.Count) % 所有控件.Count];
			var 下一个 = 所有控件[(i + 1) % 所有控件.Count];
			当前.FocusNeighborTop = 上一个.GetPath();
			当前.FocusNeighborBottom = 下一个.GetPath();
			当前.FocusNeighborLeft = 上一个.GetPath();
			当前.FocusNeighborRight = 下一个.GetPath();
		}

		if (所有控件.Count > 0)
		{
			当前选中的动作行 = 所有控件[0];
			当前选中索引 = 0;
			当前选中的动作行.GrabFocus();
			UpdateScrollToSelected();
		}
	}

	private void UpdateScrollToSelected()
	{
		if (当前选中的动作行 != null)
			列表容器.EnsureControlVisible(当前选中的动作行);
	}

	private void 连接信号() { }

	public override void _Input(InputEvent @event)
	{
		if (正在等待按键)
		{
			if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.IsEcho())
			{
				绑定按键到当前动作(@event);
				关闭弹窗();
				GetViewport().SetInputAsHandled();
			}
			else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				绑定按键到当前动作(@event);
				关闭弹窗();
				GetViewport().SetInputAsHandled();
			}
			else if (@event is InputEventJoypadButton joyEvent && joyEvent.Pressed)
			{
				绑定按键到当前动作(@event);
				关闭弹窗();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (@event.IsActionPressed("确定") && !正在等待按键)
		{
			if (当前选中的动作行 != null && 当前选中的动作行 != 恢复默认按钮行)
			{
				开始等待按键();
				GetViewport().SetInputAsHandled();
			}
			else if (当前选中的动作行 == 恢复默认按钮行)
			{
				恢复默认按键();
				GetViewport().SetInputAsHandled();
			}
		}
		else if (@event.IsActionPressed("取消") && !正在等待按键)
		{
			关闭界面();
			GetViewport().SetInputAsHandled();
		}
	}

	private void 开始等待按键()
	{
		if (当前选中的动作行 == null) return;
		string 动作英文 = null;
		foreach (var kv in 动作行映射)
		{
			if (kv.Value == 当前选中的动作行)
			{
				动作英文 = kv.Key;
				break;
			}
		}
		if (string.IsNullOrEmpty(动作英文)) return;

		待重绑定的动作名 = 动作英文;
		正在等待按键 = true;

		弹窗文字.Text = $"请按下新的按键（用于“{所有动作.Find(a=>a.动作名英文==动作英文)?.动作名中文}”）";
		自定义弹窗.Visible = true;
		自定义弹窗.MouseFilter = MouseFilterEnum.Stop;
	}

	private void 关闭弹窗()
	{
		自定义弹窗.Visible = false;
		正在等待按键 = false;
		if (当前选中的动作行 != null)
			当前选中的动作行.GrabFocus();
	}

	private void 绑定按键到当前动作(InputEvent 新事件)
	{
		if (string.IsNullOrEmpty(待重绑定的动作名)) return;

		foreach (var 其他动作 in 所有动作)
		{
			if (其他动作.动作名英文 == 待重绑定的动作名) continue;
			var 现有事件 = InputMap.ActionGetEvents(其他动作.动作名英文);
			if (现有事件.Count > 0 && 事件相等(现有事件[0], 新事件))
			{
				InputMap.ActionEraseEvents(其他动作.动作名英文);
			}
		}

		InputMap.ActionEraseEvents(待重绑定的动作名);
		InputMap.ActionAddEvent(待重绑定的动作名, 新事件);

		刷新所有绑定显示();
		保存键位配置();
		待重绑定的动作名 = null;
	}

	private bool 事件相等(InputEvent a, InputEvent b)
	{
		if (a is InputEventKey keyA && b is InputEventKey keyB)
			return keyA.Keycode == keyB.Keycode;
		if (a is InputEventMouseButton mouseA && b is InputEventMouseButton mouseB)
			return mouseA.ButtonIndex == mouseB.ButtonIndex;
		if (a is InputEventJoypadButton joyA && b is InputEventJoypadButton joyB)
			return joyA.ButtonIndex == joyB.ButtonIndex;
		return false;
	}

	private void 恢复默认按键()
	{
		foreach (var 动作 in 所有动作)
		{
			InputMap.ActionEraseEvents(动作.动作名英文);
			if (动作.默认按键事件.Count > 0)
				InputMap.ActionAddEvent(动作.动作名英文, 动作.默认按键事件[0]);
		}
		刷新所有绑定显示();
		保存键位配置();
	}

	private void 保存键位配置()
	{
		var config = new ConfigFile();
		foreach (var 动作 in 所有动作)
		{
			var 事件列表 = InputMap.ActionGetEvents(动作.动作名英文);
			if (事件列表.Count > 0)
			{
				string 存储字符串 = 序列化输入事件(事件列表[0]);
				config.SetValue(配置节, 动作.动作名英文, 存储字符串);
			}
		}
		Error err = config.Save(配置文件路径);
	}

	private void 加载保存的键位()
	{
		var config = new ConfigFile();
		if (config.Load(配置文件路径) != Error.Ok) return;

		foreach (var 动作 in 所有动作)
		{
			string 存储字符串 = (string)config.GetValue(配置节, 动作.动作名英文, "");
			if (!string.IsNullOrEmpty(存储字符串))
			{
				var 事件 = 反序列化输入事件(存储字符串);
				if (事件 != null)
				{
					InputMap.ActionEraseEvents(动作.动作名英文);
					InputMap.ActionAddEvent(动作.动作名英文, 事件);
				}
			}
		}
	}

	private string 序列化输入事件(InputEvent 事件)
	{
		if (事件 is InputEventKey key) return $"Key:{key.Keycode}";
		if (事件 is InputEventMouseButton mouse) return $"Mouse:{mouse.ButtonIndex}";
		if (事件 is InputEventJoypadButton joy) return $"Joy:{(int)joy.ButtonIndex}";
		return "";
	}

	private InputEvent 反序列化输入事件(string 字符串)
	{
		var parts = 字符串.Split(':');
		if (parts.Length != 2) return null;
		string type = parts[0];
		string value = parts[1];
		if (type == "Key" && Enum.TryParse<Key>(value, out Key key))
			return 创建按键事件(key);
		if (type == "Mouse" && Enum.TryParse<MouseButton>(value, out MouseButton mouse))
		{
			var ev = new InputEventMouseButton();
			ev.ButtonIndex = mouse;
			return ev;
		}
		if (type == "Joy" && int.TryParse(value, out int joyIdx))
		{
			var ev = new InputEventJoypadButton();
			ev.ButtonIndex = (JoyButton)joyIdx;
			return ev;
		}
		return null;
	}

	private void 关闭界面()
	{
		正在等待按键 = false;
		if (自定义弹窗 != null && 自定义弹窗.Visible)
			自定义弹窗.Hide();
		QueueFree();
	}

	public override void _ExitTree()
	{
		保存键位配置();
	}
}
