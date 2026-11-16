using CommunityToolkit.Mvvm.ComponentModel;

namespace Shyd.Prism.Mvvm;

/// <summary>
/// MVVM 视图模型基座：基于 CommunityToolkit 的 <see cref="ObservableObject"/>，
/// 供区域导航 / 弹窗契约（INavigationAware、IDialogAware）作为 ViewModel 约束使用。
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
