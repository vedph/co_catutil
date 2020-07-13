using System;
using System.Collections.Generic;

namespace Catutil.Migration.Sql
{
    /// <summary>
    /// General-purpose RDBMS database manager.
    /// </summary>
    public interface IDbManager
    {
        /// <summary>
        /// Clears the database.
        /// </summary>
        /// <param name="name">The name.</param>
        void ClearDatabase(string name);

        /// <summary>
        /// Creates the database with the specified name, or clears it if it
        /// exists.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="schema">The optional schema script to be executed only
        /// when the database is created.</param>
        /// <param name="seed">The optional seed script to be executed.</param>
        /// <param name="identityTablesToReset">if not null, a list of table
        /// names whose identity should be reset before executing the seed
        /// script.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        void CreateDatabase(string name, string schema, string seed,
            IList<string> identityTablesToReset = null);

        /// <summary>
        /// Executes the specified set of commands against the database.
        /// </summary>
        /// <param name="database">The database or null to use the default.</param>
        /// <param name="commands">The SQL commands array.</param>
        void ExecuteCommands(string database, params string[] commands);

        /// <summary>
        /// Checks if the specified database exists.
        /// </summary>
        /// <param name="name">The database name.</param>
        /// <returns>true if exists, else false</returns>
        bool Exists(string name);

        /// <summary>
        /// Removes the database with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        void RemoveDatabase(string name);
    }
}
