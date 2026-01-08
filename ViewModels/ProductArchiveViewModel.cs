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
    public partial class ProductArchiveViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<ProductModel> List { get; } = new();
        [ObservableProperty] private ProductModel _newItem = new();
        [ObservableProperty] private string _searchText = "";

        public ProductArchiveViewModel()
        {
            _dbService = new DatabaseService();
            _ = Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            var data = await _dbService.GetProductsAsync();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                data = data.Where(x => (x.Name?.Contains(SearchText) ?? false) || (x.Spec?.Contains(SearchText) ?? false)).ToList();
            }
            List.Clear();
            foreach (var item in data) List.Add(item);
        }

        partial void OnSearchTextChanged(string value) => Refresh();

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewItem.Name)) { MessageBox.Show("品名不能为空"); return; }
            await _dbService.SaveProductAsync(NewItem);
            NewItem = new ProductModel(); // 重置
            await Refresh();
        }

        [RelayCommand]
        private void Edit(ProductModel item)
        {
            // 复制一份以便编辑，避免直接修改列表显示
            NewItem = new ProductModel { Id = item.Id, Name = item.Name, Spec = item.Spec, Unit = item.Unit, Price = item.Price, Remark = item.Remark };
        }

        [RelayCommand]
        private void Cancel() => NewItem = new ProductModel();

        [RelayCommand]
        private async Task Delete(ProductModel item)
        {
            if (MessageBox.Show("确定删除该商品档案吗？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteProductAsync(item);
                await Refresh();
            }
        }
    }
}