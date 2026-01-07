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

                // 🟢 关键修复：迁移旧数据
                // 如果是以前录入的数据没有Status字段，或者为NULL，默认设为 "已验收"，否则库存会变0
                // 注意：SQLite添加新列后默认是null，这里做一个容错更新
                try
                {
                    // 检查是否存在 Status 列，如果表结构已自动更新，这里确保数据正确
                    var count = db.Execute("UPDATE InboundModel SET Status = '已验收' WHERE Status IS NULL OR Status = ''");
                }
                catch { /* 忽略异常 */ }
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        // --- 用户管理 ---
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

        // --- 统计计数 ---
        public Task<int> GetTotalInboundCountAsync() => _database.Table<InboundModel>().CountAsync();
        public Task<int> GetTotalOutboundCountAsync() => _database.Table<OutboundModel>().CountAsync();
        public Task<int> GetTotalReturnCountAsync() => _database.Table<ReturnModel>().CountAsync();

        // --- 总金额计算 ---
        private async Task<decimal> GetTableTotalAmountAsync<T>(string tableName) where T : new()
        {
            string sql = $"SELECT SUM(Price * Quantity) FROM {tableName}";
            // 🟢 如果是 InboundModel，只计算已验收的金额
            if (tableName == nameof(InboundModel))
            {
                sql += " WHERE Status = '已验收'";
            }

            try
            {
                var result = await _database.ExecuteScalarAsync<decimal?>(sql);
                return result ?? 0m;
            }
            catch
            {
                return 0m;
            }
        }

        public Task<decimal> GetTotalInboundAmountAsync() => GetTableTotalAmountAsync<InboundModel>(nameof(InboundModel));
        public Task<decimal> GetTotalOutboundAmountAsync() => GetTableTotalAmountAsync<OutboundModel>(nameof(OutboundModel));
        public async Task<decimal> GetTotalReturnAmountAsync() => await GetTableTotalAmountAsync<ReturnModel>(nameof(ReturnModel));

        // --- 财务报表 ---
        public async Task<List<FinancialSummaryModel>> GetFinancialSummaryAsync(DateTime start, DateTime end)
        {
            var allIn = await _database.Table<InboundModel>().ToListAsync();
            var allOut = await _database.Table<OutboundModel>().ToListAsync();
            var allRet = await _database.Table<ReturnModel>().ToListAsync();

            var inbounds = allIn.Where(x => x.InboundDate >= start && x.InboundDate <= end).ToList();
            var outbounds = allOut.Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToList();
            var returns = allRet.Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToList();

            var allProducts = inbounds.Select(x => x.ProductName)
                                      .Union(outbounds.Select(x => x.ProductName))
                                      .Union(returns.Select(x => x.ProductName))
                                      .Where(x => !string.IsNullOrEmpty(x))
                                      .Distinct()
                                      .Select(x => x!)
                                      .ToList();

            var list = new List<FinancialSummaryModel>();
            foreach (var name in allProducts)
            {
                // 🟢 成本计算：只计算已验收的入库单
                var cost = inbounds.Where(x => x.ProductName == name && x.Status == "已验收")
                                   .Sum(x => x.Quantity * x.Price);

                var rev = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var refd = returns.Where(x => x.ProductName == name).Sum(x => x.Price * x.Quantity);

                list.Add(new FinancialSummaryModel
                {
                    ProductName = name,
                    TotalCost = cost,
                    TotalRevenue = rev,
                    TotalRefund = refd
                });
            }
            return list.OrderByDescending(x => x.GrossProfit).ToList();
        }

        public async Task<List<FinancialReportModel>> GetPeriodReportAsync(bool isMonthly, DateTime start, DateTime end)
        {
            var allIn = await _database.Table<InboundModel>().ToListAsync();
            var allOut = await _database.Table<OutboundModel>().ToListAsync();
            var allRet = await _database.Table<ReturnModel>().ToListAsync();

            var inbounds = allIn.Where(x => x.InboundDate >= start && x.InboundDate <= end).ToList();
            var outbounds = allOut.Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToList();
            var returns = allRet.Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToList();

            string dateFormat = isMonthly ? "yyyy-MM" : "yyyy";

            var periods = inbounds.Select(x => x.InboundDate.ToString(dateFormat))
                                  .Union(outbounds.Select(x => x.OutboundDate.ToString(dateFormat)))
                                  .Union(returns.Select(x => x.ReturnDate.ToString(dateFormat)))
                                  .Distinct()
                                  .OrderByDescending(x => x)
                                  .ToList();

            var report = new List<FinancialReportModel>();

            foreach (var p in periods)
            {
                var currentIn = inbounds.Where(x => x.InboundDate.ToString(dateFormat) == p).ToList();
                var currentOut = outbounds.Where(x => x.OutboundDate.ToString(dateFormat) == p).ToList();
                var currentRet = returns.Where(x => x.ReturnDate.ToString(dateFormat) == p).ToList();

                DateTime periodDate = DateTime.MinValue;
                DateTime.TryParse(p + (isMonthly ? "-01" : "-01-01"), out periodDate);

                var productsInPeriod = currentIn.Select(x => x.ProductName)
                                                .Union(currentOut.Select(x => x.ProductName))
                                                .Union(currentRet.Select(x => x.ProductName))
                                                .Where(x => !string.IsNullOrEmpty(x))
                                                .Distinct()
                                                .Select(x => x!)
                                                .ToList();

                var details = new List<FinancialDetailModel>();

                foreach (var prod in productsInPeriod)
                {
                    details.Add(new FinancialDetailModel
                    {
                        ProductName = prod,
                        // 🟢 成本只算已验收
                        Cost = currentIn.Where(x => x.ProductName == prod && x.Status == "已验收").Sum(x => x.Quantity * x.Price),
                        Revenue = currentOut.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price),
                        Refund = currentRet.Where(x => x.ProductName == prod).Sum(x => x.Price * x.Quantity)
                    });
                }

                report.Add(new FinancialReportModel
                {
                    PeriodName = p + (isMonthly ? " 月" : " 年"),
                    PeriodDate = periodDate,
                    Cost = details.Sum(x => x.Cost),
                    Revenue = details.Sum(x => x.Revenue),
                    Refund = details.Sum(x => x.Refund),
                    Details = details.OrderByDescending(x => x.Profit).ToList()
                });
            }

            return report;
        }

        // --- 库存概览 ---
        public async Task<List<InventorySummaryModel>> GetInventorySummaryAsync()
        {
            var inbounds = await _database.Table<InboundModel>().ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().ToListAsync();
            var returns = await _database.Table<ReturnModel>().ToListAsync();

            var allProducts = inbounds.Select(x => x.ProductName)
                                      .Union(outbounds.Select(x => x.ProductName))
                                      .Union(returns.Select(x => x.ProductName))
                                      .Where(x => !string.IsNullOrEmpty(x))
                                      .Distinct()
                                      .Select(x => x!)
                                      .ToList();

            var summaryList = new List<InventorySummaryModel>();
            foreach (var name in allProducts)
            {
                var inList = inbounds.Where(x => x.ProductName == name).ToList();

                // 🟢 核心修改：入库量 = 仅状态为 "已验收" 的数量
                // "待验收" 的不计入库存，"已退货" 的也不计入
                var inQty = inList.Where(x => x.Status == "已验收").Sum(x => x.Quantity);

                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                // 退货单(ReturnModel)是客户退给我们的，所以要加回库存
                var retQty = returns.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                var currentStock = inQty - outQty + retQty;

                // 计算平均成本（只基于已验收的）
                decimal avgPrice = 0;
                var acceptedInList = inList.Where(x => x.Status == "已验收").ToList();
                if (acceptedInList.Any())
                {
                    var totalInCost = acceptedInList.Sum(x => x.Quantity * x.Price);
                    var totalInQty = acceptedInList.Sum(x => x.Quantity);
                    if (totalInQty > 0)
                        avgPrice = totalInCost / totalInQty;
                }

                summaryList.Add(new InventorySummaryModel
                {
                    ProductName = name,
                    TotalInbound = inQty,
                    TotalOutbound = outQty,
                    CurrentStock = currentStock,
                    TotalAmount = currentStock * avgPrice
                });
            }
            return summaryList.OrderByDescending(x => x.CurrentStock).ToList();
        }

        // --- 获取最近一次交易记录 (自动填充) ---
        public async Task<InboundModel?> GetLastInboundByProductAsync(string productName)
        {
            return await _database.Table<InboundModel>()
                                  .Where(x => x.ProductName == productName)
                                  .OrderByDescending(x => x.InboundDate)
                                  .FirstOrDefaultAsync();
        }

        public async Task<OutboundModel?> GetLastOutboundByProductAsync(string productName)
        {
            return await _database.Table<OutboundModel>()
                                  .Where(x => x.ProductName == productName)
                                  .OrderByDescending(x => x.OutboundDate)
                                  .FirstOrDefaultAsync();
        }

        public async Task<ReturnModel?> GetLastReturnByProductAsync(string productName)
        {
            return await _database.Table<ReturnModel>()
                                  .Where(x => x.ProductName == productName)
                                  .OrderByDescending(x => x.ReturnDate)
                                  .FirstOrDefaultAsync();
        }

        // --- 业务数据操作 ---
        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteInboundOrderAsync(InboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetSupplierListAsync()
        {
            var result = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT Supplier FROM InboundModel");
            return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        }

        public Task<List<OutboundModel>> GetOutboundOrdersAsync() => _database.Table<OutboundModel>().ToListAsync();
        public Task SaveOutboundOrderAsync(OutboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteOutboundOrderAsync(OutboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetCustomerListAsync()
        {
            var result = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT Customer FROM OutboundModel");
            return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        }

        public Task<List<ReturnModel>> GetReturnOrdersAsync() => _database.Table<ReturnModel>().ToListAsync();
        public Task SaveReturnOrderAsync(ReturnModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteReturnOrderAsync(ReturnModel item) => _database.DeleteAsync(item);

        public async Task<List<string>> GetProductListAsync()
        {
            var result = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM InboundModel");
            return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        }

        public async Task<List<string>> GetShippedProductListAsync()
        {
            var result = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM OutboundModel");
            return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        }
    }
}