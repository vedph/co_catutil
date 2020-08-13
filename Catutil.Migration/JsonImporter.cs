using Cadmus.Core;
using Cadmus.Core.Storage;
using Microsoft.Extensions.Logging;
using Proteus.Core;
using System;
using System.IO;
using System.Text.Json;

namespace Catutil.Migration
{
    /// <summary>
    /// JSON text and apparatus dumps importer.
    /// </summary>
    public sealed class JsonImporter : IHasLogger
    {
        private readonly JsonDocumentOptions _options;
        private readonly ICadmusRepository _repository;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dry mode is enabled.
        /// In dry mode, no change is made to the database.
        /// </summary>
        public bool IsDry { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonImporter"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public JsonImporter(ICadmusRepository repository)
        {
            _options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        private static IItem ReadItem(JsonElement itemElem)
        {
            return new Item
            {
                Id = itemElem.GetProperty("id").GetString(),
                Title = itemElem.GetProperty("title").GetString(),
                Description = itemElem.GetProperty("description").GetString(),
                FacetId = itemElem.GetProperty("facetId").GetString(),
                GroupId = itemElem.GetProperty("groupId").GetString(),
                SortKey = itemElem.GetProperty("sortKey").GetString(),
                TimeCreated = itemElem.GetProperty("timeCreated").GetDateTime(),
                CreatorId = itemElem.GetProperty("creatorId").GetString(),
                TimeModified = itemElem.GetProperty("timeModified").GetDateTime(),
                UserId = itemElem.GetProperty("userId").GetString(),
                Flags = itemElem.GetProperty("flags").GetInt32()
            };
        }

        /// <summary>
        /// Imports text from the specified JSON stream.
        /// </summary>
        /// <param name="stream">The text stream.</param>
        /// <exception cref="ArgumentNullException">txtStream or appStream</exception>
        public void ImportText(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            JsonDocument doc = JsonDocument.Parse(stream, _options);

            // for each item
            foreach (JsonElement itemElem in doc.RootElement.EnumerateArray())
            {
                // read its metadata
                IItem item = ReadItem(itemElem);

                // import it
                Logger?.LogInformation("Importing item {ItemId}: {Title}",
                    item.Id, item.Title);
                if (!IsDry) _repository.AddItem(item);

                // import its parts
                foreach (JsonElement partElem in itemElem.GetProperty("parts")
                    .EnumerateArray())
                {
                    if (!IsDry)
                        _repository.AddPartFromContent(partElem.ToString());
                }
            }
        }

        /// <summary>
        /// Imports the apparatus from the specified JSON stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="ArgumentNullException">stream</exception>
        public void ImportApparatus(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            JsonDocument doc = JsonDocument.Parse(stream, _options);

            // apparatus
            int n = 0;
            foreach (JsonElement partElem in doc.RootElement.EnumerateArray())
            {
                Logger?.LogInformation($"Importing layer part #{++n}");
                if (!IsDry)
                    _repository.AddPartFromContent(partElem.ToString());
            }
        }
    }
}
