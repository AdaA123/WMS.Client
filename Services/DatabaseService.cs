using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
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
            // 1. 设置路径
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _dbPath = Path.Combine(docPath, "WMS_Database.db");

            // 2. 同步初始化 (确保表结构存在)
            using (var db = new SQLiteConnection(_dbPath))
            {
                db.CreateTable<UserModel>();
                db.CreateTable<InboundModel>();
                db.CreateTable<OutboundModel>();

                // 初始化管理员
                if (db.Table<UserModel>().Count() == 0)
                {
                    db.Insert(new UserModel { Username = "admin", Password = "888888" });
                }
            }

            // 3. 创建异步连接
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

        // ==============================
        // 2. 首页统计 (HomeViewModel 需要)
        // ==============================
        public Task<int> GetTotalInboundCountAsync()
        {
            return _database.Table<InboundModel>().CountAsync();
        }

        public Task<int> GetTotalOutboundCountAsync()
        {
            return _database.Table<OutboundModel>().CountAsync();
        }

        // ==============================
        // 3. 入库管理 (InboundViewModel 需要)
        // ==============================
        public Task<List<InboundModel>> GetInboundOrdersAsync()
        {
            return _database.Table<InboundModel>().ToListAsync();
        }

        public Task SaveInboundOrderAsync(InboundModel item)
        {
            if (item.Id != 0)
                return _database.UpdateAsync(item);
            else
                return _database.InsertAsync(item);
        }

        public Task DeleteInboundOrderAsync(InboundModel item)
        {
            return _database.DeleteAsync(item);
        }

        // 获取供应商列表 (简单起见，从现有记录中查不重复的供应商)
        public async Task<List<string>> GetSupplierListAsync()
        {
            // 使用 SQL 查询不重复的供应商名称
            return await _database.QueryScalarsAsync<string>("SELECT DISTINCT Supplier FROM InboundModel WHERE Supplier IS NOT NULL");
        }

        // ==============================
        // 4. 出库管理 (OutboundViewModel 需要)
        // ==============================
        public Task<List<OutboundModel>> GetOutboundOrdersAsync()
        {
            return _database.Table<OutboundModel>().ToListAsync();
        }

        public Task SaveOutboundOrderAsync(OutboundModel item)
        {
            if (item.Id != 0)
                return _database.UpdateAsync(item);
            else
                return _database.InsertAsync(item);
        }

        public Task DeleteOutboundOrderAsync(OutboundModel item)
        {
            return _database.DeleteAsync(item);
        }

        // 获取客户列表
        public async Task<List<string>> GetCustomerListAsync()
        {
            return await _database.QueryScalarsAsync<string>("SELECT DISTINCT Customer FROM OutboundModel WHERE Customer IS NOT NULL");
        }

        // ==============================
        // 5. 通用数据
        // ==============================
        // 获取产品列表 (从入库单里找不重复的产品名)
        public async Task<List<string>> GetProductListAsync()
        {
            return await _database.QueryScalarsAsync<string>("SELECT DISTINCT ProductName FROM InboundModel WHERE ProductName IS NOT NULL");
        }
        public async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            // 1. 先验证旧密码是否正确
            var user = await _database.Table<UserModel>()
                                      .Where(u => u.Username == username && u.Password == oldPassword)
                                      .FirstOrDefaultAsync();

            if (user == null)
            {
                return false; // 旧密码错误或用户不存在
            }

            // 2. 更新密码
            user.Password = newPassword;
            await _database.UpdateAsync(user);
            return true; // 修改成功
        }
    }
}