using Godot;
using 你的项目.Scripts.资源;
using 你的项目.Scripts.管理器;

namespace 你的项目.Scripts.UI
{
	/// <summary>
	/// UI for selecting a save slot (save or load mode).
	/// </summary>
	public partial class SaveSelectUI : CanvasLayer
	{
		[Signal] public delegate void 存档选中EventHandler(int 存档位);
		[Signal] public delegate void 界面关闭EventHandler();

		// 节点引用
		private Control _视口容器;
		private HBoxContainer _图片容器;
		private Label _详细信息标签;
		private Button _确认按钮;
		private Button _取消按钮;

		private int _当前选中存档位 = -1;
		private bool _是存档模式 = false;

		private const int ITEM_WIDTH = 120;
		private const int ITEM_HEIGHT = 140;
		private const int COLOR_RECT_SIZE = 100;
		private const float SELECTED_SCALE = 1.2f;
		private const float NORMAL_SCALE = 0.8f;
		private const float SLIDE_DURATION = 0.3f;

		private int _总项数 = 0;
		private Tween _滑动Tween;
		private ButtonGroup _按钮组;
		private bool _焦点在按钮区域 = false;

		[Export] private Font 自定义字体;

		[ExportGroup("整体缩放")]
		[Export] private float uiScale = 1.0f;

		[ExportGroup("布局")]
		[Export] private float _项间距 = 10f;

		public override void _Ready()
		{
			_视口容器 = GetNode<Control>("ColorRect/视口容器");
			_图片容器 = GetNode<HBoxContainer>("ColorRect/视口容器/图片容器");
			_详细信息标签 = GetNode<Label>("ColorRect/详细信息标签");
			_确认按钮 = GetNode<Button>("ColorRect/确认按钮");
			_取消按钮 = GetNode<Button>("ColorRect/取消按钮");

			_确认按钮.Pressed += 确认按钮按下;
			_取消按钮.Pressed += 取消按钮点击;

			Hide();
		}

		private void 更新容器间距()
		{
			if (_图片容器 != null)
			{
				_图片容器.AddThemeConstantOverride("separation", Mathf.RoundToInt(_项间距));
			}
		}

		/// <summary>Opens the save/load interface.</summary>
		public void 打开界面(bool 是存档模式 = false)
		{
			_是存档模式 = 是存档模式;
			_当前选中存档位 = -1;
			_确认按钮.Text = _是存档模式 ? "保存" : "加载";
			刷新存档位显示();
			Show();
			ProcessMode = ProcessModeEnum.Always;

			_焦点在按钮区域 = false;
			GetViewport().GuiReleaseFocus();
		}

		private async void 刷新存档位显示()
		{
			foreach (Node child in _图片容器.GetChildren())
				child.QueueFree();
			await ToSignal(GetTree(), "process_frame");

			更新容器间距();
			_按钮组 = new ButtonGroup();

			var 所有存档数据 = SaveManager.实例.获取所有存档数据();
			_总项数 = 所有存档数据.Length;

			for (int i = 0; i < _总项数; i++)
			{
				var 图片项 = 创建图片项(i, 所有存档数据[i]);
				_图片容器.AddChild(图片项);
			}

			if (_总项数 > 0)
			{
				_当前选中存档位 = 0;
				_图片容器.Position = new Vector2(计算容器偏移(0), _图片容器.Position.Y);
				更新缩放状态();
				更新详细信息();
				var 第一个按钮 = _图片容器.GetChild<Button>(0);
				第一个按钮.ButtonPressed = true;
				GetViewport().GuiReleaseFocus();
			}
		}

		private Button 创建图片项(int 存档位索引, SaveDataResource 存档数据)
		{
			var 图片按钮 = new Button();
			图片按钮.Name = $"图片项{存档位索引}";
			图片按钮.ToggleMode = true;
			图片按钮.ButtonGroup = _按钮组;
			图片按钮.FocusMode = Control.FocusModeEnum.None;

			图片按钮.Flat = true;
			图片按钮.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
			图片按钮.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
			图片按钮.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
			图片按钮.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			图片按钮.AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());
			图片按钮.MouseFilter = Control.MouseFilterEnum.Stop;

