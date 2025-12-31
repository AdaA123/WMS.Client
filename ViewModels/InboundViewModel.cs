using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq; // 用于 Where 过滤
using System.Threading.Tasks;
using WMS.Client.Models;
using WMS.Client.Services; // 引用服务
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class InboundViewModel : ObservableObject
    {
        // 引用数据库服务
        private readonly DatabaseService _dbService;

        // 内存缓存（用于搜索过滤，避免每次搜索都查库）
        private List<InboundModel> _allData = new();

        [ObservableProperty]
        private ObservableCollection<InboundModel> _displayedData = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public InboundViewModel()
        {
            // 初始化数据库服务
            _dbService = new DatabaseService();

            // 启动时加载数据
            // 注意：构造函数不能 await，所以用非阻塞方式调用
            LoadDataAsync().ConfigureAwait(false);
        }

        // 从数据库加载数据
        private async Task LoadDataAsync()
        {
            // 1. 从数据库查
            var list = await _dbService.GetInboundOrdersAsync();

            // 2. 更新内存缓存
            _allData = list;

            // 3. 更新界面 (必须回到主线程，MVVM Toolkit 通常会自动处理，但在异步里要小心)
            // 这里重新创建一个 ObservableCollection
            DisplayedData = new ObservableCollection<InboundModel>(_allData);
        }

        // 搜索逻辑 (和之前一样，只是改用 _allData 过滤)
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                DisplayedData = new ObservableCollection<InboundModel>(_allData);
            }
            else
            {
                var filtered = _allData.Where(x =>
                    x.OrderNo.Contains(value) ||
                    x.Supplier.Contains(value) ||
                    // ✅ 新增：支持搜索產品名稱
                    (x.ProductName != null && x.ProductName.Contains(value)));

                DisplayedData = new ObservableCollection<InboundModel>(filtered);
            }
        }

        [RelayCommand]
        private async Task AddNew()
        {
            // --- 1. 计算单号 (基于数据库现有数据) ---
            string currentMonthPrefix = $"RK-{DateTime.Now:yyyyMM}-";
            int nextSequence = 1;

            // 确保 _allData 是最新的
            if (_allData.Any())
            {
                var currentMonthOrders = _allData
                    .Where(x => x.OrderNo.StartsWith(currentMonthPrefix))
                    .ToList();

                if (currentMonthOrders.Any())
                {
                    var maxIndex = currentMonthOrders
                        .Select(x =>
                        {
                            string numPart = x.OrderNo.Substring(currentMonthPrefix.Length);
                            if (int.TryParse(numPart, out int num)) return num;
                            return 0;
                        })
                        .Max();
                    nextSequence = maxIndex + 1;
                }
            }
            string nextOrderNo = $"{currentMonthPrefix}{nextSequence:D3}";

            // --- 2. 准备数据 ---
            var newOrder = new InboundModel
            {
                OrderNo = nextOrderNo,
                ProductName = "", // ✅ 这一行其实可以不写，因为 Model 里已经初始化了
                Supplier = "",
                Count = 0,
                Status = "待验收",
                Date = DateTime.Now
            };

            // --- 3. 弹窗 ---

            // --- 获取供应商历史列表 ---
            var supplierList = await _dbService.GetSupplierListAsync();

            // --- 传给弹窗 ---
            var view = new InboundDialog(supplierList);
            view.DataContext = newOrder;

            var result = await DialogHost.Show(view, "RootDialog");

            // --- 4. 保存到数据库 ---
            bool isConfirmed = false;
            if (result is bool b) isConfirmed = b;
            else if (result is string s) isConfirmed = bool.Parse(s);

            if (isConfirmed)
            {
                // 1. 檢查產品名稱
                if (string.IsNullOrWhiteSpace(newOrder.ProductName))
                {
                    System.Windows.MessageBox.Show("保存失敗：產品名稱不能為空！", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return; // 直接返回，不執行後面的保存
                }

                // 2. 檢查數量
                if (newOrder.Count <= 0)
                {
                    System.Windows.MessageBox.Show("保存失敗：入庫數量必須大於 0！", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 3. 檢查供應商 (可選，看你是否強制要求)
                if (string.IsNullOrWhiteSpace(newOrder.Supplier))
                {
                    System.Windows.MessageBox.Show("保存失敗：請選擇或輸入供應商！", "提示",
                       System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                // 🔴 关键差异：保存到数据库！
                await _dbService.SaveInboundOrderAsync(newOrder);

                // 🔴 保存成功后，重新加载整个列表（保持数据一致性）
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task Delete(InboundModel model)
        {
            // 1. 弹出确认框 (使用原生 MessageBox 最简单稳妥)
            var result = System.Windows.MessageBox.Show(
                $"确定要删除单号 [{model.OrderNo}] 吗？\n删除后无法恢复！",
                "删除确认",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            // 2. 如果用户点了“是”
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // 3. 数据库删除
                await _dbService.DeleteInboundOrderAsync(model);

                // 4. 刷新列表 (最简单的方法)
                await LoadDataAsync();
            }
        }
    }
}