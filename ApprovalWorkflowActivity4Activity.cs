/*
Task 5: Approval Workflow
Develop a custom workflow for an approval process. 
When a new record (e.g., Opportunity) is created and its amount is above a certain threshold, 
the workflow should send an approval request to a designated user. 
The user's response should update a custom field on the record, indicating approval or rejection.
 */

using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace CustomWorkflow
{
     class ApprovalWorkflowActivity4Activity : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            // Service object creation
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService tracingService = context.GetExtension<ITracingService>();

            // Get Opportunity details
            Entity opportunity = service.Retrieve("opportunity", workflowContext.PrimaryEntityId, new ColumnSet("name", "estimatedvalue", "parentaccountid"));

            // Check if the Opportunity amount is above the threshold
            decimal thresholdAmount = 100000; // Set your threshold amount here
            Money estimatedValue = opportunity.GetAttributeValue<Money>("estimatedvalue");

            if (estimatedValue.Value > thresholdAmount)
            {
                // Retrieve the associated Account's primary contact
                EntityReference accountRef = opportunity.GetAttributeValue<EntityReference>("parentaccountid");
                Entity account = service.Retrieve("account", accountRef.Id, new ColumnSet("primarycontactid"));

                EntityReference primaryContactRef = account.GetAttributeValue<EntityReference>("primarycontactid");

                // Retrieve primary contact's email address
                ColumnSet contactColumns = new ColumnSet("emailaddress1");
                Entity primaryContact = service.Retrieve(primaryContactRef.LogicalName, primaryContactRef.Id, contactColumns);

                // Send approval request email to the primary contact
                string recipientEmail = primaryContact.GetAttributeValue<string>("emailaddress1");
                string emailSubject = "Approval Required for Opportunity: " + opportunity.GetAttributeValue<string>("name");
                string emailBody = "Please approve the Opportunity with the estimated value of " + estimatedValue.Value;

                // Create "From" and "To" Activity Party entities
                Entity fromActivityParty = new Entity("activityparty");
                fromActivityParty["partyid"] = new EntityReference("systemuser", workflowContext.UserId);

                Entity toActivityParty = new Entity("activityparty");
                toActivityParty["partyid"] = primaryContactRef; // Set to primary contact

                // Create the email entity with the "From," "To," and other fields
                Entity email = new Entity("email");
                email["from"] = new Entity[] { fromActivityParty };
                email["to"] = new Entity[] { toActivityParty };
                email["subject"] = emailSubject;
                email["description"] = emailBody;
                email["directioncode"] = true; // Outgoing email

                // Create the email record
                Guid emailId = service.Create(email);

                // Log the action
                tracingService.Trace("Approval request email sent for Opportunity: " + opportunity.GetAttributeValue<string>("name"));
            }
            else
            {
                // Log that no approval is needed
                tracingService.Trace("No approval needed for Opportunity: " + opportunity.GetAttributeValue<string>("name"));
            }
        }
    }
}

