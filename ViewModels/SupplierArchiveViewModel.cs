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
        private readonly PrintService _printService;

        public ObservableCollection<SupplierModel> List { get; } = new();
        [ObservableProperty] private SupplierModel _newItem = new();
        [ObservableProperty] private string _searchText = "";

        public ObservableCollection<InboundModel> DetailInbounds { get; } = new();
        [ObservableProperty] private string _detailTitle = "";

        private SupplierModel? _currentSupplier;

        public SupplierArchiveViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
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
            _currentSupplier = item;
            DetailTitle = $"供应商详情：{item.Name}";

            var inbounds = await _dbService.GetInboundsBySupplierAsync(item.Name);
            DetailInbounds.Clear();
            foreach (var i in inbounds) DetailInbounds.Add(i);

            var view = new SupplierDetailDialog { DataContext = this };
            await DialogHost.Show(view, "SupplierArchiveDialog");
        }

        [RelayCommand]
        private void PrintDetail()
        {
            if (_currentSupplier != null)
                _printService.PrintSupplierDetails(_currentSupplier, DetailInbounds);
        }
    }
}