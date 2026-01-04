using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // ✅ 必须引用 Linq 用于分组统计
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

                if (db.Table<UserModel>().Count() == 0)
                {
                    db.Insert(new UserModel { Username = "admin", Password = "888888" });
                }
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        // ==============================
        // 1. 登录模块
        // ==============================
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

        // ==============================
        // 2. 首页统计 & 库存汇总 (HomeViewModel 需要)
        // ==============================
        public Task<int> GetTotalInboundCountAsync()
        {
            return _database.Table<InboundModel>().CountAsync();
        }

        public Task<int> GetTotalOutboundCountAsync()
        {
            return _database.Table<OutboundModel>().CountAsync();
        }

        // 🔴 新增：获取库存汇总列表
        public async Task<List<InventorySummaryModel>> GetInventorySummaryAsync()
        {
            // 1. 取出所有入库和出库记录
            var inbounds = await _database.Table<InboundModel>().ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().ToListAsync();

            // 2. 找出所有出现过的产品名称 (去重)
            var allProducts = inbounds.Select(x => x.ProductName)
                                      .Union(outbounds.Select(x => x.ProductName))
                                      .Distinct()
                                      .Where(x => !string.IsNullOrEmpty(x)) // 过滤空名
                                      .ToList();

            var summaryList = new List<InventorySummaryModel>();

            // 3. 遍历每个产品，计算库存
            foreach (var name in allProducts)
            {
                // 算入库总数
                var inQty = inbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                // 算出库总数
                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                summaryList.Add(new InventorySummaryModel
                {
                    ProductName = name,
                    TotalInbound = inQty,
                    TotalOutbound = outQty,
                    CurrentStock = inQty - outQty // 剩余库存
                });
            }

            // 4. 按库存量从大到小排序返回
            return summaryList.OrderByDescending(x => x.CurrentStock).ToList();
        }

        // ==============================
        // 3. 入库管理
        // ==============================
        public Task<List<InboundModel>> GetInboundOrdersAsync()
        {
            return _database.Table<InboundModel>().ToListAsync();
        }

        public Task SaveInboundOrderAsync(InboundModel item)
        {
            if (item.Id != 0) return _database.UpdateAsync(item);
            else return _database.InsertAsync(item);
        }

        public Task DeleteInboundOrderAsync(InboundModel item)
        {
            return _database.DeleteAsync(item);
        }

        public async Task<List<string>> GetSupplierListAsync()
        {
            return await _database.QueryScalarsAsync<string>("SELECT DISTINCT Supplier FROM InboundModel WHERE Supplier IS NOT NULL");
        }

        // ==============================
        // 4. 出库管理
        // ==============================
        public Task<List<OutboundModel>> GetOutboundOrdersAsync()
        {
            return _database.Table<OutboundModel>().ToListAsync();
        }

        public Task SaveOutboundOrderAsync(OutboundModel item)
        {
            if (item.Id != 0) return _database.UpdateAsync(item);
            else return _database.InsertAsync(item);
        }

        public Task DeleteOutboundOrderAsync(OutboundModel item)
        {
            return _database.DeleteAsync(item);
        }

        public async Task<List<string>> GetCustomerListAsync()
        {
            return await _database.QueryScalarsAsync<string>("SELECT DISTINCT Customer FROM OutboundModel WHERE Customer IS NOT NULL");
        }

        public async Task<List<string>> GetProductListAsync()
        {
            return await _database.QueryScalarsAsync<string>("SELECT DISTINCT ProductName FROM InboundModel WHERE ProductName IS NOT NULL");
        }
    }
}