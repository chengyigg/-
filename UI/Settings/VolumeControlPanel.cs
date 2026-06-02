using Godot;
using System;

/// <summary>
/// Panel for controlling background music and sound effect volumes.
/// </summary>
public partial class VolumeControlPanel : Control
{
	private AudioStreamPlayer 背景音乐播放器;
	private AudioStreamPlayer 音效播放器;
	private HSlider 背景音乐滑块;
	private HSlider 音效滑块;

	public override void _Ready()
	{
		背景音乐播放器 = GetNodeOrNull<AudioStreamPlayer>("背景音乐");
		音效播放器 = GetNodeOrNull<AudioStreamPlayer>("音效");
		背景音乐滑块 = GetNodeOrNull<HSlider>("排列/背景音乐滑块");
		音效滑块 = GetNodeOrNull<HSlider>("排列/音效滑块");

		if (背景音乐滑块 != null)
		{
			背景音乐滑块.MinValue = 0;
			背景音乐滑块.MaxValue = 25;
			背景音乐滑块.Value = 12;
		}
		if (音效滑块 != null)
		{
			音效滑块.MinValue = 0;
			音效滑块.MaxValue = 25;
			音效滑块.Value = 12;
		}

		if (背景音乐滑块 != null)
		{
			背景音乐滑块.ValueChanged += 当背景音乐滑块变化;
			if (全局背景音乐管理器.实例 != null)
				背景音乐滑块.Value = 全局背景音乐管理器.实例.获取背景音乐音量() * 背景音乐滑块.MaxValue;
		}
		if (音效滑块 != null)
		{
			音效滑块.ValueChanged += _调节全局音效音量;
			if (全局背景音乐管理器.实例 != null)
				音效滑块.Value = 全局背景音乐管理器.实例.获取音效音量() * 音效滑块.MaxValue;
		}

		if (背景音乐滑块 != null) 设置滑块样式(背景音乐滑块);
		if (音效滑块 != null) 设置滑块样式(音效滑块);
		设置滑块导航();

		Modulate = Colors.Transparent;
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = true;
	}

	private void _调节全局音效音量(double 数值)
	{
		if (全局背景音乐管理器.实例 == null) return;
		float 线性音量 = (float)数值 / (float)音效滑块.MaxValue;
		全局背景音乐管理器.实例.设置音效音量(线性音量);
	}

	private void 当背景音乐滑块变化(double 数值)
	{
		float 线性音量 = (float)数值 / (float)背景音乐滑块.MaxValue;
		if (全局背景音乐管理器.实例 != null)
			全局背景音乐管理器.实例.设置背景音乐音量(线性音量);
	}

	private void 设置滑块导航()
	{
		if (背景音乐滑块 == null || 音效滑块 == null) return;
		背景音乐滑块.FocusMode = FocusModeEnum.All;
		音效滑块.FocusMode = FocusModeEnum.All;
		背景音乐滑块.FocusNeighborBottom = 音效滑块.GetPath();
		音效滑块.FocusNeighborTop = 背景音乐滑块.GetPath();
		背景音乐滑块.FocusNeighborLeft = 背景音乐滑块.GetPath();
		背景音乐滑块.FocusNeighborRight = 背景音乐滑块.GetPath();
		音效滑块.FocusNeighborLeft = 音效滑块.GetPath();
		音效滑块.FocusNeighborRight = 音效滑块.GetPath();
	}

	/// <summary>Sets default focus to the BGM slider.</summary>
	public void 设置默认焦点()
	{
		背景音乐滑块?.GrabFocus();
	}

