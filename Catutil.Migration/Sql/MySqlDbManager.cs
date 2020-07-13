using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace Catutil.Migration.Sql
{
    /// <summary>
    /// MySql database manager.
    /// </summary>
    /// <seealso cref="IDbManager" />
    public sealed class MySqlDbManager : IDbManager
    {
        private readonly string _csTemplate;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlDbManager"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string with placeholder
        /// <c>{0}</c> for the database name.</param>
        /// <exception cref="ArgumentNullException">connectionString</exception>
        public MySqlDbManager(string connectionString)
        {
            _csTemplate = connectionString ??
                throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Gets the connection string to the database with the specified
        /// name from the specified template, where the database name placeholder
        /// is represented by the string <c>{0}</c>.
        /// </summary>
        /// <param name="template">The connection string template with placeholder
        /// at the database name value.</param>
        /// <param name="name">The database name, or null or empty to avoid
        /// setting the database name at all in the connection string.</param>
        /// <returns>The connection string.</returns>
        public static string GetConnectionString(string template, string name)
        {
            Regex dbRegex = new Regex("Database=[^;]+;", RegexOptions.IgnoreCase);

            return string.IsNullOrEmpty(name)
                ? dbRegex.Replace(template, "")
                : string.Format(template, name);
        }

        /// <summary>
        /// Executes the specified set of commands against the database.
        /// </summary>
        /// <param name="database">The optional database name.</param>
        /// <param name="commands">The SQL commands array.</param>
        public void ExecuteCommands(string database, params string[] commands)
        {
            using (MySqlConnection connection = new MySqlConnection(
                GetConnectionString(_csTemplate, database)))
            {
                connection.Open();
                foreach (string command in commands)
                {
                    if (string.IsNullOrWhiteSpace(command)) continue;

                    // https://stackoverflow.com/questions/1324693/c-mysql-ado-net-delimiter-causing-syntax-error
                    MySqlScript script = new MySqlScript(connection, command);
                    script.Execute();
                }
            }
        }

        /// <summary>
        /// Checks if the specified database exists.
        /// </summary>
        /// <param name="database">The database name.</param>
        /// <returns>true if exists, else false</returns>
        public bool Exists(string database)
        {
            using (MySqlConnection connection = new MySqlConnection(
                GetConnectionString(_csTemplate, null)))
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(
                    "SELECT COUNT(SCHEMA_NAME) FROM INFORMATION_SCHEMA.SCHEMATA " +
                    $"WHERE SCHEMA_NAME = '{database}'", connection);
                int n = Convert.ToInt32(cmd.ExecuteScalar());
                connection.Close();
                return n > 0;
            }
        }

        /// <summary>
        /// Removes the database with the specified name.
        /// </summary>
        /// <param name="database">The database name.</param>
        /// <exception cref="ArgumentNullException">database</exception>
        public void RemoveDatabase(string database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            if (Exists(database))
                ExecuteCommands(null, $"DROP DATABASE `{database}`");
        }

        /// <summary>
        /// Creates the database with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="schema">The optional schema script to be executed only when the
        /// database is created.</param>
        /// <param name="seed">The optional seed script to be executed.</param>
        /// <param name="identityTablesToReset">if not null, a list of table names
        /// whose identity should be reset before executing the seed script.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public void CreateDatabase(string name, string schema, string seed,
            IList<string> identityTablesToReset = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            ExecuteCommands(null, $"CREATE DATABASE `{name}`",
                $"USE `{name}`", schema, seed);
        }

        private IList<string> GetTableNames(MySqlConnection connection,
            string name)
        {
            List<string> tables = new List<string>();
            MySqlCommand cmd = new MySqlCommand(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
                $"WHERE table_schema='{name}';", connection);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            return tables;
        }

        /// <summary>
        /// Clears the database by removing all the rows and resetting autonumber.
        /// </summary>
        /// <exception cref="ArgumentNullException">database</exception>
        public void ClearDatabase(string database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            // https://stackoverflow.com/questions/1912813/truncate-all-tables-in-a-mysql-database-in-one-command
            using (MySqlConnection connection = new MySqlConnection(
                GetConnectionString(_csTemplate, database)))
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand($"USE `{database}`", connection);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SET FOREIGN_KEY_CHECKS=0";
                cmd.ExecuteNonQuery();

                foreach (string table in GetTableNames(connection, database))
                {
                    cmd.CommandText = $"TRUNCATE TABLE `{table}`";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = "SET FOREIGN_KEY_CHECKS=1";
            }
        }
    }
}
