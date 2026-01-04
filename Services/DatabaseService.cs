using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WMS.Client.Models;

namespace WMS.Client.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _dbPath = Path.Combine(docPath, "WMS_Database.db");

            using (var db = new SQLiteConnection(_dbPath))
            {
                db.CreateTable<UserModel>();
                db.CreateTable<InboundModel>();
                db.CreateTable<OutboundModel>();
                db.CreateTable<ReturnModel>();

                if (db.Table<UserModel>().Count() == 0)
                {
                    db.Insert(new UserModel { Username = "admin", Password = "888888" });
                }
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        // --- 登录 ---
        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = await _database.Table<UserModel>()
                                      .Where(u => u.Username == username && u.Password == password)
                                      .FirstOrDefaultAsync();
            return user != null;
        }

        public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            var user = await _database.Table<UserModel>()
                                      .Where(u => u.Username == username && u.Password == oldPassword)
                                      .FirstOrDefaultAsync();
            if (user == null) return false;
            user.Password = newPassword;
            await _database.UpdateAsync(user);
            return true;
        }

        // --- 统计 & 汇总 ---
        public Task<int> GetTotalInboundCountAsync() => _database.Table<InboundModel>().CountAsync();
        public Task<int> GetTotalOutboundCountAsync() => _database.Table<OutboundModel>().CountAsync();
        public Task<int> GetTotalReturnCountAsync() => _database.Table<ReturnModel>().CountAsync();

        public async Task<List<InventorySummaryModel>> GetInventorySummaryAsync()
        {
            var inbounds = await _database.Table<InboundModel>().ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().ToListAsync();
            var returns = await _database.Table<ReturnModel>().ToListAsync();

            var allProducts = inbounds.Select(x => x.ProductName)
                                      .Union(outbounds.Select(x => x.ProductName))
                                      .Union(returns.Select(x => x.ProductName))
                                      .Distinct()
                                      .Where(x => !string.IsNullOrEmpty(x))
                                      .ToList();

            var summaryList = new List<InventorySummaryModel>();
            foreach (var name in allProducts)
            {
                var inQty = inbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var retQty = returns.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                summaryList.Add(new InventorySummaryModel
                {
                    ProductName = name,
                    TotalInbound = inQty,
                    TotalOutbound = outQty,
                    CurrentStock = inQty - outQty + retQty
                });
            }
            return summaryList.OrderByDescending(x => x.CurrentStock).ToList();
        }

        // --- 入库 ---
        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteInboundOrderAsync(InboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetSupplierListAsync() => await _database.QueryScalarsAsync<string>("SELECT DISTINCT Supplier FROM InboundModel WHERE Supplier IS NOT NULL");

        // --- 出库 ---
        public Task<List<OutboundModel>> GetOutboundOrdersAsync() => _database.Table<OutboundModel>().ToListAsync();
        public Task SaveOutboundOrderAsync(OutboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteOutboundOrderAsync(OutboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetCustomerListAsync() => await _database.QueryScalarsAsync<string>("SELECT DISTINCT Customer FROM OutboundModel WHERE Customer IS NOT NULL");

        // --- 退货 ---
        public Task<List<ReturnModel>> GetReturnOrdersAsync() => _database.Table<ReturnModel>().ToListAsync();
        public Task SaveReturnOrderAsync(ReturnModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteReturnOrderAsync(ReturnModel item) => _database.DeleteAsync(item);

        // --- 下拉列表辅助 ---
        public async Task<List<string>> GetProductListAsync()
            => await _database.QueryScalarsAsync<string>("SELECT DISTINCT ProductName FROM InboundModel WHERE ProductName IS NOT NULL");

        public async Task<List<string>> GetShippedProductListAsync()
            => await _database.QueryScalarsAsync<string>("SELECT DISTINCT ProductName FROM OutboundModel WHERE ProductName IS NOT NULL");
    }
}