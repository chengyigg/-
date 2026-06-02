using Godot;

public partial class CardAppearanceComponent : Control
{
	// 引用原CardUI中的UI节点（由外部传入）
	private TextureRect _卡面;
	private TextureRect _图片;
	private TextureRect _卡面标志;
	private TextureRect _卡面标志2;
	private Label _名称标签;
	private Label _描述标签;
	private Label _伤害标签;
	private Label _速度标签;

	private FontFile _卡牌字体;
	private int _名称标签字号 = 16;
	private int _描述标签字号 = 14;
	private int _伤害标签字号 = 14;
	private int _速度标签字号 = 14;

	public CardInstance 当前卡牌 { get; private set; }
	private bool _是敌人卡牌;

	public void 获取UI节点引用(CardUI 宿主)
	{
		_卡面 = 宿主.GetNode<TextureRect>("卡面");
		_图片 = 宿主.GetNode<TextureRect>("图片");
		_卡面标志 = 宿主.GetNode<TextureRect>("卡面标志");
		_卡面标志2 = 宿主.GetNode<TextureRect>("卡面标志2");
		_名称标签 = 宿主.GetNode<Label>("名称标签");
		_描述标签 = 宿主.GetNode<Label>("描述标签");
		_伤害标签 = 宿主.GetNode<Label>("伤害标签");
		_速度标签 = 宿主.GetNode<Label>("速度标签");
	}

	public void 设置字体参数(FontFile 字体, int 名称字号, int 描述字号, int 伤害字号, int 速度字号)
	{
		_卡牌字体 = 字体;
		_名称标签字号 = 名称字号;
		_描述标签字号 = 描述字号;
		_伤害标签字号 = 伤害字号;
		_速度标签字号 = 速度字号;
	}

	public void 应用字体设置()
	{
		if (_卡牌字体 != null)
		{
			_名称标签?.AddThemeFontOverride("font", _卡牌字体);
			_描述标签?.AddThemeFontOverride("font", _卡牌字体);
			_伤害标签?.AddThemeFontOverride("font", _卡牌字体);
			_速度标签?.AddThemeFontOverride("font", _卡牌字体);
		}
		_名称标签?.AddThemeFontSizeOverride("font_size", _名称标签字号);
		_描述标签?.AddThemeFontSizeOverride("font_size", _描述标签字号);
		_伤害标签?.AddThemeFontSizeOverride("font_size", _伤害标签字号);
		_速度标签?.AddThemeFontSizeOverride("font_size", _速度标签字号);
	}

	public void 初始化(CardInstance 卡牌, bool 是敌人)
	{
		当前卡牌 = 卡牌;
		_是敌人卡牌 = 是敌人;

		if (是敌人)
		{
			if (卡牌?.基础数据?.卡背贴图 != null)
				_卡面.Texture = 卡牌.基础数据.卡背贴图;
			_名称标签.Visible = false;
			_描述标签.Visible = false;
			_伤害标签.Visible = false;
			_速度标签.Visible = false;
			_图片.Visible = false;
			_卡面标志.Visible = false;
			_卡面标志2.Visible = false;
		}
		else
		{
			if (卡牌?.基础数据 == null) return;
			_名称标签.Text = 卡牌.基础数据.卡牌名称;
			_描述标签.Text = 句号换行(卡牌.基础数据.卡牌描述);
			_伤害标签.Text = 卡牌.当前伤害.ToString();
			_速度标签.Text = 卡牌.当前速度值.ToString();
			_描述标签.AutowrapMode = TextServer.AutowrapMode.WordSmart;

			if (卡牌.基础数据.卡面贴图 != null)
				_卡面.Texture = 卡牌.基础数据.卡面贴图;
			if (_图片 != null && 卡牌.基础数据.卡牌图标 != null)
			{
				_图片.Texture = 卡牌.基础数据.卡牌图标;
				_图片.Visible = true;
			}

			var 样式 = _名称标签.GetThemeColor("font_color");
			switch (卡牌.基础数据.类型)
			{
				case CardData.卡牌类型.攻击: 样式 = new Color(0.8f, 0.3f, 0.3f); break;
				case CardData.卡牌类型.防御: 样式 = new Color(0.3f, 0.8f, 0.3f); break;
				case CardData.卡牌类型.特殊: 样式 = new Color(0.3f, 0.3f, 0.8f); break;
			}
			_名称标签.AddThemeColorOverride("font_color", 样式);
		}
	}

	public void 设置为查看模式(CardData 数据)
	{
		if (数据 == null) return;
		_名称标签.Text = 数据.卡牌名称;
		_描述标签.Text = 句号换行(数据.卡牌描述);
		_伤害标签.Text = 数据.基础伤害.ToString();
		_速度标签.Text = 数据.速度值.ToString();
		if (_卡面 != null && 数据.卡面贴图 != null)
			_卡面.Texture = 数据.卡面贴图;
		if (_图片 != null && 数据.卡牌图标 != null)
			_图片.Texture = 数据.卡牌图标;
		// 隐藏卡背节点（如果有）
		var 卡背 = GetParent().GetNodeOrNull<Control>("卡背");
		if (卡背 != null) 卡背.Visible = false;
	}

	public void 更新描述根据强化模式(bool 是否强化)
	{
		if (当前卡牌?.基础数据 != null)
		{
			string 新描述 = 是否强化 && !string.IsNullOrEmpty(当前卡牌.基础数据.强化描述)
				? 当前卡牌.基础数据.强化描述
				: 当前卡牌.基础数据.卡牌描述;
			if (_描述标签 != null && _描述标签.Text != 新描述)
				_描述标签.Text = 新描述;
		}
	}

	public string 获取卡牌描述()
	{
		var 父 = GetParent() as CardUI;
		if (父?.当前查看CardData != null)
			return 父.当前查看CardData.卡牌描述;
		return 当前卡牌?.基础数据?.卡牌描述 ?? "暂无描述";
	}

	private string 句号换行(string 原文)
	{
		if (string.IsNullOrEmpty(原文)) return 原文;
		return 原文.Replace("。", "。\n").Replace(".", ".\n");
	}
}
