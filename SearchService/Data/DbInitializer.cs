// Imports the official .NET client for communicating with Meilisearch.
using Meilisearch;
using SearchService.Services;


// Provides the classes used to convert JSON into C# objects.
using System.Text.Json;

namespace SearchService.Data
{
    /// <summary>
    /// Initializes the Meilisearch index when SearchService starts.
    ///
    /// On the first application start, this class:
    /// 1. Reads auction data from the auctions.json file.
    /// 2. Converts the JSON data into Item objects.
    /// 3. Adds those objects to the "items" Meilisearch index.
    /// 4. Configures the index for searching, filtering, and sorting.
    ///
    /// If the index already contains documents, initialization is skipped.
    /// </summary>
    public class DbInitializer
    {
        /// <summary>
        /// The unique name of the Meilisearch index.
        ///
        /// An index is similar to a table in a relational database.
        /// In this application, every document in the "items" index
        /// represents one auction.
        /// </summary>
        private const string IndexUid = "items";

        /// <summary>
        /// Initializes the Meilisearch index with auction documents.
        /// </summary>
        /// <param name="app">
        /// The running ASP.NET Core application.
        /// It provides access to registered services, including
        /// the MeilisearchClient.
        /// </param>
        public static async Task ConfigureIndex(WebApplication app)
        {
            // Get the MeilisearchClient that was registered in Program.cs.
            //
            // The client contains:
            // - The Meilisearch address, such as http://localhost:7700
            // - The API key, such as masterkey
            //
            // We use this client to send HTTP requests to Meilisearch.
            var client =
                app.Services.GetRequiredService<MeilisearchClient>();

            // Get a reference to the Meilisearch index named "items".
            //
            // This line does not download the documents.
            // It creates an object that we can use to communicate
            // with this particular index.
            var index = client.Index(IndexUid);

            // Configure the behavior of the "items" index.
            //
            // Updating settings is also an asynchronous Meilisearch task.
            var settingsTask = await index.UpdateSettingsAsync(
                new Settings
                {
                    // Users can search for words inside these properties.
                    //
                    // Example:
                    // Searching for "Ford sports car" checks make,
                    // model, and description.
                    SearchableAttributes =
                    [
                        "make",
                        "model",
                        "description"
                    ],

                    // These properties can be used to restrict results.
                    //
                    // Example:
                    // Return only auctions where seller = "bob".
                    FilterableAttributes =
                    [
                        "seller",
                        "winner",
                        "status",
                        "auctionEnd"
                    ],

                    // Search results can be ordered by these properties.
                    //
                    // Example:
                    // Sort auctions by auctionEnd or currentHighBid.
                    SortableAttributes =
                    [
                        "auctionEnd",
                        "currentHighBid",
                        "createdAt",
                        "updatedAt",
                        "make",
                        "model"
                    ]
                });

            // Wait until Meilisearch finishes updating the settings.
            await client.WaitForTaskAsync(settingsTask.TaskUid);


        }

        public static async Task FetchMissingAuctions(WebApplication app)
        {
            var client = app.Services.GetRequiredService<MeilisearchClient>();

            var index = client.Index(IndexUid);

            using var scope = app.Services.CreateScope();
            var auctionSvc = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();

            var items = await auctionSvc.GetItemsForSearch();

            if (items is null || items.Count == 0)
            {
                Console.WriteLine(
                    "No items found in the JSON file. " +
                    "Skipping initialization.");

                return;
            }

            var addTask = await index.AddDocumentsAsync(
                items,
                primaryKey: "id");

            await client.WaitForTaskAsync(addTask.TaskUid);

            Console.WriteLine($"Search index populated with {items.Count} items.");
        }
    }
}