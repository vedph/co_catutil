using System;
using System.Collections.Generic;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Interface implemented by the parser context objects in a
    /// <see cref="EntryPipeline"/>.
    /// </summary>
    public interface IParserContext
    {
        /// <summary>
        /// Starts the parsing. Any initialization task goes here.
        /// </summary>
        void Start();

        /// <summary>
        /// Ends the parsing. Any teardown task goes here.
        /// </summary>
        void End();
    }
}
