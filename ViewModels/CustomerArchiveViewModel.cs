using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class CustomerArchiveViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        public ObservableCollection<CustomerModel> List { get; } = new();
        [ObservableProperty] private CustomerModel _newItem = new();
        [ObservableProperty] private string _searchText = "";

        // 🟢 详情页数据源
        public ObservableCollection<OutboundModel> DetailOutbounds { get; } = new();
        public ObservableCollection<ReturnModel> DetailReturns { get; } = new();
        [ObservableProperty] private string _detailTitle = "";

        public CustomerArchiveViewModel()
        {
            _dbService = new DatabaseService();
            // 🟢 修复：添加 "_ =" 消除警告
            _ = Task.Run(() => Refresh());
        }

        [RelayCommand]
        private async Task Refresh()
        {
            var data = await _dbService.GetCustomersAsync();
            if (!string.IsNullOrWhiteSpace(SearchText))
                data = data.Where(x => (x.Name?.Contains(SearchText) ?? false) || (x.ContactPerson?.Contains(SearchText) ?? false)).ToList();
            List.Clear();
            foreach (var item in data) List.Add(item);
        }

        partial void OnSearchTextChanged(string value) => Refresh();

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewItem.Name)) { MessageBox.Show("名称不能为空"); return; }
            await _dbService.SaveCustomerAsync(NewItem);
            NewItem = new CustomerModel();
            await Refresh();
        }

        [RelayCommand]
        private void Edit(CustomerModel item) => NewItem = new CustomerModel { Id = item.Id, Name = item.Name, ContactPerson = item.ContactPerson, Phone = item.Phone, Address = item.Address, Remark = item.Remark };

        [RelayCommand]
        private void Cancel() => NewItem = new CustomerModel();

        [RelayCommand]
        private async Task Delete(CustomerModel item)
        {
            if (MessageBox.Show("确定删除？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteCustomerAsync(item);
                await Refresh();
            }
        }

        // 🟢 查看详情命令
        [RelayCommand]
        private async Task ViewDetail(CustomerModel item)
        {
            if (item == null || string.IsNullOrEmpty(item.Name)) return;
            DetailTitle = $"客户详情：{item.Name}";

            var t1 = _dbService.GetOutboundsByCustomerAsync(item.Name);
            var t2 = _dbService.GetReturnsByCustomerAsync(item.Name);
            await Task.WhenAll(t1, t2);

            DetailOutbounds.Clear(); foreach (var i in t1.Result) DetailOutbounds.Add(i);
            DetailReturns.Clear(); foreach (var i in t2.Result) DetailReturns.Add(i);

            var view = new CustomerDetailDialog { DataContext = this };
            await DialogHost.Show(view, "CustomerArchiveDialog");
        }
    }
}