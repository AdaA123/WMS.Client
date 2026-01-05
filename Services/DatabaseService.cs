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

        public Task<int> GetTotalInboundCountAsync() => _database.Table<InboundModel>().CountAsync();
        public Task<int> GetTotalOutboundCountAsync() => _database.Table<OutboundModel>().CountAsync();
        public Task<int> GetTotalReturnCountAsync() => _database.Table<ReturnModel>().CountAsync();

        // 🟢 1. 单品财务汇总 (支持日期筛选)
        public async Task<List<FinancialSummaryModel>> GetFinancialSummaryAsync(DateTime start, DateTime end)
        {
            // 先获取所有数据，再在内存中筛选日期 (SQLite LINQ 对日期支持有时不稳定，内存筛选更稳健)
            var allIn = await _database.Table<InboundModel>().ToListAsync();
            var allOut = await _database.Table<OutboundModel>().ToListAsync();
            var allRet = await _database.Table<ReturnModel>().ToListAsync();

            // 筛选符合日期的记录
            var inbounds = allIn.Where(x => x.InboundDate >= start && x.InboundDate <= end).ToList();
            var outbounds = allOut.Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToList();
            var returns = allRet.Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToList();

            var allProducts = inbounds.Select(x => x.ProductName)
                                      .Union(outbounds.Select(x => x.ProductName))
                                      .Union(returns.Select(x => x.ProductName))
                                      .Distinct()
                                      .Where(x => !string.IsNullOrEmpty(x))
                                      .ToList();

            var list = new List<FinancialSummaryModel>();
            foreach (var name in allProducts)
            {
                var cost = inbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var rev = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var refd = returns.Where(x => x.ProductName == name).Sum(x => x.Price);

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

        // 🟢 2. 时间段报表 (支持日期筛选)
        public async Task<List<FinancialReportModel>> GetPeriodReportAsync(bool isMonthly, DateTime start, DateTime end)
        {
            var allIn = await _database.Table<InboundModel>().ToListAsync();
            var allOut = await _database.Table<OutboundModel>().ToListAsync();
            var allRet = await _database.Table<ReturnModel>().ToListAsync();

            // 筛选符合日期的记录
            var inbounds = allIn.Where(x => x.InboundDate >= start && x.InboundDate <= end).ToList();
            var outbounds = allOut.Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToList();
            var returns = allRet.Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToList();

            string dateFormat = isMonthly ? "yyyy-MM" : "yyyy";

            // 获取该范围内涉及的所有时间段
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

                // 解析时间段为日期对象，方便后续处理
                DateTime periodDate = DateTime.MinValue;
                DateTime.TryParse(p + (isMonthly ? "-01" : "-01-01"), out periodDate);

                var productsInPeriod = currentIn.Select(x => x.ProductName)
                                       .Union(currentOut.Select(x => x.ProductName))
                                       .Union(currentRet.Select(x => x.ProductName))
                                       .Distinct()
                                       .ToList();

                var details = new List<FinancialDetailModel>();

                foreach (var prod in productsInPeriod)
                {
                    details.Add(new FinancialDetailModel
                    {
                        ProductName = prod,
                        Cost = currentIn.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price),
                        Revenue = currentOut.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price),
                        Refund = currentRet.Where(x => x.ProductName == prod).Sum(x => x.Price)
                    });
                }

                report.Add(new FinancialReportModel
                {
                    PeriodName = p + (isMonthly ? " 月" : " 年"),
                    PeriodDate = periodDate, // 🟢 存入日期
                    Cost = details.Sum(x => x.Cost),
                    Revenue = details.Sum(x => x.Revenue),
                    Refund = details.Sum(x => x.Refund),
                    Details = details.OrderByDescending(x => x.Profit).ToList()
                });
            }

            return report;
        }

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

        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteInboundOrderAsync(InboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetSupplierListAsync() => await _database.QueryScalarsAsync<string>("SELECT DISTINCT Supplier FROM InboundModel WHERE Supplier IS NOT NULL");

        public Task<List<OutboundModel>> GetOutboundOrdersAsync() => _database.Table<OutboundModel>().ToListAsync();
        public Task SaveOutboundOrderAsync(OutboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteOutboundOrderAsync(OutboundModel item) => _database.DeleteAsync(item);
        public async Task<List<string>> GetCustomerListAsync() => await _database.QueryScalarsAsync<string>("SELECT DISTINCT Customer FROM OutboundModel WHERE Customer IS NOT NULL");

        public Task<List<ReturnModel>> GetReturnOrdersAsync() => _database.Table<ReturnModel>().ToListAsync();
        public Task SaveReturnOrderAsync(ReturnModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteReturnOrderAsync(ReturnModel item) => _database.DeleteAsync(item);

        public async Task<List<string>> GetProductListAsync()
            => await _database.QueryScalarsAsync<string>("SELECT DISTINCT ProductName FROM InboundModel WHERE ProductName IS NOT NULL");

        public async Task<List<string>> GetShippedProductListAsync()
            => await _database.QueryScalarsAsync<string>("SELECT DISTINCT ProductName FROM OutboundModel WHERE ProductName IS NOT NULL");
    }
}