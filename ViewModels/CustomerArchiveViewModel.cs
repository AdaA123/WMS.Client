using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class CustomerArchiveViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        public ObservableCollection<CustomerModel> List { get; } = new();
        [ObservableProperty] private CustomerModel _newItem = new();
        [ObservableProperty] private string _searchText = "";

        public CustomerArchiveViewModel()
        {
            _dbService = new DatabaseService();
            _ = Refresh();
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
    }
}