using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WMS.Client.Models;
using WMS.Client.Services; // 引用服务
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class OutboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private List<OutboundModel> _allData = new();

        [ObservableProperty]
        private ObservableCollection<OutboundModel> _displayedData = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public OutboundViewModel()
        {
            _dbService = new DatabaseService();
            LoadDataAsync().ConfigureAwait(false);
        }

        private async Task LoadDataAsync()
        {
            var list = await _dbService.GetOutboundOrdersAsync();
            _allData = list;
            DisplayedData = new ObservableCollection<OutboundModel>(_allData);
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                DisplayedData = new ObservableCollection<OutboundModel>(_allData);
            }
            else
            {
                var filtered = _allData.Where(x =>
                    x.OutboundNo.Contains(value) ||
                    x.Customer.Contains(value) ||
                    // ✅ 新增：支持搜索產品名稱
                    (x.ProductName != null && x.ProductName.Contains(value)));

                DisplayedData = new ObservableCollection<OutboundModel>(filtered);
            }
        }

        [RelayCommand]
        private async Task AddNew()
        {
            // --- 1. 自动计算出库单号 (CK-202512-xxx) ---
            string currentMonthPrefix = $"CK-{DateTime.Now:yyyyMM}-";
            int nextSequence = 1;

            if (_allData.Any())
            {
                var currentMonthOrders = _allData
                    .Where(x => x.OutboundNo.StartsWith(currentMonthPrefix))
                    .ToList();

                if (currentMonthOrders.Any())
                {
                    var maxIndex = currentMonthOrders
                        .Select(x =>
                        {
                            string numPart = x.OutboundNo.Substring(currentMonthPrefix.Length);
                            if (int.TryParse(numPart, out int num)) return num;
                            return 0;
                        })
                        .Max();
                    nextSequence = maxIndex + 1;
                }
            }
            string nextOrderNo = $"{currentMonthPrefix}{nextSequence:D3}";

            // --- 2. 准备数据 ---
            var newOrder = new OutboundModel
            {
                OutboundNo = nextOrderNo,
                Customer = "",
                Count = 0,
                Status = "待拣货",
                Date = DateTime.Now
            };
            // --- 3. 弹窗 (确保你新建了 OutboundDialog) ---
            // 如果你还没建 OutboundDialog，暂时可以用 InboundDialog 顶一下，但字段显示会有点怪
            // A. 从数据库获取去重后的产品列表
            var productList = await _dbService.GetProductListAsync();

            // 2. ✅ 新增：获取客户列表
            var customerList = await _dbService.GetCustomerListAsync();

            // B. 创建弹窗时，把列表传进去
            var view = new OutboundDialog(productList, customerList);

            view.DataContext = newOrder;

            var result = await DialogHost.Show(view, "RootDialog");

            // --- 4. 保存 ---
            bool isConfirmed = false;
            if (result is bool b) isConfirmed = b;
            else if (result is string s) isConfirmed = bool.Parse(s);

            if (isConfirmed)
            {
                if (string.IsNullOrWhiteSpace(newOrder.ProductName))
                {
                    System.Windows.MessageBox.Show("保存失敗：產品名稱不能為空！", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (newOrder.Count <= 0)
                {
                    System.Windows.MessageBox.Show("保存失敗：出庫數量必須大於 0！", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(newOrder.Customer))
                {
                    System.Windows.MessageBox.Show("保存失敗：請選擇或輸入客戶！", "提示",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                await _dbService.SaveOutboundOrderAsync(newOrder);
                await LoadDataAsync();
            }
        }
        [RelayCommand]
        private async Task Delete(OutboundModel model)
        {
            var result = System.Windows.MessageBox.Show(
                $"确定要删除出库单 [{model.OutboundNo}] 吗？",
                "删除确认",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _dbService.DeleteOutboundOrderAsync(model);
                await LoadDataAsync();
            }
        }
    }
}