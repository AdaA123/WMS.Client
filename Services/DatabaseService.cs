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
                    db.Insert(new UserModel
                    {
                        Username = "admin",
                        Password = "888888",
                        SecurityQuestion = "默认恢复密钥",
                        SecurityAnswer = "888888"
                    });
                }

                // 修复老数据
                try
                {
                    db.Execute("UPDATE UserModel SET SecurityQuestion = '默认恢复密钥', SecurityAnswer = '888888' WHERE SecurityAnswer IS NULL OR SecurityAnswer = ''");
                    db.Execute("UPDATE InboundModel SET Status = '已验收' WHERE Status IS NULL OR Status = ''");
                    db.Execute("UPDATE InboundModel SET AcceptedQuantity = Quantity, CheckDate = InboundDate WHERE Status = '已验收' AND AcceptedQuantity = 0");
                }
                catch { }
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        // 🟢 修改：返回 UserModel 对象，而不是 bool
        public async Task<UserModel?> LoginAsync(string username, string password)
        {
            return await _database.Table<UserModel>()
                                  .Where(u => u.Username == username && u.Password == password)
                                  .FirstOrDefaultAsync();
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

        public async Task<bool> VerifyAndResetPasswordAsync(string username, string answer, string newPassword)
        {
            var user = await _database.Table<UserModel>().Where(u => u.Username == username).FirstOrDefaultAsync();
            if (user == null) return false;

            if (string.Equals(user.SecurityAnswer, answer, StringComparison.OrdinalIgnoreCase))
            {
                user.Password = newPassword;
                await _database.UpdateAsync(user);
                return true;
            }
            return false;
        }

        public async Task<string> GetSecurityQuestionAsync(string username)
        {
            var user = await _database.Table<UserModel>().Where(u => u.Username == username).FirstOrDefaultAsync();
            return user?.SecurityQuestion ?? "未找到该用户或未设置密保";
        }

        // --- 统计与业务 ---
        public Task<int> GetTotalInboundCountAsync() => _database.Table<InboundModel>().CountAsync();
        public Task<int> GetTotalOutboundCountAsync() => _database.Table<OutboundModel>().CountAsync();
        public Task<int> GetTotalReturnCountAsync() => _database.Table<ReturnModel>().CountAsync();

        private async Task<decimal> GetTableTotalAmountAsync<T>(string tableName) where T : new()
        {
            string sql = $"SELECT SUM(Price * Quantity) FROM {tableName}";
            if (tableName == nameof(InboundModel)) sql += " WHERE Status = '已验收'";
            try { var result = await _database.ExecuteScalarAsync<decimal?>(sql); return result ?? 0m; } catch { return 0m; }
        }

        public Task<decimal> GetTotalInboundAmountAsync() => GetTableTotalAmountAsync<InboundModel>(nameof(InboundModel));
        public Task<decimal> GetTotalOutboundAmountAsync() => GetTableTotalAmountAsync<OutboundModel>(nameof(OutboundModel));
        public async Task<decimal> GetTotalReturnAmountAsync() => await GetTableTotalAmountAsync<ReturnModel>(nameof(ReturnModel));

        public async Task<List<FinancialSummaryModel>> GetFinancialSummaryAsync(DateTime start, DateTime end)
        {
            var inbounds = await _database.Table<InboundModel>().Where(x => x.InboundDate >= start && x.InboundDate <= end).ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToListAsync();
            var returns = await _database.Table<ReturnModel>().Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToListAsync();

            var allProducts = inbounds.Select(x => x.ProductName).Union(outbounds.Select(x => x.ProductName)).Union(returns.Select(x => x.ProductName)).Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(x => x!).ToList();
            var list = new List<FinancialSummaryModel>();

            foreach (var name in allProducts)
            {
                var cost = inbounds.Where(x => x.ProductName == name && x.Status == "已验收").Sum(x => x.AcceptedQuantity * x.Price);
                var rev = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity * x.Price);
                var refd = returns.Where(x => x.ProductName == name).Sum(x => x.Price * x.Quantity);
                list.Add(new FinancialSummaryModel { ProductName = name, TotalCost = cost, TotalRevenue = rev, TotalRefund = refd });
            }
            return list.OrderByDescending(x => x.GrossProfit).ToList();
        }

        public async Task<List<FinancialReportModel>> GetPeriodReportAsync(bool isMonthly, DateTime start, DateTime end)
        {
            var inbounds = await _database.Table<InboundModel>().Where(x => x.InboundDate >= start && x.InboundDate <= end).ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToListAsync();
            var returns = await _database.Table<ReturnModel>().Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToListAsync();

            string dateFormat = isMonthly ? "yyyy-MM" : "yyyy";
            var periods = inbounds.Select(x => x.InboundDate.ToString(dateFormat)).Union(outbounds.Select(x => x.OutboundDate.ToString(dateFormat))).Union(returns.Select(x => x.ReturnDate.ToString(dateFormat))).Distinct().OrderByDescending(x => x).ToList();

            var report = new List<FinancialReportModel>();
            foreach (var p in periods)
            {
                var currentIn = inbounds.Where(x => x.InboundDate.ToString(dateFormat) == p).ToList();
                var currentOut = outbounds.Where(x => x.OutboundDate.ToString(dateFormat) == p).ToList();
                var currentRet = returns.Where(x => x.ReturnDate.ToString(dateFormat) == p).ToList();
                DateTime.TryParse(p + (isMonthly ? "-01" : "-01-01"), out DateTime periodDate);

                var products = currentIn.Select(x => x.ProductName).Union(currentOut.Select(x => x.ProductName)).Union(currentRet.Select(x => x.ProductName)).Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(x => x!).ToList();
                var details = new List<FinancialDetailModel>();
                foreach (var prod in products)
                {
                    details.Add(new FinancialDetailModel
                    {
                        ProductName = prod,
                        Cost = currentIn.Where(x => x.ProductName == prod && x.Status == "已验收").Sum(x => x.AcceptedQuantity * x.Price),
                        Revenue = currentOut.Where(x => x.ProductName == prod).Sum(x => x.Quantity * x.Price),
                        Refund = currentRet.Where(x => x.ProductName == prod).Sum(x => x.Price * x.Quantity)
                    });
                }
                report.Add(new FinancialReportModel { PeriodName = p + (isMonthly ? " 月" : " 年"), PeriodDate = periodDate, Cost = details.Sum(x => x.Cost), Revenue = details.Sum(x => x.Revenue), Refund = details.Sum(x => x.Refund), Details = details });
            }
            return report;
        }

        public async Task<List<InventorySummaryModel>> GetInventorySummaryAsync()
        {
            var inbounds = await _database.Table<InboundModel>().ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().ToListAsync();
            var returns = await _database.Table<ReturnModel>().ToListAsync();
            var allProducts = inbounds.Select(x => x.ProductName).Union(outbounds.Select(x => x.ProductName)).Union(returns.Select(x => x.ProductName)).Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(x => x!).ToList();

            var list = new List<InventorySummaryModel>();
            foreach (var name in allProducts)
            {
                var inQty = inbounds.Where(x => x.ProductName == name && x.Status == "已验收").Sum(x => x.AcceptedQuantity);
                var outQty = outbounds.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var retQty = returns.Where(x => x.ProductName == name).Sum(x => x.Quantity);
                var currentStock = inQty - outQty + retQty;

                decimal avgPrice = 0;
                var accepted = inbounds.Where(x => x.ProductName == name && x.Status == "已验收").ToList();
                if (accepted.Any() && accepted.Sum(x => x.AcceptedQuantity) > 0)
                    avgPrice = accepted.Sum(x => x.AcceptedQuantity * x.Price) / accepted.Sum(x => x.AcceptedQuantity);

                list.Add(new InventorySummaryModel { ProductName = name, TotalInbound = inQty, TotalOutbound = outQty, CurrentStock = currentStock, TotalAmount = currentStock * avgPrice });
            }
            return list.OrderByDescending(x => x.CurrentStock).ToList();
        }

        public async Task<InboundModel?> GetLastInboundByProductAsync(string n) => await _database.Table<InboundModel>().Where(x => x.ProductName == n).OrderByDescending(x => x.InboundDate).FirstOrDefaultAsync();
        public async Task<OutboundModel?> GetLastOutboundByProductAsync(string n) => await _database.Table<OutboundModel>().Where(x => x.ProductName == n).OrderByDescending(x => x.OutboundDate).FirstOrDefaultAsync();
        public async Task<ReturnModel?> GetLastReturnByProductAsync(string n) => await _database.Table<ReturnModel>().Where(x => x.ProductName == n).OrderByDescending(x => x.ReturnDate).FirstOrDefaultAsync();

        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel i) => i.Id != 0 ? _database.UpdateAsync(i) : _database.InsertAsync(i);
        public Task DeleteInboundOrderAsync(InboundModel i) => _database.DeleteAsync(i);
        public async Task<List<string>> GetSupplierListAsync() => (await _database.QueryScalarsAsync<string?>("SELECT DISTINCT Supplier FROM InboundModel")).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        public Task<List<OutboundModel>> GetOutboundOrdersAsync() => _database.Table<OutboundModel>().ToListAsync();
        public Task SaveOutboundOrderAsync(OutboundModel i) => i.Id != 0 ? _database.UpdateAsync(i) : _database.InsertAsync(i);
        public Task DeleteOutboundOrderAsync(OutboundModel i) => _database.DeleteAsync(i);
        public async Task<List<string>> GetCustomerListAsync() => (await _database.QueryScalarsAsync<string?>("SELECT DISTINCT Customer FROM OutboundModel")).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        public Task<List<ReturnModel>> GetReturnOrdersAsync() => _database.Table<ReturnModel>().ToListAsync();
        public Task SaveReturnOrderAsync(ReturnModel i) => i.Id != 0 ? _database.UpdateAsync(i) : _database.InsertAsync(i);
        public Task DeleteReturnOrderAsync(ReturnModel i) => _database.DeleteAsync(i);
        public async Task<List<string>> GetProductListAsync() => (await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM InboundModel")).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        public async Task<List<string>> GetShippedProductListAsync() => (await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM OutboundModel")).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
    }
}