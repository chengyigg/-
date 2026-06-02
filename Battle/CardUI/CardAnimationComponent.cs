using Godot;
using System;
using System.Threading.Tasks;

public partial class CardAnimationComponent : Control
{
	private Tween _当前动画;
	private Vector2 _原始缩放;

	public void 设置原始缩放(Vector2 缩放) => _原始缩放 = 缩放;
	public void 停止当前动画() => _当前动画?.Kill();

	public void 播放缩放动画(Control 目标, Vector2 目标缩放, float 时长)
	{
		停止当前动画();
		_当前动画 = 目标.CreateTween();
		_当前动画.SetEase(Tween.EaseType.Out);
		_当前动画.SetTrans(Tween.TransitionType.Quad);
		_当前动画.TweenProperty(目标, "scale", 目标缩放, 时长);
	}

	public void 播放移动动画(Control 目标, Vector2 目标位置, float 时长)
	{
		停止当前动画();
		_当前动画 = 目标.CreateTween();
		_当前动画.SetEase(Tween.EaseType.Out);
		_当前动画.SetTrans(Tween.TransitionType.Quad);
		_当前动画.TweenProperty(目标, "position", 目标位置, 时长);
	}

	public void 执行流畅入场动画(Control 目标, Vector2 目标位置, Vector2 起始位置, Vector2 原始缩放, Action 回调 = null)
	{
		目标.Position = 起始位置;
		目标.Scale = new Vector2(0.1f, 0.1f);
		目标.Rotation = Mathf.Pi;
		目标.Modulate = new Color(1, 1, 1, 0.8f);
		目标.ZIndex = 1000;

		var 动画 = 目标.CreateTween();
		动画.SetParallel(true);
		动画.TweenProperty(目标, "position", 目标位置, 0.8f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "rotation", 0f, 0.6f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "scale", 原始缩放, 0.8f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "modulate:a", 1f, 0.5f);
		if (回调 != null) 动画.Connect("finished", Callable.From(回调));
	}

	public void 执行平滑移动动画(Control 目标, Vector2 目标位置, float 目标旋转, int 目标层级, float 时长, float 延迟)
	{
		var 动画 = 目标.CreateTween();
		if (延迟 > 0) 动画.TweenInterval(延迟);
		动画.SetParallel(true);
		动画.TweenProperty(目标, "position", 目标位置, 时长).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "rotation", 目标旋转, 时长).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		动画.TweenCallback(Callable.From(() => 目标.ZIndex = 目标层级));
	}

	public void 执行使用动画(Control 目标, Action 回调 = null)
	{
		var 当前缩放 = 目标.Scale;
		var 当前透明度 = 目标.Modulate.A;
		var 动画 = 目标.CreateTween();
		动画.SetParallel(true);
		动画.TweenProperty(目标, "scale", 当前缩放 * 1.3f, 0.8f).From(当前缩放).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "modulate:a", 0f, 0.8f).From(当前透明度).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		if (回调 != null) 动画.Connect("finished", Callable.From(回调));
	}

	public async Task 执行使用动画Lerp(Control 目标, Action 回调 = null)
	{
		var 当前缩放 = 目标.Scale;
		await Lerp动画工具.缩放Lerp动画(目标, 当前缩放, 当前缩放 * 1.3f, 0.8f);
		var 透明度动画 = 目标.CreateTween();
		透明度动画.TweenProperty(目标, "modulate:a", 0f, 0.8f).From(目标.Modulate.A).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		await ToSignal(透明度动画, "finished");
		回调?.Invoke();
	}

	public void 执行弃置动画(Control 目标, Vector2 弃牌堆位置, Action 回调 = null)
	{
		var 动画 = 目标.CreateTween();
		动画.SetParallel(true);
		动画.TweenProperty(目标, "position", 弃牌堆位置, 0.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "rotation_degrees", 180f, 0.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "scale", Vector2.Zero, 0.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "modulate:a", 0f, 0.5f);
		if (回调 != null) 动画.Connect("finished", Callable.From(回调));
	}

	public void 执行移动到位置动画(Control 目标, Vector2 目标位置, Vector2 原始缩放, float 时长)
	{
		var 动画 = 目标.CreateTween();
		动画.SetParallel(true);
		动画.TweenProperty(目标, "position", 目标位置, 时长).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "scale", 原始缩放, 时长).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	public void 执行入场动画(Control 目标, Vector2 起始位置, Vector2 原始位置, Vector2 原始缩放, Action 回调 = null)
	{
		目标.Position = 起始位置;
		目标.Scale = new Vector2(0.1f, 0.1f);
		目标.Rotation = Mathf.Pi;
		目标.Modulate = new Color(1, 1, 1, 0);

		var 动画 = 目标.CreateTween();
		动画.SetParallel(true);
		动画.TweenProperty(目标, "position", 原始位置, 0.8f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "rotation", 0f, 0.8f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "scale", 原始缩放, 0.8f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
		动画.TweenProperty(目标, "modulate:a", 1f, 0.5f);
		if (回调 != null) 动画.Connect("finished", Callable.From(回调));
	}

	public void 重置动画() => 停止当前动画();
}