	private void 设置滑块样式(HSlider 滑块)
	{
		滑块.RemoveThemeStyleboxOverride("grabber");
		滑块.RemoveThemeStyleboxOverride("grabber_highlight");
		滑块.RemoveThemeStyleboxOverride("grabber_pressed");
		滑块.RemoveThemeStyleboxOverride("grabber_focus");
		滑块.RemoveThemeStyleboxOverride("focus");
		滑块.RemoveThemeStyleboxOverride("grabber_area");
		滑块.RemoveThemeStyleboxOverride("grabber_area_highlight");
		滑块.RemoveThemeStyleboxOverride("fill");
		滑块.RemoveThemeStyleboxOverride("fill_highlight");

		var 背景样式 = new StyleBoxFlat();
		背景样式.BgColor = Color.FromHtml("#d0f0d0");
		背景样式.SetCornerRadiusAll(4);
		滑块.AddThemeStyleboxOverride("grabber_area", 背景样式);

		var 背景高亮样式 = new StyleBoxFlat();
		背景高亮样式.BgColor = Color.FromHtml("#b0e0b0");
		背景高亮样式.SetCornerRadiusAll(4);
		滑块.AddThemeStyleboxOverride("grabber_area_highlight", 背景高亮样式);

		var 填充样式 = new StyleBoxFlat();
		填充样式.BgColor = Color.FromHtml("#a0ffa0");
		填充样式.SetCornerRadiusAll(4);
		滑块.AddThemeStyleboxOverride("fill", 填充样式);

		var 填充高亮样式 = new StyleBoxFlat();
		填充高亮样式.BgColor = Color.FromHtml("#80e080");
		填充高亮样式.SetCornerRadiusAll(4);
		滑块.AddThemeStyleboxOverride("fill_highlight", 填充高亮样式);

		var 抓柄样式 = new StyleBoxFlat();
		抓柄样式.BgColor = Color.FromHtml("#1e6b1e");
		抓柄样式.SetCornerRadiusAll(8);
		抓柄样式.BorderWidthTop = 2;
		抓柄样式.BorderWidthBottom = 2;
		抓柄样式.BorderWidthLeft = 2;
		抓柄样式.BorderWidthRight = 2;
		抓柄样式.BorderColor = Color.FromHtml("#e0ffe0");
		抓柄样式.ShadowColor = Color.FromHtml("#0a2a0a");
		抓柄样式.ShadowSize = 3;
		抓柄样式.ShadowOffset = new Vector2(0, 1);
		滑块.AddThemeStyleboxOverride("grabber", 抓柄样式);

		var 抓柄高亮样式 = new StyleBoxFlat();
		抓柄高亮样式.BgColor = Color.FromHtml("#2a8a2a");
		抓柄高亮样式.SetCornerRadiusAll(8);
		抓柄高亮样式.BorderWidthTop = 2;
		抓柄高亮样式.BorderWidthBottom = 2;
		抓柄高亮样式.BorderWidthLeft = 2;
		抓柄高亮样式.BorderWidthRight = 2;
		抓柄高亮样式.BorderColor = Color.FromHtml("#f0fff0");
		抓柄高亮样式.ShadowColor = Color.FromHtml("#0a2a0a");
		抓柄高亮样式.ShadowSize = 3;
		抓柄高亮样式.ShadowOffset = new Vector2(0, 1);
		滑块.AddThemeStyleboxOverride("grabber_highlight", 抓柄高亮样式);

		var 抓柄按下样式 = new StyleBoxFlat();
		抓柄按下样式.BgColor = Color.FromHtml("#145214");
		抓柄按下样式.SetCornerRadiusAll(16);
		抓柄按下样式.BorderWidthTop = 4;
		抓柄按下样式.BorderWidthBottom = 4;
		抓柄按下样式.BorderWidthLeft = 4;
		抓柄按下样式.BorderWidthRight = 4;
		抓柄按下样式.BorderColor = Color.FromHtml("#ccffcc");
		抓柄按下样式.ShadowColor = Color.FromHtml("#0a2a0a");
		抓柄按下样式.ShadowSize = 6;
		抓柄按下样式.ShadowOffset = new Vector2(0, 2);
		抓柄按下样式.ContentMarginLeft = 20;
		抓柄按下样式.ContentMarginRight = 20;
		抓柄按下样式.ContentMarginTop = 20;
		抓柄按下样式.ContentMarginBottom = 20;
		滑块.AddThemeStyleboxOverride("grabber_pressed", 抓柄按下样式);

		var 抓柄焦点样式 = new StyleBoxFlat();
		抓柄焦点样式.BgColor = Color.FromHtml("#2a8a2a");
		抓柄焦点样式.SetCornerRadiusAll(12);
		抓柄焦点样式.BorderWidthTop = 2;
		抓柄焦点样式.BorderWidthBottom = 2;
		抓柄焦点样式.BorderWidthLeft = 2;
		抓柄焦点样式.BorderWidthRight = 2;
		抓柄焦点样式.BorderColor = Color.FromHtml("#c8ffc8");
		抓柄焦点样式.ShadowColor = Color.FromHtml("#32cd32");
		抓柄焦点样式.ShadowSize = 12;
		抓柄焦点样式.ShadowOffset = Vector2.Zero;
		抓柄焦点样式.ContentMarginLeft = 6;
		抓柄焦点样式.ContentMarginRight = 6;
		抓柄焦点样式.ContentMarginTop = 6;
		抓柄焦点样式.ContentMarginBottom = 6;
		滑块.AddThemeStyleboxOverride("grabber_focus", 抓柄焦点样式);

		var 焦点轮廓样式 = new StyleBoxFlat();
		焦点轮廓样式.BgColor = Colors.Transparent;
		焦点轮廓样式.BorderWidthTop = 4;
		焦点轮廓样式.BorderWidthBottom = 4;
		焦点轮廓样式.BorderWidthLeft = 4;
		焦点轮廓样式.BorderWidthRight = 4;
		焦点轮廓样式.BorderColor = Color.FromHtml("#90ee90");
		焦点轮廓样式.ShadowColor = Color.FromHtml("#00ff00");
		焦点轮廓样式.ShadowSize = 14;
		焦点轮廓样式.ShadowOffset = Vector2.Zero;
		滑块.AddThemeStyleboxOverride("focus", 焦点轮廓样式);
	}

	private void 延迟刷新焦点样式()
	{
		if (背景音乐滑块 != null)
		{
			背景音乐滑块.ReleaseFocus();
			背景音乐滑块.GrabFocus();
			背景音乐滑块.ReleaseFocus();
		}
	}
}
