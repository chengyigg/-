using Godot;

public enum 卡牌模式 { 战斗, 查看 }

public partial class CardModeComponent : Control
{
	[Signal] public delegate void 模式改变EventHandler(int 新模式);
	[Signal] public delegate void 请求缩放动画EventHandler(Vector2 目标缩放, float 时长);
	[Signal] public delegate void 请求ZIndex改变EventHandler(int 新ZIndex);

	public 卡牌模式 当前模式 { get; private set; } = 卡牌模式.战斗;
	public CardData 查看CardData { get; set; }
	private bool _是否选中;
	private ColorRect _动态选中效果;
	private Control _宿主;

	private void 确保初始化()
	{
		if (_宿主 != null) return;
		_宿主 = GetParent<Control>();
		if (_宿主 == null) return;
		创建选中效果();
	}

	private void 创建选中效果()
	{
		_动态选中效果 = new ColorRect();
		_动态选中效果.Name = "动态选中效果";
		_动态选中效果.Color = new Color(1, 1, 0, 0.3f);
		_动态选中效果.MouseFilter = MouseFilterEnum.Ignore;
		_动态选中效果.Visible = false;
		_动态选中效果.AnchorLeft = 0;
		_动态选中效果.AnchorTop = 0;
		_动态选中效果.AnchorRight = 1;
		_动态选中效果.AnchorBottom = 1;
		_宿主.AddChild(_动态选中效果);
	}

	public void 设置模式(卡牌模式 新模式)
	{
		确保初始化();
		if (_宿主 == null) return;

		if (当前模式 == 新模式) return;
		当前模式 = 新模式;
		EmitSignal(SignalName.模式改变, (int)当前模式);

		if (当前模式 == 卡牌模式.查看)
		{
			_宿主.MouseFilter = MouseFilterEnum.Stop;
			var 当前尺寸 = _宿主.Size;
			_宿主.CustomMinimumSize = 当前尺寸;
			_宿主.SetSize(当前尺寸);
			_宿主.PivotOffset = _宿主.Size * 0.5f;
		}
		else
		{
			_宿主.MouseFilter = MouseFilterEnum.Pass;
			_宿主.CustomMinimumSize = Vector2.Zero;
			取消选中();
		}
	}

	public void 设置选中(bool 选中)
	{
		确保初始化();
		if (_宿主 == null) return;
		if (当前模式 != 卡牌模式.查看) return;

		_是否选中 = 选中;
		// 隐藏黄色高光，保留缩放动画和层级效果
		// if (_动态选中效果 != null) _动态选中效果.Visible = 选中;

		if (选中)
		{
			EmitSignal(SignalName.请求缩放动画, _宿主.Scale * 1.2f, 0.2f);
			EmitSignal(SignalName.请求ZIndex改变, 4);
		}
		else
		{
			EmitSignal(SignalName.请求缩放动画, _宿主.Scale, 0.2f);
			EmitSignal(SignalName.请求ZIndex改变, 0);
		}
	}

	public void 取消选中()
	{
		if (_是否选中)
			设置选中(false);
	}
}
