using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WMS.Client.Models;

namespace WMS.Client.ViewModels
{
    public partial class InboundViewModel : ObservableObject
    {
        // 1. 所有数据 (备份用)
        private List<InboundModel> _allData;

        // 2. 界面显示的数据 (绑定到 DataGrid)
        [ObservableProperty]
        private ObservableCollection<InboundModel> _displayedData;

        // 3. 搜索框文字
        [ObservableProperty]
        private string _searchText;

        public InboundViewModel()
        {
            LoadMockData();
        }

        private void LoadMockData()
        {
            // 模拟生成 20 条数据
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

            // 初始显示所有
            DisplayedData = new ObservableCollection<InboundModel>(_allData);
        }

        // 当搜索框文字变化时，自动触发 (Source Generator 特性)
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                DisplayedData = new ObservableCollection<InboundModel>(_allData);
            }
            else
            {
                // 模糊查询：单号 或 供应商
                var filtered = _allData.Where(x =>
                    x.OrderNo.Contains(value) ||
                    x.Supplier.Contains(value));

                DisplayedData = new ObservableCollection<InboundModel>(filtered);
            }
        }

        // 模拟一个新建命令
        [RelayCommand]
        private void AddNew()
        {
            // 下一步我们再实现弹窗
            System.Windows.MessageBox.Show("点击了新建按钮！");
        }
    }
}