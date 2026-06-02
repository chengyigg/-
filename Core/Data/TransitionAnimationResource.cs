using Godot;
using System;

namespace 你的项目.Scripts.资源
{
	/// <summary>
	/// Resource defining a scene transition animation with fade, text display, and optional progress bar.
	/// </summary>
	[GlobalClass]
	public partial class TransitionAnimationResource : Resource
	{
		[Export] public string 动画名称 { get; set; } = "默认转场";
		
		[Export(PropertyHint.MultilineText)] 
		public string 显示文本 { get; set; } = "加载中...";
		
		[Export] public Color 文本颜色 { get; set; } = Colors.White;
		
		[Export] public Color 背景颜色 { get; set; } = new Color(0, 0, 0, 1);
		
		[Export] public float 淡入时间 { get; set; } = 0.5f;
		
		[Export] public float 显示时间 { get; set; } = 1.0f;
		
		[Export] public float 淡出时间 { get; set; } = 0.5f;
		
		[Export] public string 自定义字体路径 { get; set; } = "";
		
		[Export] public FontFile 自定义字体 { get; set; }
		
		[Export] public int 字体大小 { get; set; } = 36;
		
		[Export] public bool 显示加载进度 { get; set; } = false;

		/// <summary>Creates a default transition animation resource.</summary>
		public static TransitionAnimationResource 创建默认()
		{
			var 资源 = new TransitionAnimationResource();
			return 资源;
		}

		/// <summary>Creates a custom transition animation resource.</summary>
		public static TransitionAnimationResource 创建自定义(string 文本, Color 文本色, Color 背景色, float 淡入 = 0.5f, float 显示 = 1.0f, float 淡出 = 0.5f)
		{
			var 资源 = new TransitionAnimationResource();
			资源.显示文本 = 文本;
			资源.文本颜色 = 文本色;
			资源.背景颜色 = 背景色;
			资源.淡入时间 = 淡入;
			资源.显示时间 = 显示;
			资源.淡出时间 = 淡出;
			return 资源;
		}
	}
}
