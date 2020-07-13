namespace Catutil.Migration.Xls
{
    // https://stackoverflow.com/questions/13338037/apache-poi-read-and-store-rich-text-content-in-db

    /// <summary>
    /// A formatting run in an Excel text cell.
    /// </summary>
    public sealed class FormattingRun
    {
        /// <summary>
        /// Gets or sets the run's start index.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the run's length.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the index of the font. The index refers to the
        /// collection in the Excel workbook hosting the sheet with the 
        /// cell being read.
        /// </summary>
        public short FontIndex { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Start}×{Length} = {FontIndex}";
        }
    }
}
