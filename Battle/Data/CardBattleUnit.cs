using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents a combat unit (player or monster) with deck, hand, draw pile, discard pile, and battle stats.
/// </summary>
[GlobalClass]
public partial class CardBattleUnit : GodotObject
{
	private int _生命值;
	private int _护盾值;
	private float _行动条 = 0f;
	private bool _正在行动 = false;
	
	private Godot.Collections.Array<CardInstance> _主卡组 = new();
	private System.Collections.Generic.Stack<CardInstance> _抽牌堆 = new();
	private Godot.Collections.Array<CardInstance> _弃牌堆 = new();
	private Godot.Collections.Array<CardInstance> _手牌 = new();
	
	private bool 已经重置过抽牌堆 = false;
	
	/// <summary>Current health points.</summary>
	public int 生命值 
	{ 
		get => _生命值; 
		set => _生命值 = value; 
	}
	
	/// <summary>Current shield amount.</summary>
	public int 护盾值 
	{ 
		get => _护盾值; 
		set => _护盾值 = value; 
	}
	
	/// <summary>Action bar progress (used for turn order).</summary>
	public float 行动条 
	{ 
		get => _行动条; 
		set => _行动条 = value; 
	}
	
	/// <summary>Whether the unit is currently acting.</summary>
	public bool 正在行动 
	{ 
		get => _正在行动; 
		set => _正在行动 = value; 
	}
	
	/// <summary>The main deck (original card list).</summary>
	public Godot.Collections.Array<CardInstance> 主卡组 => _主卡组;
	
	/// <summary>The draw pile stack.</summary>
	public System.Collections.Generic.Stack<CardInstance> 抽牌堆 => _抽牌堆;
	
	/// <summary>The discard pile array.</summary>
	public Godot.Collections.Array<CardInstance> 弃牌堆 => _弃牌堆;
	
	/// <summary>The hand array.</summary>
	public Godot.Collections.Array<CardInstance> 手牌 => _手牌;
	
	/// <summary>Maximum hand size.</summary>
	public int 手牌上限 { get; set; } = 10;
	
	/// <summary>Initializes the deck from a list of card data.</summary>
	public void 初始化卡组(Godot.Collections.Array<CardData> 卡组数据)
	{
		_主卡组.Clear();
		if (卡组数据 == null)
		{
			return;
		}
		foreach (var 数据 in 卡组数据)
		{
			if (数据 == null) continue;
			_主卡组.Add(new CardInstance(数据));
		}
		洗牌();
	}
	
	/// <summary>Shuffles the main deck into the draw pile.</summary>
	public void 洗牌()
	{
		if (_主卡组.Count == 0)
		{
			return;
		}
		var 随机 = new System.Random();
		var 临时列表 = new System.Collections.Generic.List<CardInstance>();
		foreach (var 卡牌 in _主卡组) 临时列表.Add(卡牌);
		for (int i = 临时列表.Count - 1; i > 0; i--)
		{
			int j = 随机.Next(i + 1);
			(临时列表[i], 临时列表[j]) = (临时列表[j], 临时列表[i]);
		}
		_抽牌堆.Clear();
		foreach (var 卡牌 in 临时列表)
		{
			卡牌.状态 = CardInstance.卡牌状态.在卡组中;
			_抽牌堆.Push(卡牌);
		}
	}
	
	/// <summary>Prepares the unit at the start of a turn (reshuffles discard pile into draw pile if needed).</summary>
	public void 回合开始准备()
	{
		if (_弃牌堆.Count > 0)
		{
			var 随机 = new System.Random();
			var 临时列表 = new System.Collections.Generic.List<CardInstance>();
			foreach (var 卡牌 in _弃牌堆)
			{
				if (卡牌 != null)
				{
					卡牌.状态 = CardInstance.卡牌状态.在卡组中;
					临时列表.Add(卡牌);
				}
			}
			var 当前抽牌堆列表 = new System.Collections.Generic.List<CardInstance>();
			while (_抽牌堆.Count > 0)
			{
				var 卡牌 = _抽牌堆.Pop();
				if (卡牌 != null) 当前抽牌堆列表.Add(卡牌);
			}
			临时列表.AddRange(当前抽牌堆列表);
			for (int i = 临时列表.Count - 1; i > 0; i--)
			{
				int j = 随机.Next(i + 1);
				(临时列表[i], 临时列表[j]) = (临时列表[j], 临时列表[i]);
			}
			_抽牌堆.Clear();
			_弃牌堆.Clear();
			foreach (var 卡牌 in 临时列表) _抽牌堆.Push(卡牌);
		}
	}
	
	/// <summary>Draws one card from the draw pile, returns null if no card available.</summary>
	public CardInstance 抽牌()
	{
		if (_抽牌堆.Count == 0 && _弃牌堆.Count > 0)
			回合开始准备();
		
		if (_抽牌堆.Count == 0)
		{
			return null;
		}
		
		var 抽到的牌 = _抽牌堆.Pop();
		if (_手牌.Count >= 手牌上限)
		{
			_弃牌堆.Add(抽到的牌);
			抽到的牌.状态 = CardInstance.卡牌状态.已弃置;
			return null;
		}
		抽到的牌.状态 = CardInstance.卡牌状态.在手牌中;
		_手牌.Add(抽到的牌);
		return 抽到的牌;
	}
	
	private void 重置抽牌堆()
	{
		if (_弃牌堆.Count == 0) return;
		var 随机 = new System.Random();
		var 临时列表 = new System.Collections.Generic.List<CardInstance>();
		foreach (var 卡牌 in _弃牌堆)
		{
			if (卡牌 != null)
			{
				卡牌.状态 = CardInstance.卡牌状态.在卡组中;
				临时列表.Add(卡牌);
			}
		}
		for (int i = 临时列表.Count - 1; i > 0; i--)
		{
			int j = 随机.Next(i + 1);
			(临时列表[i], 临时列表[j]) = (临时列表[j], 临时列表[i]);
		}
		_抽牌堆.Clear();
		foreach (var 卡牌 in 临时列表) _抽牌堆.Push(卡牌);
		_弃牌堆.Clear();
	}
	
	/// <summary>Discards a card from hand to the discard pile.</summary>
	public void 弃牌(CardInstance 卡牌)
	{
		_手牌.Remove(卡牌);
		_弃牌堆.Add(卡牌);
		卡牌.状态 = CardInstance.卡牌状态.已弃置;
		发射手牌变化信号();
	}
	
	/// <summary>Uses a card (moves from hand to discard pile after use).</summary>
	public void 使用卡牌(CardInstance 卡牌)
	{
		_手牌.Remove(卡牌);
		_弃牌堆.Add(卡牌);
		卡牌.状态 = CardInstance.卡牌状态.已使用;
		卡牌.重置状态();
		发射手牌变化信号();
	}
	
	/// <summary>Prints deck state (debug method kept for potential use, but no output).</summary>
	public void 打印卡组状态()
	{
		// 保留调试方法但移除内部输出
	}
	
	[Signal] public delegate void 手牌变化EventHandler();
	
	private void 发射手牌变化信号()
	{
		EmitSignal(SignalName.手牌变化);
	}
	
	/// <summary>Draws the starting hand of the given size.</summary>
	public void 抽起始手牌(int 数量 = 2)
	{
		for (int i = 0; i < 数量; i++) 抽牌();
	}
}
