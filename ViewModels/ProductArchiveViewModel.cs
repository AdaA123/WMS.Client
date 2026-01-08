using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views; // 引用视图命名空间

namespace WMS.Client.ViewModels
{
    public partial class ProductArchiveViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<ProductModel> List { get; } = new();
        [ObservableProperty] private ProductModel _newItem = new();
        [ObservableProperty] private string _searchText = "";

        // 🟢 详情页数据源
        public ObservableCollection<InboundModel> DetailInbounds { get; } = new();
        public ObservableCollection<OutboundModel> DetailOutbounds { get; } = new();
        public ObservableCollection<ReturnModel> DetailReturns { get; } = new();
        [ObservableProperty] private string _detailTitle = "";

        public ProductArchiveViewModel()
        {
            _dbService = new DatabaseService();
            // 🟢 修复：添加 "_ =" 消除警告
            _ = Task.Run(() => Refresh());
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

        // 🟢 查看详情命令
        [RelayCommand]
        private async Task ViewDetail(ProductModel item)
        {
            if (item == null || string.IsNullOrEmpty(item.Name)) return;

            DetailTitle = $"商品详情：{item.Name}";

            // 并行加载数据
            var t1 = _dbService.GetInboundsByProductAsync(item.Name);
            var t2 = _dbService.GetOutboundsByProductAsync(item.Name);
            var t3 = _dbService.GetReturnsByProductAsync(item.Name);

            await Task.WhenAll(t1, t2, t3);

            DetailInbounds.Clear(); foreach (var i in t1.Result) DetailInbounds.Add(i);
            DetailOutbounds.Clear(); foreach (var i in t2.Result) DetailOutbounds.Add(i);
            DetailReturns.Clear(); foreach (var i in t3.Result) DetailReturns.Add(i);

            // 打开弹窗
            var view = new ProductDetailDialog { DataContext = this };
            await DialogHost.Show(view, "ProductArchiveDialog");
        }
    }
}