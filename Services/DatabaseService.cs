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

                // 🟢 数据库迁移：确保旧数据兼容新逻辑
                try
                {
                    // 1. 确保 Status 字段存在
                    db.Execute("UPDATE InboundModel SET Status = '已验收' WHERE Status IS NULL OR Status = ''");

                    // 2. 确保新字段 AcceptedQuantity 有值（旧的已验收数据，合格数=总数）
                    // 逻辑：如果状态是已验收，且合格数为0（说明是新加字段），则把Quantity的值赋给AcceptedQuantity
                    db.Execute("UPDATE InboundModel SET AcceptedQuantity = Quantity, CheckDate = InboundDate WHERE Status = '已验收' AND AcceptedQuantity = 0");
                }
                catch { /* 忽略列已存在的错误 */ }
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        // --- 用户管理 ---
        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = await _database.Table<UserModel>().Where(u => u.Username == username && u.Password == password).FirstOrDefaultAsync();
            return user != null;
        }

        public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            var user = await _database.Table<UserModel>().Where(u => u.Username == username && u.Password == oldPassword).FirstOrDefaultAsync();
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
            if (tableName == nameof(InboundModel))
            {
                // 仅统计已验收的金额 (或者视财务需求而定，这里假设统计有效资产)
                sql += " WHERE Status = '已验收'";
            }
            try { var result = await _database.ExecuteScalarAsync<decimal?>(sql); return result ?? 0m; } catch { return 0m; }
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
                                      .Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(x => x!).ToList();

            var list = new List<FinancialSummaryModel>();
            foreach (var name in allProducts)
            {
                // 🟢 修正成本计算：使用 AcceptedQuantity
                var cost = inbounds.Where(x => x.ProductName == name && x.Status == "已验收")
                                   .Sum(x => x.AcceptedQuantity * x.Price);

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
                                  .Distinct().OrderByDescending(x => x).ToList();

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
                                                .Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(x => x!).ToList();

                var details = new List<FinancialDetailModel>();
                foreach (var prod in productsInPeriod)
                {
                    details.Add(new FinancialDetailModel
                    {
                        ProductName = prod,
                        // 🟢 修正成本计算：使用 AcceptedQuantity
                        Cost = currentIn.Where(x => x.ProductName == prod && x.Status == "已验收")
                                        .Sum(x => x.AcceptedQuantity * x.Price),
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
                                      .Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(x => x!).ToList();

            var summaryList = new List<InventorySummaryModel>();
            foreach (var name in allProducts)
            {
                var inList = inbounds.Where(x => x.ProductName == name).ToList();

                // 🟢 核心修改：入库量 = 已验收记录的【合格数量 AcceptedQuantity】
                var inQty = inList.Where(x => x.Status == "已验收").Sum(x => x.AcceptedQuantity);

                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var retQty = returns.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                var currentStock = inQty - outQty + retQty;

                decimal avgPrice = 0;
                // 计算均价时，也只考虑合格品
                var acceptedList = inList.Where(x => x.Status == "已验收").ToList();
                if (acceptedList.Any())
                {
                    var totalInCost = acceptedList.Sum(x => x.AcceptedQuantity * x.Price);
                    var totalInQty = acceptedList.Sum(x => x.AcceptedQuantity);
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

        // --- 历史记录查询 (保留) ---
        public async Task<InboundModel?> GetLastInboundByProductAsync(string productName)
            => await _database.Table<InboundModel>().Where(x => x.ProductName == productName).OrderByDescending(x => x.InboundDate).FirstOrDefaultAsync();
        public async Task<OutboundModel?> GetLastOutboundByProductAsync(string productName)
            => await _database.Table<OutboundModel>().Where(x => x.ProductName == productName).OrderByDescending(x => x.OutboundDate).FirstOrDefaultAsync();
        public async Task<ReturnModel?> GetLastReturnByProductAsync(string productName)
            => await _database.Table<ReturnModel>().Where(x => x.ProductName == productName).OrderByDescending(x => x.ReturnDate).FirstOrDefaultAsync();

        // --- 基础操作 (保留) ---
        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteInboundOrderAsync(InboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetSupplierListAsync() { var r = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT Supplier FROM InboundModel"); return r.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList(); }
        public Task<List<OutboundModel>> GetOutboundOrdersAsync() => _database.Table<OutboundModel>().ToListAsync();
        public Task SaveOutboundOrderAsync(OutboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteOutboundOrderAsync(OutboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetCustomerListAsync() { var r = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT Customer FROM OutboundModel"); return r.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList(); }
        public Task<List<ReturnModel>> GetReturnOrdersAsync() => _database.Table<ReturnModel>().ToListAsync();
        public Task SaveReturnOrderAsync(ReturnModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteReturnOrderAsync(ReturnModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetProductListAsync() { var r = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM InboundModel"); return r.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList(); }
        public async Task<List<string>> GetShippedProductListAsync() { var r = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM OutboundModel"); return r.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList(); }
    }
}