using Godot;
using System;

/// <summary>
/// Runtime instance of a card, tracking current stats (damage, speed) and state (in deck, hand, used, discarded).
/// </summary>
[GlobalClass]
public partial class CardInstance : GodotObject
{
	private CardData _基础数据;
	private int _当前伤害;
	private int _当前速度值;
	private 卡牌状态 _状态 = 卡牌状态.在卡组中;
	
	/// <summary>Static data of the card.</summary>
	public CardData 基础数据 
	{ 
		get => _基础数据; 
		set => _基础数据 = value; 
	}
	
	/// <summary>Current damage value (may be modified by buffs).</summary>
	public int 当前伤害 
	{ 
		get => _当前伤害; 
		set => _当前伤害 = value; 
	}
	
	/// <summary>Current speed value (may be modified).</summary>
	public int 当前速度值 
	{ 
		get => _当前速度值; 
		set => _当前速度值 = value; 
	}
	
	/// <summary>Current state of the card.</summary>
	public 卡牌状态 状态 
	{ 
		get => _状态; 
		set => _状态 = value; 
	}
	
	public CardInstance()
	{
	}
	
	/// <summary>Constructs an instance from static card data.</summary>
	public CardInstance(CardData 数据)
	{
		_基础数据 = 数据;
		_当前伤害 = 数据.基础伤害;
		_当前速度值 = 数据.速度值;
	}
	
	/// <summary>Resets card stats to base values (used after being discarded or used).</summary>
	public void 重置状态()
	{
		// 重置卡牌状态以便可以再次使用
		当前伤害 = 基础数据?.基础伤害 ?? 0;
		当前速度值 = 基础数据?.速度值 ?? 0;
		// 重置其他可能的状态
	}
	
	/// <summary>Card state enumeration.</summary>
	public enum 卡牌状态
	{
		在卡组中,
		在手牌中,
		已使用,
		已弃置
	}
}
