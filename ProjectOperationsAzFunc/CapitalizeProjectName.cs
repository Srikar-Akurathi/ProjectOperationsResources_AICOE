using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace ProjectOperationsAzFunc
{
    public class CapitalizeProjectName
    {
        private readonly ILogger<CapitalizeProjectName> _logger;

        public CapitalizeProjectName(ILogger<CapitalizeProjectName> logger)
        {
            _logger = logger;
        }

        [Function("CapitalizeProjectName")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("CapitalizeProjectName function triggered.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

            string projectId = data.GetProperty("projectId").GetString()!;
            _logger.LogInformation("Project ID: {ProjectId}", projectId);

            // Connect to Dataverse
            string connectionString = Environment.GetEnvironmentVariable("DataverseConnectionString")!;
            using var serviceClient = new ServiceClient(connectionString);

            if (!serviceClient.IsReady)
            {
                _logger.LogError("Failed to connect to Dataverse: {Error}", serviceClient.LastError);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Dataverse connection failed: {serviceClient.LastError}");
                return errorResponse;
            }

            // Retrieve project
            var project = serviceClient.Retrieve(
                "msdyn_project",
                Guid.Parse(projectId),
                new Microsoft.Xrm.Sdk.Query.ColumnSet("msdyn_subject", "msdyn_description")
            );

            string originalName = project.GetAttributeValue<string>("msdyn_subject") ?? "";
            string capitalizedName = originalName.ToUpper()+ "_FROM_AZ_FUNC";

            // Update project
            var updateEntity = new Entity("msdyn_project", Guid.Parse(projectId));
            updateEntity["msdyn_subject"] = capitalizedName;
            updateEntity["msdyn_description"] = $"Name capitalized by Azure Function on {DateTime.UtcNow:dd-MM-yyyy HH:mm} UTC. Original: {originalName}";

            serviceClient.Update(updateEntity);

            _logger.LogInformation("Project name updated: {Original} → {Capitalized}", originalName, capitalizedName);

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                originalName,
                capitalizedName,
                message = "Project name capitalized successfully"
            });

            return response;
        }
    }
}