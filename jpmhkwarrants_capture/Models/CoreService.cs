using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace jpmhkwarrants_capture.Models
{
    public class CoreService
    {
        protected readonly string ConnectionString = ConfigurationManager.ConnectionStrings["StockEntities"].ConnectionString;

        protected async Task Select(DataTable dt, string sql, params KeyValuePair<string, object>[] parameters)
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var connection = new SqlConnection(ConnectionString))
                    using (var adapter = new SqlDataAdapter(sql, connection))
                    {
                        foreach (var parameter in parameters)
                        {
                            adapter.SelectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                        }
                        adapter.Fill(dt);
                    }
                });
            }
            catch (Exception ex)
            {
                //    Logger.Log(UserName, ex);
                throw;
            }
        }

        protected async Task<DataRow> SelectOne(string sql, params KeyValuePair<string, object>[] parameters)
        {
            using (var dt = new DataTable())
            {
                await Select(dt, sql, parameters);
                return dt.AsEnumerable().FirstOrDefault();
            }
        }

        protected async Task<int> Execute(string sql, params KeyValuePair<string, object>[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
                try
                {
                    connection.Open();
                    var result = await command.ExecuteNonQueryAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    //Logger.Log(UserName, ex);
                    throw;
                }
            }
        }
        protected async Task<int> ExecuteProcedure(string procedure_name, params KeyValuePair<string, object>[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(procedure_name, connection) { CommandType = CommandType.StoredProcedure })
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
                try
                {
                    connection.Open();
                    var result = await command.ExecuteNonQueryAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    //Logger.Log(UserName, ex);
                    throw;
                }
            }
        }
        protected int ExecuteProcedureNonAsync(string procedure_name, params KeyValuePair<string, object>[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(procedure_name, connection) { CommandType = CommandType.StoredProcedure })
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
                try
                {
                    connection.Open();
                    var result = command.ExecuteNonQuery();
                    return result;
                }
                catch (Exception ex)
                {
                    //Logger.Log(UserName, ex);
                    throw;
                }
            }
        }
        protected async Task ExecuteSelectProcedure(DataTable dt, string procedure_name, params KeyValuePair<string, object>[] parameters)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var adapter = new SqlDataAdapter(procedure_name, connection))
                {
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    foreach (var parameter in parameters)
                    {
                        adapter.SelectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                    }
                    await Task.Run(() => adapter.Fill(dt));
                }
            }
            catch (Exception ex)
            {
                //Logger.Log(UserName, ex);
                throw;
            }
        }

        protected void SelectNonAsync(DataTable dt, string sql, params KeyValuePair<string, object>[] parameters)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                using (var adapter = new SqlDataAdapter(sql, connection))
                {
                    foreach (var parameter in parameters)
                    {
                        adapter.SelectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                    }
                    adapter.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                //Logger.Log(UserName, ex);
                throw;
            }
        }
        protected void ExecuteNonAsync(string sql, params KeyValuePair<string, object>[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    //Logger.Log(UserName, ex);
                    throw;
                }
            }
        }
    }
}