using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Transactions;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

namespace SimpleDemo
{
    public class SimpleDemo
    {
        enum MethodType
        {
            One,
            Two,
            Three,
            Four
        }

        public static void Main(string[] args)
        {
            // There is no need to cache Database object, they are cached in DatabaseProviderFactory
            var database = CreateDatabase(MethodType.One) as SqlDatabase;

            var command = GetCommand(database, MethodType.One);

            // the ExecuteDataSet method returns a DataSet object that contains all the data. 
            // This gives you your own local copy. The call to ExecuteDataSet opens a connection, 
            // populates a DataSet, and closes the connection before returning the result.
            var dataset = ExecuteDataSet(database, command, MethodType.Three);

            // For the ExecuteReader method, it is unclear when to close the connection,
            // so always use the using statement
            ExecuteReader(database, command, MethodType.Three);

            Transaction(database, command);

            // The object type you specify when using the default mapping feature 
            // must have a default public constructor that initializes a new instance 
            // without accepting any parameters. 
            // Also keep in mind that the default row mapper uses reflection to discover the properties and types. 
            // This may be resource intensive and affect performance. Consider caching the mapper.
            DataReadMapping(database, MethodType.Three);
        }

        private static Database CreateDatabase(MethodType type)
        {
            switch (type)
            {
                case MethodType.One:
                    {
                        // Should only exist one DatabaseProviderFactory object in your application, 
                        // since all the Database objects are cached in the factory
                        var factory = new DatabaseProviderFactory();
                        return factory.CreateDefault();
                    }
                case MethodType.Two:
                    {
                        var factory = new DatabaseProviderFactory();
                        DatabaseFactory.SetDatabaseProviderFactory(factory);
                        return DatabaseFactory.CreateDatabase();
                    }
                case MethodType.Three:
                    {
                        var factory = new DatabaseProviderFactory();
                        DatabaseFactory.SetDatabaseProviderFactory(factory);
                        return DatabaseFactory.CreateDatabase("LocalDb");
                    }
                case MethodType.Four:
                    {
                        return new SqlDatabase(ConfigurationManager.ConnectionStrings["LocalDb"].ConnectionString);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static DbCommand GetCommand(SqlDatabase database, MethodType type)
        {
            switch (type)
            {
                case MethodType.One:
                    {
                        // Anytime you are passing a SQL string to the Data Access Application Block, 
                        // you should consider carefully whether to check the string for malicious code such as a SQL injection attack. 
                        // This is especially important for user generated SQL code.
                        return database.GetSqlStringCommand("SELECT * FROM Employees");
                    }
                case MethodType.Two:
                    {
                        return database.GetStoredProcCommand("Ten Most Expensive Products");
                    }
                case MethodType.Three:
                    {
                        return database.GetStoredProcCommand("SalesByCategory", "Beverages", "1998");
                    }
                case MethodType.Four:
                    {
                        var command = database.GetStoredProcCommand("SalesByCategory");
                        database.AddInParameter(command, "CategoryName", SqlDbType.NVarChar, "Beverages");
                        database.AddInParameter(command, "OrdYear", SqlDbType.NVarChar, "1998");
                        return command;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static DataSet ExecuteDataSet(SqlDatabase database, DbCommand command, MethodType type)
        {
            switch (type)
            {
                case MethodType.One:
                    {
                        // No need to open the connection; just make the call.
                        return database.ExecuteDataSet(command);
                    }
                case MethodType.Two:
                    {
                        return database.ExecuteDataSet(CommandType.Text, "SELECT * FROM Employees");
                    }
                case MethodType.Three:
                    {
                        return database.ExecuteDataSet("SalesByCategory", "Beverages", "1998");
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static void ExecuteReader(SqlDatabase database, DbCommand command, MethodType type)
        {
            switch (type)
            {
                case MethodType.One:
                    {
                        using (var reader = database.ExecuteReader(command))
                        {
                            // Process the results
                        }
                        return;
                    }
                case MethodType.Two:
                    {
                        using (var reader = database.ExecuteReader(CommandType.Text, "SELECT * FROM Employees"))
                        {
                            // Process the results
                        }
                        return;
                    }
                case MethodType.Three:
                    {
                        using (var reader = database.ExecuteReader("SalesByCategory", "Beverages", "1998"))
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("Product name: {0}, Total purchase: {1}", reader[reader.GetOrdinal("ProductName")], reader[reader.GetOrdinal("TotalPurchase")]);
                            }
                        }
                        return;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private static void Transaction(SqlDatabase database, DbCommand command)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                database.ExecuteNonQuery(CommandType.Text,
                                         @"INSERT INTO [dbo].[Region] ([RegionID], [RegionDescription]) VALUES (5, N'Middle')");
                database.ExecuteNonQuery(CommandType.Text, "DELETE [dbo].[Region] WHERE [RegionID]=5");
                var count = database.ExecuteScalar(CommandType.Text, "SELECT COUNT(*) FROM Employees");
            }
        }

        private static void DataReadMapping(SqlDatabase database, MethodType type)
        {
            IEnumerable<Employee> employeeData;

            switch (type)
            {
                case MethodType.One:
                    {
                        // Create an output row mapper that maps all properties based on the column names
                        IRowMapper<Employee> mapper = MapBuilder<Employee>.BuildAllProperties();

                        // Create a stored procedure accessor that uses this output mapper
                        var accessor = database.CreateSqlStringAccessor("SELECT * FROM Employees", mapper);

                        // Execute the accessor to obtain the results
                        employeeData = accessor.Execute();

                        break;
                    }
                case MethodType.Two:
                    {
                        employeeData = database.ExecuteSqlStringAccessor<Employee>("SELECT * FROM Employees");

                        break;
                    }
                case MethodType.Three:
                    {
                        IRowMapper<Employee> mapper = MapBuilder<Employee>.MapAllProperties()
                                                      .MapByName(x => x.LastName)
                                                      .DoNotMap(x => x.TitleOfCourtesy)
                                                      .Map(x => x.City).ToColumn("City")
                                                      .Map(x => x.HireDate).WithFunc(x => x.GetDateTime(x.GetOrdinal("HireDate")))
                                                      .Build();
                        var accessor = database.CreateSqlStringAccessor("SELECT * FROM Employees", mapper);
                        employeeData = accessor.Execute();

                        break;
                    }
                default:
                    throw new NotSupportedException();
            }

            // Perform a client-side query on the returned data 
            var results = from employee in employeeData
                          where employee.Country == "USA"
                          orderby employee.EmployeeID
                          select new { Name = employee.FirstName };
            results.ToList().ForEach(obj => Console.WriteLine(obj.Name));

            var products = database.ExecuteSprocAccessor<Product>("Ten Most Expensive Products");
            products.ToList().ForEach(product => Console.WriteLine(product.TenMostExpensiveProducts, product.UnitPrice));

            var sales = database.ExecuteSprocAccessor<Sale>("SalesByCategory", "Beverages", "1998");
            sales.ToList().ForEach(sale => Console.WriteLine(sale.ProductName, sale.TotalPurchase));
        }

        private static void UpdateUsingDataSet(SqlDatabase database)
        {
            var productsDataSet = new DataSet();
            var cmd = database.GetSqlStringCommand("Select ProductID, ProductName, CategoryID, UnitPrice, LastUpdate From Products");
            
            // Retrieve the initial data.
            database.LoadDataSet(cmd, productsDataSet, "Products");

            // Get the table that will be modified.
            var productsTable = productsDataSet.Tables["Products"];

            // Add a new product to existing DataSet.
            productsTable.Rows.Add(new object[] { DBNull.Value, "New product", 11, 25 });

            // Modify an existing product.
            productsTable.Rows[0]["ProductName"] = "Modified product";

            // Establish the Insert, Delete, and Update commands.
            var insertCommand = database.GetStoredProcCommand("AddProduct");
            database.AddInParameter(insertCommand, "ProductName", DbType.String, "ProductName", DataRowVersion.Current);
            database.AddInParameter(insertCommand, "CategoryID", DbType.Int32, "CategoryID", DataRowVersion.Current);
            database.AddInParameter(insertCommand, "UnitPrice", DbType.Currency, "UnitPrice", DataRowVersion.Current);

            var deleteCommand = database.GetStoredProcCommand("DeleteProduct");
            database.AddInParameter(deleteCommand, "ProductID", DbType.Int32, "ProductID", DataRowVersion.Current);

            var updateCommand = database.GetStoredProcCommand("UpdateProduct");
            database.AddInParameter(updateCommand, "ProductID", DbType.Int32, "ProductID", DataRowVersion.Current);
            database.AddInParameter(updateCommand, "ProductName", DbType.String, "ProductName", DataRowVersion.Current);
            database.AddInParameter(updateCommand, "LastUpdate", DbType.DateTime, "LastUpdate", DataRowVersion.Current);

            // Submit the DataSet, capturing the number of rows that were affected.
            var rowsAffected = database.UpdateDataSet(productsDataSet, "Products", insertCommand, updateCommand, deleteCommand,
                                UpdateBehavior.Standard);
        }
    }
}