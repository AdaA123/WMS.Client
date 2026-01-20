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

        // --- 详情页数据源 ---
        public ObservableCollection<InboundModel> DetailInbounds { get; } = new();
        public ObservableCollection<OutboundModel> DetailOutbounds { get; } = new();
        public ObservableCollection<ReturnModel> DetailReturns { get; } = new();

        [ObservableProperty] private string _detailTitle = "";

        public SupplierArchiveViewModel()
        {
            _dbService = new DatabaseService();
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            // 🟢 修改：直接读取数据库，不再填充演示数据
            var data = await _dbService.GetSuppliersAsync();

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

            // 1. 获取该供应商的入库记录（直接关联）
            var inbounds = await _dbService.GetInboundsBySupplierAsync(item.Name);
            DetailInbounds.Clear();
            foreach (var i in inbounds) DetailInbounds.Add(i);

            // 2. 关联出库和退货 (间接关联：基于该供应商供货过的产品)
            var suppliedProducts = inbounds.Select(x => x.ProductName).Distinct().Where(x => !string.IsNullOrEmpty(x)).ToList();

            var relatedOutbounds = new List<OutboundModel>();
            var relatedReturns = new List<ReturnModel>();

            foreach (var pName in suppliedProducts)
            {
                if (pName == null) continue;
                // 查询该产品的销售记录
                var outs = await _dbService.GetOutboundsByProductAsync(pName);
                relatedOutbounds.AddRange(outs);

                // 查询该产品的退货记录
                var rets = await _dbService.GetReturnsByProductAsync(pName);
                relatedReturns.AddRange(rets);
            }

            // 填充并按时间倒序
            DetailOutbounds.Clear();
            foreach (var o in relatedOutbounds.OrderByDescending(x => x.OutboundDate)) DetailOutbounds.Add(o);

            DetailReturns.Clear();
            foreach (var r in relatedReturns.OrderByDescending(x => x.ReturnDate)) DetailReturns.Add(r);

            var view = new SupplierDetailDialog { DataContext = this };
            await DialogHost.Show(view, "SupplierArchiveDialog");
        }
    }
}