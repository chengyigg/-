using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manages the visual display, positioning, and animation of cards in hand.
/// </summary>
public partial class HandManager : Control
{
	[Export] private float 手牌区域宽度 = 400f;
	[Export] public Control 手牌容器;
	[Export] public PackedScene CardUIPrefab;
	[Export] public Control 卡牌生成位置;
	[Export] private Control 弃牌堆位置;
	[Export] public Control 卡组位置;
	
	public List<CardUI> 当前CardUI列表 = new();
	private CardBattleUnit 所属单位;
	private CardBattleManager _战斗管理器;
	private bool 队列处理暂停 = false;
	
	[Signal] public delegate void 卡牌被使用EventHandler(GodotObject 卡牌);
	
	private Queue<CardInstance> 待添加卡牌队列 = new Queue<CardInstance>();
	private bool 正在添加卡牌 = false;
	private float 上次排列时间 = 0f;
	private bool 正在排列 = false;
	
	public override void _Ready() { }
	
	/// <summary>Initializes the hand manager with a combat unit and battle manager.</summary>
	public void 初始化(CardBattleUnit 单位, CardBattleManager 战斗管理器)
	{
		所属单位 = 单位;
		_战斗管理器 = 战斗管理器;
		if (所属单位 == null)
		{
		}
		if (手牌容器 == null)
		{
		}
		if (CardUIPrefab == null)
		{
		}
		更新手牌显示();
	}
	
	/// <summary>Pauses the card addition queue processing.</summary>
	public void 暂停队列处理() => 队列处理暂停 = true;
	
	/// <summary>Refreshes the highlight state of all cards based on usability.</summary>
	public void 刷新所有卡牌高光()
	{
		if (_战斗管理器 == null) return;
		if (所属单位 == null) return;
		foreach (var CardUI in 当前CardUI列表)
		{
			bool 可用 = _战斗管理器.卡牌是否可用(CardUI.获取CardData());
			CardUI.设置高光启用(可用);
		}
	}
	
	/// <summary>Resumes card addition queue processing.</summary>
	public void 恢复队列处理() => 队列处理暂停 = false;
	
	/// <summary>Adds a card to hand via a queue, with optional animation.</summary>
	public void 添加卡牌到手中排队(CardInstance 卡牌, bool 播放动画 = true)
	{
		待添加卡牌队列.Enqueue(卡牌);
		if (!正在添加卡牌)
			开始处理卡牌队列(播放动画);
	}
	
	private async void 开始处理卡牌队列(bool 播放动画)
	{
		正在添加卡牌 = true;
		int 添加前的卡牌数量 = 当前CardUI列表.Count;
		int 总共卡牌数量 = 添加前的卡牌数量 + 待添加卡牌队列.Count;
		
		Vector2[] 所有目标位置 = new Vector2[总共卡牌数量];
		for (int i = 0; i < 总共卡牌数量; i++)
			所有目标位置[i] = 计算单张卡牌位置(i, 总共卡牌数量);
		
		for (int i = 0; i < 添加前的卡牌数量; i++)
		{
			var 现有卡牌 = 当前CardUI列表[i];
			var 新位置 = 所有目标位置[i];
			if (!现有卡牌.是拖拽状态())
				现有卡牌.执行平滑移动动画(新位置, 0f, i, 0.3f, 0f);
			现有卡牌.更新原始位置(新位置);
		}
		
		int 新牌索引 = 0;
		while (待添加卡牌队列.Count > 0)
		{
			var 当前卡牌 = 待添加卡牌队列.Dequeue();
			bool 已存在 = false;
			foreach (var 已有CardUI in 当前CardUI列表)
			{
				if (已有CardUI.获取CardData() == 当前卡牌)
				{
					已存在 = true;
					break;
				}
			}
			if (已存在) continue;
			
			var CardUI实例 = CardUIPrefab.Instantiate<CardUI>();
			手牌容器.AddChild(CardUI实例);
			CardUI实例.初始化(当前卡牌);
			
			int 新牌总索引 = 添加前的卡牌数量 + 新牌索引;
			Vector2 新牌目标位置 = 所有目标位置[新牌总索引];
			当前CardUI列表.Add(CardUI实例);
			CardUI实例.更新原始位置(新牌目标位置);
			
			CardUI实例.卡牌开始拖拽 += (CardUI) => 处理卡牌拖拽开始((CardUI)CardUI);
			CardUI实例.卡牌结束拖拽 += (CardUI, 位置) => 处理卡牌拖拽结束((CardUI)CardUI, 位置);
			CardUI实例.卡牌被使用 += (CardInstance) => 处理卡牌被使用(CardInstance);
			
			if (播放动画 && 卡组位置 != null)
			{
				CardUI实例.Position = 卡组位置.Position;
				CardUI实例.Scale = new Vector2(0.1f, 0.1f);
				CardUI实例.Rotation = Mathf.Pi;
				CardUI实例.Modulate = new Color(1, 1, 1, 0.8f);
				CardUI实例.ZIndex = 1000;
				CardUI实例.执行流畅入场动画(新牌目标位置, 卡组位置.Position, null);
			}
			else
			{
				CardUI实例.Position = 新牌目标位置;
				CardUI实例.Scale = CardUI实例.获取原始缩放();
			}
			新牌索引++;
			if (待添加卡牌队列.Count > 0)
				await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
		}
		
		正在添加卡牌 = false;
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		if (IsInsideTree())
			执行流畅排列动画();
	}
	
