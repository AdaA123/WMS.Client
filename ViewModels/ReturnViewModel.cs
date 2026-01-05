using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class ReturnViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly ExportService _exportService;

        public ObservableCollection<ReturnModel> ReturnList { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();
        public ObservableCollection<string> Customers { get; } = new();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "客户" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => SortData();

        [ObservableProperty] private ReturnModel _newReturn = new();

        public ReturnViewModel()
        {
            _dbService = new DatabaseService();
            _exportService = new ExportService();
            _ = RefreshDataAsync(); // 初始加载
        }

        // 🟢 关键：这个方法供 MainViewModel 切换页面时调用
        public async Task RefreshDataAsync()
        {
            await LoadData();
            await LoadLists(); // 重新加载下拉列表（这就包含了新出库的香蕉）
        }

        [RelayCommand]
        private void Edit(ReturnModel item)
        {
            if (item == null) return;
            NewReturn = new ReturnModel
            {
                Id = item.Id,
                ReturnNo = item.ReturnNo,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                Customer = item.Customer,
                Reason = item.Reason,
                ReturnDate = item.ReturnDate
            };
        }

        [RelayCommand]
        private void Cancel()
        {
            NewReturn = new ReturnModel();
        }

        [RelayCommand]
        private void Export()
        {
            if (ReturnList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
            _exportService.ExportReturn(ReturnList);
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewReturn.ProductName)) { MessageBox.Show("请选择产品！"); return; }
            if (NewReturn.Quantity <= 0) { MessageBox.Show("数量必须大于 0！"); return; }

            try
            {
                if (NewReturn.Id == 0)
                {
                    NewReturn.ReturnNo = $"TH{DateTime.Now:yyyyMMddHHmmss}";
                    NewReturn.ReturnDate = DateTime.Now;
                }
                if (string.IsNullOrEmpty(NewReturn.Customer)) NewReturn.Customer = "散客";
                if (string.IsNullOrEmpty(NewReturn.Reason)) NewReturn.Reason = "无理由退货";

                await _dbService.SaveReturnOrderAsync(NewReturn);
                await RefreshDataAsync(); // 保存后自动刷新
                NewReturn = new ReturnModel();
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand]
        private async Task Delete(ReturnModel item)
        {
            if (MessageBox.Show($"确定删除单号 {item.ReturnNo} 吗？", "删除确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteReturnOrderAsync(item);
                await RefreshDataAsync();
                if (NewReturn.Id == item.Id) NewReturn = new ReturnModel();
            }
        }

        private async Task LoadData()
        {
            var list = await _dbService.GetReturnOrdersAsync();
            ReturnList.Clear();
            foreach (var item in list) ReturnList.Add(item);
            SortData();
        }

        private async Task LoadLists()
        {
            var prods = await _dbService.GetShippedProductListAsync();
            ProductList.Clear();
            foreach (var p in prods) if (!string.IsNullOrEmpty(p)) ProductList.Add(p);

            var custs = await _dbService.GetCustomerListAsync();
            Customers.Clear();
            foreach (var c in custs) if (!string.IsNullOrEmpty(c)) Customers.Add(c);
        }

        private void SortData()
        {
            if (ReturnList.Count == 0) return;
            var sorted = SelectedSortOption switch
            {
                "时间 (最新)" => ReturnList.OrderByDescending(x => x.ReturnDate).ToList(),
                "时间 (最早)" => ReturnList.OrderBy(x => x.ReturnDate).ToList(),
                "产品名称" => ReturnList.OrderBy(x => x.ProductName).ToList(),
                "客户" => ReturnList.OrderBy(x => x.Customer).ToList(),
                _ => ReturnList.OrderByDescending(x => x.ReturnDate).ToList()
            };
            ReturnList.Clear();
            foreach (var item in sorted) ReturnList.Add(item);
        }
    }
}