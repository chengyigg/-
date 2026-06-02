using Godot;
using System;

namespace 你的项目.Scripts.资源
{
	/// <summary>
	/// Represents a save slot data, storing player progress, position, items, conditions, and animation states.
	/// </summary>
	[GlobalClass]
	public partial class SaveDataResource : Resource
	{
		[Export] public Godot.Collections.Dictionary<string, bool> 已触发动画记录 { get; set; } = new Godot.Collections.Dictionary<string, bool>();
		[Export] public string 存档场景路径 { get; set; } = "";
		[Export] public Vector2 存档位置 { get; set; } = Vector2.Zero;
		[Export] public string 存档点名称 { get; set; } = "";
		[Export] public string 玩家名字 { get; set; } = "玩家";
		[Export] public int 金币数量 { get; set; } = 0;
		[Export] public string 存档时间字符串 { get; set; } = "";
		[Export] public Godot.Collections.Dictionary<string, Vector2> 场景存档位置 { get; set; } = new Godot.Collections.Dictionary<string, Vector2>();
		[Export] public Godot.Collections.Dictionary<string, string> 场景存档点 { get; set; } = new Godot.Collections.Dictionary<string, string>();
		[Export] public Godot.Collections.Dictionary<string, bool> 传送后对话触发记录 { get; set; } = new Godot.Collections.Dictionary<string, bool>();
		[Export] public Godot.Collections.Dictionary<string, bool> 看法解锁状态 { get; set; } = new Godot.Collections.Dictionary<string, bool>();
		[Export] public Godot.Collections.Array<string> 跟随者预制体路径列表 { get; set; } = new Godot.Collections.Array<string>();
		[Export] public Godot.Collections.Dictionary<string, bool> 已改变对话序列 { get; set; } = new Godot.Collections.Dictionary<string, bool>();
		[Export] public Godot.Collections.Dictionary<string, bool> 条件状态记录 { get; set; } = new Godot.Collections.Dictionary<string, bool>();
		[Export] public Godot.Collections.Array<string> 已购买商品路径列表 { get; set; } = new Godot.Collections.Array<string>();
		[Export] public Godot.Collections.Array<string> 卡牌路径列表 { get; set; } = new Godot.Collections.Array<string>();
		[Export] public Godot.Collections.Dictionary<string, bool> 已触发剧情记录 { get; set; } = new Godot.Collections.Dictionary<string, bool>();

		/// <summary>Saves the follower list to the save data.</summary>
		public void 保存跟随者列表(Godot.Collections.Array<string> 路径列表)
		{
			跟随者预制体路径列表 = 路径列表;
		}

		/// <summary>Retrieves the follower list from the save data.</summary>
		public Godot.Collections.Array<string> 获取跟随者列表()
		{
			return 跟随者预制体路径列表;
		}

		/// <summary>Records a story trigger state.</summary>
		public void 记录剧情触发(string 剧情资源路径, bool 已触发)
		{
			已触发剧情记录[剧情资源路径] = 已触发;
		}

		/// <summary>Checks if a story has been triggered.</summary>
		public bool 获取剧情触发状态(string 剧情资源路径)
		{
			return 已触发剧情记录.ContainsKey(剧情资源路径) && 已触发剧情记录[剧情资源路径];
		}

		/// <summary>Checks if a dialogue sequence has been changed.</summary>
		public bool 获取对话改变状态(string 序列路径)
		{
			return 已改变对话序列.ContainsKey(序列路径) && 已改变对话序列[序列路径];
		}

		/// <summary>Records an animation trigger state.</summary>
		public void 记录动画触发(string 唯一标识, bool 已触发)
		{
			已触发动画记录[唯一标识] = 已触发;
		}

		/// <summary>Checks if an animation has been triggered.</summary>
		public bool 获取动画触发状态(string 唯一标识)
		{
			return 已触发动画记录.ContainsKey(唯一标识) && 已触发动画记录[唯一标识];
		}

		/// <summary>Sets the changed state of a dialogue sequence.</summary>
		public void 设置对话改变状态(string 序列路径, bool 已改变)
		{
			已改变对话序列[序列路径] = 已改变;
		}

		/// <summary>Gets or sets the save time as DateTime.</summary>
		public DateTime 存档时间
		{
			get
			{
				if (string.IsNullOrEmpty(存档时间字符串))
					return DateTime.Now;
				if (DateTime.TryParse(存档时间字符串, out DateTime 结果))
					return 结果;
				return DateTime.Now;
			}
			set
			{
				存档时间字符串 = value.ToString("yyyy-MM-dd HH:mm:ss");
			}
		}

