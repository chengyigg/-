using Godot;

public partial class CardInteractionComponent : Control
{
	[Signal] public delegate void 鼠标进入EventHandler();
	[Signal] public delegate void 鼠标离开EventHandler();
	[Signal] public delegate void 卡牌被点击EventHandler(GodotObject 卡牌);
	[Signal] public delegate void 请求开始拖拽EventHandler();
	[Signal] public delegate void 请求结束拖拽EventHandler(Vector2 鼠标位置, bool 应该使用);
	[Signal] public delegate void 请求开始预览EventHandler();
	[Signal] public delegate void 请求结束预览EventHandler();
	[Signal] public delegate void 可使用状态改变EventHandler(bool 可使用);
	[Signal] public delegate void 查看卡牌RequestEventHandler();

	private CardUI _宿主;
	private CardAppearanceComponent _外观;
	private float _可使用高度阈值;
	private float _长按时间阈值;
	private float _拖动敏感度阈值;

	public bool 正在拖拽 { get; private set; }
	public bool 正在查看 { get; set; }
	private bool 正在长按;
	public bool 正在预览 { get; set; }
	private float 长按计时器;
	private Vector2 上次鼠标位置;
	private Vector2 拖拽偏移;
	private bool 当前可使用;
	private bool 正在使用中;
	private bool _禁用战斗交互;  // 只禁用战斗交互（拖拽/长按），不禁用悬停

	public void 初始化(CardUI 宿主, CardAppearanceComponent 外观, float 高度阈值, float 长按阈值, float 拖动阈值)
	{
		_宿主 = 宿主;
		_外观 = 外观;
		_可使用高度阈值 = 高度阈值;
		_长按时间阈值 = 长按阈值;
		_拖动敏感度阈值 = 拖动阈值;

		_宿主.MouseEntered += () => 处理鼠标进入();
		_宿主.MouseExited += () => 处理鼠标离开();
		_宿主.GuiInput += (输入事件) => 处理输入(输入事件);
	}

	public override void _Process(double delta)
	{
		if (_禁用战斗交互 || 正在使用中) return;
		if (!正在长按 || 正在拖拽 || 正在预览) return;
		长按计时器 += (float)delta;
		if (长按计时器 >= _长按时间阈值)
		{
			开始预览();
		}
	}

	private void 处理输入(InputEvent 事件)
	{
		if (正在使用中) return;

		// 查看模式：只处理左键点击（选中），不处理任何战斗输入
		if (_宿主.当前模式 == 卡牌模式.查看)
		{
			if (事件 is InputEventMouseButton 鼠标事件 && 鼠标事件.Pressed && 鼠标事件.ButtonIndex == MouseButton.Left)
			{
				EmitSignal(SignalName.卡牌被点击, (GodotObject)_宿主);
				AcceptEvent();
			}
			return;
		}

		// 战斗模式且禁用了战斗交互则完全跳过
		if (_禁用战斗交互) return;

		// 战斗模式原有逻辑
		if (事件 is InputEventMouseButton 战斗鼠标事件)
		{
			if (战斗鼠标事件.ButtonIndex == MouseButton.Left)
			{
				if (战斗鼠标事件.Pressed)
				{
					正在长按 = true;
					正在预览 = false;
					长按计时器 = 0f;
					上次鼠标位置 = _宿主.GetGlobalMousePosition();
					var 父容器 = _宿主.GetParent() as Control;
					if (父容器 != null)
						拖拽偏移 = (父容器.GetLocalMousePosition() - _宿主.Position) * _宿主.Scale;
				}
				else
				{
					if (正在预览 && !正在拖拽)
						结束预览();
					else if (正在拖拽)
					{
						var 父容器 = _宿主.GetParent() as Control;
						var 鼠标位置 = 父容器 != null ? 父容器.GetLocalMousePosition() : _宿主.GetLocalMousePosition();
						bool 应该使用 = 当前可使用 && _宿主.GlobalPosition.Y < _可使用高度阈值;
						EmitSignal(SignalName.请求结束拖拽, 鼠标位置, 应该使用);
					}
					正在长按 = false;
					正在预览 = false;
				}
			}
			else if (战斗鼠标事件.ButtonIndex == MouseButton.Right && 战斗鼠标事件.Pressed)
			{
				EmitSignal(SignalName.查看卡牌Request);
			}
		}
		else if (事件 is InputEventMouseMotion 移动事件)
		{
			if (正在拖拽)
			{
				var 父容器 = _宿主.GetParent() as Control;
				if (父容器 != null)
				{
					var 鼠标位置 = 父容器.GetLocalMousePosition();
					_宿主.Position = 鼠标位置 - (拖拽偏移 / _宿主.Scale);
					检测可使用状态();
				}
			}
			else if (正在长按 || 正在预览)
			{
				var 当前鼠标位置 = _宿主.GetGlobalMousePosition();
				if (上次鼠标位置.DistanceTo(当前鼠标位置) > _拖动敏感度阈值)
				{
					正在长按 = false;
					if (正在预览)
					{
						_宿主.Position = _宿主.原始位置;
						_宿主.Scale = _宿主.原始缩放;
						正在预览 = false;
					}
					正在拖拽 = true;
					EmitSignal(SignalName.请求开始拖拽);
				}
			}
		}
	}

	private void 处理鼠标进入()
	{
		if (正在使用中) return;
		// 不再检查 _禁用战斗交互，确保查看模式也能触发悬停
		if (_宿主.当前模式 == 卡牌模式.查看)
		{
			EmitSignal(SignalName.鼠标进入);
			return;
		}
		if (!正在拖拽 && !正在查看 && !正在预览)
		{
			EmitSignal(SignalName.鼠标进入);
		}
	}

	private void 处理鼠标离开()
	{
		if (正在使用中) return;
		if (_宿主.当前模式 == 卡牌模式.查看)
		{
			EmitSignal(SignalName.鼠标离开);
			return;
		}
		if (!正在查看 && !正在拖拽 && !正在预览)
		{
			EmitSignal(SignalName.鼠标离开);
		}
	}

	private void 开始预览()
	{
		正在预览 = true;
		正在长按 = false;
		EmitSignal(SignalName.请求开始预览);
	}

	private void 结束预览()
	{
		正在预览 = false;
		EmitSignal(SignalName.请求结束预览);
	}

	private void 检测可使用状态()
	{
		bool 新可使用 = _宿主.GlobalPosition.Y < _可使用高度阈值;
		if (新可使用 != 当前可使用)
		{
			当前可使用 = 新可使用;
			EmitSignal(SignalName.可使用状态改变, 当前可使用);
		}
	}

	public void 设置禁用(bool 禁用)
	{
		_禁用战斗交互 = 禁用;
		// 设置禁用时，只影响战斗交互，但鼠标过滤器仍为 Pass 以保证悬停事件能收到
		_宿主.MouseFilter = MouseFilterEnum.Pass;
	}

	public void 重置交互状态()
	{
		正在拖拽 = false;
		正在查看 = false;
		正在长按 = false;
		正在预览 = false;
		当前可使用 = false;
		正在使用中 = false;
		_禁用战斗交互 = false;
		_宿主.MouseFilter = MouseFilterEnum.Pass;
	}

	public void 重置拖拽状态() => 正在拖拽 = false;
	public void 标记正在使用() => 正在使用中 = true;
	public void 禁用战斗输入(bool 禁用) => _禁用战斗交互 = 禁用;
	
	public bool IsMouseOver()
	{
		var 鼠标位置 = _宿主.GetLocalMousePosition();
		return new Rect2(Vector2.Zero, _宿主.Size).HasPoint(鼠标位置);
	}
}