	private async Task 播放单张卡牌入场动画(CardUI CardUI, int 卡牌索引)
	{
		var 目标位置 = 计算单张卡牌位置(卡牌索引);
		CardUI.执行流畅入场动画(目标位置, 卡组位置.Position, null);
		await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
	}
	
	/// <summary>Adds a card to hand directly, with optional animation.</summary>
	public void 添加卡牌到手中(CardInstance 卡牌, bool 播放动画 = true)
	{
		foreach (var 已有CardUI in 当前CardUI列表)
		{
			if (已有CardUI.获取CardData() == 卡牌)
				return;
		}
		
		var CardUI实例 = CardUIPrefab.Instantiate<CardUI>();
		手牌容器.AddChild(CardUI实例);
		CardUI实例.初始化(卡牌);
		当前CardUI列表.Add(CardUI实例);
		
		if (播放动画 && 卡组位置 != null)
		{
			CardUI实例.Position = 卡组位置.Position;
			CardUI实例.Scale = new Vector2(0.1f, 0.1f);
			CardUI实例.Rotation = Mathf.Pi;
			CardUI实例.Modulate = new Color(1, 1, 1, 0.8f);
			var 最终位置 = 计算单张卡牌位置(当前CardUI列表.Count - 1);
			bool 正在播放入场动画 = false;
			if (!正在播放入场动画)
			{
				正在播放入场动画 = true;
				CardUI实例.执行流畅入场动画(最终位置, 卡组位置.Position, () => {
					正在播放入场动画 = false;
					检查并执行最终排列();
				});
			}
			Callable.From(() => 刷新所有卡牌高光()).CallDeferred();
		}
		else
		{
			排列卡牌();
			刷新所有卡牌高光();
		}
		
		CardUI实例.卡牌开始拖拽 += (CardUI) => 处理卡牌拖拽开始((CardUI)CardUI);
		CardUI实例.卡牌结束拖拽 += (CardUI, 位置) => 处理卡牌拖拽结束((CardUI)CardUI, 位置);
		CardUI实例.卡牌被使用 += (CardInstance) => 处理卡牌被使用(CardInstance);
	}
	