		/// <summary>Clears all post-teleport dialogue records.</summary>
		public void 清空所有对话记录()
		{
			传送后对话触发记录.Clear();
		}

		/// <summary>Checks if a save exists.</summary>
		public bool 是否有存档()
		{
			return !string.IsNullOrEmpty(存档场景路径);
		}

		/// <summary>Returns formatted save time string.</summary>
		public string 获取格式化时间()
		{
			return 存档时间.ToString("yyyy-MM-dd HH:mm");
		}

		/// <summary>Gets the saved position for a specific scene.</summary>
		public Vector2 获取场景存档位置(string 场景路径)
		{
			if (场景存档位置.ContainsKey(场景路径))
				return 场景存档位置[场景路径];
			return Vector2.Zero;
		}

		/// <summary>Sets the saved position and save point for a scene.</summary>
		public void 设置场景存档位置(string 场景路径, Vector2 位置, string 存档点名称 = "")
		{
			场景存档位置[场景路径] = 位置;
			if (!string.IsNullOrEmpty(存档点名称))
				场景存档点[场景路径] = 存档点名称;
			
			存档场景路径 = 场景路径;
			存档位置 = 位置;
			this.存档点名称 = 存档点名称;
		}

		/// <summary>Checks if a scene has a saved position.</summary>
		public bool 场景是否有存档(string 场景路径)
		{
			return 场景存档位置.ContainsKey(场景路径);
		}

		/// <summary>Gets the trigger state of a post-teleport dialogue.</summary>
		public bool 获取传送后对话触发状态(string 对话标识)
		{
			return 传送后对话触发记录.ContainsKey(对话标识) && 传送后对话触发记录[对话标识];
		}

		/// <summary>Sets the trigger state of a post-teleport dialogue.</summary>
		public void 设置传送后对话触发状态(string 对话标识, bool 已触发)
		{
			传送后对话触发记录[对话标识] = 已触发;
		}

		/// <summary>Removes a post-teleport dialogue trigger record.</summary>
		public void 移除传送后对话触发记录(string 对话标识)
		{
			if (传送后对话触发记录.ContainsKey(对话标识))
				传送后对话触发记录.Remove(对话标识);
		}

		/// <summary>Clears all post-teleport dialogue trigger records.</summary>
		public void 清空传送后对话触发记录()
		{
			传送后对话触发记录.Clear();
		}

		/// <summary>Records a memory unlock state.</summary>
		public void 记录看法解锁状态(string 角色ID, string 主题, bool 已解锁)
		{
			string 唯一标识 = $"{角色ID}|{主题}";
			看法解锁状态[唯一标识] = 已解锁;
		}

		/// <summary>Gets a memory unlock state.</summary>
		public bool 获取看法解锁状态(string 角色ID, string 主题)
		{
			string 唯一标识 = $"{角色ID}|{主题}";
			return 看法解锁状态.ContainsKey(唯一标识) && 看法解锁状态[唯一标识];
		}

		/// <summary>Applies all memory unlock states to the memory system.</summary>
		public void 应用看法解锁状态()
		{
			if (回忆系统管理器.实例 == null) return;

			foreach (var 键值对 in 看法解锁状态)
			{
				string 唯一标识 = 键值对.Key;
				bool 已解锁 = 键值对.Value;
				string[] 部分 = 唯一标识.Split('|');
				if (部分.Length == 2)
				{
					string 角色名称 = 部分[0];
					string 主题 = 部分[1];
					var 角色数据 = 回忆系统管理器.实例.获取角色看法(角色名称);
					if (角色数据 != null)
					{
						foreach (看法条目 看法 in 角色数据.看法列表)
						{
							if (看法.主题 == 主题)
							{
								看法.已解锁 = 已解锁;
								break;
							}
						}
					}
				}
			}
		}

		/// <summary>Records a condition state.</summary>
		public void 记录条件状态(string 条件名称, bool 已满足)
		{
			条件状态记录[条件名称] = 已满足;
		}

		/// <summary>Gets a condition state.</summary>
		public bool 获取条件状态(string 条件名称)
		{
			return 条件状态记录.ContainsKey(条件名称) && 条件状态记录[条件名称];
		}

		/// <summary>Applies all condition states to the condition manager.</summary>
		public void 应用条件状态()
		{
			if (ConditionManager.实例 == null) return;

			foreach (var 键值对 in 条件状态记录)
			{
				if (键值对.Value)
					ConditionManager.实例.设置条件满足(键值对.Key);
			}
		}

		public SaveDataResource()
		{
			存档时间 = DateTime.Now;
		}
	}
}
