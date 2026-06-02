using Godot;
using 你的项目.Scripts.资源;

/// <summary>
/// Resource representing a story sequence, containing camera animation, avatar animation, and optional dialogue.
/// </summary>
[GlobalClass]
public partial class StorySequenceResource : Resource
{
	[Export] public CameraAnimationResource 摄像头动画配置 { get; set; }
	
	// Legacy field (single animation) kept for backward compatibility
	[Export] public string 替身动画名称 { get; set; } = "";
	[Export] public NodePath 替身节点路径 { get; set; }
	[Export] public bool 启用退化 { get; set; } = false;
	[Export] public bool 是否为一次性动画 { get; set; }
	[Export] public NodePath 一次性动画节点路径 { get; set; }
	[Export] public bool 动画结束后强制空闲 { get; set; } = true;
	[Export] public bool 是否为替身动画 { get; set; } = true;
	
	/// <summary>List of avatar animations (if not empty, takes precedence over legacy fields).</summary>
	[Export] public 替身动画条目[] 动画列表 { get; set; } = new 替身动画条目[0];
	
	[Export] public 对话序列 对话序列 { get; set; }
}