	/// <summary>Calculates the position for a card at given index based on hand size.</summary>
	public Vector2 计算单张卡牌位置(int 索引, int 用于计算的总卡牌数量)
	{
		float 容器宽度 = 手牌区域宽度;
		float 卡牌宽度 = 120f;
		if (用于计算的总卡牌数量 == 0) return Vector2.Zero;
		float 基础间距 = 计算动态间距(用于计算的总卡牌数量);
		float 总宽度 = 用于计算的总卡牌数量 * 卡牌宽度 + Mathf.Max(0, 用于计算的总卡牌数量 - 1) * 基础间距;
		if (总宽度 > 容器宽度 * 0.9f && 用于计算的总卡牌数量 > 1)
		{
			总宽度 = 容器宽度 * 0.9f;
			基础间距 = (总宽度 - 用于计算的总卡牌数量 * 卡牌宽度) / Mathf.Max(1, 用于计算的总卡牌数量 - 1);
			基础间距 = Mathf.Max(5f, 基础间距);
			if (用于计算的总卡牌数量 * 卡牌宽度 > 容器宽度)
			{
				float 重叠量 = (用于计算的总卡牌数量 * 卡牌宽度 - 容器宽度) / Mathf.Max(1, 用于计算的总卡牌数量 - 1);
				基础间距 = -重叠量;
				总宽度 = 容器宽度;
			}
		}
		float 起始位置 = (容器宽度 - 总宽度) / 2f;
		float x = 起始位置 + 索引 * (卡牌宽度 + 基础间距);
		float y = 100f;
		return new Vector2(x, y);
	}
	
	private Vector2 计算单张卡牌位置(int 索引) => 计算单张卡牌位置(索引, 当前CardUI列表.Count);
	
	private void 检查并执行最终排列()
	{
		float 当前时间 = (float)Time.GetTicksMsec() / 1000f;
		if (当前时间 - 上次排列时间 > 0.5f)
		{
			上次排列时间 = 当前时间;
			var 计时器 = new Timer();
			计时器.OneShot = true;
			计时器.WaitTime = 0.1f;
			AddChild(计时器);
			计时器.Timeout += () => {
				计时器.QueueFree();
				if (IsInsideTree())
					执行流畅排列动画();
			};
			计时器.Start();
		}
	}
	
	private void 处理移除卡牌后的排列() => 执行流畅排列动画();
	
	/// <summary>Performs a smooth repositioning animation for all cards in hand.</summary>
	public void 执行流畅排列动画()
	{
		if (正在排列) return;
		正在排列 = true;
		Callable.From(() => {
			int 卡牌数量 = 当前CardUI列表.Count;
			if (卡牌数量 == 0) { 正在排列 = false; return; }
			float 容器宽度 = 手牌区域宽度;
			float 卡牌宽度 = 120f;
			float 基础间距 = 计算动态间距(卡牌数量);
			float 总宽度 = 卡牌数量 * 卡牌宽度 + (卡牌数量 - 1) * 基础间距;
			if (总宽度 > 容器宽度 && 卡牌数量 > 1)
			{
				基础间距 = 5f;
				总宽度 = 卡牌数量 * 卡牌宽度 + (卡牌数量 - 1) * 基础间距;
			}
			float 起始位置 = (容器宽度 - 总宽度) / 2f;
			float y = 100f;
			for (int i = 0; i < 卡牌数量; i++)
			{
				var 卡牌 = 当前CardUI列表[i];
				float x = 起始位置 + i * (卡牌宽度 + 基础间距);
				var 目标位置 = new Vector2(x, y);
				卡牌.更新原始位置(目标位置);
				if (!卡牌.是拖拽状态())
					卡牌.执行平滑移动动画(目标位置, 0f, i, 0.3f, Mathf.Abs(i - (卡牌数量-1)/2f)*0.02f);
			}
			正在排列 = false;
		}).CallDeferred();
	}
	
	private void 计算动态弧线参数(int 卡牌数量, out float 半径, out float 总角度)
	{
		半径 = 300f;
		总角度 = Mathf.Pi * 0.6f;
		if (卡牌数量 <= 1)
		{
			半径 = 0f;
			总角度 = 0f;
		}
		else if (卡牌数量 == 2)
		{
			半径 = 200f;
			总角度 = Mathf.Pi * 0.3f;
		}
		else if (卡牌数量 == 3)
		{
			半径 = 250f;
			总角度 = Mathf.Pi * 0.45f;
		}
		else if (卡牌数量 == 4)
		{
			半径 = 300f;
			总角度 = Mathf.Pi * 0.6f;
		}
		else if (卡牌数量 == 5)
		{
			半径 = 350f;
			总角度 = Mathf.Pi * 0.7f;
		}
		else
		{
			半径 = Mathf.Min(400f + (卡牌数量 - 5) * 20f, 600f);
			总角度 = Mathf.Min(Mathf.Pi * 0.8f + (卡牌数量 - 5) * 0.1f, Mathf.Pi * 1.2f);
		}
	}
	
