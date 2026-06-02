using Godot;
using 你的项目.Scripts.资源;
using System;
using System.Collections.Generic;
using 你的项目.Scripts.全局;

namespace 你的项目.Scripts.管理器
{
	public partial class SaveManager  : Node
	{
		private static SaveManager  _实例;
		public static SaveManager  实例 => _实例;
		
		private const int 存档数量 = 7;
		private 存档数据资源[] _存档数组 = new 存档数据资源[存档数量];
		
		public override void _Ready()
		{
			if (_实例 == null)
			{
				_实例 = this;
				ProcessMode = ProcessModeEnum.Always;
				加载所有存档();
			}
			else
			{
				QueueFree();
			}
		}
	
		public void 保存游戏(int 存档位, string 场景路径, Vector2 位置, string 存档点名称 = "")
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
			{
				GD.PrintErr($"SaveManager : 存档位 {存档位} 无效！");
				return;
			}
			
			if (场景路径.Contains("黑屏过渡场景") || 场景路径.Contains("过渡场景"))
			{
				GD.PrintErr($"SaveManager : 不允许在过渡场景中保存游戏: {场景路径}");
				return;
			}
			
			if (_存档数组[存档位] == null)
				_存档数组[存档位] = new 存档数据资源();
			
			var 存档数据 = _存档数组[存档位];
			存档数据.设置场景存档位置(场景路径, 位置, 存档点名称);
			存档数据.存档时间 = DateTime.Now;
			
			if (玩家数据管理器.实例 != null)
				存档数据.玩家名字 = 玩家数据管理器.实例.玩家名字;
			
			保存看法解锁状态(存档数据);
			保存条件状态(存档数据);
			
			存档数据.金币数量 = 金币管理器.实例?.金币数量 ?? 0;
			
			if (玩家卡组管理器.实例 != null)
			{
				存档数据.卡牌路径列表 = 玩家卡组管理器.实例.获取卡牌路径列表();
				var 商店场景 = GetTree().CurrentScene;
				if (商店场景 != null && 商店场景.SceneFilePath.Contains("商店"))
				{
					var 商店管理器 = 商店场景.GetNodeOrNull<商店管理器>("商店管理器");
					if (商店管理器 != null)
					{
						存档数据.已购买商品路径列表.Clear();
						foreach (var 商品 in 商店管理器.所有商品)
						{
							if (商品.购买按钮 != null && 商品.购买按钮.Disabled)
							{
								var 卡牌路径 = 商品.获取卡牌数据()?.ResourcePath;
								if (!string.IsNullOrEmpty(卡牌路径))
									存档数据.已购买商品路径列表.Add(卡牌路径);
							}
						}
					}
				}
			}
			
			if (玩家管理器.实例?.当前玩家 != null)
			{
				var 路径列表 = 玩家管理器.实例.当前玩家.获取跟随者预制体路径列表();
				存档数据.保存跟随者列表(路径列表);
			}
			
			string 存档路径 = 获取存档路径(存档位);
			var 错误 = ResourceSaver.Save(存档数据, 存档路径);
			if (错误 == Error.Ok)
			{
				GD.Print($"SaveManager : 游戏已保存到存档位 {存档位}");
			}
			else
			{
				GD.PrintErr($"SaveManager : 保存失败，错误码 {错误}");
			}
		}
		
		public void 应用金币状态(int 存档位)
		{
			var 存档数据 = _存档数组[存档位];
			if (存档数据 != null && 金币管理器.实例 != null)
				金币管理器.实例.金币数量 = 存档数据.金币数量;
		}

		public void 应用卡组状态(int 存档位)
		{
			var 存档数据 = _存档数组[存档位];
			if (存档数据 != null && 玩家卡组管理器.实例 != null)
				玩家卡组管理器.实例.从路径列表重建卡组(存档数据.卡牌路径列表);
		}

