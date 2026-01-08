using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class SupplierArchiveViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<SupplierModel> List { get; } = new();
        [ObservableProperty] private SupplierModel _newItem = new();
        [ObservableProperty] private string _searchText = "";

        // 详情页数据源
        public ObservableCollection<InboundModel> DetailInbounds { get; } = new();
        [ObservableProperty] private string _detailTitle = "";

        public SupplierArchiveViewModel()
        {
            _dbService = new DatabaseService();
            // 🟢 修复：去掉 Task.Run，直接调用 Refresh
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            var data = await _dbService.GetSuppliersAsync();

            // 自动填充演示数据
            if (data.Count == 0 && string.IsNullOrWhiteSpace(SearchText))
            {
                var demoSuppliers = new List<SupplierModel>
                {
                    new SupplierModel { Name = "苹果电脑贸易(上海)有限公司", ContactPerson = "渠道部", Phone = "021-61000000", Address = "上海市浦东新区源深路", Remark = "官方直供" },
                    new SupplierModel { Name = "联想(北京)有限公司", ContactPerson = "大客户经理", Phone = "010-58888888", Address = "北京市海淀区上地西路", Remark = "长期合作" },
                    new SupplierModel { Name = "戴尔(中国)有限公司", ContactPerson = "销售代表", Phone = "0592-8188888", Address = "厦门市湖里区", Remark = "显示器采购" },
                    new SupplierModel { Name = "京东企业购", ContactPerson = "企业服务部", Phone = "400-606-5500", Address = "北京市亦庄经济开发区", Remark = "电商采购平台" },
                    new SupplierModel { Name = "顺丰速运耗材供应", ContactPerson = "李主管", Phone = "95338", Address = "深圳市宝安区", Remark = "包材供应商" }
                };

                foreach (var demo in demoSuppliers)
                {
                    await _dbService.SaveSupplierAsync(demo);
                }
                data = await _dbService.GetSuppliersAsync();
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
                data = data.Where(x => (x.Name?.Contains(SearchText) ?? false) || (x.ContactPerson?.Contains(SearchText) ?? false)).ToList();

            List.Clear();
            foreach (var item in data) List.Add(item);
        }

        partial void OnSearchTextChanged(string value) => _ = Refresh();

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewItem.Name)) { MessageBox.Show("名称不能为空"); return; }
            await _dbService.SaveSupplierAsync(NewItem);
            NewItem = new SupplierModel();
            await Refresh();
        }

        [RelayCommand]
        private void Edit(SupplierModel item) => NewItem = new SupplierModel { Id = item.Id, Name = item.Name, ContactPerson = item.ContactPerson, Phone = item.Phone, Address = item.Address, Remark = item.Remark };

        [RelayCommand]
        private void Cancel() => NewItem = new SupplierModel();

        [RelayCommand]
        private async Task Delete(SupplierModel item)
        {
            if (MessageBox.Show("确定删除？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteSupplierAsync(item);
                await Refresh();
            }
        }

        [RelayCommand]
        private async Task ViewDetail(SupplierModel item)
        {
            if (item == null || string.IsNullOrEmpty(item.Name)) return;
            DetailTitle = $"供应商详情：{item.Name}";

            var data = await _dbService.GetInboundsBySupplierAsync(item.Name);
            DetailInbounds.Clear(); foreach (var i in data) DetailInbounds.Add(i);

            var view = new SupplierDetailDialog { DataContext = this };
            await DialogHost.Show(view, "SupplierArchiveDialog");
        }
    }
}