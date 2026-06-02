using Godot;
using System;

/// <summary>
/// A filter UI for backpack card types. Attach this script to a VBoxContainer.
/// </summary>
public partial class BackpackFilter : VBoxContainer
{
	// 按钮文本与卡牌类型的对应关系
	private readonly (string 名称, CardData.卡牌类型 类型)[] 按钮数据 = new[]
	{
		("攻击", CardData.卡牌类型.攻击),
		("防御", CardData.卡牌类型.防御),
		("特殊", CardData.卡牌类型.特殊),
		("情绪", CardData.卡牌类型.情绪),
		("装备", CardData.卡牌类型.装备)
	};

	// 字体导出字段，可在检查器中拖入
	[Export] private FontFile 按钮字体;

	// 用于保存当前选中的按钮
	private Button 当前选中按钮;

	// 引用主界面脚本（用于调用刷新背包）
	private StatusBarUI 主界面;

	// 颜色常量（遵循你的方案）
	private readonly Color 未选中背景色 = new Color(0.9098f, 0.9412f, 0.8784f); // #E8F0E0
	private readonly Color 未选中边框色 = new Color(0.6235f, 0.7020f, 0.6118f); // #9FB39C
	private readonly Color 未选中文字色 = new Color(0.2471f, 0.3686f, 0.2471f); // #3F5E3F

	private readonly Color 选中背景色 = new Color(0.4235f, 0.6510f, 0.4235f);   // #6ca66c
	private readonly Color 选中边框色 = new Color(0.3098f, 0.5098f, 0.3098f);   // #4F824F
	private readonly Color 选中文字色 = Colors.White;

	public override void _Ready()
	{
		// 获取父节点上的主界面脚本
		主界面 = GetNode<StatusBarUI>("..");
		if (主界面 == null)
		{
			return;
		}

		// 设置容器自身的属性
		CustomMinimumSize = new Vector2(100, 0); // 宽度固定，高度由内容决定

		// 修改点：使用主题常量设置按钮间距，替代不存在的 Separation 属性
		AddThemeConstantOverride("separation", 10); // 设置垂直间距为10像素

		// 创建五个按钮
		foreach (var (名称, 类型) in 按钮数据)
		{
			var 按钮 = new Button();
			按钮.Text = 名称;
			按钮.CustomMinimumSize = new Vector2(100, 50); // 固定大小 100x50
			按钮.SizeFlagsHorizontal = 0; // 不扩展
			按钮.SizeFlagsVertical = 0;   // 不扩展

			// 应用样式
			应用未选中样式(按钮);

			// 绑定点击事件
			按钮.Pressed += () => 当按钮点击(按钮, 类型);

			// 绑定鼠标悬停事件（用于动画）
			按钮.MouseEntered += () => 当鼠标进入按钮(按钮);
			按钮.MouseExited += () => 当鼠标离开按钮(按钮);

			AddChild(按钮);
		}
	}

	/// <summary>Resets the selected filter to none, unhighlighting all buttons.</summary>
	public void 重置选中()
	{
		// 如果当前有选中按钮，先恢复它为未选中样式并恢复大小
		if (当前选中按钮 != null)
		{
			应用未选中样式(当前选中按钮);
			恢复按钮大小(当前选中按钮);
			当前选中按钮 = null;
		}

		// 确保所有按钮都是未选中样式（如果有任何按钮因之前状态残留）
		foreach (Node child in GetChildren())
		{
			if (child is Button 按钮)
			{
				应用未选中样式(按钮);
				恢复按钮大小(按钮);
			}
		}
	}

	// 应用未选中样式（纸片风格）
	private void 应用未选中样式(Button 按钮)
	{
		var 样式盒 = new StyleBoxFlat();
		样式盒.BgColor = 未选中背景色;
		样式盒.BorderColor = 未选中边框色;
		样式盒.BorderWidthLeft = 1;
		样式盒.BorderWidthTop = 1;
		样式盒.BorderWidthRight = 1;
		样式盒.BorderWidthBottom = 1;
		样式盒.CornerRadiusTopLeft = 8;
		样式盒.CornerRadiusTopRight = 8;
		样式盒.CornerRadiusBottomRight = 8;
		样式盒.CornerRadiusBottomLeft = 8;
		样式盒.ContentMarginLeft = 10;
		样式盒.ContentMarginRight = 10;
		样式盒.ContentMarginTop = 5;
		样式盒.ContentMarginBottom = 5;

		按钮.AddThemeStyleboxOverride("normal", 样式盒);
		按钮.AddThemeColorOverride("font_color", 未选中文字色);
		if (按钮字体 != null)
		{
			按钮.AddThemeFontOverride("font", 按钮字体);
		}
		按钮.AddThemeFontSizeOverride("font_size", 14);
	}

	// 应用选中样式
	private void 应用选中样式(Button 按钮)
	{
		var 样式盒 = new StyleBoxFlat();
		样式盒.BgColor = 选中背景色;
		样式盒.BorderColor = 选中边框色;
		样式盒.BorderWidthLeft = 1;
		样式盒.BorderWidthTop = 1;
		样式盒.BorderWidthRight = 1;
		样式盒.BorderWidthBottom = 1;
		样式盒.CornerRadiusTopLeft = 8;
		样式盒.CornerRadiusTopRight = 8;
		样式盒.CornerRadiusBottomRight = 8;
		样式盒.CornerRadiusBottomLeft = 8;
		样式盒.ContentMarginLeft = 10;
		样式盒.ContentMarginRight = 10;
		样式盒.ContentMarginTop = 5;
		样式盒.ContentMarginBottom = 5;

		按钮.AddThemeStyleboxOverride("normal", 样式盒);
		按钮.AddThemeColorOverride("font_color", 选中文字色);
		// 字体保持不变（沿用未选中时的字体）
	}

	// 按钮点击处理
	private void 当按钮点击(Button 点击的按钮, CardData.卡牌类型 类型)
	{
		if (当前选中按钮 == 点击的按钮) return; // 已选中则忽略

		// 恢复上一个按钮为未选中样式
		if (当前选中按钮 != null)
		{
			应用未选中样式(当前选中按钮);
			恢复按钮大小(当前选中按钮); // 取消放大
		}

		// 设置新按钮为选中样式
		当前选中按钮 = 点击的按钮;
		应用选中样式(当前选中按钮);
		放大按钮(当前选中按钮); // 选中时放大

		// 调用主界面刷新
		主界面?.刷新背包按类型(类型);
	}

	// 鼠标进入按钮（悬停效果）
	private void 当鼠标进入按钮(Button 按钮)
	{
		if (按钮 == 当前选中按钮) return; // 已选中不额外处理

		// 悬停时轻微放大，但保持未选中样式（颜色不变）
		放大按钮(按钮);
	}

	private void 当鼠标离开按钮(Button 按钮)
	{
		if (按钮 == 当前选中按钮) return;

		恢复按钮大小(按钮);
	}

	// 放大动画
	private void 放大按钮(Button 按钮)
	{
		var tween = CreateTween();
		tween.TweenProperty(按钮, "scale", new Vector2(1.1f, 1.1f), 0.1f)
			 .SetEase(Tween.EaseType.Out)
			 .SetTrans(Tween.TransitionType.Quad);
	}

	private void 恢复按钮大小(Button 按钮)
	{
		var tween = CreateTween();
		tween.TweenProperty(按钮, "scale", Vector2.One, 0.1f)
			 .SetEase(Tween.EaseType.Out)
			 .SetTrans(Tween.TransitionType.Quad);
	}
}
