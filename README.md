# Dragonfly.Umbraco8FormsMembers #

Umbraco 8 Forms additions to work with Members created by [Heather Floyd](https://www.HeatherFloyd.com).

With special thanks to [Tim Geyssens](https://github.com/TimGeyssens/MemberToolsForUmbracoForms.git) for original v7 code. 

## Installation ##
[![Nuget Downloads](https://buildstats.info/nuget/Dragonfly.Umbraco8FormsMembers)](https://www.nuget.org/packages/Dragonfly.Umbraco8FormsMembers/)

     PM>   Install-Package Dragonfly.Umbraco8FormsMembers



## Features ##
- You can map form fields to standard and custom member properties.
- You can optionally specify a Member Group for the new member to be assigned to.
- If the "Email Field Alias is specified in the Workflow Settings, the email will be checked against all current Members for a duplicate and return a validation error if found.
- If desired, the new member can be automatically logged-in.

## Usage ##

Create a Member registration form and add the "Save As Member" workflow.

Make sure to map the first 4 properties to form fields:
1. Node name		
1. Login		
1. Email		
1. Password

Map any other properties for the Member Type as desired.

Set other options as desired.

When the workflow runs on form submit, some information is added to the Session, which you can access on your 'Thank You' page.

`Umbraco8FormsMembersHelper.GetWorkflowResult()` returns an object of type `MemberRecordResult`, which includes information about the newly created member (if successful), as well as the full form record and any workflow errors.

*Example:*

    @inherits UmbracoViewPage<IPublishedContent>
    @{
	    var result = Umbraco8FormsMembersHelper.GetWorkflowResult();
	    if (result != null)
	    {
	        if (result.WorkflowResult == WorkflowExecutionStatus.Failed)
	        {
	            //Failure - Show some messages
	            <h3>Oops! There was a problem registering you with that information.</h3>
	
	            if (result.Errors.ContainsKey(Umbraco8FormsMembersHelper.SaveError.InvalidPassword))
	            {
	                <p>That password doesn't meet security requirements</p>
	            }
	
	            if (result.Errors.ContainsKey(Umbraco8FormsMembersHelper.SaveError.EmailAlreadyRegistered))
	            {
	                <p>That email address is already registered. Would you like to <a href="/Login">Login</a>?</p>
	            }
	
	            foreach (var error in @result.Errors)
	            {
	                <!--@error.Key = @error.Value-->
	            }
	
	            <input action="action" type="button" value="Try Registering Again" onclick="window.history.go(-1); return false;" />
	        }
	        else
	        {
	            //Success
	
	            //Optional - run other post-registration code
	            var updatedMember = UpdateNewMember(result.NewMemberId);
	
	            if (updatedMember != null)
	            {
	                <h3>Thanks for Registering, @updatedMember.FirstName!</h3>
	            }
	            else
	            {
	                <h3>Thanks for Registering!</h3>
	            }
	
	        }
	    }
	    else
	    {
	        <p>Hmm... something didn't work.</p>
	    }
	
	    @Html.Partial("MemberLogout", Model)
	}


**Notes:**
For your Registration Form "Password" field, be sure to set a proper Validation rule, based on what your Web.config has for "UmbracoMembershipProvider" rules (minRequiredPasswordLength & minRequiredNonalphanumericCharacters). 

*Example:*
 
- minRequiredPasswordLength = 10
- minRequiredNonalphanumericCharacters = 0


- RegEx: `\w{10,}`
- Message: Passwords must be at least 10 characters


If you do not have field-level validation set for the Password field, but the provided value does not meet the security requirements, the Workflow will fail and the member will not be created. `Umbraco8FormsMembersHelper.SaveError.InvalidPassword` will be added to the result errors list for you to handle on the Thank you page.

## Resources ##
GitHub Repository: [https://github.com/hfloyd/Dragonfly.Umbraco8FormsMembers](https://github.com/hfloyd/Dragonfly.Umbraco8FormsMembers)