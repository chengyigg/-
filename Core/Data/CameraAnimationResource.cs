using Godot;
using System;

namespace 你的项目.Scripts.资源
{
	/// <summary>
	/// Resource defining a sequence of camera movements and pauses for cinematic effects.
	/// </summary>
	[GlobalClass]
	public partial class CameraAnimationResource : Resource
	{
		[Export] public bool 启用动画 { get; set; } = false;
		
		/// <summary>Animation sequence steps.</summary>
		[Export] public 动画序列[] 序列 { get; set; } = new 动画序列[0];
		
		/// <summary>Total animation duration in seconds.</summary>
		public float 总时长
		{
			get
			{
				float 时长 = 0f;
				foreach (var 动画 in 序列)
				{
					时长 += 动画.移动时长 + 动画.停留时间;
				}
				return 时长;
			}
		}
	}
}
