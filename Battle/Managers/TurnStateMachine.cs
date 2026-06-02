using Godot;
using System;

/// <summary>
/// Controls the turn flow between player and monster, handling turn start/end logic.
/// </summary>
public partial class TurnStateMachine : Node
{
	public bool 战斗进行中 = false;
	public bool 玩家回合中 = true;

	private CardBattleManager _battleManager;
	private 战斗UI控制器 _uiController;
	private EnemyAIController _enemyAI;

	/// <summary>Initializes the state machine with required references.</summary>
	public void 初始化(CardBattleManager battleManager, 战斗UI控制器 uiController, EnemyAIController enemyAI)
	{
		_battleManager = battleManager;
		_uiController = uiController;
		_enemyAI = enemyAI;
	}

	/// <summary>Checks if it's the player's turn.</summary>
	public bool 是玩家回合() => 玩家回合中;
	
	/// <summary>Checks if it's the monster's turn.</summary>
	public bool 是怪物回合() => !玩家回合中;

	/// <summary>Starts the player's turn, optionally drawing cards.</summary>
	public void 开始玩家回合(bool 是否抽牌 = true)
	{
		玩家回合中 = true;
		_battleManager.增加能量(1);
		_uiController.添加战斗日志($"回合开始，能量 +1，当前能量: {_battleManager.是否强化模式}");
		_battleManager.EmitSignal(CardBattleManager.SignalName.玩家回合开始);
		_uiController.添加战斗日志("玩家回合开始");
		_uiController.通知玩家卡牌禁用状态(false, _battleManager.GetParent() as 卡牌游戏主场景);
		效果_锋利匕首.重置回合标记(_battleManager.玩家单位);
		_uiController.更新UI(_battleManager.玩家单位, _battleManager.怪物单位);
		_battleManager.玩家单位.回合开始准备();
		_battleManager.处理装备回合开始效果();

		if (是否抽牌)
		{
			_uiController.添加战斗日志("玩家回合开始，准备抽3张牌");
			_battleManager.玩家抽三张牌();
		}
		else if (_battleManager.玩家单位.手牌.Count == 0)
		{
			_uiController.添加战斗日志("玩家没有手牌，自动结束回合");
			var 计时器 = new Timer();
			计时器.OneShot = true;
			计时器.WaitTime = 1.0f;
			AddChild(计时器);
			计时器.Timeout += () => { 计时器.QueueFree(); 结束玩家回合(); };
		}
		(_battleManager.GetParent() as 卡牌游戏主场景)?.玩家HandManager?.刷新所有卡牌高光();
	}

	/// <summary>Ends the player's turn and starts the monster's turn.</summary>
	public void 结束玩家回合()
	{
		if (!战斗进行中 || !玩家回合中) return;
		if (_battleManager.是否强化模式)
			_battleManager.关闭强化模式();
		_uiController.添加战斗日志("玩家回合结束");
		开始怪物回合(true);
	}

	private void 开始怪物回合(bool 是否抽牌 = true)
	{
		玩家回合中 = false;
		_battleManager.EmitSignal(CardBattleManager.SignalName.怪物回合开始);
		_uiController.添加战斗日志("怪物回合开始");
		_uiController.通知玩家卡牌禁用状态(true, _battleManager.GetParent() as 卡牌游戏主场景);
		效果_锋利匕首.重置回合标记(_battleManager.怪物单位);
		_battleManager.怪物单位.回合开始准备();
		_uiController.更新UI(_battleManager.玩家单位, _battleManager.怪物单位);

		if (是否抽牌)
		{
			_uiController.添加战斗日志("怪物回合开始，准备抽2张牌");
			_enemyAI.开始怪物抽牌序列();
		}
		else
		{
			if (_battleManager.怪物单位.手牌.Count == 0)
			{
				_uiController.添加战斗日志("怪物没有手牌，跳过回合");
				var 计时器 = new Timer();
				计时器.OneShot = true;
				计时器.WaitTime = 1.0f;
				AddChild(计时器);
				计时器.Timeout += () => { 计时器.QueueFree(); 结束怪物回合(); };
				return;
			}
			var AI计时器 = new Timer();
			AI计时器.OneShot = true;
			AI计时器.WaitTime = 0.5f;
			AddChild(AI计时器);
			AI计时器.Timeout += () => { AI计时器.QueueFree(); _enemyAI.执行怪物AI(); };
		}
	}

	/// <summary>Ends the monster's turn and starts the player's turn.</summary>
	public void 结束怪物回合(bool 是否抽牌 = true)
	{
		if (!战斗进行中 || 玩家回合中) return;
		_uiController.添加战斗日志("怪物回合结束");
		if (_battleManager.敌人HandManager != null)
			_battleManager.敌人HandManager.清空手牌();
		开始玩家回合(是否抽牌);
	}
}
