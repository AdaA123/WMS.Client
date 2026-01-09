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
                // 基础业务表
                db.CreateTable<UserModel>();
                db.CreateTable<InboundModel>();
                db.CreateTable<OutboundModel>();
                db.CreateTable<ReturnModel>();

                // 🟢 升级：创建批发主表和明细表
                db.CreateTable<WholesaleOrder>();
                db.CreateTable<WholesaleItem>();

                // 档案表
                db.CreateTable<ProductModel>();
                db.CreateTable<CustomerModel>();
                db.CreateTable<SupplierModel>();

                if (db.Table<UserModel>().Count() == 0)
                {
                    db.Insert(new UserModel { Username = "admin", Password = "888888", SecurityQuestion = "默认恢复密钥", SecurityAnswer = "888888" });
                }

                // 智能数据迁移与填充
                MigrateAndEnrichData(db);
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        private void MigrateAndEnrichData(SQLiteConnection db)
        {
            // 1. 商品档案补全
            if (db.Table<ProductModel>().Count() == 0)
            {
                var p1 = db.QueryScalars<string>("SELECT DISTINCT ProductName FROM InboundModel");
                var p2 = db.QueryScalars<string>("SELECT DISTINCT ProductName FROM OutboundModel");
                // 🟢 批发商品来源变更为 WholesaleItem 表
                var p3 = db.QueryScalars<string>("SELECT DISTINCT ProductName FROM WholesaleItem");
                var allProducts = p1.Union(p2).Union(p3).Where(x => !string.IsNullOrEmpty(x)).Distinct();
                db.InsertAll(allProducts.Select(name => new ProductModel { Name = name }));
            }

            // 2. 客户档案补全
            if (db.Table<CustomerModel>().Count() == 0)
            {
                var c1 = db.QueryScalars<string>("SELECT DISTINCT Customer FROM OutboundModel");
                var c2 = db.QueryScalars<string>("SELECT DISTINCT Customer FROM WholesaleOrder"); // 🟢 查主表
                var allCustomers = c1.Union(c2).Where(x => !string.IsNullOrEmpty(x)).Distinct();
                db.InsertAll(allCustomers.Select(name => new CustomerModel { Name = name }));
            }

            // 3. 供应商档案补全
            if (db.Table<SupplierModel>().Count() == 0)
            {
                var suppliers = db.QueryScalars<string>("SELECT DISTINCT Supplier FROM InboundModel").Where(x => !string.IsNullOrEmpty(x)).Distinct();
                db.InsertAll(suppliers.Select(name => new SupplierModel { Name = name }));
            }
        }

        // --- 🟢 批发业务 (一单多品) ---

        // 获取所有批发单 (包含明细)
        public async Task<List<WholesaleOrder>> GetWholesaleOrdersAsync()
        {
            var orders = await _database.Table<WholesaleOrder>().OrderByDescending(x => x.OrderDate).ToListAsync();
            foreach (var order in orders)
            {
                order.Items = await _database.Table<WholesaleItem>().Where(x => x.OrderId == order.Id).ToListAsync();
            }
            return orders;
        }

        // 保存批发单 (主表+明细表)
        public async Task SaveWholesaleOrderAsync(WholesaleOrder order)
        {
            if (order.Id == 0)
            {
                await _database.InsertAsync(order); // 先插入主表获取ID
            }
            else
            {
                await _database.UpdateAsync(order);
                // 更新时，简单策略：先删旧明细，再插新明细
                var oldItems = await _database.Table<WholesaleItem>().Where(x => x.OrderId == order.Id).ToListAsync();
                foreach (var item in oldItems) await _database.DeleteAsync(item);
            }

            foreach (var item in order.Items)
            {
                item.Id = 0; // 重置ID以作为新记录插入
                item.OrderId = order.Id; // 关联主单ID
                await _database.InsertAsync(item);
            }
        }

        // 删除批发单 (级联删除明细)
        public async Task DeleteWholesaleOrderAsync(WholesaleOrder order)
        {
            var items = await _database.Table<WholesaleItem>().Where(x => x.OrderId == order.Id).ToListAsync();
            foreach (var item in items) await _database.DeleteAsync(item);
            await _database.DeleteAsync(order);
        }

        // 历史查询 (用于档案详情，仅查明细)
        public async Task<List<WholesaleItem>> GetWholesalesByProductAsync(string name) =>
            await _database.Table<WholesaleItem>().Where(x => x.ProductName == name).ToListAsync();

        // 用于客户详情查询 (暂不返回明细，只返回主单)
        public async Task<List<WholesaleOrder>> GetWholesalesByCustomerAsync(string name) =>
            await _database.Table<WholesaleOrder>().Where(x => x.Customer == name).OrderByDescending(x => x.OrderDate).ToListAsync();


        // --- 原有业务 CRUD (保持不变) ---
        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel i) => i.Id != 0 ? _database.UpdateAsync(i) : _database.InsertAsync(i);
        public Task DeleteInboundOrderAsync(InboundModel i) => _database.DeleteAsync(i);

        public Task<List<OutboundModel>> GetOutboundOrdersAsync() => _database.Table<OutboundModel>().ToListAsync();
        public Task SaveOutboundOrderAsync(OutboundModel i) => i.Id != 0 ? _database.UpdateAsync(i) : _database.InsertAsync(i);
        public Task DeleteOutboundOrderAsync(OutboundModel i) => _database.DeleteAsync(i);

        public Task<List<ReturnModel>> GetReturnOrdersAsync() => _database.Table<ReturnModel>().ToListAsync();
        public Task SaveReturnOrderAsync(ReturnModel i) => i.Id != 0 ? _database.UpdateAsync(i) : _database.InsertAsync(i);
        public Task DeleteReturnOrderAsync(ReturnModel i) => _database.DeleteAsync(i);

        // --- 档案管理 CRUD ---
        public Task<List<ProductModel>> GetProductsAsync() => _database.Table<ProductModel>().ToListAsync();
        public Task SaveProductAsync(ProductModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteProductAsync(ProductModel item) => _database.DeleteAsync(item);

        public Task<List<CustomerModel>> GetCustomersAsync() => _database.Table<CustomerModel>().ToListAsync();
        public Task SaveCustomerAsync(CustomerModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteCustomerAsync(CustomerModel item) => _database.DeleteAsync(item);

        public Task<List<SupplierModel>> GetSuppliersAsync() => _database.Table<SupplierModel>().ToListAsync();
        public Task SaveSupplierAsync(SupplierModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteSupplierAsync(SupplierModel item) => _database.DeleteAsync(item);

        // --- 辅助查询 ---
        public async Task<List<string>> GetProductListAsync()
        {
            var list = await _database.Table<ProductModel>().ToListAsync();
            return list.Select(x => x.Name!).Distinct().ToList();
        }
        public async Task<List<string>> GetSupplierListAsync()
        {
            var list = await _database.Table<SupplierModel>().ToListAsync();
            return list.Select(x => x.Name!).Distinct().ToList();
        }
        public async Task<List<string>> GetCustomerListAsync()
        {
            var list = await _database.Table<CustomerModel>().ToListAsync();
            return list.Select(x => x.Name!).Distinct().ToList();
        }
        public async Task<List<string>> GetShippedProductListAsync() => await GetProductListAsync();

        // --- 历史单据查询 ---
        public Task<List<InboundModel>> GetInboundsByProductAsync(string name) => _database.Table<InboundModel>().Where(x => x.ProductName == name).OrderByDescending(x => x.InboundDate).ToListAsync();
        public Task<List<OutboundModel>> GetOutboundsByProductAsync(string name) => _database.Table<OutboundModel>().Where(x => x.ProductName == name).OrderByDescending(x => x.OutboundDate).ToListAsync();
        public Task<List<ReturnModel>> GetReturnsByProductAsync(string name) => _database.Table<ReturnModel>().Where(x => x.ProductName == name).OrderByDescending(x => x.ReturnDate).ToListAsync();
        public Task<List<OutboundModel>> GetOutboundsByCustomerAsync(string name) => _database.Table<OutboundModel>().Where(x => x.Customer == name).OrderByDescending(x => x.OutboundDate).ToListAsync();
        public Task<List<ReturnModel>> GetReturnsByCustomerAsync(string name) => _database.Table<ReturnModel>().Where(x => x.Customer == name).OrderByDescending(x => x.ReturnDate).ToListAsync();
        public Task<List<InboundModel>> GetInboundsBySupplierAsync(string name) => _database.Table<InboundModel>().Where(x => x.Supplier == name).OrderByDescending(x => x.InboundDate).ToListAsync();

        // --- 用户逻辑 ---
        public async Task<UserModel?> LoginAsync(string username, string password) => await _database.Table<UserModel>().Where(u => u.Username == username && u.Password == password).FirstOrDefaultAsync();
        public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            var user = await _database.Table<UserModel>().Where(u => u.Username == username && u.Password == oldPassword).FirstOrDefaultAsync();
            if (user == null) return false;
            user.Password = newPassword;
            await _database.UpdateAsync(user);
            return true;
        }
        public async Task<bool> VerifyAndResetPasswordAsync(string username, string answer, string newPassword)
        {
            var user = await _database.Table<UserModel>().Where(u => u.Username == username).FirstOrDefaultAsync();
            if (user == null) return false;
            if (string.Equals(user.SecurityAnswer, answer, StringComparison.OrdinalIgnoreCase)) { user.Password = newPassword; await _database.UpdateAsync(user); return true; }
            return false;
        }
        public async Task<string> GetSecurityQuestionAsync(string username) { var user = await _database.Table<UserModel>().Where(u => u.Username == username).FirstOrDefaultAsync(); return user?.SecurityQuestion ?? "未找到用户"; }

        // --- 统计与报表 ---
        public Task<int> GetTotalInboundCountAsync() => _database.Table<InboundModel>().CountAsync();
        public Task<int> GetTotalOutboundCountAsync() => _database.Table<OutboundModel>().CountAsync();
        public Task<int> GetTotalReturnCountAsync() => _database.Table<ReturnModel>().CountAsync();
        public Task<int> GetTotalWholesaleCountAsync() => _database.Table<WholesaleOrder>().CountAsync(); // 🟢 查主表

        // 🟢 修复：安全处理空值
        private async Task<decimal> GetTableTotalAmountAsync<T>(string tableName) where T : new()
        {
            string sql = $"SELECT SUM(Price * Quantity) FROM {tableName}";
            if (tableName == nameof(InboundModel)) sql += " WHERE Status = '已验收'";
            try
            {
                var result = await _database.ExecuteScalarAsync<decimal?>(sql);
                return result.GetValueOrDefault();
            }
            catch { return 0m; }
        }

        // 批发总金额：直接查主表
        public async Task<decimal> GetTotalWholesaleAmountAsync()
        {
            try
            {
                var result = await _database.ExecuteScalarAsync<decimal?>("SELECT SUM(TotalAmount) FROM WholesaleOrder");
                return result.GetValueOrDefault();
            }
            catch { return 0m; }
        }

        public Task<decimal> GetTotalInboundAmountAsync() => GetTableTotalAmountAsync<InboundModel>(nameof(InboundModel));
        public Task<decimal> GetTotalOutboundAmountAsync() => GetTableTotalAmountAsync<OutboundModel>(nameof(OutboundModel));
        public async Task<decimal> GetTotalReturnAmountAsync() => await GetTableTotalAmountAsync<ReturnModel>(nameof(ReturnModel));

        // 🟢 库存统计 (需扣减 WholesaleItem 中的数量)
        public async Task<List<InventorySummaryModel>> GetInventorySummaryAsync()
        {
            var inbounds = await _database.Table<InboundModel>().ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().ToListAsync();
            var returns = await _database.Table<ReturnModel>().ToListAsync();
            var wholesaleItems = await _database.Table<WholesaleItem>().ToListAsync(); // 🟢 读取所有批发商品

            var allProducts = inbounds.Select(x => x.ProductName)
                .Union(outbounds.Select(x => x.ProductName))
                .Union(wholesaleItems.Select(x => x.ProductName)) // 🟢
                .Distinct().Where(x => !string.IsNullOrEmpty(x)).ToList();

            var list = new List<InventorySummaryModel>();
            foreach (var name in allProducts)
            {
                var inQty = inbounds.Where(x => x.ProductName == name && x.Status == "已验收").Sum(x => x.AcceptedQuantity);
                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var wsQty = wholesaleItems.Where(x => x.ProductName == name).Sum(x => x.Quantity); // 🟢 批发数量
                var retQty = returns.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                // 🟢 库存公式：入库 - 出库 - 批发 + 退货
                var currentStock = inQty - outQty - wsQty + retQty;

                // 成本均价
                decimal avgPrice = 0;
                var accepted = inbounds.Where(x => x.ProductName == name && x.Status == "已验收").ToList();
                if (accepted.Any() && accepted.Sum(x => x.AcceptedQuantity) > 0)
                    avgPrice = accepted.Sum(x => x.AcceptedQuantity * x.Price) / accepted.Sum(x => x.AcceptedQuantity);

                list.Add(new InventorySummaryModel
                {
                    ProductName = name,
                    TotalInbound = inQty,
                    TotalOutbound = outQty + wsQty, // 总出库 = 零售 + 批发
                    CurrentStock = currentStock,
                    TotalAmount = currentStock * avgPrice
                });
            }
            return list.OrderByDescending(x => x.CurrentStock).ToList();
        }

        // 🟢 财务报表 (需统计 WholesaleItem 的收入)
        public async Task<List<FinancialSummaryModel>> GetFinancialSummaryAsync(DateTime start, DateTime end)
        {
            var inbounds = await _database.Table<InboundModel>().Where(x => x.InboundDate >= start && x.InboundDate <= end).ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToListAsync();
            var returns = await _database.Table<ReturnModel>().Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToListAsync();

            // 🟢 查询符合日期的主单，再找明细
            var wsOrders = await _database.Table<WholesaleOrder>().Where(x => x.OrderDate >= start && x.OrderDate <= end).ToListAsync();
            var wsOrderIds = wsOrders.Select(x => x.Id).ToList();
            var wsItems = new List<WholesaleItem>();
            if (wsOrderIds.Any())
            {
                var allItems = await _database.Table<WholesaleItem>().ToListAsync();
                wsItems = allItems.Where(x => wsOrderIds.Contains(x.OrderId)).ToList();
            }

            var allProducts = inbounds.Select(x => x.ProductName)
                .Union(outbounds.Select(x => x.ProductName))
                .Union(wsItems.Select(x => x.ProductName))
                .Distinct().Where(x => !string.IsNullOrEmpty(x)).ToList();

            var list = new List<FinancialSummaryModel>();
            foreach (var name in allProducts)
            {
                var cost = inbounds.Where(x => x.ProductName == name && x.Status == "已验收").Sum(x => x.AcceptedQuantity * x.Price);
                var outRev = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var wsRev = wsItems.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price); // 🟢 批发收入
                var refd = returns.Where(x => x.ProductName == name).Sum(x => x.Price * x.Quantity);

                list.Add(new FinancialSummaryModel
                {
                    ProductName = name,
                    TotalCost = cost,
                    TotalRevenue = outRev + wsRev,
                    TotalRefund = refd
                });
            }
            return list.OrderByDescending(x => x.GrossProfit).ToList();
        }

        // 🟢 周期报表 (包含批发)
        public async Task<List<FinancialReportModel>> GetPeriodReportAsync(bool isMonthly, DateTime start, DateTime end)
        {
            var inbounds = await _database.Table<InboundModel>().Where(x => x.InboundDate >= start && x.InboundDate <= end).ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToListAsync();
            var returns = await _database.Table<ReturnModel>().Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToListAsync();
            var wsOrders = await _database.Table<WholesaleOrder>().Where(x => x.OrderDate >= start && x.OrderDate <= end).ToListAsync(); // 🟢

            string dateFormat = isMonthly ? "yyyy-MM" : "yyyy";
            var periods = inbounds.Select(x => x.InboundDate.ToString(dateFormat))
                .Union(outbounds.Select(x => x.OutboundDate.ToString(dateFormat)))
                .Union(returns.Select(x => x.ReturnDate.ToString(dateFormat)))
                .Union(wsOrders.Select(x => x.OrderDate.ToString(dateFormat))) // 🟢
                .Distinct().OrderByDescending(x => x).ToList();

            var report = new List<FinancialReportModel>();
            foreach (var p in periods)
            {
                var currentIn = inbounds.Where(x => x.InboundDate.ToString(dateFormat) == p).ToList();
                var currentOut = outbounds.Where(x => x.OutboundDate.ToString(dateFormat) == p).ToList();
                var currentRet = returns.Where(x => x.ReturnDate.ToString(dateFormat) == p).ToList();
                var currentWsOrder = wsOrders.Where(x => x.OrderDate.ToString(dateFormat) == p).ToList(); // 🟢

                // 获取本周期的批发单ID
                var wsIds = currentWsOrder.Select(x => x.Id).ToList();
                // 暂时简单处理：获取所有明细然后过滤（如果数据量极大建议优化SQL）
                var allItems = await _database.Table<WholesaleItem>().ToListAsync();
                var currentWsItems = allItems.Where(x => wsIds.Contains(x.OrderId)).ToList();

                DateTime.TryParse(p + (isMonthly ? "-01" : "-01-01"), out DateTime periodDate);

                var products = currentIn.Select(x => x.ProductName)
                    .Union(currentOut.Select(x => x.ProductName))
                    .Union(currentRet.Select(x => x.ProductName))
                    .Union(currentWsItems.Select(x => x.ProductName))
                    .Distinct().ToList();

                var details = new List<FinancialDetailModel>();
                foreach (var prod in products)
                {
                    if (string.IsNullOrEmpty(prod)) continue;
                    details.Add(new FinancialDetailModel
                    {
                        ProductName = prod,
                        Cost = currentIn.Where(x => x.ProductName == prod && x.Status == "已验收").Sum(x => x.AcceptedQuantity * x.Price),
                        Revenue = currentOut.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price)
                                + currentWsItems.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price), // 🟢 批发
                        Refund = currentRet.Where(x => x.ProductName == prod).Sum(x => x.Price * x.Quantity)
                    });
                }
                report.Add(new FinancialReportModel { PeriodName = p + (isMonthly ? " 月" : " 年"), PeriodDate = periodDate, Cost = details.Sum(x => x.Cost), Revenue = details.Sum(x => x.Revenue), Refund = details.Sum(x => x.Refund), Details = details });
            }
            return report;
        }

        // 辅助方法
        public async Task<InboundModel?> GetLastInboundByProductAsync(string n) => await _database.Table<InboundModel>().Where(x => x.ProductName == n).OrderByDescending(x => x.InboundDate).FirstOrDefaultAsync();
        public async Task<OutboundModel?> GetLastOutboundByProductAsync(string n) => await _database.Table<OutboundModel>().Where(x => x.ProductName == n).OrderByDescending(x => x.OutboundDate).FirstOrDefaultAsync();
        public async Task<ReturnModel?> GetLastReturnByProductAsync(string n) => await _database.Table<ReturnModel>().Where(x => x.ProductName == n).OrderByDescending(x => x.ReturnDate).FirstOrDefaultAsync();
    }
}