		private void 保存看法解锁状态(存档数据资源 存档数据)
		{
			if (回忆系统管理器.实例 == null)
			{
				GD.PrintErr("SaveManager : 回忆系统管理器实例为空，无法保存看法解锁状态");
				return;
			}

			string[] 所有角色ID = 回忆系统管理器.实例.获取所有角色ID();
			foreach (string 角色ID in 所有角色ID)
			{
				var 角色数据 = 回忆系统管理器.实例.获取角色看法(角色ID);
				if (角色数据 != null)
				{
					foreach (看法条目 看法 in 角色数据.看法列表)
					{
						if (看法 != null)
							存档数据.记录看法解锁状态(角色数据.角色名称, 看法.主题, 看法.已解锁);
					}
				}
			}
		}

		private void 保存条件状态(存档数据资源 存档数据)
		{
			if (ConditionManager.实例 == null)
			{
				GD.PrintErr("SaveManager : ConditionManager实例为空，无法保存条件状态");
				return;
			}

			string[] 所有已满足条件 = ConditionManager.实例.获取所有已满足条件();
			foreach (string 条件 in 所有已满足条件)
			{
				bool 状态 = ConditionManager.实例.检查条件(条件);
				存档数据.记录条件状态(条件, 状态);
			}
		}
		
		public void 应用看法解锁状态(int 存档位)
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
			{
				GD.PrintErr($"SaveManager : 存档位 {存档位} 无效！");
				return;
			}

			var 存档数据 = _存档数组[存档位];
			if (存档数据 == null)
			{
				GD.PrintErr($"SaveManager : 存档位 {存档位} 无存档数据！");
				return;
			}
			
			存档数据.应用看法解锁状态();
		}

		public void 应用条件状态(int 存档位)
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
			{
				GD.PrintErr($"SaveManager : 存档位 {存档位} 无效！");
				return;
			}

			var 存档数据 = _存档数组[存档位];
			if (存档数据 == null)
			{
				GD.PrintErr($"SaveManager : 存档位 {存档位} 无存档数据！");
				return;
			}
			
			存档数据.应用条件状态();
		}
		
		public 存档数据资源 获取场景存档数据(int 存档位, string 场景路径)
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
				return null;
			var 存档数据 = _存档数组[存档位];
			return (存档数据 != null && 存档数据.场景是否有存档(场景路径)) ? 存档数据 : null;
		}
		
		public bool 场景是否有存档(int 存档位, string 场景路径)
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
				return false;
			return _存档数组[存档位]?.场景是否有存档(场景路径) ?? false;
		}
		
		public 存档数据资源 获取存档数据(int 存档位)
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
				return null;
			return _存档数组[存档位];
		}
		
		public 存档数据资源[] 获取所有存档数据() => _存档数组;
		
		public bool 是否有存档(int 存档位)
		{
			if (存档位 < 0 || 存档位 >= 存档数量)
				return false;
			return _存档数组[存档位]?.是否有存档() ?? false;
		}
		
		public bool 是否有任何存档()
		{
			for (int i = 0; i < 存档数量; i++)
			{
				if (是否有存档(i))
					return true;
			}
			return false;
		}
		
		public void 同步玩家名字()
		{
			if (玩家数据管理器.实例 != null)
			{
				for (int i = 0; i < _存档数组.Length; i++)
				{
					if (_存档数组[i] != null && _存档数组[i].是否有存档())
					{
						玩家数据管理器.实例.玩家名字 = _存档数组[i].玩家名字;
						break;
					}
				}
			}
		}
		
		private void 加载所有存档()
		{
			for (int i = 0; i < 存档数量; i++)
			{
				string 路径 = 获取存档路径(i);
				if (ResourceLoader.Exists(路径))
				{
					_存档数组[i] = ResourceLoader.Load<存档数据资源>(路径);
				}
				else
				{
					_存档数组[i] = null;
				}
			}
		}
		
		private string 获取存档路径(int 存档位) => $"user://游戏存档_{存档位}.res";
	}
}
