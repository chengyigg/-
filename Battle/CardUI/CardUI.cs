using Godot;
using System;
using System.Threading.Tasks;

public partial class CardUI : Control
{
	// 对外保留的公共数据
	public CardData 当前查看CardData => _模式组件?.查看CardData;
	public CardInstance 当前卡牌 => _外观组件?.当前卡牌;
	public 卡牌模式 当前模式 { get; private set; }

	// 对外保留的信号
	[Signal] public delegate void 鼠标进入卡牌EventHandler(CardUI 卡牌);
	[Signal] public delegate void 鼠标离开卡牌EventHandler(CardUI 卡牌);
	[Signal] public delegate void 卡牌被点击EventHandler(GodotObject 卡牌);
	[Signal] public delegate void 卡牌开始拖拽EventHandler(GodotObject 卡牌);
	[Signal] public delegate void 卡牌结束拖拽EventHandler(GodotObject 卡牌, Vector2 位置);
	[Signal] public delegate void 卡牌被使用EventHandler(GodotObject 卡牌);

	// 子组件引用
	private CardAppearanceComponent _外观组件;
	private CardInteractionComponent _交互组件;
	private CardAnimationComponent _动画组件;
	private CardModeComponent _模式组件;

	private int _悬停前ZIndex;

	// 主控自身状态
	public Vector2 原始位置 { get; set; }
	public Vector2 原始缩放 { get; set; }
	private bool _已初始化 = false;

	// 确保子组件已初始化
	private void 确保初始化()
	{
		if (_已初始化) return;
		
		_外观组件 = new CardAppearanceComponent();
		_交互组件 = new CardInteractionComponent();
		_动画组件 = new CardAnimationComponent();
		_模式组件 = new CardModeComponent();

		AddChild(_外观组件);
		AddChild(_交互组件);
		AddChild(_动画组件);
		AddChild(_模式组件);

		// 连接子组件信号
		_交互组件.鼠标进入 += () => EmitSignal(SignalName.鼠标进入卡牌, this);
		_交互组件.鼠标离开 += () => EmitSignal(SignalName.鼠标离开卡牌, this);
		_交互组件.卡牌被点击 += (obj) => EmitSignal(SignalName.卡牌被点击, obj);
		_交互组件.请求开始拖拽 += () => 处理开始拖拽();
		_交互组件.请求结束拖拽 += (鼠标位置, 是否使用) => 处理结束拖拽(鼠标位置, 是否使用);
		_交互组件.请求开始预览 += () => 处理开始预览();
		_交互组件.请求结束预览 += () => 处理结束预览();
		_交互组件.可使用状态改变 += (可使用) => 处理可使用状态改变(可使用);
		_交互组件.查看卡牌Request += () => 处理查看卡牌();

		_模式组件.模式改变 += (新模式Int) => {
			当前模式 = (卡牌模式)新模式Int;
			if (当前模式 == 卡牌模式.查看) _交互组件.禁用战斗输入(true);
			else _交互组件.禁用战斗输入(false);
		};
		_模式组件.请求缩放动画 += (目标缩放, 时长) => _动画组件.播放缩放动画(this, 目标缩放, 时长);
		_模式组件.请求ZIndex改变 += (新ZIndex) => ZIndex = 新ZIndex;

		原始位置 = Position;
		原始缩放 = Scale;
		_动画组件.设置原始缩放(原始缩放);
		_外观组件.获取UI节点引用(this);
		_外观组件.设置字体参数(卡牌字体, 名称标签字号, 描述标签字号, 伤害标签字号, 速度标签字号);
		_外观组件.应用字体设置();
		_交互组件.初始化(this, _外观组件, 可使用高度阈值, 长按时间阈值, 拖动敏感度阈值);

		_已初始化 = true;
	}

	// ==================== 高光动画控制 ====================
	[Export] public AnimationPlayer 高光动画器;
	private bool _高光信号已连接 = false;

	public void 设置高光启用(bool 启用)
	{
		if (高光动画器 == null)
		{
			GD.PrintErr("CardUI: 未设置高光动画器，请在检查器中拖入");
			return;
		}

		if (启用)
		{
			高光动画器.Stop();
			高光动画器.Seek(0);
			高光动画器.Play("高光出现");
		}
		else
		{
			高光动画器.Stop();
			高光动画器.Seek(0);
		}
	}

