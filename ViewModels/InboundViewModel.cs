using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf; // 必须引用：用于弹窗
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WMS.Client.Models;
using WMS.Client.Views;         // 必须引用：用于找到 InboundDialog

namespace WMS.Client.ViewModels
{
    // 🔴 检查点 1：这里必须是 public，不能是 private
    public partial class InboundViewModel : ObservableObject
    {
        // 1. 所有数据 (备份用)
        private List<InboundModel> _allData = new();

        // 2. 界面显示的数据
        [ObservableProperty]
        private ObservableCollection<InboundModel> _displayedData = new();

        // 3. 搜索框文字
        [ObservableProperty]
        private string _searchText = string.Empty;

        public InboundViewModel()
        {
            LoadMockData();
        }

        private void LoadMockData()
        {
            _allData = new List<InboundModel>();
            for (int i = 1; i <= 20; i++)
            {
                _allData.Add(new InboundModel
                {
                    OrderNo = $"RK-{DateTime.Now:yyyyMM}-{i:000}",
                    Supplier = i % 2 == 0 ? "京东物流" : "顺丰供应链",
                    Status = i % 3 == 0 ? "待验收" : "已入库",
                    Count = i * 10,
                    Date = DateTime.Now.AddDays(-i)
                });
            }

            DisplayedData = new ObservableCollection<InboundModel>(_allData);
        }

        // 🔴 检查点 2：这个方法前面千万不能加 private！
        // 正确写法：partial void 方法名
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (_allData != null)
                    DisplayedData = new ObservableCollection<InboundModel>(_allData);
            }
            else
            {
                var filtered = _allData.Where(x =>
                    x.OrderNo.Contains(value) ||
                    x.Supplier.Contains(value));

                DisplayedData = new ObservableCollection<InboundModel>(filtered);
            }
        }

        [RelayCommand]
        private async Task AddNew()
        {
            // --- 1. 更穩健的單號計算邏輯 (Max + 1) ---

            // 定義當前月份的前綴，例如 "RK-202512-"
            string currentMonthPrefix = $"RK-{DateTime.Now:yyyyMM}-";
            int nextSequence = 1;

            // 確保數據源不為空
            if (_allData != null && _allData.Any())
            {
                // A. 找出所有屬於這個月的單號
                var currentMonthOrders = _allData
                    .Where(x => x.OrderNo != null && x.OrderNo.StartsWith(currentMonthPrefix))
                    .ToList();

                if (currentMonthOrders.Any())
                {
                    // B. 提取每個單號後面的數字 (例如 RK-202512-005 -> 5)
                    // 我們找出其中最大的數字
                    var maxIndex = currentMonthOrders
                        .Select(x =>
                        {
                            // 截取後面的數字部分
                            string numPart = x.OrderNo.Substring(currentMonthPrefix.Length);
                            if (int.TryParse(numPart, out int num)) return num;
                            return 0;
                        })
                        .Max(); // 獲取最大值

                    // C. 最大值 + 1
                    nextSequence = maxIndex + 1;
                }
            }

            // 拼接成新單號：RK-202512-003
            string nextOrderNo = $"{currentMonthPrefix}{nextSequence:D3}";

            // ---------------------------------------------------------
            // --- 2. 下面是之前的代碼，保持不變 ---
            // ---------------------------------------------------------

            var newOrder = new InboundModel
            {
                OrderNo = nextOrderNo,
                Supplier = "",
                Count = 0,
                Status = "待驗收",
                Date = DateTime.Now
            };

            var view = new InboundDialog();
            view.DataContext = newOrder;

            var result = await DialogHost.Show(view, "RootDialog");

            // ... 後面的判斷邏輯不用動 ...
            bool isConfirmed = false;
            if (result is bool b) isConfirmed = b;
            else if (result is string s) isConfirmed = bool.Parse(s);

            if (isConfirmed)
            {
                if (string.IsNullOrEmpty(newOrder.Supplier)) newOrder.Supplier = "未知供應商";

                if (_allData != null) _allData.Insert(0, newOrder);
                DisplayedData?.Insert(0, newOrder);
            }
        }
    }
}