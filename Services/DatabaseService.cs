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

            // 同步连接用于初始化表和默认数据
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


        // ==========================================
        //  新增：总金额计算服务 (用于主页和各业务页)
        // ==========================================

        /// <summary>
        /// 【通用方法】计算指定表的总金额 (数量 * 单价)
        /// 直接使用 SQL 计算，性能极高，避免加载所有数据到内存
        /// </summary>
        /// <typeparam name="T">表实体类型</typeparam>
        /// <returns>总金额</returns>
        private async Task<decimal> GetTableTotalAmountAsync<T>(string tableName) where T : new()
        {
            // 注意：SQL 语句假设你的模型中都有 Price 和 Quantity 字段
            string sql = $"SELECT SUM(Price * Quantity) FROM {tableName}";

            try
            {
                // ExecuteScalarAsync 在没有数据时可能返回 null，需做处理
                var result = await _database.ExecuteScalarAsync<decimal?>(sql);
                return result ?? 0m;
            }
            catch
            {
                // 容错处理：如果发生异常（例如表为空或字段不存在），返回0
                return 0m;
            }
        }

        // --- 1. 主页/仪表盘 (Dashboard) 总览数据 ---

        public Task<decimal> GetTotalInboundAmountAsync()
            => GetTableTotalAmountAsync<InboundModel>(nameof(InboundModel));

        public Task<decimal> GetTotalOutboundAmountAsync()
            => GetTableTotalAmountAsync<OutboundModel>(nameof(OutboundModel));

        public async Task<decimal> GetTotalReturnAmountAsync()
        {
            // 假设 ReturnModel 也有 Price 和 Quantity。如果是只有总价，请自行调整 SQL。
            return await GetTableTotalAmountAsync<ReturnModel>(nameof(ReturnModel));
        }

        // --- 2. 业务页面特定查询 (带筛选功能的总额计算) ---
        // 修复 CS8625: 参数改为 string? 允许为空

        /// <summary>
        /// 【入库页】计算筛选条件下的入库总金额
        /// </summary>
        public async Task<decimal> GetInboundTotalByFilterAsync(DateTime? start, DateTime? end, string? productName = null)
        {
            var query = _database.Table<InboundModel>();

            if (start.HasValue) query = query.Where(x => x.InboundDate >= start.Value);
            if (end.HasValue) query = query.Where(x => x.InboundDate <= end.Value);

            // 修复 CS8602: 增加非空检查
            if (!string.IsNullOrEmpty(productName))
            {
                query = query.Where(x => x.ProductName != null && x.ProductName.Contains(productName));
            }

            var list = await query.ToListAsync();
            return list.Sum(x => x.Price * x.Quantity);
        }

        /// <summary>
        /// 【出库页】计算筛选条件下的出库总金额
        /// </summary>
        public async Task<decimal> GetOutboundTotalByFilterAsync(DateTime? start, DateTime? end, string? customer = null)
        {
            var query = _database.Table<OutboundModel>();

            if (start.HasValue) query = query.Where(x => x.OutboundDate >= start.Value);
            if (end.HasValue) query = query.Where(x => x.OutboundDate <= end.Value);

            if (!string.IsNullOrEmpty(customer))
            {
                query = query.Where(x => x.Customer != null && x.Customer.Contains(customer));
            }

            var list = await query.ToListAsync();
            return list.Sum(x => x.Price * x.Quantity);
        }

        /// <summary>
        /// 【退货页】计算筛选条件下的退货总金额
        /// </summary>
        public async Task<decimal> GetReturnTotalByFilterAsync(DateTime? start, DateTime? end)
        {
            var query = _database.Table<ReturnModel>();

            if (start.HasValue) query = query.Where(x => x.ReturnDate >= start.Value);
            if (end.HasValue) query = query.Where(x => x.ReturnDate <= end.Value);

            var list = await query.ToListAsync();
            return list.Sum(x => x.Price * x.Quantity);
        }

        // --- 财务报表 ---

        // 1. 单品财务汇总 (支持日期筛选)
        public async Task<List<FinancialSummaryModel>> GetFinancialSummaryAsync(DateTime start, DateTime end)
        {
            var allIn = await _database.Table<InboundModel>().ToListAsync();
            var allOut = await _database.Table<OutboundModel>().ToListAsync();
            var allRet = await _database.Table<ReturnModel>().ToListAsync();

            // 筛选符合日期的记录
            var inbounds = allIn.Where(x => x.InboundDate >= start && x.InboundDate <= end).ToList();
            var outbounds = allOut.Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToList();
            var returns = allRet.Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToList();

            // 修复 CS8601/CS8604: 显式过滤 null 值，并使用 (!) 断言非空
            var allProducts = inbounds.Select(x => x.ProductName)
                                      .Union(outbounds.Select(x => x.ProductName))
                                      .Union(returns.Select(x => x.ProductName))
                                      .Where(x => !string.IsNullOrEmpty(x))
                                      .Distinct()
                                      .Select(x => x!) // 断言 x 不为 null
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

        // 2. 时间段报表 (支持日期筛选)
        public async Task<List<FinancialReportModel>> GetPeriodReportAsync(bool isMonthly, DateTime start, DateTime end)
        {
            var allIn = await _database.Table<InboundModel>().ToListAsync();
            var allOut = await _database.Table<OutboundModel>().ToListAsync();
            var allRet = await _database.Table<ReturnModel>().ToListAsync();

            var inbounds = allIn.Where(x => x.InboundDate >= start && x.InboundDate <= end).ToList();
            var outbounds = allOut.Where(x => x.OutboundDate >= start && x.OutboundDate <= end).ToList();
            var returns = allRet.Where(x => x.ReturnDate >= start && x.ReturnDate <= end).ToList();

            string dateFormat = isMonthly ? "yyyy-MM" : "yyyy";

            // 构造时间段列表
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

                // 同样进行非空过滤
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
                        Refund = currentRet.Where(x => x.ProductName == prod).Sum(x => x.Price)
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

        // --- 库存概览 (带总金额计算) ---
        public async Task<List<InventorySummaryModel>> GetInventorySummaryAsync()
        {
            var inbounds = await _database.Table<InboundModel>().ToListAsync();
            var outbounds = await _database.Table<OutboundModel>().ToListAsync();
            var returns = await _database.Table<ReturnModel>().ToListAsync();

            // 修复警告：过滤空产品名
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

                // 计算当前库存
                var currentStock = inQty - outQty + retQty;

                // 计算平均入库单价 (用于估算库存总值)
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
                    TotalAmount = currentStock * avgPrice // 库存总值
                });
            }
            return summaryList.OrderByDescending(x => x.CurrentStock).ToList();
        }

        // --- 业务数据操作 ---
        public Task<List<InboundModel>> GetInboundOrdersAsync() => _database.Table<InboundModel>().ToListAsync();
        public Task SaveInboundOrderAsync(InboundModel item) => item.Id != 0 ? _database.UpdateAsync(item) : _database.InsertAsync(item);
        public Task DeleteInboundOrderAsync(InboundModel item) => _database.DeleteAsync(item);

        // 修复：返回类型 string? 处理空值，Select(x => x!) 断言非空
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

        // 获取所有出现过的产品名称 (用于下拉框提示)
        public async Task<List<string>> GetProductListAsync()
        {
            var result = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM InboundModel");
            return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        }

        // 获取已出库过的产品名称
        public async Task<List<string>> GetShippedProductListAsync()
        {
            var result = await _database.QueryScalarsAsync<string?>("SELECT DISTINCT ProductName FROM OutboundModel");
            return result.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToList();
        }
    }
}