	private void On高光动画结束(string animName)
	{
		if (animName == "高光出现" && 高光动画器 != null)
		{
			高光动画器.Play("高光循环");
		}
	}

	// ==================== Godot 生命周期 ====================
	public override void _Ready()
	{
		确保初始化();

		// 连接高光动画信号（只连接一次）
		if (高光动画器 != null && !_高光信号已连接)
		{
			高光动画器.Connect("animation_finished", new Callable(this, nameof(On高光动画结束)), (uint)ConnectFlags.OneShot);
			_高光信号已连接 = true;
		}

		// 重新连接悬停动画
		_交互组件.鼠标进入 += () => {
			EmitSignal(SignalName.鼠标进入卡牌, this);
			if (!_交互组件.正在拖拽 && !_交互组件.正在预览)
			{
				_动画组件.播放缩放动画(this, 原始缩放 * 悬停缩放倍数, 动画时长);
				_悬停前ZIndex = ZIndex;
				ZIndex = (当前模式 == 卡牌模式.查看) ? 4 : 2;
			}
		};
		_交互组件.鼠标离开 += () => {
			EmitSignal(SignalName.鼠标离开卡牌, this);
			if (!_交互组件.正在拖拽 && !_交互组件.正在预览)
			{
				_动画组件.播放缩放动画(this, 原始缩放, 动画时长);
				ZIndex = _悬停前ZIndex;
			}
		};
	}

	// 对外公共方法
	public void 初始化(CardInstance 卡牌, bool 是敌人卡牌 = false)
	{
		确保初始化();
		当前模式 = 卡牌模式.战斗;
		_模式组件.设置模式(卡牌模式.战斗);
		_外观组件.初始化(卡牌, 是敌人卡牌);
		_交互组件.重置交互状态();
		_动画组件.重置动画();
		Modulate = Colors.White;
		ZIndex = 0;
	}

	public void 设置禁用状态(bool 禁用)
	{
		确保初始化();
		_交互组件.设置禁用(禁用);
		Modulate = 禁用 ? new Color(0.6f, 0.6f, 0.6f, 0.8f) : Colors.White;
	}

	public void 更新原始位置(Vector2 新位置) => 原始位置 = 新位置;
	public Vector2 获取原始缩放() => 原始缩放;
	public void 设置原始缩放(Vector2 新原始缩放) => 原始缩放 = 新原始缩放;
	public bool 是拖拽状态() => _交互组件?.正在拖拽 ?? false;

	public void 重置状态()
	{
		确保初始化();
		_交互组件.重置交互状态();
		_动画组件.重置动画();
		Position = 原始位置;
		Scale = 原始缩放;
		ZIndex = 0;
		Modulate = Colors.White;
		设置高光启用(false);
	}

	public void 设置选中(bool 选中) => _模式组件?.设置选中(选中);
	public void 取消选中() => _模式组件?.取消选中();

	public void 执行流畅入场动画(Vector2 目标位置, Vector2 起始位置, Action 动画完成回调 = null)
		=> _动画组件?.执行流畅入场动画(this, 目标位置, 起始位置, 原始缩放, 动画完成回调);
	public void 执行平滑移动动画(Vector2 目标位置, float 目标旋转, int 目标层级, float 时长 = 0.3f, float 延迟 = 0f)
		=> _动画组件?.执行平滑移动动画(this, 目标位置, 目标旋转, 目标层级, 时长, 延迟);
	public void 执行使用动画(Action 动画完成回调 = null)
		=> _动画组件?.执行使用动画(this, 动画完成回调);
	public async Task 执行使用动画Lerp(Action 动画完成回调 = null)
	{
		if (_动画组件 != null)
			await _动画组件.执行使用动画Lerp(this, 动画完成回调);
		else
			动画完成回调?.Invoke();
	}
	public void 执行弃置动画(Vector2 弃牌堆位置, Action 动画完成回调 = null)
		=> _动画组件?.执行弃置动画(this, 弃牌堆位置, 动画完成回调);
	public void 执行移动到位置动画(Vector2 目标位置, float 时长 = 0.3f)
		=> _动画组件?.执行移动到位置动画(this, 目标位置, 原始缩放, 时长);
	public void 执行入场动画(Vector2 起始位置, Action 动画完成回调 = null)
		=> _动画组件?.执行入场动画(this, 起始位置, 原始位置, 原始缩放, 动画完成回调);