	/// <summary>Removes a CardUI from hand, optionally playing an animation.</summary>
	public void 移除CardUI(CardUI CardUI, bool 播放动画 = true)
	{
		var 卡牌索引 = 当前CardUI列表.IndexOf(CardUI);
		if (卡牌索引 >= 0)
		{
			当前CardUI列表.RemoveAt(卡牌索引);
			if (播放动画 && 弃牌堆位置 != null)
			{
				CardUI.执行弃置动画(弃牌堆位置.GlobalPosition, () => {
					CardUI.QueueFree();
					执行流畅排列动画();
				});
			}
			else
			{
				CardUI.QueueFree();
				执行流畅排列动画();
			}
		}
	}
	
	/// <summary>Adds a card to hand without animation.</summary>
	public void 添加卡牌(CardInstance 卡牌) => 添加卡牌到手中(卡牌, false);
	
	/// <summary>Removes a card instance from hand.</summary>
	public void 移除卡牌(CardInstance 卡牌)
	{
		var CardUI = 当前CardUI列表.Find(ui => ui.获取CardData() == 卡牌);
		if (CardUI != null) 移除CardUI(CardUI, false);
	}
	
	/// <summary>Handles drag end event for a card.</summary>
	public void 处理卡牌拖拽结束(CardUI CardUI, Vector2 释放位置) => 执行流畅排列动画();
	
	/// <summary>Handles card usage event, removes the card from hand.</summary>
	public void 处理卡牌被使用(GodotObject CardInstance对象)
	{
		CardInstance CardData = CardInstance对象 as CardInstance;
		if (CardData != null)
		{
			var CardUI = 当前CardUI列表.Find(ui => ui.获取CardData() == CardData);
			if (CardUI != null)
			{
				当前CardUI列表.Remove(CardUI);
				CardUI.QueueFree();
				执行流畅排列动画();
				EmitSignal(SignalName.卡牌被使用, CardInstance对象);
			}
			else
			{
			}
		}
	}
	
	/// <summary>Immediately removes a CardUI without animation.</summary>
	public void 立即移除CardUI(CardUI CardUI)
	{
		if (当前CardUI列表.Contains(CardUI))
			当前CardUI列表.Remove(CardUI);
		if (CardUI.IsInsideTree())
		{
			CardUI.Visible = false;
			CardUI.QueueFree();
		}
		执行流畅排列动画();
	}
	
