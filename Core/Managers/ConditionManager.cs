using Godot;
using System.Collections.Generic;

/// <summary>
/// 全局ConditionManager，负责存储和查询游戏中的各种条件（如剧情解锁、技能可用等）。
/// </summary>
public partial class ConditionManager : Node
{
	private static ConditionManager _实例;
	public static ConditionManager 实例 => _实例;

	private HashSet<string> 已满足条件 = new HashSet<string>();

	public override void _Ready()
	{
		if (_实例 == null)
		{
			_实例 = this;
			ProcessMode = ProcessModeEnum.Always;
		}
		else
		{
			QueueFree();
		}
	}

	/// <summary>检查指定条件是否已满足。</summary>
	public bool 检查条件(string 条件名称)
	{
		return 已满足条件.Contains(条件名称);
	}

	/// <summary>将指定条件标记为已满足。</summary>
	public void 设置条件满足(string 条件名称)
	{
		已满足条件.Add(条件名称);
	}

	/// <summary>移除指定条件（用于重置或撤销）。</summary>
	public void 移除条件(string 条件名称)
	{
		已满足条件.Remove(条件名称);
	}

	/// <summary>清空所有已满足的条件。</summary>
	public void 清空所有条件()
	{
		已满足条件.Clear();
	}

	/// <summary>获取当前所有已满足条件的数组副本。</summary>
	public string[] 获取所有已满足条件()
	{
		string[] 条件数组 = new string[已满足条件.Count];
		已满足条件.CopyTo(条件数组);
		return 条件数组;
	}
}