	public void 设置为查看模式(CardData CardData)
	{
		确保初始化();
		_交互组件.重置交互状态();
		_交互组件.禁用战斗输入(true);
		_模式组件.设置模式(卡牌模式.查看);
		_模式组件.查看CardData = CardData;
		_外观组件.设置为查看模式(CardData);
		CustomMinimumSize = Size;
		PivotOffset = Size * 0.5f;
	}

	public void 应用初始缩放() => Scale = 原始缩放;
	public CardInstance 获取CardData() => _外观组件?.当前卡牌;
	public string 获取卡牌描述() => _外观组件?.获取卡牌描述() ?? "暂无描述";
	public void 更新描述根据强化模式(bool 是否强化) => _外观组件?.更新描述根据强化模式(是否强化);

	// 内部协调方法
	private void 处理开始拖拽()
	{
		_动画组件.停止当前动画();
		设置高光启用(false);
		ZIndex = 2;
		EmitSignal(SignalName.卡牌开始拖拽, (GodotObject)this);
	}

	private void 处理结束拖拽(Vector2 鼠标位置, bool 应该使用)
	{
		if (应该使用)
		{
			_交互组件.标记正在使用();
			_动画组件.执行使用动画(this, () => {
				EmitSignal(SignalName.卡牌被使用, (GodotObject)_外观组件.当前卡牌);
				QueueFree();
			});
		}
		else
		{
			_动画组件.播放移动动画(this, 原始位置, 0.3f);
			_动画组件.播放缩放动画(this, 原始缩放, 0.3f);
			EmitSignal(SignalName.卡牌结束拖拽, (GodotObject)this, 鼠标位置);
		}
		ZIndex = 0;
		_交互组件.重置拖拽状态();
	}

	private void 处理开始预览()
	{
		var 目标位置 = 原始位置 - new Vector2(0, 预览上移距离);
		_动画组件.播放移动动画(this, 目标位置, 0.15f);
		_动画组件.播放缩放动画(this, 原始缩放 * 1.5f, 0.15f);
		ZIndex = 2;
	}

	private void 处理结束预览()
	{
		_动画组件.播放移动动画(this, 原始位置, 0.15f);
		_动画组件.播放缩放动画(this, 原始缩放, 0.15f);
		ZIndex = 0;
	}

	private void 处理可使用状态改变(bool 可使用)
	{
		Modulate = 可使用 ? 可使用高亮颜色 : Colors.White;
	}

	private void 处理查看卡牌()
	{
		if (当前模式 == 卡牌模式.战斗)
		{
			if (!_交互组件.正在查看)
			{
				_交互组件.正在查看 = true;
				var 目标位置 = 原始位置 - new Vector2(0, 80);
				_动画组件.播放移动动画(this, 目标位置, 0.2f);
				_动画组件.播放缩放动画(this, 原始缩放 * 查看缩放倍数, 0.2f);
				ZIndex = 4;
			}
			else
			{
				_交互组件.正在查看 = false;
				_动画组件.播放移动动画(this, 原始位置, 0.2f);
				_动画组件.播放缩放动画(this, 原始缩放 * (_交互组件.IsMouseOver() ? 悬停缩放倍数 : 1f), 0.2f);
				ZIndex = 0;
			}
		}
	}

	// 导出属性
	[Export] private TextureRect 卡面;
	[Export] private TextureRect 图片;
	[Export] private TextureRect 卡面标志;
	[Export] private TextureRect 卡面标志2;
	[Export] private Label 名称标签;
	[Export] private Label 描述标签;
	[Export] private Label 伤害标签;
	[Export] private Label 速度标签;
	[Export] private FontFile 卡牌字体;
	[Export] private int 名称标签字号 = 16;
	[Export] private int 描述标签字号 = 14;
	[Export] private int 伤害标签字号 = 14;
	[Export] private int 速度标签字号 = 14;
	[Export] private float 可使用高度阈值 = 300f;
	[Export] private Color 可使用高亮颜色 = new Color(1.5f, 1.5f, 1.5f);
	[Export] private float 预览上移距离 = 400f;
	[Export] private float 长按时间阈值 = 0.3f;
	[Export] private float 动画时长 = 0.2f;
	[Export] private float 拖动敏感度阈值 = 10f;
	[Export] private float 悬停缩放倍数 = 1.1f;
	[Export] private float 查看缩放倍数 = 1.3f;
	[Export] private bool 是敌人卡牌 = false;
}
