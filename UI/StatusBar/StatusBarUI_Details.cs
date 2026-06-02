using Godot;

public partial class StatusBarUI
{
	// ======================== 卡牌详情框 ========================
	private void 创建详情框()
	{
		// 创建一个 CanvasLayer 专门放描述框
		var 描述层 = new CanvasLayer();
		描述层.Layer = 10;   // 10 足够大，绝对高于所有普通 UI（普通 UI 的 ZIndex 范围建议 0-9）
		描述层.Name = "描述框图层";
		
		详情框 = new Control();
		详情框.Name = "卡牌详情框";
		详情框.Visible = false;
		详情框.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		// 创建背景面板
		var 背景面板 = new Panel();
		背景面板.Name = "背景面板";
		背景面板.MouseFilter = Control.MouseFilterEnum.Ignore;
		if (详情框背景样式 != null)
		{
			背景面板.AddThemeStyleboxOverride("panel", 详情框背景样式);
		}
		背景面板.ThemeTypeVariation = "";
		背景面板.AnchorLeft = 背景面板.AnchorTop = 0;
		背景面板.AnchorRight = 背景面板.AnchorBottom = 1;
		
		详情描述标签 = new Label();
		详情描述标签.Name = "描述标签";
		详情描述标签.HorizontalAlignment = HorizontalAlignment.Left;
		详情描述标签.VerticalAlignment = VerticalAlignment.Top;
		详情描述标签.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		详情描述标签.SizeFlagsHorizontal = 详情描述标签.SizeFlagsVertical = 0;
		if (详情框字体 != null)
		{
			var settings = new LabelSettings();
			settings.Font = 详情框字体;
			settings.FontSize = 17;
			详情描述标签.LabelSettings = settings;
		}
		else
		{
			详情描述标签.AddThemeFontSizeOverride("font_size", 17);
		}
		
		背景面板.AddChild(详情描述标签);
		详情框.AddChild(背景面板);
		描述层.AddChild(详情框);
		
		// ✅ 关键修改：延迟添加到根节点，避免 “Parent node is busy setting up children” 错误
		GetTree().Root.CallDeferred("add_child", 描述层);
	}

	private void 显示详情框(CardUI 目标卡牌, string 描述文本)
	{
		if (详情框 == null)
		{
			创建详情框();  // 重新创建
		}
		详情描述标签.Text = 描述文本;
		详情描述标签.Size = Vector2.Zero;
		详情描述标签.CustomMinimumSize = Vector2.Zero;
		
		// 扩大基础尺寸：宽度从 250 变为 300（250 * 1.2）
		float 框宽度 = 300f;
		float 手动边距 = 30f;
		float 标签可用宽度 = 框宽度 - 手动边距 * 2;  // 300 - 60 = 240
		详情描述标签.Size = new Vector2(标签可用宽度, 0);
		详情描述标签.UpdateMinimumSize();
		float 文本高度 = 详情描述标签.GetLineHeight() * (详情描述标签.GetLineCount() + 1);
		float 框高度 = 文本高度 + 手动边距 * 2;
		详情框.Size = new Vector2(框宽度, 框高度);
		详情描述标签.Position = new Vector2(手动边距, 手动边距);
		
		// 判断是否在强化预览面板内
		bool 在强化面板内 = false;
		Node 当前节点 = 目标卡牌.GetParent();
		while (当前节点 != null)
		{
			if (当前节点 is 强化预览面板)
			{
				在强化面板内 = true;
				break;
			}
			当前节点 = 当前节点.GetParent();
		}
		
		float 横向偏移 = 150 + (在强化面板内 ? 80 : 0);
		Vector2 目标位置 = 目标卡牌.GlobalPosition + new Vector2(横向偏移, 50);
		详情框.Position = 目标位置;
		
		// 缩放动画：从 0.833 到 1（0.833 = 1 / 1.2）
		详情框.Scale = new Vector2(0.833f, 0.833f);
		详情框.Visible = true;
		
		var tween = 详情框.CreateTween();
		tween.SetTrans(Tween.TransitionType.Back);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(详情框, "scale", Vector2.One, 0.2f);
		// 没有其他动画（无淡入、无位移）
	}

	private void 隐藏详情框()
	{
		if (详情框 != null)
		{
			var tween = 详情框.CreateTween();
			tween.Stop();
			详情框.Scale = Vector2.One;
			详情框.Visible = false;
			
			var 描述层 = 详情框.GetParent()?.GetParent() as CanvasLayer;
			if (描述层 != null)
			{
				描述层.QueueFree();
			}
			else
			{
				详情框.QueueFree();
			}
			详情框 = null;
			详情描述标签 = null;
		}
	}
}
