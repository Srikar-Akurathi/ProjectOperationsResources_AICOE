using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace ProjectOperationsPlugins
{
    public class CapitalizeText : PluginBoilerplate
    {
        public override void Action(
            IPluginExecutionContext context,
            IOrganizationService service,
            ITracingService tracingService)
        {
            try
            {
                tracingService.Trace("CapitalizeText plugin started.");

                if (context.InputParameters.Contains("Target") &&
                    context.InputParameters["Target"] is Entity entity)
                {
                    tracingService.Trace("Entity: {0}", entity.LogicalName);

                    if (entity.Contains("msdyn_subject") && entity["msdyn_subject"] != null)
                    {
                        string originalValue = entity["msdyn_subject"].ToString();
                        string capitalizedValue = originalValue.ToUpper() + "_WORKFLOW_WORKING";

                        entity["msdyn_subject"] = capitalizedValue;

                        tracingService.Trace("msdyn_subject changed: {0} → {1}",
                            originalValue, capitalizedValue);
                    }
                }
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracingService.Trace("Error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    $"CapitalizeText plugin failed: {ex.Message}", ex);
            }
        }
    }
}