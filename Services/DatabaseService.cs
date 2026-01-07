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
                var cost = inbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var rev = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var refd = returns.Where(x => x.ProductName == name).Sum(x => x.Price * x.Quantity); // 修正：退货也按总价算

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
                        Cost = currentIn.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price),
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
                var inQty = inList.Sum(x => x.Quantity);
                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var retQty = returns.Where(x => x.ProductName == name).Sum(x => x.Quantity);

                var currentStock = inQty - outQty + retQty;

                decimal avgPrice = 0;
                if (inList.Any())
                {
                    var totalInCost = inList.Sum(x => x.Quantity * x.Price);
                    var totalInQty = inList.Sum(x => x.Quantity);
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

        // --- 🟢 新增：获取最近一次交易记录 (用于自动填充) ---

        public async Task<InboundModel?> GetLastInboundByProductAsync(string productName)
        {
            // 获取该产品最后一次入库的记录
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