			图片按钮.CustomMinimumSize = new Vector2(ITEM_WIDTH * uiScale, ITEM_HEIGHT * uiScale);
			图片按钮.Size = new Vector2(ITEM_WIDTH * uiScale, ITEM_HEIGHT * uiScale);
			图片按钮.Scale = Vector2.One;
			图片按钮.PivotOffset = new Vector2(0.5f, 0.5f);

			var vbox = new VBoxContainer();
			vbox.Name = "VBox";
			vbox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			vbox.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			vbox.Alignment = BoxContainer.AlignmentMode.Center;
			vbox.MouseFilter = Control.MouseFilterEnum.Ignore;

			float glowSize = (COLOR_RECT_SIZE + 10) * uiScale;
			float boxSize = COLOR_RECT_SIZE * uiScale;

			var 重叠容器 = new Control();
			重叠容器.Name = "Overlap";
			重叠容器.Size = new Vector2(glowSize, glowSize);
			重叠容器.MouseFilter = Control.MouseFilterEnum.Ignore;
			重叠容器.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);

			var 外发光 = new ColorRect();
			外发光.Name = "Glow";
			外发光.Size = new Vector2(glowSize, glowSize);
			外发光.Color = new Color(Colors.Yellow, 0f);
			外发光.MouseFilter = Control.MouseFilterEnum.Ignore;
			外发光.Position = Vector2.Zero;
			重叠容器.AddChild(外发光);

			var 方块 = new ColorRect();
			方块.Name = "方块";
			方块.Size = new Vector2(boxSize, boxSize);
			方块.Color = (存档数据 != null && 存档数据.是否有存档()) ? Colors.Green : Colors.Gray;
			方块.MouseFilter = Control.MouseFilterEnum.Ignore;
			方块.Position = new Vector2((glowSize - boxSize) / 2, (glowSize - boxSize) / 2);
			重叠容器.AddChild(方块);

			vbox.AddChild(重叠容器);

			var 简短标签 = new Label();
			简短标签.Name = "简短标签";
			简短标签.Text = 存档数据 != null && 存档数据.是否有存档() ? $"存档{存档位索引 + 1}" : $"空存档{存档位索引 + 1}";
			简短标签.HorizontalAlignment = HorizontalAlignment.Center;
			简短标签.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			简短标签.MouseFilter = Control.MouseFilterEnum.Ignore;

			if (自定义字体 != null)
			{
				简短标签.AddThemeFontOverride("font", 自定义字体);
			}

			vbox.AddChild(简短标签);

			图片按钮.AddChild(vbox);

			图片按钮.SetMeta("slot_index", 存档位索引);
			图片按钮.SetMeta("slot_data", 存档数据);

			图片按钮.Toggled += (bool 按下) =>
			{
				if (按下)
				{
					int 索引 = (int)图片按钮.GetMeta("slot_index");
					_当前选中存档位 = 存档位索引;
					滑动到索引(_当前选中存档位);
				}
			};