	/// <summary>Refreshes the hand display from the combat unit's hand list.</summary>
	public void 更新手牌显示(bool 使用动画 = false)
	{
		foreach (var CardUI in 当前CardUI列表)
			CardUI.QueueFree();
		当前CardUI列表.Clear();
		if (所属单位 == null) 
		{
			return;
		}
		if (所属单位.手牌.Count == 0) return;
		
		if (使用动画 && 卡组位置 != null)
		{
			for (int i = 0; i < 所属单位.手牌.Count; i++)
			{
				if (所属单位.手牌[i] == null) continue;
				if (CardUIPrefab == null) continue;
				var CardUI实例 = CardUIPrefab.Instantiate<CardUI>();
				手牌容器.AddChild(CardUI实例);
				CardUI实例.初始化(所属单位.手牌[i]);
				CardUI实例.ZIndex = 1000;
				CardUI实例.Position = 卡组位置.Position;
				CardUI实例.Scale = new Vector2(0.1f, 0.1f);
				CardUI实例.Rotation = Mathf.Pi;
				CardUI实例.Modulate = new Color(1, 1, 1, 0.8f);
				当前CardUI列表.Add(CardUI实例);
				CardUI实例.卡牌开始拖拽 += (CardUI) => 处理卡牌拖拽开始((CardUI)CardUI);
				CardUI实例.卡牌结束拖拽 += (CardUI, 位置) => 处理卡牌拖拽结束((CardUI)CardUI, 位置);
				CardUI实例.卡牌被使用 += (CardInstance) => 处理卡牌被使用(CardInstance);
			}
			Callable.From(() => {
				for (int i = 0; i < 当前CardUI列表.Count; i++)
				{
					var CardUI = 当前CardUI列表[i];
					float 延迟 = i * 0.2f;
					Callable.From(() => {
						var 目标位置 = 计算单张卡牌位置(i);
						CardUI.执行流畅入场动画(目标位置, 卡组位置.Position, () => {
							if (i == 当前CardUI列表.Count - 1)
								执行流畅排列动画();
						});
					}).CallDeferred(延迟);
				}
			}).CallDeferred(0.5f);
		}
		else
		{
			for (int i = 0; i < 所属单位.手牌.Count; i++)
			{
				if (所属单位.手牌[i] == null) continue;
				if (CardUIPrefab == null) continue;
				var CardUI实例 = CardUIPrefab.Instantiate<CardUI>();
				手牌容器.AddChild(CardUI实例);
				CardUI实例.初始化(所属单位.手牌[i]);
				CardUI实例.卡牌开始拖拽 += (CardUI) => 处理卡牌拖拽开始((CardUI)CardUI);
				CardUI实例.卡牌结束拖拽 += (CardUI, 位置) => 处理卡牌拖拽结束((CardUI)CardUI, 位置);
				当前CardUI列表.Add(CardUI实例);
			}
			排列卡牌();
		}
		if (手牌容器 is Control control) control.QueueRedraw();
	}
	
	/// <summary>Handles drag start event for a card.</summary>
	public void 处理卡牌拖拽开始(CardUI CardUI) => CardUI.ZIndex = 100;
	
	private void 排列卡牌()
	{
		int 卡牌数量 = 当前CardUI列表.Count;
		if (卡牌数量 == 0) return;
		float 卡牌宽度 = 120f;
		float 容器宽度 = 手牌区域宽度;
		float 基础间距 = 计算动态间距(卡牌数量);
		float 总宽度 = 卡牌数量 * 卡牌宽度 + Mathf.Max(0, 卡牌数量 - 1) * 基础间距;
		if (总宽度 > 容器宽度 && 卡牌数量 > 1)
		{
			float 最小间距 = 5f;
			基础间距 = 最小间距;
			总宽度 = 卡牌数量 * 卡牌宽度 + (卡牌数量 - 1) * 基础间距;
		}
		float 起始位置 = (容器宽度 - 总宽度) / 2f;
		float y = 100f;
		for (int i = 0; i < 卡牌数量; i++)
		{
			var 卡牌 = 当前CardUI列表[i];
			float x = 起始位置 + i * (卡牌宽度 + 基础间距);
			var 新位置 = new Vector2(x, y);
			if (!卡牌.是拖拽状态())
				卡牌.Position = 新位置;
			卡牌.更新原始位置(新位置);
			卡牌.Rotation = 0f;
			卡牌.ZIndex = i;
			卡牌.Scale = 卡牌.获取原始缩放();
		}
	}
	
	/// <summary>Refreshes all card descriptions based on enhanced mode.</summary>
	public void 刷新所有卡牌描述(bool 是否强化)
	{
		foreach (var CardUI in 当前CardUI列表)
			CardUI.更新描述根据强化模式(是否强化);
	}
	
	private float 计算动态间距(int 卡牌数量)
	{
		float 卡牌宽度 = 120f;
		if (卡牌数量 <= 1) return 0f;
		float 理想总宽度 = 卡牌数量 * 卡牌宽度;
		float 可用宽度 = 手牌区域宽度 - 理想总宽度;
		if (可用宽度 <= 0) return 5f;
		float 基础间距 = 可用宽度 / (卡牌数量 - 1);
		if (卡牌数量 <= 3) return Mathf.Min(100f, 基础间距);
		if (卡牌数量 <= 6) return Mathf.Clamp(基础间距, 10f, 70f);
		return Mathf.Clamp(基础间距, 5f, 30f);
	}
}