			return 图片按钮;
		}

		private float 计算容器偏移(int index)
		{
			float 视口中心X = _视口容器.Size.X / 2;
			float 项左边界 = index * (ITEM_WIDTH * uiScale + _项间距);
			float 项中心X = 项左边界 + (ITEM_WIDTH * uiScale) / 2;
			return 视口中心X - 项中心X;
		}

		private void 滑动到索引(int index)
		{
			if (index < 0 || index >= _总项数) return;

			float 目标X = 计算容器偏移(index);

			if (_滑动Tween != null && _滑动Tween.IsRunning())
				_滑动Tween.Kill();

			_滑动Tween = CreateTween();
			_滑动Tween.SetParallel(true);

			_滑动Tween.TweenProperty(_图片容器, "position:x", 目标X, SLIDE_DURATION)
					.SetEase(Tween.EaseType.Out)
					.SetTrans(Tween.TransitionType.Quint);

			for (int i = 0; i < _图片容器.GetChildCount(); i++)
			{
				var 图片按钮 = _图片容器.GetChild<Button>(i);
				float targetScale = (i == index) ? SELECTED_SCALE : NORMAL_SCALE;
				_滑动Tween.TweenProperty(图片按钮, "scale", new Vector2(targetScale, targetScale), SLIDE_DURATION)
						.SetEase(Tween.EaseType.Out)
						.SetTrans(Tween.TransitionType.Quad);

				var 外发光 = 图片按钮.GetNodeOrNull<ColorRect>("VBox/Overlap/Glow");
				if (外发光 != null)
				{
					Color targetColor = (i == index) 
						? new Color(Colors.Yellow, 0.5f)
						: new Color(Colors.Yellow, 0f);
					_滑动Tween.TweenProperty(外发光, "color", targetColor, SLIDE_DURATION)
							.SetEase(Tween.EaseType.Out)
							.SetTrans(Tween.TransitionType.Quad);
				}
			}

			_滑动Tween.SetParallel(false);
			_滑动Tween.TweenCallback(Callable.From(() =>
			{
				var 目标按钮 = _图片容器.GetChild<Button>(index);
				目标按钮.ButtonPressed = true;
				if (!_焦点在按钮区域)
				{
					GetViewport().GuiReleaseFocus();
				}
			}));

			更新详细信息();
			_滑动Tween.Play();
		}

		private void 更新缩放状态()
		{
			for (int i = 0; i < _图片容器.GetChildCount(); i++)
			{
				var 图片按钮 = _图片容器.GetChild<Button>(i);
				float targetScale = (i == _当前选中存档位) ? SELECTED_SCALE : NORMAL_SCALE;
				图片按钮.Scale = new Vector2(targetScale, targetScale);
			}
		}

		private void 更新详细信息()
		{
			if (_详细信息标签 == null) return;
			_详细信息标签.Visible = true;
			_详细信息标签.Modulate = Colors.White;

			if (_当前选中存档位 < 0 || _当前选中存档位 >= SaveManager.实例.获取所有存档数据().Length)
			{
				_详细信息标签.Text = "请选择一个存档";
				return;
			}

			var 存档数据 = SaveManager.实例.获取存档数据(_当前选中存档位);
			if (存档数据 != null && 存档数据.是否有存档())
			{
				string 文本 = $"玩家: {存档数据.玩家名字}\n" +
							  $"已存档场景: {存档数据.场景存档位置.Count}\n" +
							  $"时间: {存档数据.获取格式化时间()}";
				_详细信息标签.Text = 文本;
			}
			else
			{
				_详细信息标签.Text = "空存档位，无存档数据";
			}
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (!Visible) return;

			if (@event.IsActionPressed("ui_left"))
			{
				if (!_焦点在按钮区域)
				{
					切换上一个();
					GetViewport().SetInputAsHandled();
				}
			}
			else if (@event.IsActionPressed("ui_right"))
			{
				if (!_焦点在按钮区域)
				{
					切换下一个();
					GetViewport().SetInputAsHandled();
				}
			}
			else if (@event.IsActionPressed("ui_accept"))
			{
				if (!_焦点在按钮区域)
				{
					_焦点在按钮区域 = true;
					_确认按钮.GrabFocus();
					GetViewport().SetInputAsHandled();
				}
			}
			else if (@event.IsActionPressed("ui_cancel"))
			{
				if (_焦点在按钮区域)
				{
					_焦点在按钮区域 = false;
					GetViewport().GuiReleaseFocus();
					GetViewport().SetInputAsHandled();
				}
				else
				{
					关闭界面();
					GetViewport().SetInputAsHandled();
				}
			}
		}

		private void 切换上一个()
		{
			if (_当前选中存档位 > 0)
			{
				var 目标索引 = _当前选中存档位 - 1;
				var 目标按钮 = _图片容器.GetChild<Button>(目标索引);
				目标按钮.ButtonPressed = true;
			}
		}

		private void 切换下一个()
		{
			if (_当前选中存档位 < _总项数 - 1)
			{
				var 目标索引 = _当前选中存档位 + 1;
				var 目标按钮 = _图片容器.GetChild<Button>(目标索引);
				目标按钮.ButtonPressed = true;
			}
		}

		private void 确认按钮按下()
		{
			if (_当前选中存档位 == -1) return;
			EmitSignal(nameof(存档选中), _当前选中存档位);
			Hide();
		}

		private void 取消按钮点击()
		{
			_焦点在按钮区域 = false;
			GetViewport().GuiReleaseFocus();
		}

		private void 关闭界面()
		{
			EmitSignal(nameof(界面关闭));
			Hide();
		}

		public new void Show()
		{
			base.Show();
			ProcessMode = ProcessModeEnum.Always;
		}

		public new void Hide()
		{
			base.Hide();
			ProcessMode = ProcessModeEnum.WhenPaused;
		}
	}
}
